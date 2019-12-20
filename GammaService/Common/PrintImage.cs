using System;
using System.Text;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Drawing;

namespace GammaService.Common
{
    class PrintImage
    {


        #region dll Wrappers
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int size);
        #endregion dll Wrappers

        #region Methods
        /// <summary>
        /// This function gets the image file name.
        /// This function opens the image file, gets all its bytes & send them to print.
        /// </summary>
        /// <param name="imageFileName">Image File Name</param>
        /// <param name="printerName">Printer Name</param>
        /// <returns>true on success, false on failure</returns>
        public static bool SendImageToPrinter(string imageFileName, string printerName = null, string modbusName = "NoModbusName")
        {
            try
            {
                #region Get Connected Printer Name
                PrintDocument pd = new PrintDocument();
                StringBuilder dp = new StringBuilder(256);
                int size = dp.Capacity;
                if (string.IsNullOrEmpty(printerName))
                {
                    if (GetDefaultPrinter(dp, ref size))
                    {
                        pd.PrinterSettings.PrinterName = dp.ToString().Trim();
                    }
                }
                else
                {
                    pd.PrinterSettings.PrinterName = printerName;
                }
                #endregion Get Connected Printer Name
                pd.PrintPage += (sender, args) =>
                {
                    Image i = Image.FromFile(imageFileName);
                    args.Graphics.DrawImage(i, args.PageBounds);
                };
                pd.Print();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(modbusName, ex.Message);
                return false;
            }
        }
        #endregion Methods
    }
}
