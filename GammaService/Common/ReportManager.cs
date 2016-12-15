using System;
using System.Data.Entity.Core.EntityClient;
using System.IO;
using System.Linq;
using FastReport;

namespace GammaService.Common
{
    static class ReportManager
    {          
        static ReportManager()
        {
            GammaBase = new GammaEntities();            
            ReportSettings.PreviewSettings.Buttons = (PreviewButtons.Print | PreviewButtons.Save);
            ReportSettings.PreviewSettings.ShowInTaskbar = true;
        }

        private static GammaEntities GammaBase { get; set; }
        
        public static void PrintReport(Guid reportid, string printerName, Guid? paramId = null, int numCopies = 1)
        {
            using (var report = new Report())
            {
                var reportTemplate = (from rep in GammaBase.Templates where rep.ReportID == reportid select rep.Template).FirstOrDefault();
                if (reportTemplate == null) return;
                var stream = new MemoryStream(reportTemplate);                
                report.Load(stream);
                if (paramId != null) report.SetParameterValue("ParamID", paramId);
                report.Dictionary.Connections[0].ConnectionString = GammaBase.Database.Connection.ConnectionString;
                report.PrintSettings.ShowDialog = false;
                report.PrintSettings.Copies = numCopies;
                report.PrintSettings.Printer = printerName;
                try
                {
                    report.Print();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
            }
        }

        public static void PrintReport(string reportName, string printerName, string reportFolder = null, Guid? paramId = null, int numCopies = 1)
        {
            var parentId = GammaBase.Reports.Where(r => r.Name == reportFolder).Select(r => r.ReportID).FirstOrDefault();
            var reports = GammaBase.Reports.Where(r => r.Name == reportName && (parentId == null || r.ParentID == parentId)).
                Select(r => r.ReportID).ToList();
            if (reports.Count == 1)
            {
                PrintReport(reports[0], printerName, paramId, numCopies);
            }
        }

        private static readonly EnvironmentSettings ReportSettings = new EnvironmentSettings();

//        private static Guid CurrentReportID { get; set; }
        
    }
}
