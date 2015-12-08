namespace Yepi
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class Log
    {
        public static StreamWriter LogFile = null;

        public static bool WithTimeStamp = false;

        public static string DefaultPath(string logDir)
        {
            DateTime now = DateTime.Now;
            int y = now.Year;
            int m = now.Month;
            int d = now.Day;
            string logFilePath = Path.Combine(logDir, String.Format("log-{0:0000}-{1:00}-{2:00}.txt", y, m, d));
            return logFilePath;
        }

        public static StreamWriter TryOpen(string path)
        {
            lock (typeof(Log))
            {
                // don't open log file twice
                if (LogFile != null)
                {
                    return LogFile;
                }
                try
                {
                    FileStream fileWriter = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                    LogFile = new StreamWriter(fileWriter);
                }
                catch
                {
                    return null;
                }
                return LogFile;
            }
        }

        public static void Close()
        {
            lock (typeof(Log))
            {
                if (null != LogFile)
                {
                    LogFile.Close();
                    LogFile = null;
                }
            }
        }

        // TODO: deprecate in favor of S()
        public static void L(string s)
        {
            S(s);
        }

        // S is for "string" i.e. Log.S() => Log.String()
        public static void S(string s)
        {
            lock (typeof(Log))
            {
                if (null == LogFile)
                {
#if DEBUG
                    Console.Write(s);
#endif
                    return;
                }

                if (WithTimeStamp)
                {
                    DateTime dt = DateTime.Now;
                    int h = dt.Hour;
                    int m = dt.Minute;
                    int sec = dt.Second;
                    string ts = String.Format("{0:00}:{1:00}:{2:00} ", h, m, sec);
                    LogFile.Write(ts);
                }
                LogFile.Write(s);
#if DEBUG
                Console.Write(s);
#endif
                LogFile.Flush();
            }
        }

        public static void Line(string s)
        {
            S(s + "\n");
        }

        public static void LineWithStackTrack(string s)
        {
            Line(s);
            var st = new StackTrace(true);
            Line(st.ToString());
        }

        public static void Time(String msg, long timeMs)
        {
            S(String.Format("Time: {0} ms, {1}\n", timeMs, msg));
        }

        // log exception
        public static void E(Exception e)
        {
            if (null != e)
            {
                Line("Exception " + e.StackTrace);
                Line(e.Message);
            }
        }
    }
}
