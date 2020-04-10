﻿using Grillbot.Database.Repository;
using Grillbot.Models.Config;
using System;

namespace Grillbot.Services.TempUnverify
{
    public class TempUnverifyFactories
    {
        private IServiceProvider Provider { get; }

        public TempUnverifyFactories(IServiceProvider provider)
        {
            Provider = provider;
        }

        private TService GetService<TService>()
        {
            return (TService)Provider.GetService(typeof(TService));
        }

        public TempUnverifyChecker GetChecker()
        {
            return GetService<TempUnverifyChecker>();
        }

        public TempUnverifyHelper GetHelper()
        {
            return GetService<TempUnverifyHelper>();
        }

        public TempUnverifyConfig GetConfig(ulong guildID)
        {
            using var repository = GetService<ConfigRepository>();
            
            var config = repository.FindConfig(guildID, "unverify", "");
            return config.GetData<TempUnverifyConfig>();
        }

        public TempUnverifyRepository GetUnverifyRepository()
        {
            return GetService<TempUnverifyRepository>();
        }
    }
}