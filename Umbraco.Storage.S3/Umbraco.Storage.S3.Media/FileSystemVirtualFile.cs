using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace Umbraco.Storage.S3.Media
{
    internal class FileSystemVirtualFile : VirtualFile
    {
        private readonly DateTimeOffset _lastModified;
        private readonly Func<Stream> _stream;

        public FileSystemVirtualFile(string virtualPath, DateTimeOffset lastModified, Func<Stream> stream) : base(virtualPath)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            _lastModified = lastModified;
            _stream = stream;
        }

        public override Stream Open()
        {
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.Public);
            HttpContext.Current.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            HttpContext.Current.Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
            HttpContext.Current.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
            HttpContext.Current.Response.Cache.SetETag(GenerateETag(_lastModified.DateTime, DateTime.Now));
            return _stream();
        }

        public override bool IsDirectory
        {
            get { return false; }
        }

        private static string GenerateETag(DateTime lastModified, DateTime now)
        {
            // Get 64-bit FILETIME stamp
            long lastModFileTime = lastModified.ToFileTime();
            long nowFileTime = now.ToFileTime();
            string hexFileTime = lastModFileTime.ToString("X8", CultureInfo.InvariantCulture);

            // Do what IIS does to determine if this is a weak ETag.
            // Compare the last modified time to now and if the difference is
            // less than or equal to 3 seconds, then it is weak
            if ((nowFileTime - lastModFileTime) <= 30000000)
            {
                return "W/\"" + hexFileTime + "\"";
            }
            return "\"" + hexFileTime + "\"";
        }
    }
}
