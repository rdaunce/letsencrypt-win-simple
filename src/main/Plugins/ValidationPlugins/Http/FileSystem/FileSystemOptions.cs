﻿using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [Plugin("1c77b3a4-5310-4c46-92c6-00d866e84d6b")]
    internal class FileSystemOptions : HttpValidationOptions<FileSystem>
    {
        public override string Name { get => "FileSystem"; }
        public override string Description { get => "Save file on local or network path"; }

        public FileSystemOptions() : base() { }
        public FileSystemOptions(HttpValidationOptions<FileSystem> source) : base(source) { }

        /// <summary>
        /// Alternative site for validation. The path will be
        /// determined from this site on each validation attempt
        /// </summary>
        public long? SiteId { get; set; }

        /// <summary>
        /// Show to use what has been configured
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            if (SiteId != null)
            {
                input.Show("Site", SiteId.ToString());
            }
        }
   
    }
}
