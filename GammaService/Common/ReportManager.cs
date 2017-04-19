using System;
using System.IO;
using System.Linq;
using FastReport;

namespace GammaService.Common
{
    static class ReportManager
    {          
        static ReportManager()
        {
            ReportSettings.PreviewSettings.Buttons = (PreviewButtons.Print | PreviewButtons.Save);
            ReportSettings.PreviewSettings.ShowInTaskbar = true;
        }
        
        private static void PrintReport(Guid reportid, string printerName, Guid? paramId = null, int numCopies = 1)
        {
            using (var report = new Report())
            {
                try
                {
                    using (var gammaBase = new GammaEntities())
                    {
                        var reportTemplate = (from rep in gammaBase.Templates
                            where rep.ReportID == reportid
                            select rep.Template).FirstOrDefault();
                        if (reportTemplate == null) return;
                        using (var stream = new MemoryStream(reportTemplate))
                        {
                            report.Load(stream);
                            if (paramId != null) report.SetParameterValue("ParamID",
                            paramId);
                            report.Dictionary.Connections[0].ConnectionString =
                            gammaBase.Database.Connection.ConnectionString;
                            report.PrintSettings.ShowDialog = false;
                            report.PrintSettings.Copies = numCopies;
                            report.PrintSettings.Printer = printerName;
                            report.Print();
                        }
                    }
                }
            catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now}: Ошибка при печати амбалажа");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static bool PrintReport(string reportName, string printerName, string reportFolder = null, Guid? paramId = null, int numCopies = 1)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    var parentId = gammaBase.Reports.Where(r => r.Name == reportFolder).Select(r => r.ReportID).FirstOrDefault();
                    var reports = gammaBase.Reports.Where(r => r.Name == reportName && (parentId == null || r.ParentID == parentId)).
                        Select(r => r.ReportID).ToList();
                    if (reports.Count == 1)
                    {
                        PrintReport(reports[0], printerName, paramId, numCopies);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Ошибка при печати амбалажа");
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private static readonly EnvironmentSettings ReportSettings = new EnvironmentSettings();

    }
}
