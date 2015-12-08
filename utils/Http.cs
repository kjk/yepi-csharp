using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;

namespace Yepi
{
    public class Http
    {
        // returns null if failed to download
        public static async Task<string> UrlDownloadAsStringAsync(string uri)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var res = await client.GetStringAsync(uri);
                    return res;
                }
            }
            catch (Exception e)
            {
                Log.E(e);
                return null;
            }
        }

        // returns null if failed to download
        public static async Task<byte[]> UrlDownloadAsync(string uri)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var res = await client.GetByteArrayAsync(uri);
                    return res;
                }
            }
            catch (Exception e)
            {
                Log.E(e);
                return null;
            }
        }


        // Note: 40x responses from the server cause an exception from HttpWebResponse.GetResponse()
        // in which case we'll return Tuple<null,null>. This might need to change.
        // http://stackoverflow.com/questions/692342/net-httpwebrequest-getresponse-raises-exception-when-http-status-code-400-bad
        public static Tuple<string, HttpWebResponse> TryUrlGet(string url)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                HttpWebResponse rsp = req.GetResponse() as HttpWebResponse;

                var defaultEncoding = Encoding.UTF8;
                if (!String.IsNullOrWhiteSpace(rsp.CharacterSet))
                {
                    // TODO: check for non utf-8 character sets
                }
                byte[] data = rsp.GetResponseStream().ReadAsBytes();
                rsp.Close();
                string s = Utils.GetString(data, defaultEncoding);
                Log.Line(String.Format("TryUrlGet(): downloaded url '{0}' of size {1} bytes", url, data.Length));
                return new Tuple<string, HttpWebResponse>(s, rsp);
            }
            catch (Exception e)
            {
                Log.Line(String.Format("TryUrlGet() for '{0}' failed", url));
                Log.E(e);
                // it's ok if we fail
                return new Tuple<string, HttpWebResponse>(null, null);
            }
        }

        // Atomic downloading of the content of a url to a file.
        // Downloads to temporary file and renames to destination path
        // after download to avoid problem of unfinished downloads caused e.g.
        // by killing the program.
        public static bool TryUrlGetToFileAtomic(string url, string dstPath)
        {
            string tmpPath = TryUrlGetToTempFile(url, System.IO.Path.GetTempPath());
            if (null == tmpPath)
                return false;
            bool ok = Utils.TryFileMove(tmpPath, dstPath);
            if (!ok)
                Utils.TryFileDelete(tmpPath);
            return ok;
        }

        // TODO: use system temp directory instead of providing it by the app
        public static string TryUrlGetToTempFile(string url, string dir)
        {
            try
            {
                var path = System.IO.Path.Combine(dir, System.IO.Path.GetRandomFileName());
                WebRequest request = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream fileReader = response.GetResponseStream();
                using (FileStream fileWriter = File.Open(path, FileMode.Create))
                {
                    fileReader.CopyTo(fileWriter);
                }
                fileReader.Close();
                var size = new FileInfo(path).Length;
                Log.Line(String.Format("TryUrlGetToTempFile(): downloaded url '{0}' of size {1} bytes", url, size));
                return path;
            }
            catch (Exception e)
            {
                Log.E(e);
                return null;
            }
        }
    }
}
