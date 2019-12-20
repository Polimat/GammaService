using System;
using System.IO;
using System.Linq;
using System.Threading;
using FastReport;
using System.Drawing;

namespace GammaService.Common
{
	internal static class ReportManager
	{
		static ReportManager()
		{
			ReportSettings.ReportSettings.ShowProgress = false;
		}

		private static void PrintReport(Guid reportid, string printerName, Guid? paramId = null, int numCopies = 1, Image paramPNG = null, string modbusName = "NoModbusName")
		{
			using (var report = new Report())
			{
				try
				{
					Monitor.Enter(FastReportLock);
					using (var gammaBase = new GammaEntities())
					{
						var reportTemplate = (from rep in gammaBase.Templates
							where rep.ReportID == reportid
							select rep.Template).FirstOrDefault();
						if (reportTemplate == null) return;
						using (var stream = new MemoryStream(reportTemplate))
						{
							report.Load(stream);
							if (paramId != null)
								report.SetParameterValue("ParamID",
									paramId);
                            if (paramPNG != null)
                                report.SetParameterValue("ParamPNG",
                                    paramPNG);
                            report.Dictionary.Connections[0].ConnectionString =
								gammaBase.Database.Connection.ConnectionString;
							report.PrintSettings.ShowDialog = false;
							report.PrintSettings.Copies = numCopies;
							report.PrintSettings.Printer = printerName;
							report.Prepare();
							report.PrintPrepared();
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(modbusName,$"{DateTime.Now}: Ошибка при печати амбалажа");
					Console.WriteLine(modbusName,ex.Message);
				}
				finally
				{
					Monitor.Exit(FastReportLock);
				}
			}
		}


		public static bool PrintReport(string reportName, string printerName, string reportFolder = null,
			Guid? paramId = null, int numCopies = 1, Image paramPNG = null, string modbusName = "NoModbusName")
		{
			try
			{
				using (var gammaBase = new GammaEntities())
				{
					var parentId = gammaBase.Reports.Where(r => r.Name == reportFolder).Select(r => r.ReportID).FirstOrDefault();
					var reports = gammaBase.Reports.Where(r => r.Name == reportName && (parentId == null || r.ParentID == parentId))
						.Select(r => r.ReportID)
						.ToList();
					if (reports.Count == 1)
						PrintReport(reports[0], printerName, paramId, numCopies, paramPNG);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(modbusName, $"{DateTime.Now}: Ошибка при печати амбалажа");
				Console.WriteLine(modbusName, ex.Message);
				return false;
			}
			return true;
		}

        /// <summary>
        ///     Глобальные настройки FastReport
        /// </summary>
        private static readonly EnvironmentSettings ReportSettings = new EnvironmentSettings();

		private static readonly object FastReportLock = new object();
	}
}
