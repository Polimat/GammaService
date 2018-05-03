using System;
using System.IO;
using System.Linq;
using GammaService.Common;
using GammaService.Interfaces;
using System.Drawing;

namespace GammaService.Services
{
    public class PrinterService : IPrinterService
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


        public bool? ActivateProductionTask(Guid productionTaskId, int placeId, int remotePrinterLabelId)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    /*if (gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
					        .UseApplicator ?? false)
				    {
                        var outPathLabels = gammaBase.LocalSettings
                            .FirstOrDefault()
                            .LabelPath;
                        var outPath = gammaBase.C1CCharacteristics
						    .First(c => c.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
						    .PackageLabelPath;
					    //var inPath = gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
						//    .ApplicatorLabelPath;
                        ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
					    if (string.IsNullOrEmpty(outPathLabels + outPath) || device == null)// || string.IsNullOrEmpty(inPath))
					    {
						    return false;
					    }
                        device.PackageLabelPath = outPathLabels + outPath;
                        return true;
                    }*/
                    /*if ((remotePrinterLabelId == 2 || remotePrinterLabelId == 3) && (gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                            .UseApplicator ?? false))
                    {
                        var groupPackageLabelPNG =
                            gammaBase.ProductionTaskConverting
                                .FirstOrDefault(c => c.ProductionTasks.ProductionTaskID == productionTaskId)
                                .GroupPackLabelPNG;
                        ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                        if (groupPackageLabelPNG == null || device == null)
                        {
                            return false;
                        }

                        return device.LoadPackageLabelPNG(placeId, remotePrinterLabelId);
                    }
                    else
				    {
					    return null;
				    }
                    */
                    ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                    if (device == null)
                    {
                        return false;
                    }
                    return device.LoadPackageLabelPNG(placeId, remotePrinterLabelId);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " :Ошибка при активации задания " + productionTaskId.ToString());
                Console.WriteLine(e);
                return false;
            }
        }

        public bool? ChangePrinterStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                if (device != null)
                {
                    return device.ChangePrinterStatus();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return null;
        }

        public bool? GetPrinterStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                if (device != null)
                {
                    return device.IsEnabledService;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return null;
        }

        public bool PrintLabel(int placeId, int remotePrinterLabelId, Guid? productId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                if (device != null)
                {
                    device.InStatus = false;
                    device.InStatus = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return false;
        }

        public bool? ChangePrintPortStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.RemotePrinterLabelId == remotePrinterLabelId);
                if (device != null)
                {
                    return device.ChangePrintPortStatus();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return null;
        }

        private Image ByteArrayToImage(byte[] inputArray)
        {
            var memoryStream = new System.IO.MemoryStream(inputArray);
            return Image.FromStream(memoryStream);
        }

        private string GetGroupPackageLabelMD5(string filename)
        {
            if (filename != null && !Directory.Exists(Path.GetDirectoryName(filename))) return "Техническая ошибка при чтении этикетки: каталог с этикетками недоступен. Обратитесь к техподдержке Гаммы.";
            if (filename != null && !File.Exists(filename)) return "Техническая ошибка при чтении этикетки: этикетка " + Path.GetFileName(filename) + " недоступна. Обратитесь в службу качества к Гилеву Д.С.";
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = System.IO.File.OpenRead(filename))
                    {
                        var checkSum = md5.ComputeHash(stream);
                        return BitConverter.ToString(checkSum).Replace("-", String.Empty);
                    }
                }
            }
            catch
            {
                return filename == null ? null : "Техническая ошибка при чтении этикетки: этикетка " + Path.GetFileName(filename) + " недоступна. Обратитесь к техподдержке Гаммы.";
            }
        }

        /// <summary>
        /// Обновление (при необходимости) изображения групповой этикетки в задании
        /// </summary>
        /// <param name="productionTaskId">ID задания</param>
        /// <returns></returns>
        public bool UpdateGroupPackageLabelInProductionTask(Guid productionTaskId)
        {
            var result =  UpdateGroupPackLabelInProductionTask(productionTaskId);
            return result.Item1;
        }
         /// <summary>
         /// Обновление (при необходимости) изображения групповой этикетки в задании
         /// </summary>
         /// <param name="productionTaskId">ID задания</param>
         /// <returns></returns>
        public Tuple<bool,string> UpdateGroupPackLabelInProductionTask(Guid productionTaskId)
        {
            string Place = String.Empty;
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    var LabelPath = 
                        gammaBase.LocalSettings
                            .Select(p => p.LabelPath)
                            .FirstOrDefault();
                    var GroupPackageLabelMD5 =
                        gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId)
                            .Select(p => p.GroupPackLabelMD5)
                            .FirstOrDefault();
                    var GroupPackLabelPath =
                        gammaBase.C1CCharacteristics.Where(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                            .Select(p => p.PackageLabelPath)
                            .FirstOrDefault();
                    Place =
                        gammaBase.Places.Where(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                            .Select(p => p.Name)
                            .FirstOrDefault();
                    var GroupPackageLabelMD5New = GetGroupPackageLabelMD5(LabelPath + GroupPackLabelPath);
                    if (GroupPackageLabelMD5New != null && (GroupPackageLabelMD5New.Length < 18 || (GroupPackageLabelMD5New.Length >= 18 && GroupPackageLabelMD5New.Substring(0,20) != "Техническая проблема")))
                    {
                        if (GroupPackageLabelMD5 != GroupPackageLabelMD5New)
                        {
                            var GroupPackageLabelPNG = PdfPrint.PdfProcessingToPng(LabelPath + GroupPackLabelPath);
                            GroupPackageLabelMD5 = GroupPackageLabelMD5New;

                            var productionTaskConverting =
                            gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId).FirstOrDefault();
                            if (productionTaskConverting == null)
                            {
                                productionTaskConverting = new ProductionTaskConverting()
                                {
                                    ProductionTaskID = productionTaskId,
                                    GroupPackLabelPNG = GroupPackageLabelPNG.ToArray(),
                                    GroupPackLabelMD5 = GroupPackageLabelMD5
                                };
                                gammaBase.ProductionTaskConverting.Add(productionTaskConverting);
                            }
                            else
                            {
                                productionTaskConverting.GroupPackLabelPNG = GroupPackageLabelPNG.ToArray();
                                productionTaskConverting.GroupPackLabelMD5 = GroupPackageLabelMD5New;
                            }
                            gammaBase.SaveChanges();
                            Console.WriteLine(DateTime.Now + " :Обновлена групповая этикетка на переделе " + Place + " в задании " + productionTaskId.ToString());
                        }
                        /*else
                        {
                            GroupPackageLabelPNG = ByteArrayToImage(
                                gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId)
                                    .Select(p => p.GroupPackLabelPNG)
                                    .FirstOrDefault());
                        }*/
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now + " :Ошибка: недоступен файл групповой этикетки "+ (LabelPath + GroupPackLabelPath).ToString() + " на переделе " + Place + " в задании " + productionTaskId.ToString());
                        return new Tuple<bool, string>(false, GroupPackageLabelMD5New ?? "Техническая ошибка: недоступен файл групповой этикетки " + (LabelPath + GroupPackLabelPath).ToString()) ;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " :Ошибка обновления групповой этикетки в задании " + productionTaskId.ToString() + " на переделе "+ Place);
                Console.WriteLine(e);
                return new Tuple<bool, string>(false, "Техническая ошибка обновления групповой этикетки в задании");
            }
            return new Tuple<bool, string>(true, "");
        }
    }
}
