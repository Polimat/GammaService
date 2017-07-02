using System;
using System.IO;
using System.Linq;
using GammaService.Common;
using GammaService.Interfaces;

namespace GammaService.Services
{
    public class PrinterService: IPrinterService
    {
        public bool PrintPallet(Guid productId)
        {
            try
            {
                ReportManager.PrintReport("Амбалаж", "ASUTP", "Pallet", productId, 2);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

	    public bool ActivateProductionTask(Guid productionTaskId)
	    {
		    try
		    {
			    using (var gammaBase = new GammaEntities())
			    {
				    if (gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
					        .UseApplicator ?? false)
				    {
					    var outPath = gammaBase.C1CCharacteristics
						    .First(c => c.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
						    .PackageLabelPath;
					    var inPath = gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
						    .ApplicatorLabelPath;
					    if (string.IsNullOrEmpty(outPath) || string.IsNullOrEmpty(inPath))
					    {
						    return false;
					    }
					    File.Copy(outPath, inPath, true);
				    }
				    else
				    {
					    return true;
				    }
			    }

		    }
		    catch (Exception e)
		    {
				Console.WriteLine(e);
			    return false;
		    }
		    return true;
	    }
	}
}
