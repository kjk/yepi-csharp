using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

﻿namespace Yepi
{
    public static class ExtensionMethods
    {
        public static void AppendName(this StringBuilder sb, int level, string name)
        {
            sb.Append(Utils.LevelSpaces(level));
            sb.AppendFormat(name + "\n");
        }

        public static void AppendNameValue(this StringBuilder sb, string name, string value)
        {
            if (null != value)
                sb.AppendFormat("{0}: {1}\n", name, value);
        }

        public static void AppendNameValue(this StringBuilder sb, int level, string name, string value)
        {
            if (null != value)
            {
                sb.Append(Utils.LevelSpaces(level));
                sb.AppendFormat("{0}: {1}\n", name, value);
            }
        }
    }

    public static class Utils
    {
        static string spaces = "                                                      ";
        public static string LevelSpaces(int level)
        {
            return spaces.Substring(0, level * 2);
        }

        public static string CleanAppVer(string appVer)
        {
            string[] parts = appVer.Split('.');
            int n = parts.Length;
            while ((n > 0) && ((parts[n-1] == "") || (parts[n-1] == "0")))
                --n;
            appVer = string.Join(".", parts, 0, n);
            if (1 == n)
                appVer += ".0"; // turn "1" into "1.0"
            return appVer;
        }

        static string v2fhelper(string v, string suff, ref float[] version, float weight)
        {
            float f = 0;
            string[] parts = v.Split(new string[] { suff }, StringSplitOptions.RemoveEmptyEntries);
            if (2 != parts.Length)
            {
                return v;
            }
            float.TryParse(v, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out f);
            version[4] = weight;
            version[5] = f;
            return parts[0];
        }

        static float version2float(string v)
        {
            float[] version = new float[]{
                    0, 0, 0, 0, // 4-part numerical revision
                    4, // alpha, beta, rc or (default) final
                    0, // alpha, beta or RC version revision
                    1 // Pre or (default) final
                };
            string[] parts = v.Split(new string[] { "pre" }, StringSplitOptions.RemoveEmptyEntries);
            if (2 == parts.Length)
            {
                version[6] = 0;
                v = parts[0];
            }

            v = v2fhelper(v, "a", ref version, 1);
            v = v2fhelper(v, "b", ref version, 2);
            v = v2fhelper(v, "rc", ref version, 3);
            parts = v.Split(new char[] { '.' }, 4);
            for (int i = 0; i < parts.Length; i++)
            {
                float f = 0;
                float.TryParse(parts[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out f);
                version[i] = f;
            }
            float ver = version[0];
            ver += version[1] / 100.0f;
            ver += version[2] / 10000.0f;
            ver += version[3] / 1000000.0f;
            ver += version[4] / 100000000.0f;
            ver += version[5] / 10000000000.0f;
            ver += version[6] / 1000000000000.0f;
            return ver;
        }

        public static bool ProgramVersionGreater(string ver1, string ver2)
        {
            var v1f = version2float(ver1.ToLower());
            var v2f = version2float(ver2.ToLower());
            bool greater = v1f > v2f;
            Log.Line(String.Format("ProgramVersionGreater(), ver1={0}, ver2={1}, v1f={2}, v2f={3}, v1f>v2f={4}", ver1, ver2, v1f, v2f, greater));
            return greater;
        }

        public static bool ParseUpdateResponse(string updateTxt, out string version, out string url)
        {
            version = url = null;
            if (null == updateTxt)
                return false;
            var parts = updateTxt.Split('\n');
            foreach (var s in parts)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (null == version)
                {
                    version = s;
                    continue;
                }
                url = s;
                break;
            }
            return url != null;
        }

        public static string FileExtenstionFromUrl(string url)
        {
            int pos = url.LastIndexOf('.');
            if (-1 == pos)
                return "";
            return url.Substring(pos + 1);
        }

        public static List<DriveInfo> GetFixedReadyDrives()
        {
            var drives = new List<DriveInfo>();
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType == DriveType.Fixed && d.IsReady)
                    drives.Add(d);
            }
            return drives;
        }

        public static List<DriveInfo> GetReadyDrives()
        {
            var drives = new List<DriveInfo>();
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                    drives.Add(d);
            }
            return drives;
        }

        /// <summary>
        /// Converts a byte array to a string, using its byte order mark to convert it to the right encoding.
        /// http://www.shrinkrays.net/code-snippets/csharp/an-extension-method-for-converting-a-byte-array-to-a-string.aspx
        /// </summary>
        /// <param name="buffer">An array of bytes to convert</param>
        /// <returns>The byte as a string.</returns>
        public static string GetString(byte[] buffer, Encoding defaultEncoding)
        {
            if (buffer == null || buffer.Length == 0)
                return "";

            Encoding encoding = defaultEncoding; // Encoding.UTF8;

            /*
                EF BB BF		UTF-8
                FF FE UTF-16	little endian
                FE FF UTF-16	big endian
                FF FE 00 00		UTF-32, little endian
                00 00 FE FF		UTF-32, big-endian
                */

            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
            {
                encoding = Encoding.UTF8;
            }
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
            {
                encoding = Encoding.Unicode;
            }
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
            {
                encoding = Encoding.BigEndianUnicode; // utf-16be
            }
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
            {
                encoding = Encoding.UTF32;
            }
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
            {
                encoding = Encoding.UTF7;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static int CompareByFileType(this String x, String y)
        {
            var xExt = System.IO.Path.GetExtension(x);
            var yExt = System.IO.Path.GetExtension(y);
            int res = String.Compare(xExt, yExt, StringComparison.OrdinalIgnoreCase);
            if (0 != res)
                return res;
            return String.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        public static bool RemoveByShift(this Array a, object o)
        {
            if (null == a)
                return false;
            for (var i = 0; i < a.Length; i++)
            {
                if (a.GetValue(i) == o)
                {
                    for (var j = i; j < a.Length - 1; j++)
                        a.SetValue(a.GetValue(j + 1), j);
                    a.SetValue(null, a.Length - 1);
                    return true;
                }
            }
            return true;
        }

        public static void ShiftRight(this Array a)
        {
            if (null == a)
                return;
            var len = a.Length;
            Array.Copy(a, 0, a, 1, len - 1);
        }

        public static string Plural(string s, int count)
        {
            if (1 == count)
                return s;
            return s + "s";
        }

        public static string SanitizeForFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(name, invalidReStr, "");
        }

        public static BitmapImage TryLoadBitmapImage(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                MemoryStream strm = new MemoryStream(bytes);

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                // based on http://www.hanselman.com/blog/DealingWithImagesWithBadMetadataCorruptedColorProfilesInWPF.aspx
                // avoid exception when loading images with corrupted color profile
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.StreamSource = strm;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                strm.Close();
                return bmp;
            }
            catch (Exception e)
            {
                Log.E(e);
                return null;
            }
        }

        public static string PrettySizeShort(long n)
        {
            return PrettySizeHelper(n, "b");
        }

        public static string PrettySize(long n)
        {
            return PrettySizeHelper(n, "bytes");
        }

        public static string PrettySizeHelper(long n, string bytesPostfix)
        {
            string postfix;

            long divider = 1;
            if (n >= 1024 * 1024 * 1024)
            {
                divider = 1024 * 1024 * 1024;
                postfix = "GB";
            }
            else if (n >= 1024 * 1024)
            {
                divider = 1024 * 1024;
                postfix = "MB";
            }
            else if (n >= 1024)
            {
                divider = 1024;
                postfix = "KB";
            }
            else
                postfix = bytesPostfix;
            float size = (float)n / (float)divider;
            return String.Format("{0:0.##} {1}", size, postfix);
        }

        public static string ByteToHex(byte[] bytes)
        {
            var s = new StringBuilder();
            foreach (byte b in bytes)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }

        private static byte hexCharToByte(char c)
        {
            if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            if (c >= 'a' && c <= 'h')
                return (byte)(c - 'a' + 10);
            if (c >= 'A' && c <= 'H')
                return (byte)(c - 'A' + 10);
            return 255;
        }

        public static byte[] HexToByte(string s)
        {
            if (s.Length % 2 == 1)
                return null;
            var len = s.Length / 2;
            byte[] res = new byte[len];
            for (var i = 0; i < len; i++)
            {
                byte v1 = hexCharToByte(s[i * 2]);
                byte v2 = hexCharToByte(s[(i * 2) + 1]);
                if (v1 == 255 || v2 == 255)
                    return null;
                int n = v1 * 16 + v2;
                res[i] = (byte)n;
            }
            return res;
        }

        public static T FindWindowOfType<T>() where T : Window
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is T)
                    return (T)win;
            }
            return null;
        }

        public static int CountWindowsOfType<T>() where T : Window
        {
            int count = 0;
            foreach (Window win in Application.Current.Windows)
            {
                if (win is T)
                    count += 1;
            }
            return count;
        }

        public static void CloseWindowsOfType<T>() where T : Window
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is T)
                    win.Close();
            }
        }

        public static string GetPostData(List<Tuple<string, string, string>> postData, string boundary)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in postData)
            {
                var name = p.Item1;
                var value = p.Item2;
                var fileName = p.Item3;
                if (fileName != null)
                {
                    sb.Append(string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\n;Content-Type: text/plain\r\n\r\n", boundary, name, fileName));
                    sb.Append(value);
                }
                else
                {
                    sb.Append(string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n", boundary, name, value));
                }
            }
            sb.Append("\r\n--" + boundary + "--\r\n");
            return sb.ToString();
        }

        public static ImageSource BitmapSourceFromBitmap(Bitmap bitmap)
        {
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            bitmap.GetHbitmap(),
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            return bitmapSource;
        }

        public static bool IsShiftPressed()
        {
            return Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ||
                   Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift);
        }

        public static bool IsCtrlPressed()
        {
            return Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                   Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);
        }

        public static double PointsToPixels(double points)
        {
            return points * (96.0 / 72.0);
        }
    }
}
