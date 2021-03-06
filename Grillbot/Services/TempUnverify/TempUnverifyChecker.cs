﻿using Discord.WebSocket;
using Grillbot.Database.Repository;
using Grillbot.Exceptions;
using Grillbot.Extensions.Discord;
using Grillbot.Models.Config.AppSettings;
using Grillbot.Models.Config.Dynamic;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Grillbot.Services.TempUnverify
{
    public class TempUnverifyChecker : IDisposable
    {
        private ConfigRepository ConfigRepository { get; }
        private Configuration Config { get; }
        private TempUnverifyRepository Repository { get; }

        public TempUnverifyChecker(ConfigRepository configRepository, IOptions<Configuration> options, TempUnverifyRepository repository)
        {
            ConfigRepository = configRepository;
            Repository = repository;
            Config = options.Value;
        }

        public void Validate(SocketGuildUser user, SocketGuild guild, bool selfunverify, List<string> subjects)
        {
            ValidateSubjects(selfunverify, subjects, guild);
            ValidateServerOwner(guild, user);
            ValidateCurrentlyUnverifiedUsers(user);
            ValidateRoles(guild, user, selfunverify);
            ValidateBotAdmin(selfunverify, user);
        }

        private void ValidateSubjects(bool selfunverify, List<string> subjects, SocketGuild guild)
        {
            if (!selfunverify || subjects?.Count <= 0)
                return;

            var config = ConfigRepository.FindConfig(guild.Id, "selfunverify", "").GetData<SelfUnverifyConfig>();

            if (subjects.Count > config.MaxSubjectsCount)
                throw new ValidationException($"Je možné si ponechat maximálně {config.MaxSubjectsCount} rolí.");

            var invalidSubjects = subjects
                .Select(o => o.ToLower())
                .Where(subject => !config.Subjects.Contains(subject))
                .ToArray();
            
            if (invalidSubjects.Length > 0)
                throw new ValidationException($"`{string.Join(", ", invalidSubjects)}` nejsou předmětové role.");
        }

        private void ValidateServerOwner(SocketGuild guild, SocketGuildUser user)
        {
            if(guild.OwnerId == user.Id)
                throw new ValidationException("Nelze provést odebrání přístupu, protože se mezi uživateli nachází vlastník serveru.");
        }

        private void ValidateCurrentlyUnverifiedUsers(SocketGuildUser user)
        {
            if(Repository.UnverifyExists(user.Id))
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetFullName()}** již má odebraný přístup.");
        }

        private void ValidateBotAdmin(bool selfunverify, SocketGuildUser user)
        {
            if (!selfunverify && Config.IsUserBotAdmin(user.Id))
                throw new ValidationException($"Nelze provést odebrání přístupu, protože uživatel **{user.GetFullName()}** je administrátor bota.");
        }

        private void ValidateRoles(SocketGuild guild, SocketGuildUser user, bool selfunverify)
        {
            if (selfunverify) return;

            var botRolePosition = guild.CurrentUser.Roles.Max(o => o.Position);
            var userMaxRolePosition = user.Roles.Max(o => o.Position);
            if (userMaxRolePosition > botRolePosition)
            {
                var higherRoles = user.Roles.Where(o => o.Position > botRolePosition);
                var higherRoleNames = string.Join(", ", higherRoles.Select(o => o.Name));

                throw new ValidationException($"Nelze provést odebírání přístupu, protože uživatel **{user.GetFullName()}** má vyšší role. **({higherRoleNames})**");
            }
        }

        public void Dispose()
        {
            Repository.Dispose();
            ConfigRepository.Dispose();
        }
    }
}
