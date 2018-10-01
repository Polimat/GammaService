using System;
using System.IO;
using System.Text;

namespace GammaService.Common
{
    static class Console
    {
        static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0:yyyMMdd}.log",DateTime.Now));

        public static void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public static void WriteLine(string line)
        {
            Write(line + Environment.NewLine);
        }

        public static void WriteLine(Exception ex)
        {
            string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3}\r\n",
                DateTime.Now, ex.TargetSite.DeclaringType, ex.TargetSite.Name, ex.Message);
            System.Console.WriteLine(fullText);
        }

        public static void WriteLine(int n)
        {
            Write(n.ToString() + Environment.NewLine);
        }

        private static object sync = new object();
        public static void Write(string line)
        {
            try
            {
                lock (sync)
                {
                    File.AppendAllText(_logPath, line, Encoding.GetEncoding("Windows-1251"));
                }
                System.Console.Write(line);
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }

        public static void Write(Exception ex)
        {
            WriteLine(ex);
        }

        public static void Write(int n)
        {
            Write(n.ToString());
        }
    }
}

