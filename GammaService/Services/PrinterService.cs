using System;
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
    }
}
