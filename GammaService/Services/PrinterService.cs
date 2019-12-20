using System;
using System.IO;
using System.Linq;
using GammaService.Common;
using GammaService.Interfaces;
using System.Drawing;
using System.ServiceModel;
using System.Net;
using System.Net.Mail;

namespace GammaService.Services
{
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
    //             ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class PrinterService : IPrinterService
    {
        public bool PrintPallet(Guid productId)
        {
            try
            {
                ReportManager.PrintReport("Амбалаж", "ASUTP", "Pallet", productId, 1);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        public bool? ActivateProductionTask(Guid productionTaskId, int placeId, int remotePrinterLabelId)
        {
            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Активация задания " + productionTaskId.ToString());
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
                    //ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && ((p.RemotePrinterLabelId == remotePrinterLabelId) || (p.RemotePrinterLabelId == (remotePrinterLabelId == 2 ? 3 : remotePrinterLabelId == 3 ? 2 : remotePrinterLabelId))));
                    bool ret = true;
                    var devices = Program.modbuseDevices.Where(p => p.PlaceId == placeId && (p.RemotePrinterLabelId == 2 || p.RemotePrinterLabelId == 4));
                    foreach (var device in devices)
                    {
                        ret = ret & ((bool)device.LoadPackageLabelPNG(placeId, device.RemotePrinterLabelId) && (device.LoadPackageLabelPath(placeId, device.RemotePrinterLabelId) ?? true));
                    }
                    if (devices.Count() == 0)
                    {  
                        return null;
                    }
                    return ret; //((bool)device.LoadPackageLabelPNG(placeId, device.RemotePrinterLabelId) && (device.LoadPackageLabelPath(placeId, device.RemotePrinterLabelId) ?? true));
                }

            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка при активации задания " + productionTaskId.ToString());
                Common.Console.WriteLine("NoModbusName", e);
                return false;
            }
        }

        public bool? ChangePrinterStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && ((p.RemotePrinterLabelId == remotePrinterLabelId) || (p.RemotePrinterLabelId == (remotePrinterLabelId == 2 ? 3 : remotePrinterLabelId == 3 ? 2 : remotePrinterLabelId))));
                if (device != null)
                {
                    return device.ChangePrinterStatus();
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", e);
                return null;
            }
            return null;
        }

        public bool? GetPrinterStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && ((p.RemotePrinterLabelId == remotePrinterLabelId) || (p.RemotePrinterLabelId == (remotePrinterLabelId == 2 ? 3 : remotePrinterLabelId == 3 ? 2 : remotePrinterLabelId))));
                if (device != null)
                {
                    return device.IsEnabledService;
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", e);
                return null;
            }
            return null;
        }

        public bool PrintLabel(int placeId, int remotePrinterLabelId, Guid? productId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && p.IsDefaultPrinterForGamma && ((p.RemotePrinterLabelId == remotePrinterLabelId) || (p.RemotePrinterLabelId == (remotePrinterLabelId == 2 ? 3 : remotePrinterLabelId == 3 ? 2 : remotePrinterLabelId))));
                if (device != null)
                {
                    device.InStatus = false;
                    device.InStatus = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", e);
                return false;
            }
            return false;
        }

        public bool? ChangePrintPortStatus(int placeId, int remotePrinterLabelId)
        {
            try
            {
                ModbusDevice device = Program.modbuseDevices.FirstOrDefault(p => p.PlaceId == placeId && ((p.RemotePrinterLabelId == remotePrinterLabelId) || (p.RemotePrinterLabelId == (remotePrinterLabelId == 2 ? 3 : remotePrinterLabelId == 3 ? 2 : remotePrinterLabelId))));
                if (device != null)
                {
                    return device.ChangePrintPortStatus();
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", e);
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
            if (filename != null && !File.Exists(filename)) return "Техническая ошибка при чтении этикетки: этикетка " + Path.GetFileName(filename) + " недоступна. Обратитесь в службу качества.";
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
            var resultGr =  UpdateGroupPackLabelInProductionTask(productionTaskId);
            //var resultTr = UpdateTransportPackLabelInProductionTask(productionTaskId);
            return (resultGr.Item1);// && resultTr.Item1);
        }
         /// <summary>
         /// Обновление (при необходимости) изображения групповой этикетки в задании
         /// </summary>
         /// <param name="productionTaskId">ID задания</param>
         /// <returns></returns>
        public Tuple<bool,string> UpdateGroupPackLabelInProductionTask(Guid productionTaskId)
        {
            Places Place;
            string errGr = "";
            try
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Начало обновления групповой этикетки на переделе в задании " + productionTaskId.ToString());
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
                            .FirstOrDefault();
                    var GroupPackageLabelZPL =
                        gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId)
                            .Select(p => p.GroupPackLabelZPL)
                            .FirstOrDefault();
                    var remotePrinter = Place == null ? null : gammaBase.RemotePrinters.Where(p => p.PlaceRemotePrinters.Any(r => r.PlaceID == Place.PlaceID && (r.IsEnabled ?? false)) && (p.RemotePrinterLabelID == 2 || p.RemotePrinterLabelID == 3)).FirstOrDefault();
                    var GroupPackageLabelMD5New = GetGroupPackageLabelMD5(LabelPath + GroupPackLabelPath);
                    Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Загружены данные для обновления групповой этикетки " + GroupPackLabelPath + " на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
                    if (GroupPackageLabelMD5New != null && (GroupPackageLabelMD5New.Length < 18 || (GroupPackageLabelMD5New.Length >= 18 && GroupPackageLabelMD5New.Substring(0,18) != "Техническая ошибка")))
                    {
                        if ((GroupPackageLabelMD5 != GroupPackageLabelMD5New) | (GroupPackageLabelZPL == String.Empty | GroupPackageLabelZPL == null))
                        {
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Начала PdfProcessingToPng групповая этикетка " + GroupPackLabelPath + " на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
                            var GroupPackageLabelPNG = PdfPrint.PdfProcessingToPng(LabelPath + GroupPackLabelPath, remotePrinter?.Rotating ?? false, remotePrinter?.Scaling ?? false, remotePrinter?.LabelWidth, remotePrinter?.LabelHeight);
                            GroupPackageLabelMD5 = GroupPackageLabelMD5New;
                            GroupPackageLabelZPL = string.Empty; ;
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Начала GrouptPackageLabelZPL(" + GroupPackageLabelPNG.Length.ToString() + ") групповая этикетка на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
                            GroupPackageLabelZPL = BitConverter.ToString(GroupPackageLabelPNG.ToArray()).Replace("-", "");
                            /*GroupPackageLabelPNG.Seek(0, System.IO.SeekOrigin.Begin);
                            int count = 0;
                            while (count < GroupPackageLabelPNG.Length)
                            {
                                var b = GroupPackageLabelPNG.ReadByte();
                                string hexRep = String.Format("{0:X}", Convert.ToByte(b));
                                if (hexRep.Length == 1)
                                    hexRep = "0" + hexRep;
                                GroupPackageLabelZPL += hexRep;
                                count++;
                            }
                            */
                            
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Начала gammaBase групповая этикетка " + GroupPackLabelPath + " на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
                            var productionTaskConverting =
                            gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId).FirstOrDefault();
                            if (productionTaskConverting == null)
                            {
                                productionTaskConverting = new ProductionTaskConverting()
                                {
                                    ProductionTaskID = productionTaskId,
                                    GroupPackLabelPNG = GroupPackageLabelPNG.ToArray(),
                                    GroupPackLabelMD5 = GroupPackageLabelMD5,
                                    GroupPackLabelZPL = GroupPackageLabelZPL
                                };
                                gammaBase.ProductionTaskConverting.Add(productionTaskConverting);
                            }
                            else
                            {
                                productionTaskConverting.GroupPackLabelPNG = GroupPackageLabelPNG.ToArray();
                                productionTaskConverting.GroupPackLabelMD5 = GroupPackageLabelMD5New;
                                productionTaskConverting.GroupPackLabelZPL = GroupPackageLabelZPL;
                            }
                            gammaBase.SaveChanges();
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Обновлена групповая этикетка " + GroupPackLabelPath + " на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
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
                        Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка: недоступен файл групповой этикетки "+ (LabelPath + GroupPackLabelPath).ToString() + " на переделе " + Place?.Name + " в задании " + productionTaskId.ToString());
                        errGr = GroupPackageLabelMD5New ?? "Техническая ошибка: недоступен файл групповой этикетки " + (LabelPath + GroupPackLabelPath).ToString();
                        //return new Tuple<bool, string>(false, GroupPackageLabelMD5New ?? "Техническая ошибка: недоступен файл групповой этикетки " + (LabelPath + GroupPackLabelPath).ToString()) ;
                    }
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка обновления групповой этикетки в задании " + productionTaskId.ToString() + " на переделе");
                Common.Console.WriteLine("NoModbusName", e);
                errGr = "Техническая ошибка обновления групповой этикетки в задании";
                //return new Tuple<bool, string>(false, "Техническая ошибка обновления групповой этикетки в задании");
            }
            var resultTr = UpdateTransportPackLabelInProductionTask(productionTaskId);
            Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Окончание обновления групповой этикетки на переделе в задании " + productionTaskId.ToString());
            //return !(resultTr.Item1) ? resultTr : new Tuple<bool, string>(true, "");
            return errGr != "" && !(resultTr.Item1) ? new Tuple<bool, string>(false, errGr + ";" + resultTr?.Item2.ToString()) : new Tuple<bool, string>(true, "");
        }

        /// <summary>
        /// Обновление (при необходимости) изображения транспортной этикетки в задании
        /// </summary>
        /// <param name="productionTaskId">ID задания</param>
        /// <returns></returns>
        public Tuple<bool, string> UpdateTransportPackLabelInProductionTask(Guid productionTaskId)
        {
            string Place = String.Empty;
            try
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Начало обновления транспортной этикетки на переделе " + Place + " в задании " + productionTaskId.ToString());
                using (var gammaBase = new GammaEntities())
                {
                    var LabelPath =
                        gammaBase.LocalSettings
                            .Select(p => p.LabelPath)
                            .FirstOrDefault();
                    var TransportPackageLabelMD5 =
                        gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId)
                            .Select(p => p.TransportPackLabelMD5)
                            .FirstOrDefault();
                    var TransportPackLabelPath = 
                        gammaBase.C1CCharacteristics.Where(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                            .Select(p => p.PackageLabelPath)
                            .FirstOrDefault();
                    Place =
                        gammaBase.Places.Where(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                            .Select(p => p.Name)
                            .FirstOrDefault();
                    var TransportPackageLabelZPL =
                        gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId)
                            .Select(p => p.TransportPackLabelZPL)
                            .FirstOrDefault();
                    TransportPackLabelPath = TransportPackLabelPath?.Replace("lab_gr", "lab_tr");
                    var TransportPackageLabelMD5New = GetGroupPackageLabelMD5(LabelPath + TransportPackLabelPath);
                    Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Загружены данные для обновления транспортной этикетки " + TransportPackLabelPath + " на переделе " + Place + " в задании " + productionTaskId.ToString());
                    Common.Console.WriteLine("NoModbusName", DateTime.Now + " :TransportPackageLabelMD5: " + TransportPackageLabelMD5?.ToString());
                    Common.Console.WriteLine("NoModbusName", DateTime.Now + " :TransportPackageLabelMD5New: " + TransportPackageLabelMD5New?.ToString());
                    Common.Console.WriteLine("NoModbusName", DateTime.Now + " :TransportPackageLabelMD5 != TransportPackageLabelMD5New: " + (TransportPackageLabelMD5 != TransportPackageLabelMD5New).ToString() + " TransportPackageLabelZPL.Length:"+ TransportPackageLabelZPL?.Length.ToString());
                    if (TransportPackageLabelMD5New != null && (TransportPackageLabelMD5New.Length < 18 || (TransportPackageLabelMD5New.Length >= 18 && TransportPackageLabelMD5New.Substring(0, 18) != "Техническая ошибка")))
                    {
                        if ((TransportPackageLabelMD5 != TransportPackageLabelMD5New) )//Транспортную этикетку в ZPL пока не сохраняем - не используем пока нигде, есть проблема по скорости с закоментированным блоком.  | (TransportPackageLabelZPL == String.Empty | TransportPackageLabelZPL == null))
                        {
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Начала PdfProcessingToPng транспортная этикетка " + TransportPackLabelPath + " на переделе " + Place + " в задании " + productionTaskId.ToString());
                            var TransportPackageLabelPNG = PdfPrint.PdfProcessingToPng(LabelPath + TransportPackLabelPath, false);
                            TransportPackageLabelMD5 = TransportPackageLabelMD5New;
                            TransportPackageLabelZPL = string.Empty;
                            /* Проблема со скоростью перевода в hex. Ярлык kleenex картинкой больше 1МБ.
                            Common.Console.WriteLine(DateTime.Now + " :Начала TransportPackageLabelZPL("+ TransportPackageLabelPNG.Length.ToString() + ") транспортная этикетка на переделе " + Place + " в задании " + productionTaskId.ToString());
                            TransportPackageLabelPNG.Seek(0, System.IO.SeekOrigin.Begin);
                            int count = 0;
                            while (count < TransportPackageLabelPNG.Length)
                            {
                                var b = TransportPackageLabelPNG.ReadByte();
                                string hexRep = String.Format("{0:X}", Convert.ToByte(b));
                                if (hexRep.Length == 1)
                                    hexRep = "0" + hexRep;
                                TransportPackageLabelZPL += hexRep;
                                count++;
                            }
                            */
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Начала gammaBase транспортная этикетка " + TransportPackLabelPath + " на переделе " + Place + " в задании " + productionTaskId.ToString());
                            var productionTaskConverting =
                            gammaBase.ProductionTaskConverting.Where(p => p.ProductionTaskID == productionTaskId).FirstOrDefault();
                            if (productionTaskConverting == null)
                            {
                                productionTaskConverting = new ProductionTaskConverting()
                                {
                                    ProductionTaskID = productionTaskId,
                                    TransportPackLabelPNG = TransportPackageLabelPNG.ToArray(),
                                    TransportPackLabelMD5 = TransportPackageLabelMD5,
                                    TransportPackLabelZPL = TransportPackageLabelZPL
                                };
                                gammaBase.ProductionTaskConverting.Add(productionTaskConverting);
                            }
                            else
                            {
                                productionTaskConverting.TransportPackLabelPNG = TransportPackageLabelPNG.ToArray();
                                productionTaskConverting.TransportPackLabelMD5 = TransportPackageLabelMD5New;
                                productionTaskConverting.TransportPackLabelZPL = TransportPackageLabelZPL;
                            }
                            gammaBase.SaveChanges();
                            Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Обновлена транспортная этикетка " + TransportPackLabelPath + " на переделе " + Place + " в задании " + productionTaskId.ToString());
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
                        Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка: недоступен файл транспортной этикетки " + (LabelPath + TransportPackLabelPath).ToString() + " на переделе " + Place + " в задании " + productionTaskId.ToString());
                        return new Tuple<bool, string>(false, TransportPackageLabelMD5New ?? "Техническая ошибка: недоступен файл транспортной этикетки " + (LabelPath + TransportPackLabelPath).ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка обновления транспортной этикетки в задании " + productionTaskId.ToString() + " на переделе " + Place);
                Common.Console.WriteLine("NoModbusName", e);
                return new Tuple<bool, string>(false, "Техническая ошибка обновления транспортной этикетки в задании");
            }
            Common.Console.WriteLine("NoModbusName", DateTime.Now + " : Окончание обновления транспортной этикетки на переделе " + Place + " в задании " + productionTaskId.ToString());
            return new Tuple<bool, string>(true, "");
        }

        public bool SendMessageNewEvent (Guid eventID)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    if (gammaBase.Departments.First(p => p.LogEvents.Any(pt => pt.EventID == eventID))
					        .Email != String.Empty)
				    {
                        var mailTo = gammaBase.Departments
                            .FirstOrDefault(p => p.LogEvents.Any(pt => pt.EventID == eventID))
                            .Email;
                        var logEvent = gammaBase.LogEvents
						    .FirstOrDefault(c => c.EventID == eventID);
                        var parentEvent = gammaBase.LogEvents
                            .FirstOrDefault(c => c.EventID == logEvent.ParentEventID);
                        if (mailTo == null)
					    {
						    return false;
					    }
                        string subject = "Новое задание " +logEvent.Number + " от " + logEvent.Date;
                        string message = subject;
                        if (parentEvent != null)
                        {
                            message = message + Environment.NewLine + Environment.NewLine +
                                "Автор: " + parentEvent.Users.Name + "("+parentEvent.PrintName +")" + Environment.NewLine +
                                "Вид: " + parentEvent.EventKinds.Name + Environment.NewLine +
                                "Обьект: " + parentEvent.Places.Name + Environment.NewLine +
                                "Узел: " + parentEvent.Devices.Name + Environment.NewLine +
                                "Описание: " + parentEvent.Description + Environment.NewLine;
                        }
                        message = message + Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine + "Требуется зайти в Гамму и открыть задание в Журнале заявок.";
                        return SendMail(@"192.168.0.220", @"service_ASU_mailer@sgbi.ru", @"89e2#bfKw2", mailTo, "Рассылка: " + subject, message, null);
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception e)
            {
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка при отправке сообщения по событию " + eventID.ToString());
                Common.Console.WriteLine("NoModbusName", e);
                return false;
            }
        }

        /// <summary>
        /// Отправка письма на почтовый ящик C# mail send
        /// </summary>
        /// <param name="smtpServer">Имя SMTP-сервера</param>
        /// <param name="from">Адрес отправителя</param>
        /// <param name="password">пароль к почтовому ящику отправителя</param>
        /// <param name="mailto">Адрес получателя</param>
        /// <param name="caption">Тема письма</param>
        /// <param name="message">Сообщение</param>
        /// <param name="attachFile">Присоединенный файл</param>
        private static bool SendMail(string smtpServer, string from, string password,
        string mailto, string caption, string message, string attachFile = null)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(from);
                mail.To.Add(new MailAddress(mailto));
                mail.Subject = caption;
                mail.Body = message;
                if (!string.IsNullOrEmpty(attachFile))
                    mail.Attachments.Add(new Attachment(attachFile));
                SmtpClient client = new SmtpClient();
                client.Host = smtpServer;
                //client.Port = 465;// 587;
                client.EnableSsl = false;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(from, password);//(from.Split('@')[0], password);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(mail);
                mail.Dispose();
                return true;
            }
            catch (Exception e)
            {
                //throw new Exception("Mail.Send: " + e.Message);
                Common.Console.WriteLine("NoModbusName", DateTime.Now + " :Ошибка при отправке сообщения: " + mailto + ": " + message);
                Common.Console.WriteLine("NoModbusName", e);
                return false;
            }
        }
    }
}
