﻿using Autofac;
using Microsoft.Win32;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal class AutofacBuilder
    {
        /// <summary>
        /// This is used to import renewals from 1.9.x
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fromUri"></param>
        /// <param name="toUri"></param>
        /// <returns></returns>
        internal ILifetimeScope Legacy(ILifetimeScope main, string fromUri, string toUri)
        {
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => new MainArguments { BaseUri = fromUri, ImportBaseUri = toUri }).
                    As<MainArguments>().
                    SingleInstance();

                builder.RegisterType<Importer>().
                    SingleInstance();

                builder.RegisterType<ArgumentsService>().
                    As<IArgumentsService>().
                    SingleInstance();
                
                builder.RegisterType<LegacySettingsService>().
                    As<ISettingsService>().
                    WithParameter(new TypedParameter(typeof(ISettingsService), main.Resolve<ISettingsService>())).
                    SingleInstance();

                builder.RegisterType<LegacyTaskSchedulerService>();
                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), RunLevel.Import)).
                    SingleInstance();

                // Check where to load Renewals from
                var hive = Registry.CurrentUser;
                var key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                if (key == null)
                {
                    hive = Registry.LocalMachine;
                    key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                }
                if (key != null)
                {
                    builder.RegisterType<RegistryLegacyRenewalService>().
                            As<ILegacyRenewalService>().
                            WithParameter(new NamedParameter("hive", hive.Name)).
                            SingleInstance();
                }
                else
                {
                    builder.RegisterType<FileLegacyRenewalService>().
                        As<ILegacyRenewalService>().
                        SingleInstance();
                }
            });
        }

        /// <summary>
        /// For revocation and configuration
        /// </summary>
        /// <param name="main"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal ILifetimeScope Configuration(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                resolver = main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>();
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginOptionsFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main, renewal.StorePluginOptions.Name)).As<List<IInstallationPluginOptionsFactory>>().SingleInstance(); 
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginOptionsFactory>().SingleInstance();
                builder.Register(c => resolver.GetCsrPlugin(main)).As<ICsrPluginOptionsFactory>().SingleInstance();
            });
        }

        /// <summary>
        /// For configuration and renewal
        /// </summary>
        /// <param name="main"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal ILifetimeScope Target(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                resolver = main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>();
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());
                builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve<ITargetPlugin>().Generate()).As<Target>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main, c.Resolve<Target>())).As<IValidationPluginOptionsFactory>().SingleInstance();
            });
        }

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel)
        {
            return target.BeginLifetimeScope(builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();

                // Used to configure TaskScheduler without renewal
                if (renewal != null)
                {
                    builder.RegisterInstance(renewal);

                    builder.RegisterInstance(renewal.StorePluginOptions).As(renewal.StorePluginOptions.GetType());
                    builder.RegisterInstance(renewal.CsrPluginOptions).As(renewal.CsrPluginOptions.GetType());
                    builder.RegisterInstance(renewal.ValidationPluginOptions).As(renewal.ValidationPluginOptions.GetType());
                    builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());

                    // Find factory based on options
                    builder.Register(x => {
                        var plugin = x.Resolve<PluginService>();
                        var match = plugin.ValidationPluginFactories(target).FirstOrDefault(vp => vp.OptionsType.PluginId() == renewal.ValidationPluginOptions.Plugin);
                        return match;
                    }).As<IValidationPluginOptionsFactory>().SingleInstance();

                    builder.RegisterType(renewal.CsrPluginOptions.Instance).As<ICsrPlugin>().SingleInstance();
                    builder.RegisterType(renewal.StorePluginOptions.Instance).As<IStorePlugin>().SingleInstance();
                    builder.RegisterType(renewal.ValidationPluginOptions.Instance).As<IValidationPlugin>().SingleInstance();
                    builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                    foreach (var i in renewal.InstallationPluginOptions)
                    {
                        builder.RegisterInstance(i).As(i.GetType());
                    }
                }
            });
        }

        /// <summary>
        /// Validation
        /// </summary>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        internal ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options, TargetPart target, string identifier)
        {
            return execution.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<HttpValidationParameters>().
                    WithParameters(new[] {
                        new TypedParameter(typeof(string), identifier),
                        new TypedParameter(typeof(TargetPart), target)
                    });
                builder.RegisterType(options.Instance).
                    WithParameters(new[] {
                        new TypedParameter(typeof(string), identifier),
                    }).
                    As<IValidationPlugin>().
                    SingleInstance();
            });
        }
    }
}
