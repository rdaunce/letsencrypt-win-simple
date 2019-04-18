﻿using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("e45d62b9-f9a8-441e-b95f-c5ee0dcd8040")]
    class ManualOptions : ValidationPluginOptions<Manual>
    {
        public override string Name => "Manual";
        public override string Description => "Manually create record";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }
    }
}
