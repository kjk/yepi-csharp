namespace Yepi
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class Log
    {
        private static string logDir = null;
        private static StreamWriter logFile = null;
        private static int lastDayOfYear = 0;

        public static bool WithTimeStamp = false;

        public static string LogDir
        {
            get { return logDir; }
            set
            {
                logDir = value;
                if (null == logDir)
                    CloseLogFile();
            }
        }

        public static StreamWriter OpenLogFile()
        {
            if (LogDir == null)
                return null;
            DateTime now = DateTime.Now;
            if (null != logFile && lastDayOfYear == now.DayOfYear)
                return logFile;
            int y = now.Year;
            int m = now.Month;
            int d = now.Day;
            string logFilePath = Path.Combine(LogDir, String.Format("log-{0:0000}-{1:00}-{2:00}.txt", y, m, d));

            try
            {
                FileStream fileWriter = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                logFile = new StreamWriter(fileWriter);
            }
            catch
            {
                return null;
            }
            lastDayOfYear = now.DayOfYear;
            return logFile;
        }

        public static void CloseLogFile()
        {
            if (null != logFile)
            {
                logFile.Close();
                logFile = null;
            }
        }

        public static void L(string s)
        {
            lock (typeof(Log))
            {
                StreamWriter logFile = OpenLogFile();
                if (null == logFile)
                    return;

                if (WithTimeStamp)
                {
                    DateTime dt = DateTime.Now;
                    int h = dt.Hour;
                    int m = dt.Minute;
                    int sec = dt.Second;
                    string ts = String.Format("{0:00}:{1:00}:{2:00} ", h, m, sec);
                    logFile.Write(ts);
                }
                logFile.Write(s);
                logFile.Flush();
            }
        }

        public static void Ll(string s)
        {
            L(s + "\r\n");
        }

        public static void LlWithStackTrack(string s)
        {
            Ll(s);
            var st = new StackTrace(true);
            Ll(st.ToString());
        }

        public static void Lt(String msg, long timeMs)
        {
            L(String.Format("Time: {0} ms, {1}\n", timeMs, msg));
        }

        // log exception
        public static void Le(Exception e)
        {
            if (null != e)
            {
                Ll("Exception " + e.StackTrace);
                Ll(e.Message);
            }
        }
    }
}
