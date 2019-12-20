using System;
using System.IO;
using System.Text;

namespace GammaService.Common
{
    static class Console
    {
        static readonly string _logPath = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string _logFile = Path.Combine(_logPath, string.Format("{0:yyyMMdd}.log",DateTime.Now));

        public static void WriteLine(string logFileName,double s)
        {
            Write(logFileName, Environment.NewLine);
        }

        public static void WriteLine(string logFileName, string line)
        {
            Write(logFileName, line + Environment.NewLine);
        }

        public static void WriteLine(string logFileName, Exception ex)
        {
            string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3}\r\n",
                DateTime.Now, ex.TargetSite.DeclaringType, ex.TargetSite.Name, ex.Message);
            //System.Console.WriteLine(fullText);
            WriteLine(logFileName, fullText);
        }

        public static void WriteLine(string logFileName, int n)
        {
            Write(logFileName, n.ToString() + Environment.NewLine);
        }

        private static object sync = new object();

        public static void Write(string logFileName, string line)
        {
            indexPrintString = 0;
            Write2(logFileName, line);
        }

        private static void Write2(string logFileName, string line)
        {
            try
            {
                //lock (sync)
                //{
                //var _logPathName = Path.Combine(_logPath, string.Format("{0}.log", logFileName));
                File.AppendAllText(Path.Combine(_logPath, string.Format("{0}.log", logFileName)), logFileName == "NoModbusName" ? DateTime.Now + " :" + line: line, Encoding.GetEncoding("Windows-1251"));
                //}
                System.Console.Write(line);
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }

        public static void Write(string logFileName, Exception ex)
        {
            WriteLine(logFileName, ex);
        }

        public static void Write(string logFileName, int n)
        {
            Write(logFileName, n.ToString());
        }

        public static void SetCursorPosition(int cursorLeft, int cursorTop)
        {
            System.Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        public static int CursorLeft { get { return System.Console.CursorLeft; } }
        public static int CursorTop { get { return System.Console.CursorTop; }}

        private static string prev0PrintString { get; set; } = String.Empty;
        private static string prev1PrintString { get; set; } = String.Empty;
        private static string prev2PrintString { get; set; } = String.Empty;
        private static string[] prevPrintStrings { get; set; } = new string[0];
        private static Int32 indexPrintString { get; set; } = 0;

        public static void WriteLineWindow(byte countLine, string logFileName, string line)
        {
            lock (sync)
            {
                if (prevPrintStrings.Length < countLine)
                {
                    var _prevPrintStrings = new string[countLine];
                    prevPrintStrings.CopyTo(_prevPrintStrings, 0);
                    prevPrintStrings = _prevPrintStrings;
                }
                if (indexPrintString > countLine) System.Console.SetCursorPosition(0, System.Console.CursorTop - countLine);
                for (int i = countLine - 1; i > 0; i--)
                {
                    prevPrintStrings[i] = prevPrintStrings[i - 1];
                    if (prevPrintStrings[i] != null && indexPrintString > countLine) System.Console.WriteLine(prevPrintStrings[i]);
                }
                line = (line + "/" + indexPrintString++).PadRight(119);
                Write2(logFileName, line + Environment.NewLine);
                prevPrintStrings[0] = line;
            }
        }

    }
}

