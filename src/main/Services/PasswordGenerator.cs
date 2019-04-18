﻿using System;
using System.Security.Cryptography;

namespace PKISharp.WACS.Services
{
    class PasswordGenerator
    {
        public string Generate()
        {
            // Set 256 bit random password that will be used to keep the .pfx file in the cache folder safe.
            var random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[32];
            random.GetBytes(buffer);
            return Convert.ToBase64String(buffer);
        }
    }
}
