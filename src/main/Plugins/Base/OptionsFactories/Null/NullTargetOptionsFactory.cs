﻿using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullTargetFactory : ITargetPluginOptionsFactory, INull
    {
        Type IHasType.InstanceType => typeof(object);
        Type IHasType.OptionsType => typeof(object);
        bool ITargetPluginOptionsFactory.Hidden => true;
        bool IHasName.Match(string name) => false;
        TargetPluginOptions ITargetPluginOptionsFactory.Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel) => null;
        TargetPluginOptions ITargetPluginOptionsFactory.Default(IArgumentsService arguments) => null;
        string IHasName.Name => "None";
        string IHasName.Description => null;
    }
}
