﻿using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class CsrPluginOptions : PluginOptions {}

    public abstract class CsrPluginOptions<TPlugin> : CsrPluginOptions where TPlugin : ICsrPlugin
    {
        public override abstract string Name { get; }
        public override abstract string Description { get; }

        public bool? OcspMustStaple { get; set; }

        public override void Show(IInputService input)
        {
            input.Show("CSR");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
            if (OcspMustStaple == true)
            {
                input.Show("OcspMustStaple", "Yes");
            }
        }
        public override Type Instance => typeof(TPlugin);
    }
}
