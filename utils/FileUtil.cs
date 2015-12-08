using Microsoft.VisualBasic.FileIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Yepi
{
    public static class FileUtil
    {
        public static bool TryCreateDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    return true; // don't generate exceptions if already exists
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Log.E(e);
                return false;
            }
            return true;
        }

        public static void TryFileDelete(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return; // don't generate exceptions if doesn't exist
                File.Delete(path);
            }
            catch (Exception e)
            {
                Log.E(e);
            }
        }

        public static bool TryCreateEmptyFile(string path)
        {
            var ok = TryCreateDirectory(System.IO.Path.GetDirectoryName(path));
            if (!ok)
                return false;

            try
            {
                using (var fs = System.IO.File.Create(path))
                {
                    // just creating an empty file
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool TryFileMove(string src, string dst)
        {
            TryFileDelete(dst);
            if (!File.Exists(src))
                return false;
            try
            {
                File.Move(src, dst);
                return true;
            }
            catch (Exception e)
            {
                Log.E(e);
                return false;
            }
        }

        public static void TryDeleteFilePermanentlyWithUI(string file)
        {
            try
            {
                FileSystem.DeleteFile(file, UIOption.AllDialogs, RecycleOption.DeletePermanently);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                    return;
                Log.E(e);
            }
        }

        public static void TryDeleteFileToTrashWithUI(string file)
        {
            try
            {
                FileSystem.DeleteFile(file, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                    return;
                Log.E(e);
            }
        }

        public static void TryDeleteFolderPermanentlyWithUI(string folder)
        {
            try
            {
                FileSystem.DeleteDirectory(folder, UIOption.AllDialogs, RecycleOption.DeletePermanently);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                    return;
                Log.E(e);
            }
        }

        public static void TryDeleteFolderToTrashWithUI(string folder)
        {
            try
            {
                FileSystem.DeleteDirectory(folder, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                    return;
                Log.E(e);
            }
        }

        // Returns sha1 (as hex-encoded string) of the contents of file at <path>
        public static string Sha1ForFile(string path)
        {
            byte[] hash;
            using (Stream stream = File.OpenRead(path))
            {
                hash = SHA1.Create().ComputeHash(stream);
            }
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        public static string RenameAsSha1(string path, string dir, bool deleteIfFailed)
        {
            var finalPath = System.IO.Path.Combine(dir, Sha1ForFile(path) + ".txt");
            if (!TryFileMove(path, finalPath))
            {
                if (deleteIfFailed)
                    TryFileDelete(path);
                return null;
            }
            return finalPath;
        }

        public static string SaveStringWithSha1Name(string dir, string s)
        {
            string tmpPath = System.IO.Path.Combine(dir, System.IO.Path.GetRandomFileName());
            bool ok = TryWriteStringToFileAsUtf8(tmpPath, s);
            if (!ok)
                return null;
            return RenameAsSha1(tmpPath, dir, true);
        }


        public static void TryShowFileInExplorer(string path)
        {
            try
            {
                var arg = String.Format("/select,{0}", path);
                Process.Start("explorer.exe", arg);
            }
            catch (Exception e)
            {
                Log.E(e);
            }
        }

        public static bool TryWriteStringToFileAsUtf8(string path, string s)
        {
            try
            {
                using (FileStream f = File.Open(path, FileMode.Create))
                {
                    using (StreamWriter stm = new StreamWriter(f, Encoding.UTF8))
                    {
                        stm.Write(s);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                TryFileDelete(path);
                Log.E(e);
                return false;
            }
        }

        public static bool TryWriteStringToFileAsUtf8Atomic(string path, string s)
        {
            string tmpPath = TempPath();
            bool ok = TryWriteStringToFileAsUtf8(tmpPath, s);
            if (ok)
            {
                ok = TryFileMove(tmpPath, path);
                if (!ok)
                    TryFileDelete(tmpPath);
            }
            return ok;
        }

        public static string TempPath()
        {
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        }

        public static byte[] ReadAsBytes(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static string TryReadUtf8FromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null; // don't log exception if doesn't exist4
                string s = null;
                using (FileStream f = File.Open(path, FileMode.Open))
                {
                    using (StreamReader stm = new StreamReader(f, Encoding.UTF8))
                    {
                        s = stm.ReadToEnd();
                    }
                }
                return s;
            }
            catch (Exception e)
            {
                Log.E(e);
                return null;
            }
        }

        // per some info on the web, Process.Start(url) might throw
        // an exception, so swallow it
        public static void TryLaunchUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception e)
            {
                Log.E(e);
            }
        }

        public static void RunDefaultCommand(string path)
        {
            FileUtil.TryLaunchUrl(path);
        }

    }
}