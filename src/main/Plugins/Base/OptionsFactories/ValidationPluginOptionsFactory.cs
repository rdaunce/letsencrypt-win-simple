﻿using System;
using System.Linq;
using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    public abstract class ValidationPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>, 
        IValidationPluginOptionsFactory 
        where TPlugin : IValidationPlugin
        where TOptions : ValidationPluginOptions, new()
    {
        private readonly string _challengeType;
        string IValidationPluginOptionsFactory.ChallengeType => _challengeType;
        public virtual bool Hidden => false;

        public ValidationPluginOptionsFactory(ILogService log, string challengeType = Http01ChallengeValidationDetails.Http01ChallengeType) : base(log)
        {
            _challengeType = challengeType;
        }

        public abstract TOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(Target target, IArgumentsService arguments);
        ValidationPluginOptions IValidationPluginOptionsFactory.Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(target, arguments, inputService, runLevel);
        }
        ValidationPluginOptions IValidationPluginOptionsFactory.Default(Target target, IArgumentsService arguments)
        {
            return Default(target, arguments);
        }

        /// <summary>
        /// By default no plugin can validate wildcards, should be overridden
        /// in the DNS plugins
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool CanValidate(Target target)
        {
            return !target.GetHosts(false).Any(x => x.StartsWith("*."));
        }

    }
}
