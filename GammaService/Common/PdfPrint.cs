using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GammaService.Common
{
    public class PdfPrint
    {
        #region dll Wrappers
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int size);
        #endregion dll Wrappers

        public static bool PrintPdfDocument(string pdfFileName, string printerName = null)
        {
            FileStream pdfPathStream = null;
                
            try
            {
                //pdfFileName = @"D:\ТекущаяЭтикетка.pdf";
                /*if (pdfFileName == @"\\server1c\Gamma_pdf\uppstg\Syktyvkar\00000002178-lab_gr.pdf")
                {
                    pdfFileName = @"D:\\00000002178-lab_gr.pdf";
                }*/
                pdfPathStream = new FileStream(pdfFileName, FileMode.Open);
                var mainRasterizer = CreateRasterizer(pdfPathStream); // нужен для посчета страниц

                PageStore = new Dictionary<int, MemoryStream>();
                PdfProcessing(1, mainRasterizer.PageCount, pdfPathStream);
                while (PageStore.Count < mainRasterizer.PageCount)
                { // ждем завершения рендеринга
                  //Console.WriteLine("{0:000.0}%", ((double)PageStore.Count) / mainRasterizer.PageCount * 100);
                  //Thread.Sleep(100);
                }
                //Console.WriteLine("Start printing");
                PrintPages(PageStore, printerName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (pdfPathStream != null)
                    pdfPathStream.Close();
            }
            }

        public static bool PrintPNGDocument(byte[] imgPNG, string printerName = null)
        {
            FileStream pdfPathStream = null;

            try
            {
                PageStore = new Dictionary<int, MemoryStream>();
                PageStore.Add(1, new MemoryStream(imgPNG));
                //Console.WriteLine("Start printing");
                PrintPages(PageStore, printerName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (pdfPathStream != null)
                    pdfPathStream.Close();
            }
        }

        public static Dictionary<int, MemoryStream> PageStore; //хранилище отрендеренных изображений
        private const int Dpi = 200;

        static GhostscriptVersionInfo _lastInstalledVersion = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);

        static GhostscriptRasterizer CreateRasterizer(FileStream file)
        {
            var rasterizer = new GhostscriptRasterizer();
            rasterizer.CustomSwitches.Add("-dNOINTERPOLATE");
            rasterizer.CustomSwitches.Add("-dCOLORSCREEN=0");
            rasterizer.CustomSwitches.Add("-sPAPERSIZE=a4");
            rasterizer.TextAlphaBits = 4;
            rasterizer.GraphicsAlphaBits = 4;

            rasterizer.Open(file, _lastInstalledVersion, true);

            return rasterizer;
        }

        static void PdfProcessing(int StartPage, int EndPage, FileStream SourcefilePath)
        {
            var rasterizer = CreateRasterizer(SourcefilePath);

            for (var pageNumber = StartPage; pageNumber <= EndPage; pageNumber++)
            {
                using (var img = rasterizer.GetPage(Dpi, Dpi, pageNumber))
                {
                    var mem = new MemoryStream();
                    img.Save(mem, ImageFormat.Jpeg);

                    lock (PageStore)
                    {
                        PageStore[pageNumber] = mem;
                    }
                }
            }
        }

        /// <summary>  
        /// the image remains the same size, and it is placed in the middle of the new canvas  
        /// </summary>  
        /// <param name="image">image to put on canvas</param>  
        /// <param name="width">canvas width</param>  
        /// <param name="height">canvas height</param>  
        /// <param name="canvasColor">canvas color</param>  
        /// <returns></returns>  
        public static Image PutOnCanvas(Image image, int width, int height, Color canvasColor)
        {
            var res = new Bitmap(width, height);
            using (var g = Graphics.FromImage(res))
            {
                g.Clear(canvasColor);
                g.DrawImageUnscaled(image, 0, 0, image.Width, image.Height);
                //var x = (width - image.Width) / 2;
                //var y = (height - image.Height) / 2;
                //g.DrawImageUnscaled(image, x, y, image.Width, image.Height);
            }

            return res;
        }  
        
        /// <summary>  
           /// resize an image and maintain aspect ratio  
           /// </summary>  
           /// <param name="image">image to resize</param>  
           /// <param name="newWidth">desired width</param>  
           /// <param name="maxHeight">max height</param>  
           /// <param name="onlyResizeIfWider">if image width is smaller than newWidth use image width</param>  
           /// <returns>resized image</returns>  
        public static Image ImageResize(Image image, int newWidth, int maxHeight, bool onlyResizeIfWider, bool resizeProportional = true)
        {
            if (onlyResizeIfWider && image.Width <= newWidth) newWidth = image.Width;

            var newHeight = resizeProportional ? image.Height * newWidth / image.Width : maxHeight;
            if (newHeight > maxHeight)
            {
                // Resize with height instead  
                newWidth = image.Width * maxHeight / image.Height;
                newHeight = maxHeight;
            }
            var res = new Bitmap(newHeight, newWidth);

            using (var graphic = Graphics.FromImage(res))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;
                //graphic.TranslateTransform(newWidth, 0F);
                graphic.TranslateTransform(newHeight/2,newWidth/2);
                graphic.RotateTransform(90.0F);
                graphic.DrawImage(image, -newWidth / 2, -newHeight/2, newWidth, newHeight);
            }

            return res;
        }

        public static MemoryStream PdfProcessingToPng(string sourceFilePath)
        {
            return PdfProcessingToPng(sourceFilePath, false);
        }

        public static MemoryStream PdfProcessingToPng (string sourceFilePath, bool scaling = false, bool rotate = false)
        {
            FileStream pdfPathStream = null;
            MemoryStream mem = null;
            GhostscriptRasterizer rasterizer = null;
            try
            {
                pdfPathStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
                rasterizer = CreateRasterizer(pdfPathStream);
                mem = new MemoryStream();

                for (int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    //string pageFilePath = Path.Combine(outputPath, "Page-" + pageNumber.ToString() + ".png");
                    using (var img = rasterizer.GetPage(Dpi, Dpi, pageNumber))
                    {
                        //var _imgRect = ImageResize(img, (int)(img.Width * (scaling ? 0.9 : 1)), img.Height, true, false);
                        var imgWidth = (scaling ? 1100 : img.Width);
                        var imgHeight = (scaling ? 700 : img.Height);
                        var _imgRect = ImageResize(img, imgWidth, imgHeight, false, false);
                        var _imgSave = rotate ? PutOnCanvas(_imgRect, imgHeight, imgWidth, Color.White) : PutOnCanvas(_imgRect, imgWidth, imgHeight, Color.White);
                        _imgSave.Save(mem, ImageFormat.Png);
                    }
                }
                rasterizer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                if (pdfPathStream != null)
                    pdfPathStream.Close();
                if (rasterizer != null)
                    rasterizer.Close();
            }
            return mem;

        }

        public static string PdfProcessingToPngFile(string SourcefilePath)
        {
            FileStream pdfPathStream = null;
            GhostscriptRasterizer rasterizer = null;
            string outputPath = SourcefilePath.Replace(".pdf", ".png");
            try
            {
                pdfPathStream = new FileStream(SourcefilePath, FileMode.Open);
                rasterizer = CreateRasterizer(pdfPathStream);

                for (int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    string pageFilePath = outputPath.Replace(".pdf", pageNumber.ToString() + ".png");
                    using (var img = rasterizer.GetPage(Dpi, Dpi, pageNumber))
                    {
                        img.Save(pageFilePath, ImageFormat.Png);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                if (pdfPathStream != null)
                    pdfPathStream.Close();
                if (rasterizer != null)
                    rasterizer.Close();
            }
            return outputPath.Replace(".pdf","1.png");

        }

        static void PrintPages(Dictionary<int, MemoryStream> pageStore, string printerName = null)
        {
            using (var pd = new PrintDocument())
            {
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

                pd.PrinterSettings.Duplex = Duplex.Simplex;
                pd.PrintController = new StandardPrintController();

                var index = 0;
                pd.PrintPage += (o, e) => {
                    var pageStream = pageStore[index + 1];
                    var img = System.Drawing.Image.FromStream(pageStream);

                    e.Graphics.DrawImage(img, e.Graphics.VisibleClipBounds);

                    index++;
                    e.HasMorePages = index < pageStore.Count;

                    //Console.WriteLine("Print {0} of {1}; complete {2:000.0}%", index, pageStore.Count, ((double)index) / pageStore.Count * 100);

                };
                pd.Print();
            }
        }


 
    }
}
