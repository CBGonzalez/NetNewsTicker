using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace NetNewsTicker.Model
{
    internal static class Logger
    {
        public enum Level { Information, Warning, Error, Debug };
        private static readonly TimerCallback cb = new TimerCallback(TimerTick);
        private static Timer timer;
        private static StreamWriter logFile;
        private static bool isInitialized = false;
        private static int syncPoint = 0;
        private static ConcurrentQueue<string> logs;

        public static (bool, string) InitializeLogger(string logFileName)
        {
            if(isInitialized)
            {
                return (false, "Already initialized");
            }
            bool success = false;
            string errorMessage = string.Empty;
            if(String.IsNullOrEmpty(logFileName))
            {
                return (success, "Invalid file name for log, empty or null");
            }
            try
            {
                logFile = File.CreateText(logFileName);
                logFile.AutoFlush = true;
                timer = new Timer(cb, null, 1000, 1000);
                logs = new ConcurrentQueue<string>();
                isInitialized = true;
                success = true;
            }
            catch(IOException ex)
            {
                errorMessage = ex.ToString();
            }
            return (success, errorMessage);
        }

        public static void Log(string entry, Level level)
        {
            if(!isInitialized)
            {
                return;
            }            
            logs.Enqueue(item: $"{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToLongTimeString()}\t{level.ToString()}\t{entry}");            
        }

        private static void TimerTick(object state)
        {
            if(logs.IsEmpty)
            {
                return;
            }
            bool stillRunning = Interlocked.CompareExchange(ref syncPoint, 1, 0) != 0; //Check if still busy
            if (stillRunning)
            {
                return;
            }                                   
            while (logs.TryDequeue(out string logEntry))
            {
                logFile.WriteLine(logEntry);
            }                    
            Interlocked.Exchange(ref syncPoint, 0); // Signal that we are done            
        }

        public static void Close()
        {
            if (isInitialized)
            {
                timer.Change(0, -1);
                isInitialized = false;
                while (Interlocked.CompareExchange(ref syncPoint, 1, 0) != 0) // wait for timer callback to fimish, if running
                {
                    Thread.Sleep(10);
                }
                Interlocked.Exchange(ref syncPoint, 1); //prevent timer callback from running
                             
                while (logs.TryDequeue(out string logEntry)) // Try to get a full log
                {
                    logFile.WriteLine(logEntry);
                }
                logFile.Flush();
                logFile.Close();
                timer.Dispose();
                timer = null;
                Interlocked.Exchange(ref syncPoint, 0); // Free the lock for next run 
            }
        }
    }
}
