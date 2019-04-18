﻿using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// StorePluginFactory interface
    /// </summary>
    public interface IStorePluginOptionsFactory : IPluginOptionsFactory
    {
        /// <summary>
        /// Check or get information needed for store (interactive)
        /// </summary>
        /// <param name="target"></param>
        StorePluginOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed for store (unattended)
        /// </summary>
        /// <param name="target"></param>
        StorePluginOptions Default(IArgumentsService arguments);
    }
}
