using System;
using System.Reflection;

namespace Logging
{
    public class Log
    {
        public static void Debug(string value)                        { InternalLog("DEBUG",   Assembly.GetCallingAssembly().GetName().Name, value); }
        public static void Debug(string format, params object[] arg)  { InternalLog("DEBUG",   Assembly.GetCallingAssembly().GetName().Name, format, arg); }
        public static void Info(string value)                         { InternalLog("INFO",    Assembly.GetCallingAssembly().GetName().Name, value); }
        public static void Info(string value, params object[] arg)    { InternalLog("INFO",    Assembly.GetCallingAssembly().GetName().Name, value, arg); }
        public static void Warning(string value)                      { InternalLog("WARNING", Assembly.GetCallingAssembly().GetName().Name, value); }
        public static void Warning(string value, params object[] arg) { InternalLog("WARNING", Assembly.GetCallingAssembly().GetName().Name, value, arg); }
        public static void Error(string value)                        { InternalLog("ERROR",   Assembly.GetCallingAssembly().GetName().Name, value); }
        public static void Error(string value, params object[] arg)   { InternalLog("ERROR",   Assembly.GetCallingAssembly().GetName().Name, value, arg); }
        public static void Fatal(string value)                        { InternalLog("FATAL",   Assembly.GetCallingAssembly().GetName().Name, value); }
        public static void Fatal(string value, params object[] arg)   { InternalLog("FATAL",   Assembly.GetCallingAssembly().GetName().Name, value, arg); }

        public static void Exception(Exception e)
        {
            InternalLog("EXCEPTION", Assembly.GetCallingAssembly().GetName().Name, "Exception thrown: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }

        private static void InternalLog(string type, string callee, string format, params object[] arg)
        {
            string text = arg != null ? string.Format(format, arg) : format;

            Console.WriteLine("{0} {1,-7} [{2,-12}]: {3}", DateTime.Now.ToString(), type, callee, text);
        }

    }
}
