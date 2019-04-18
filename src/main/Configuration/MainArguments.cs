﻿namespace PKISharp.WACS.Configuration
{
    public class MainArguments
    {
        public string BaseUri { get; set; }
        public string ImportBaseUri { get; set; }

        public bool Import { get; set; }
        public bool Test { get; set; }
        public bool Verbose { get; set; }
        public bool Help { get; set; }
        public bool Version { get; set; }
        public bool List { get; set; }

        public bool Renew { get; set; }
        public bool Force { get; set; }

        public string FriendlyName { get; set; }
        public bool Cancel { get; set; }

        public string Target { get; set; }
        public string Validation { get; set; }
        public string ValidationMode { get; set; }
        public string Csr { get; set; }
        public string Store { get; set; }
        public string Installation { get; set; }

        public bool CloseOnFinish { get; set; }
        public bool HideHttps { get; set; }

        public bool NoTaskScheduler { get; set; }
        public bool UseDefaultTaskUser { get; set; }

        public bool AcceptTos { get; set; }
        public string EmailAddress { get; set; }
    }
}