﻿using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// InstallationPluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class InstallationPluginFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IInstallationPluginOptionsFactory
        where TPlugin : IInstallationPlugin
        where TOptions : InstallationPluginOptions, new()
    {
        public InstallationPluginFactory(ILogService log) : base(log) { }

        InstallationPluginOptions IInstallationPluginOptionsFactory.Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(target, arguments, inputService, runLevel);
        }
        InstallationPluginOptions IInstallationPluginOptionsFactory.Default(Target target, IArgumentsService arguments)
        {
            return Default(target, arguments);
        }
        public abstract TOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(Target target, IArgumentsService arguments);
        public virtual bool CanInstall(string storeType) => true;
    }

}
