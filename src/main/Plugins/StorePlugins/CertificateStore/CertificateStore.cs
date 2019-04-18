﻿using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Properties;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStore : IStorePlugin, IDisposable
    {
        private ILogService _log;
        private const string _defaultStoreName = nameof(StoreName.My);
        private string _storeName;
        private X509Store _store;
        private IIISClient _iisClient;
        private CertificateStoreOptions _options;

        public CertificateStore(ILogService log, IIISClient iisClient, CertificateStoreOptions options)
        {
            _log = log;
            _iisClient = iisClient;
            _options = options;
            ParseCertificateStore();
            _store = new X509Store(_storeName, StoreLocation.LocalMachine);
        }

        private void ParseCertificateStore()
        {
            try
            {
                // First priority: specified in the parameters
                _storeName = _options.StoreName;

                // Second priority: specified in the .config 
                if (string.IsNullOrEmpty(_storeName))
                {
                    _storeName = Properties.Settings.Default.DefaultCertificateStore;
                }

                // Third priority: defaults
                if (string.IsNullOrEmpty(_storeName))
                {
                    // Default store should be WebHosting on IIS8+, and My (Personal) for IIS7.x
                    _storeName = _iisClient.Version.Major < 8 ? nameof(StoreName.My) : "WebHosting";
                }

                // Rewrite
                if (string.Equals(_storeName, "Personal", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Users trying to use the "My" store might have set "Personal" in their 
                    // config files, because that's what the store is called in mmc
                    _storeName = nameof(StoreName.My);
                }

                _log.Debug("Certificate store: {_certificateStore}", _storeName);
            }
            catch (Exception ex)
            {
                _log.Warning("Error reading CertificateStore from config, defaulting to {_certificateStore} Error: {@ex}", _defaultStoreName, ex);
            }
        }

        public void Save(CertificateInfo input)
        {
            var existing = FindByThumbprint(input.Certificate.Thumbprint);
            if (existing != null)
            {
                _log.Warning("Certificate with thumbprint {thumbprint} is already in the store", input.Certificate.Thumbprint);
                input.StorePath = existing.StorePath;
            }
            else
            {
                var certificate = input.Certificate;
                if (!Settings.Default.PrivateKeyExportable)
                {
                    certificate = new X509Certificate2(
                        input.CacheFile.FullName,
                        input.CacheFilePassword,
                        X509KeyStorageFlags.MachineKeySet |
                        X509KeyStorageFlags.PersistKeySet);
                }
                _log.Information("Installing certificate in the certificate store");
                input.StorePath = _store.Name;
                InstallCertificate(certificate);
            }
        }

        public void Delete(CertificateInfo input)
        {
            _log.Information("Uninstalling certificate from the certificate store");
            UninstallCertificate(input.Certificate.Thumbprint);
        }

        public CertificateInfo FindByThumbprint(string thumbprint)
        {
            return ToInfo(GetCertificate(CertificateService.ThumbprintFilter(thumbprint)));
        }

        private CertificateInfo ToInfo(X509Certificate2 cert)
        {
            if (cert != null)
            {
                return new CertificateInfo()
                {
                    Certificate = cert,
                    StorePath = _store.Name
                };
            }
            else
            {
                return null;
            }
        }

        private void InstallCertificate(X509Certificate2 certificate)
        {
            X509Store rootStore = null;
            try
            {
                rootStore = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine);
                rootStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch
            {
                _log.Warning("Error encountered while opening root store");
                rootStore = null;
            }

            X509Store imStore = null;
            try
            {
                imStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch
            {
                _log.Warning("Error encountered while opening intermediate certificate store");
                imStore = null;
            }

            try
            {
                _store.Open(OpenFlags.ReadWrite);
                _log.Debug("Opened certificate store {Name}", _store.Name);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store {name}", _store.Name);
                throw;
            }

            try
            {
                _log.Information(true, "Adding certificate {FriendlyName} to store {name}", certificate.FriendlyName, _store.Name);
                var chain = new X509Chain();
                chain.Build(certificate);
                foreach (var chainElement in chain.ChainElements)
                {
                    var cert = chainElement.Certificate;
                    if (cert.HasPrivateKey)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb})", cert.Subject, cert.Issuer, cert.Thumbprint);
                        _store.Add(cert);
                    }
                    else if (cert.Subject != cert.Issuer && imStore != null)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb}) to CA store", cert.Subject, cert.Issuer, cert.Thumbprint);
                        imStore.Add(cert);
                    }
                    else if (cert.Subject == cert.Issuer && rootStore != null)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb}) to AuthRoot store", cert.Subject, cert.Issuer, cert.Thumbprint);
                        rootStore.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error saving certificate");
            }
            _log.Debug("Closing certificate stores");
            _store.Close();
            imStore.Close();
            rootStore.Close();
        }

        private void UninstallCertificate(string thumbprint)
        {
            try
            {
                _store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                throw;
            }

            _log.Debug("Opened certificate store {Name}", _store.Name);
            try
            {
                var col = _store.Certificates;
                foreach (var cert in col)
                {
                    if (string.Equals(cert.Thumbprint, thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _log.Information(true, "Removing certificate {cert} from store {name}", cert.FriendlyName, _store.Name);
                        _store.Remove(cert);
                    }
                }
                _log.Debug("Closing certificate store");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error removing certificate");
                throw;
            }
            _store.Close();
        }

        private X509Certificate2 GetCertificate(Func<X509Certificate2, bool> filter)
        {
            var possibles = new List<X509Certificate2>();
            try
            {
                _store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                return null;
            }
            try
            {
                var col = _store.Certificates;
                foreach (var cert in col)
                {
                    if (filter(cert))
                    {
                        possibles.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error finding certificate in certificate store");
                return null;
            }
            _store.Close();
            return possibles.OrderByDescending(x => x.NotBefore).FirstOrDefault();
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _store.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}