﻿using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpOptionsFactory : InstallationPluginFactory<IISFtp, IISFtpOptions>
    {
        private IIISClient _iisClient;

        public IISFtpOptionsFactory(ILogService log, IIISClient iisClient) : base(log)
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall(string storeType)
        {
            return _iisClient.HasFtpSites && storeType == CertificateStoreOptions.PluginName;
        }

        public override IISFtpOptions Aquire(Target renewal, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
            ret.SiteId = chosen;
            return ret;
        }

        public override IISFtpOptions Default(Target renewal, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<IISFtpArguments>();
            var ret = new IISFtpOptions();
            var siteId = args.FtpSiteId;
            if (siteId == null)
            {
                throw new Exception($"Missing parameter --{nameof(args.FtpSiteId).ToLower()}");
            }
            // Throws exception when site is not found
            var site = _iisClient.GetFtpSite(siteId.Value);
            ret.SiteId = site.Id;
            return ret;
        }
    }
}
