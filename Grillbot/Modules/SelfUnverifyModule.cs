﻿using Discord.Commands;
using Grillbot.Attributes;
using Grillbot.Extensions.Discord;
using Grillbot.Services.Permissions.Preconditions;
using Grillbot.Services.TempUnverify;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Grillbot.Modules
{
    [RequirePermissions]
    [Group("selfunverify")]
    [ModuleID("SelfUnverifyModule")]
    [Name("Odebrání přístupu sobě sama")]
    public class SelfUnverifyModule : BotModuleBase
    {
        private TempUnverifyService UnverifyService { get; }

        public SelfUnverifyModule(TempUnverifyService unverifyService)
        {
            UnverifyService = unverifyService;
        }

        [Command("")]
        [Summary("Odebrání práv sám sobě.")]
        [Remarks("Parametr time je ve formátu {cas}{m/h/d}. Např.: 30m.\nPopis: m: minuty, h: hodiny, d: dny.\nJe možné zadat maximálně 5 předmětových " +
            "rolí, které bude možné si během doby odebraného přístupu ponechat.")]
        public async Task SetSelfUnverify(string time, params string[] subjects)
        {
            try
            {
                if (subjects != null)
                    subjects = subjects.Distinct().Select(o => o.Trim().ToLower()).ToArray();

                var user = await Context.Guild.GetUserFromGuildAsync(Context.User.Id);
                var message = await UnverifyService.SetSelfUnverify(user, Context.Guild, time, subjects);
                await ReplyAsync(message);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is ValidationException || ex is FormatException)
                {
                    await ReplyAsync(ex.Message);
                    return;
                }

                throw;
            }
        }
    }
}
