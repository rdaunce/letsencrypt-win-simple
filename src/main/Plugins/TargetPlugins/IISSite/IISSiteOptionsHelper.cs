﻿using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class IISSiteOptionsHelper
    {
        private ILogService _log;

        public IISSiteOptionsHelper(ILogService log)
        {
            _log = log;
        }

        public bool AquireAdvancedOptions(IInputService input, IEnumerable<string> chosen, RunLevel runLevel, IIISSiteOptions ret)
        {
            if (runLevel.HasFlag(RunLevel.Advanced))
            {
                // Exclude bindings 
                input.WritePagedList(chosen.Select(x => Choice.Create(x, "")));
                ret.ExcludeBindings = input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions").ParseCsv();
            }

            var remaining = chosen.Except(ret.ExcludeBindings ?? new List<string>());
            if (remaining.Count() == 0)
            {
                _log.Error("No bindings remain");
                return false;
            }

            // Set common name
            if (remaining.Count() > 1)
            {
                ret.CommonName = input.ChooseFromList(
                    "Select primary domain (common name)",
                    remaining,
                    x => Choice.Create(x),
                    "Default");
            }
            return true;
        }

        public bool DefaultAdvancedOptions(IISSiteArguments args, IEnumerable<string> chosen, RunLevel runLevel, IIISSiteOptions ret)
        {
            ret.ExcludeBindings = args.ExcludeBindings.ParseCsv();
            if (ret.ExcludeBindings != null)
            {
                ret.ExcludeBindings = ret.ExcludeBindings.Select(x => x.ConvertPunycode()).ToList();
            }
            var remaining = chosen.Except(ret.ExcludeBindings ?? new List<string>());
            var commonName = args.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = commonName.ToLower().Trim().ConvertPunycode();
                if (remaining.Contains(commonName))
                {
                    ret.CommonName = commonName;
                }
                else
                {
                    _log.Error("Common name {commonName} not found or excluded", commonName);
                    return false;
                }
            }
            return true;
        }

    }

    public interface IIISSiteOptions
    {
        List<string> ExcludeBindings { get; set; }
        string CommonName { get; set; }
    }
}
