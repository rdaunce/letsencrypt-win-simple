﻿using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("2bfb3ef8-64b8-47f1-8185-ea427b793c1a")]
    class DreamhostOptions : ValidationPluginOptions<DreamhostDnsValidation>
    {
        public override string Name => "Dreamhost";

        public override string Description => "Change records in Dreamhost DNS";

        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string SecretSafe { get; set; }

        [JsonIgnore]
        public string ApiKey
        {
            get => SecretSafe.Unprotect();
            set => SecretSafe = value.Protect();
        }
    }
}
