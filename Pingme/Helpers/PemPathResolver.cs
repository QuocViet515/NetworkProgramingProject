using System;
using System.IO;

namespace Pingme.Helpers
{
    public static class PemPathResolver
    {
        public static string GetSslPath(string filename)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, @"..\..\ssl", filename));
        }
    }
}
