﻿using Newtonsoft.Json;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Clients
{
    class AcmeDnsClient
    {
        private ProxyService _proxy;
        private LookupClientProvider _dnsClient;
        private ILogService _log;
        private readonly string _dnsConfigPath;
        private string _baseUri;
        private IInputService _input;

        public AcmeDnsClient(LookupClientProvider dnsClient, ProxyService proxy, ILogService log, ISettingsService settings, IInputService input, string baseUri)
        {
            _baseUri = baseUri;
            _proxy = proxy;
            _dnsClient = dnsClient;
            _log = log;
            _input = input;
            _dnsConfigPath = Path.Combine(settings.ConfigPath, "acme-dns", _baseUri.CleanBaseUri());
            var di = new DirectoryInfo(_dnsConfigPath);
            if (!di.Exists)
            {
                di.Create();
            }
            _log.Verbose("Using {path} for acme-dns configuration", _dnsConfigPath);
        }

        /// <summary>
        /// Check for existing registration linked to the domain, or create a new one
        /// </summary>
        /// <param name="domain"></param>
        public bool EnsureRegistration(string domain, bool interactive)
        {
            var oldReg = RegistrationForDomain(domain);
            if (oldReg == null)
            {
                if (interactive)
                {
                    _log.Information($"Creating new acme-dns registration for domain {domain}");
                    var newReg = Register();
                    if (newReg != null)
                    {
                        // Verify correctness
                        do
                        {
                            _input.Show("Domain", domain, true);
                            _input.Show("Record", $"_acme-challenge.{domain}");
                            _input.Show("Type", "CNAME");
                            _input.Show("Content", newReg.Fulldomain + ".");
                            _input.Show("Note 1", "Some DNS control panels add the final dot automatically. Only one is required.");
                            _input.Show("Note 2", "Make sure your name servers are synchronised, this may take several minutes!");
                            if (!_input.Wait("Please press enter after you've created and verified the record"))
                            {
                                throw new Exception("User aborted");
                            }
                        }
                        while (!VerifyConfiguration(domain, newReg.Fulldomain));
                        File.WriteAllText(FileForDomain(domain), JsonConvert.SerializeObject(newReg));
                        return true;
                    }
                }
                else
                {
                    _log.Error("No previous acme-dns registration found for domain {domain}", domain);
                    return false;
                }
            }
            else
            {
                _log.Information($"Existing acme-dns registration for domain {domain} found");
                _log.Information($"Record: _acme-challenge.{domain}");
                _log.Information("CNAME: " + oldReg.Fulldomain);
                while (!VerifyConfiguration(domain, oldReg.Fulldomain))
                {
                    if (interactive)
                    {
                        if (!_input.Wait("Please press enter after you've corrected the record."))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Verify configuration
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cname"></param>
        /// <returns></returns>
        private bool VerifyConfiguration(string domain, string expected)
        {
            var lookup = _dnsClient.GetClient(domain);
            var result = lookup.LookupClient.Query($"_acme-challenge.{domain}", DnsClient.QueryType.CNAME);
            var value = result.Answers.CnameRecords().
                Select(cnameRecord => cnameRecord?.CanonicalName?.Value?.TrimEnd('.')).
                Where(txtRecord => txtRecord != null).
                FirstOrDefault();
            if (string.Equals(expected, value, StringComparison.CurrentCultureIgnoreCase))
            {
                _log.Debug("Verification of CNAME record successful");
                return true;
            }
            else
            {
                _log.Warning("Verification failed, {domain} found value {found} but expected {expected}", $"_acme-challenge.{domain}", value ?? "(null)", expected);
                return false;
            }
        }

        private string FileForDomain(string domain)
        {
            return Path.Combine(_dnsConfigPath, $"{domain.CleanBaseUri()}.json");
        }

        private RegisterResponse RegistrationForDomain(string domain)
        {
            var file = FileForDomain(domain);
            if (!File.Exists(file))
            {
                return null;
            }
            try
            {
                var text = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<RegisterResponse>(text);
            }
            catch
            {
                _log.Error($"Unable to read acme-dns registration from {file}");
                return null;
            }
        }

        private RegisterResponse Register()
        {
            WebClient client = Client();
            try
            {
                var response = client.UploadString($"/register", "");
                return JsonConvert.DeserializeObject<RegisterResponse>(response);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating acme-dns registration");
                return null;
            }
        }

        public bool Update(string domain, string token)
        {
            var reg = RegistrationForDomain(domain);
            if (reg == null)
            {
                _log.Error("No registration found for domain {domain}", domain);
                return false;
            }
            if (!VerifyConfiguration(domain, reg.Fulldomain))
            {
                _log.Warning("Registration for domain {domain} appears invalid", domain);
            }
            var client = Client();
            client.Headers.Add("X-Api-User", reg.UserName);
            client.Headers.Add("X-Api-Key", reg.Password);
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            var request = new UpdateRequest()
            {
                Subdomain = reg.Subdomain,
                Token = token
            };
            try
            {
                client.UploadString($"/update", JsonConvert.SerializeObject(request));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error sending update request to acme-dns for domain {domain}", domain);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Construct common WebClient
        /// </summary>
        /// <returns></returns>
        private WebClient Client()
        {
            var x = new WebClient
            {
                Proxy = _proxy.GetWebProxy(),
                BaseAddress = _baseUri,
            };
            return x;
        }

        public class UpdateRequest
        {
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; }
            [JsonProperty(PropertyName = "txt")]
            public string Token { get; set; }
        }

        public class RegisterResponse
        {
            [JsonProperty(PropertyName = "username")]
            public string UserName { get; set; }
            [JsonProperty(PropertyName = "password")]
            public string Password { get; set; }
            [JsonProperty(PropertyName = "fulldomain")]
            public string Fulldomain { get; set; }
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; }
        }
    }
}
