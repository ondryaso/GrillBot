﻿using Discord.WebSocket;
using DBUserChannel = Grillbot.Database.Entity.Users.UserChannel;
using DBDiscordUser = Grillbot.Database.Entity.Users.DiscordUser;
using Grillbot.Database.Repository;
using Grillbot.Extensions.Discord;
using Grillbot.Models.Users;
using Grillbot.Services.MessageCache;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Grillbot.Services.TempUnverify;

namespace Grillbot.Services.UserManagement
{
    public partial class UserService
    {
        private IServiceProvider Services { get; }
        public Dictionary<string, DateTime> LastPointsCalculatedAt { get; set; }
        private static readonly object locker = new object();
        private Random Random { get; }
        private IMessageCache MessageCache { get; }
        private DiscordSocketClient DiscordClient { get; }

        public UserService(IServiceProvider services, IMessageCache messageCache, DiscordSocketClient discordClient)
        {
            Services = services;
            MessageCache = messageCache;
            LastPointsCalculatedAt = new Dictionary<string, DateTime>();
            Random = new Random();
            DiscordClient = discordClient;
        }

        public async Task<List<DiscordUser>> GetUsersList(WebAdminUserListFilter filter)
        {
            var users = new List<DiscordUser>();

            using var scope = Services.CreateScope();
            using var repository = scope.ServiceProvider.GetService<UsersRepository>();
            var dbUsers = repository.GetUsers(filter.Order, filter.SortDesc, filter.GuildID, filter.Limit, filter.UserID).ToList();

            foreach (var user in dbUsers)
            {
                var mappedUser = await MapUserAsync(user, null);
                if (mappedUser != null)
                    users.Add(mappedUser);
            }

            return users;
        }

        public async Task<DiscordUser> GetUserAsync(long id)
        {
            using var scope = Services.CreateScope();
            using var repository = scope.ServiceProvider.GetService<UsersRepository>();
            using var unverifyLogService = scope.ServiceProvider.GetService<TempUnverifyLogService>();

            var userData = repository.GetUserDetail(id);

            if (userData == null)
                return null;

            var unverifyHistory = await unverifyLogService.GetUnverifyHistoryOfUserAsync(userData);
            return await MapUserAsync(userData, unverifyHistory);
        }

        public async Task<DiscordUser> GetUserDetailAsync(SocketGuild guild, SocketUser user)
        {
            using var scope = Services.CreateScope();
            using var repository = scope.ServiceProvider.GetService<UsersRepository>();

            var userId = await repository.FindUserIDFromDiscordIDAsync(guild.Id, user.Id);

            if (userId == null)
                return null;

            return await GetUserAsync(userId.Value);
        }

        private async Task<DiscordUser> MapUserAsync(DBDiscordUser dbUser, List<UserUnverifyHistoryItem> unverifyHistory)
        {
            var guild = DiscordClient.GetGuild(dbUser.GuildIDSnowflake);

            if (guild == null)
                return null;

            var socketUser = await guild.GetUserFromGuildAsync(dbUser.UserIDSnowflake);

            if (socketUser == null)
                return null;

            return new DiscordUser(guild, socketUser, dbUser, unverifyHistory);
        }

        public void IncrementMessage(SocketGuildUser guildUser, SocketGuild guild, SocketGuildChannel channel)
        {
            lock (locker)
            {
                using var scope = Services.CreateScope();
                using var repository = scope.ServiceProvider.GetService<UsersRepository>();
                var user = repository.GetOrCreateUser(guild.Id, guildUser.Id, true, false, false, false);
                var channelEntity = user.Channels.FirstOrDefault(o => o.ChannelIDSnowflake == channel.Id);

                if (channelEntity == null)
                {
                    channelEntity = new DBUserChannel()
                    {
                        ChannelIDSnowflake = channel.Id,
                        Count = 1,
                        LastMessageAt = DateTime.Now,
                        DiscordUserIDSnowflake = guildUser.Id,
                        GuildIDSnowflake = guild.Id
                    };

                    user.Channels.Add(channelEntity);
                }
                else
                {
                    channelEntity.Count++;
                    channelEntity.LastMessageAt = DateTime.Now;
                }

                if (CanIncrementPoints(guild, guildUser))
                {
                    user.Points += Random.Next(15, 25);

                    var key = GenerateKey(guild, guildUser);
                    if (LastPointsCalculatedAt.ContainsKey(key))
                        LastPointsCalculatedAt[key] = DateTime.Now;
                    else
                        LastPointsCalculatedAt.Add(key, DateTime.Now);
                }

                repository.SaveChanges();
            }
        }

        public void DecrementMessage(SocketGuildUser guildUser, SocketGuild guild, SocketGuildChannel channel)
        {
            lock (locker)
            {
                using var scope = Services.CreateScope();
                using var repository = scope.ServiceProvider.GetService<UsersRepository>();
                var user = repository.GetUser(guild.Id, guildUser.Id, true, false, false, false);

                if (user == null)
                    return;

                var channelEntity = user.Channels.FirstOrDefault(o => o.ChannelIDSnowflake == channel.Id);
                if (channelEntity == null || channelEntity.Count == 0)
                    return;

                channelEntity.Count--;
                repository.SaveChanges();
            }
        }

        private bool CanIncrementPoints(SocketGuild guild, SocketGuildUser user)
        {
            var key = GenerateKey(guild, user);

            lock (locker)
            {
                if (!LastPointsCalculatedAt.ContainsKey(key))
                    return true;

                var lastMessageAt = LastPointsCalculatedAt[key];

                return (DateTime.Now - lastMessageAt).TotalMinutes > 1.0;
            }
        }

        private string GenerateKey(IGuild guild, IUser user) => GenerateKey(guild.Id, user.Id);
        private string GenerateKey(ulong guildID, ulong userID) => $"{userID}|{guildID}";

        public void IncrementReaction(SocketReaction reaction)
        {
            ProcessReaction(reaction, IncrementReaction);
        }

        public void DecrementReaction(SocketReaction reaction)
        {
            ProcessReaction(reaction, DecrementReaction);
        }

        private void ProcessReaction(SocketReaction reaction, Action<SocketGuildUser, SocketGuildUser> action)
        {
            if (!reaction.User.IsSpecified || !(reaction.User.Value is SocketGuildUser reactingUser))
                return;

            if (reaction.Message.IsSpecified && reaction.Message.Value.Author is SocketGuildUser rawMsgAuthor)
            {
                action(rawMsgAuthor, reactingUser);
                return;
            }

            var message = MessageCache.Get(reaction.MessageId);
            if (message != null && message.Author is SocketGuildUser cachedMsgAuthor)
            {
                action(cachedMsgAuthor, reactingUser);
            }
        }

        private void IncrementReaction(SocketGuildUser author, SocketGuildUser reactingUser)
        {
            if (author.Id == reactingUser.Id)
                return; // Author add reaction to his message.

            lock (locker)
            {
                using var scope = Services.CreateScope();
                using var repository = scope.ServiceProvider.GetService<UsersRepository>();

                var authorEntity = repository.GetOrCreateUser(author.Guild.Id, author.Id, false, false, false, false);
                var reactingUserEntity = repository.GetOrCreateUser(reactingUser.Guild.Id, reactingUser.Id, false, false, false, false);

                authorEntity.ObtainedReactionsCount++;
                reactingUserEntity.GivenReactionsCount++;

                repository.SaveChanges();
            }
        }

        private void DecrementReaction(SocketGuildUser author, SocketGuildUser reactingUser)
        {
            if (author.Id == reactingUser.Id)
                return; // Author add reaction to his message.

            lock (locker)
            {
                using var scope = Services.CreateScope();
                using var repository = scope.ServiceProvider.GetService<UsersRepository>();

                var authorEntity = repository.GetUser(author.Guild.Id, author.Id, false, false, false, false);
                var reactingUserEntity = repository.GetUser(reactingUser.Guild.Id, reactingUser.Id, false, false, false, false);

                if (authorEntity != null && authorEntity.ObtainedReactionsCount > 0)
                    authorEntity.ObtainedReactionsCount--;

                if (reactingUserEntity != null && reactingUserEntity.GivenReactionsCount > 0)
                    reactingUserEntity.GivenReactionsCount--;

                repository.SaveChanges();
            }
        }

        public async Task<Dictionary<ulong, string>> GetUsersForFilterAsync()
        {
            var dict = new Dictionary<ulong, string>();

            using var scope = Services.CreateScope();
            using var repository = scope.ServiceProvider.GetService<UsersRepository>();
            var users = await repository.GetUsersForFilterAsync();

            foreach (var guild in DiscordClient.Guilds)
                await guild.SyncGuildAsync();

            foreach (var user in users)
            {
                var userID = Convert.ToUInt64(user);
                var dcUser = DiscordClient.GetUser(userID);

                if (dcUser != null && !dict.ContainsKey(userID))
                    dict.Add(userID, dcUser.GetShortName());
            }

            return dict;
        }
    }
}
