﻿using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Does the actual work
    /// </summary>
    public interface IInstallationPlugin
    {
        /// <summary>
        /// Do the installation work
        /// </summary>
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="newCertificateInfo"></param>
        /// <param name="oldCertificateInfo"></param>
        void Install(IStorePlugin store, CertificateInfo newCertificateInfo, CertificateInfo oldCertificateInfo);
    }
}
