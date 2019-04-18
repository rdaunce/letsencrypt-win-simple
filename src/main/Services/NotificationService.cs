﻿using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;
using System.Net.Mail;

namespace PKISharp.WACS.Services
{
    class NotificationService
    {
        private ILogService _log;
        private ICertificateService _certificateService;
        private EmailClient _email;

        public NotificationService(ILogService log, EmailClient email, ICertificateService certificateService)
        {
            _log = log;
            _certificateService = certificateService;
            _email = email;
        }

        /// <summary>
        /// Handle success notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal void NotifySuccess(RunLevel runLevel, Renewal renewal)
        {
            // Do not send emails when running interactively
            _log.Information(true, "Renewal for {friendlyName} succeeded", renewal.LastFriendlyName);
            if (runLevel.HasFlag(RunLevel.Unattended) &&
                Properties.Settings.Default.EmailOnSuccess)
            {
                _email.Send(
                    "Certificate renewal completed",
                    $"<p>Certificate <b>{renewal.LastFriendlyName}</b> succesfully renewed.</p> {NotificationInformation(renewal)}",
                    MailPriority.Low);
            }
        }

        /// <summary>
        /// Handle failure notification
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        internal void NotifyFailure(RunLevel runLevel, Renewal renewal, string errorMessage)
        {
            // Do not send emails when running interactively       
            _log.Error("Renewal for {friendlyName} failed, will retry on next run", renewal.LastFriendlyName);
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                _email.Send("Error processing certificate renewal",
                    $"<p>Renewal for <b>{renewal.FriendlyName}</b> failed with error <b>{errorMessage}</b>, will retry on next run.</p> {NotificationInformation(renewal)}",
                    MailPriority.High);
            }
        }

        private string NotificationInformation(Renewal renewal)
        {
            try
            {
                var extraMessage = "";
                extraMessage += $"<p>Hosts: {NotificationHosts(renewal)}</p>";
                extraMessage += "<p><table><tr><td>Plugins</td><td></td></tr>";
                extraMessage += $"<tr><td>Target: </td><td> {renewal.TargetPluginOptions.Name}</td></tr>";
                extraMessage += $"<tr><td>Validation: </td><td> {renewal.ValidationPluginOptions.Name}</td></tr>";
                extraMessage += $"<tr><td>CSR: </td><td> {renewal.CsrPluginOptions.Name}</td></tr>";
                extraMessage += $"<tr><td>Store: </td><td> {renewal.StorePluginOptions.Name}</td></tr>";
                extraMessage += $"<tr><td>Installation: </td><td> {string.Join(", ", renewal.InstallationPluginOptions.Select(x => x.Name))}</td></tr>";
                extraMessage += "</table></p>";
                return extraMessage;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Retrieval of metadata for email failed.");
                return "";
            }
        }

        private string NotificationHosts(Renewal renewal)
        {
            try
            {
                var cache = _certificateService.CachedInfo(renewal);
                if (cache == null)
                {
                    return "Unknown";
                }
                else
                {
                    return string.Join(", ", cache.HostNames);
                }
            }
            catch
            {
                return "Error";
            }
        }
    }
}
