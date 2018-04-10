using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Advantech.Adam;
using GammaService.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GammaService
{
    public class ModbusDevice
    {
        public ModbusDevice(DeviceType deviceType, string ipAddress, int placeId, string printerName, int remotePrinterLabelId, Dictionary<string,string> placeRemotePrinterSettings, int timerTickTime, string remotePrinterIpAdress, int? remotePrinterPort)
        {
            IpAddress = ipAddress;
            DeviceType = deviceType;
            PrinterName = printerName;
            PlaceId = placeId;
            RemotePrinterLabelId = remotePrinterLabelId;
            RemotePrinterIpAdress = remotePrinterIpAdress;
            RemotePrinterPort = remotePrinterPort;
            //SignalChannelNumber = signalChannelNumber;
            //ConfirmChannelNumber = confirmChannelNumber;
            PlaceRemotePrinterSettings = placeRemotePrinterSettings;
            string value;
            PlaceRemotePrinterSettings.TryGetValue("SignalChannelNumber", out value);
            if (value != null)
                SignalChannelNumber = Convert.ToInt32(value);
            else
            {
                SignalChannelNumber = 1;
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Внимание!!!! Параметр SignalChannelNumber не указан! (установлен по умолчанию в 1)");
            }
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("ErrorChannelNumber", out value);
            if (value != null)
                ErrorChannelNumber = Convert.ToInt32(value);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("ResetErrorChannelNumber", out value);
            if (value != null)
                ResetErrorChannelNumber = Convert.ToInt32(value);
            InitializeDevice(deviceType, ipAddress);
            //PackageLabelPath = LoadPackageLabelPath(PlaceId, RemotePrinterLabelId);
            LoadPackageLabelPNG(PlaceId, RemotePrinterLabelId);
            MainTimer = new Timer(ReadCoil, null, 0, timerTickTime);
        }

        public string IpAddress { get; set; }
        private DeviceType DeviceType { get; set; }

        public bool IsConnected { get; private set; }

        public string PrinterName { get; set; }

        public int PlaceId { get; set; }

        public int RemotePrinterLabelId { get; set; }

        private string RemotePrinterIpAdress;

        private int? RemotePrinterPort;

        private string _packageLabelPath;

        public string PackageLabelPath
        {
            get { return _packageLabelPath; }
            set
            {
                _packageLabelPath = value;
                //if (value != @"" && SaveLabelToPrinterZPL(value))
                //    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Этикетка " + _packageLabelPath + " активирована");
            }
        }

        private byte[] _packageLabelPNG;

        public byte[] PackageLabelPNG
        {
            get { return _packageLabelPNG; }
            set
            {
                _packageLabelPNG = value;
                if (value != null && SaveLabelToPrinterZPL(value))
                    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Этикетка активирована");
            }
        }

        private bool _isEnabledService = true;

        public bool IsEnabledService
        {
            get { return _isEnabledService; }
            set
            {
                if (value && !_isEnabledService)
                {
                    SendCommandZPL("~JA");
                    LoadPackageLabelPNG(PlaceId, RemotePrinterLabelId);
                }
                _isEnabledService = value;
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Сервис " + (_isEnabledService ? "активирован" : " остановлен"));
            }
        }

        private AdamSocket AdamModbus { get; set; }

        private int m_iDiTotal;
        private int m_iDoTotal;
        /// <summary>
        /// Номер входа с сигналом "печатай!"
        /// </summary>
        private int SignalChannelNumber { get; set; }
        
        /// <summary>
        /// Номер выхода с сигналом "Ошибка"
        /// </summary>
        private int? ErrorChannelNumber { get; set; }

        /// <summary>
        /// Номер входа с сигналом "Сбросить ошибку"
        /// </summary>
        private int? ResetErrorChannelNumber { get; set; }

        Dictionary<string, string> PlaceRemotePrinterSettings { get; set; }

        private Timer MainTimer { get; }
        private Timer RestoreConnectTimer { get; set; }

        /// <summary>
        /// Количество итераций подряд при которых AdamModbus.Modbus().ReadCoilStatus возвращает false
        /// </summary>
        private int countReadCoilStatusFalse = 0;

        private void RestoreConnect(object obj)
        {
            if (!AdamModbus.Connected)
            {
                ReinitializeADAM();
                if (!AdamModbus.Connected) return;
                IsConnected = true;
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Связь восстановлена");
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
            else
            {
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
        }

        private bool IsPrintPortStatus { get; set; }

        public bool ChangePrintPortStatus()
        {
            return IsPrintPortStatus = !IsPrintPortStatus;
        }

        public bool ChangePrinterStatus()
        {
            return IsEnabledService = !IsEnabledService;
        }

        private void ReadCoil(object obj)
        {
            if (IsEnabledService)
            {
                if (!AdamModbus.Connected || countReadCoilStatusFalse > 10)
                {
                    if (IsConnected && countReadCoilStatusFalse > 10)
                    {
                        AdamModbus.Disconnect();
                        Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Принудительная переинициализация (завис) " + IpAddress);
                    }
                    countReadCoilStatusFalse = 0;
                    IsConnected = false;
                    if (RestoreConnectTimer != null) return;
                    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Пропала связь");
                    RestoreConnectTimer = new Timer(RestoreConnect, null, 0, 1000);
                    return;
                }
                int iDiStart = 1, iDoStart = 17;
                int iChTotal;
                bool[] bDiData, bDoData, bData;
                //            if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
                //                !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData)) return;
                if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Опрос адама " + IpAddress + " Статус: " + AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData).ToString() + "/" + AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData).ToString());
                if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
                !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData))
                {
                    countReadCoilStatusFalse++;
                    return;
                }
                countReadCoilStatusFalse = 0;
                if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :" + PrinterName + "                        :Опрос адама " + IpAddress + " после проверки статуса");
                OnDIDataReceived?.Invoke(bDiData);
                /*
                iChTotal = m_iDiTotal + m_iDoTotal;
                bData = new bool[iChTotal];
                if (bDiData == null || bDoData == null) return;
                Array.Copy(bDiData, 0, bData, 0, m_iDiTotal);
                Array.Copy(bDoData, 0, bData, m_iDiTotal, m_iDoTotal);
                InStatus = bData[SignalChannelNumber-1];
                if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " Состояние на входе: " + InStatus.ToString());
                */
            }
        }

        private void InitializeDevice(DeviceType deviceType, string ipAddress)
        {
            //AdamModbus?.Disconnect();
            AdamModbus = new AdamSocket();
            AdamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP
            if (AdamModbus.Connect(ipAddress, ProtocolType.Tcp, 502))
            {
                if (RestoreConnectTimer == null)
                    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Инициализация прошла успешно.");
                IsConnected = true;
            }
            else
            {
                if (RestoreConnectTimer == null)
                    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Не удалось инициализировать.");
                IsConnected = false;
            }
            switch (deviceType)
            {
                case DeviceType.ADAM6060:
                    m_iDiTotal = 6;
                    m_iDoTotal = 6;
                    break;
            }
        }

        public void ReinitializeADAM()
        {
            InitializeDevice(DeviceType, IpAddress);
        }

        public void DestroyDevice()
        {
            //AdamModbus?.Disconnect();
            if (IsConnected)
                AdamModbus.Disconnect();
            if (RestoreConnectTimer != null)
            {
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
            if (MainTimer != null)
            {
                MainTimer?.Dispose();
            }
        }

        private ConcurrentQueue<Guid> FaultIds { get; } = new ConcurrentQueue<Guid>();

        #region Events

        public event Action<bool[]> OnDIDataReceived;

        #endregion

        private bool _isErrorPrinting = false;

        public bool IsErrorPrinting
        {
            get { return _isErrorPrinting; }
            set
            {
                if (_isErrorPrinting == value) return;
                _isErrorPrinting = value;
                if (ErrorChannelNumber != null)
                {
                    try
                    {
                        var outSignals = new Dictionary<int, bool>
                    {
                        {
                            (int) ErrorChannelNumber, _isErrorPrinting
                        }
                    };
                        SendSignal(outSignals);
                        if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :" + PrinterName + " Выход " + ErrorChannelNumber + "; Сигнал " + _isErrorPrinting);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(DateTime.Now + " :" + PrinterName + ":Ошибка! Не удалось отправить сигнал " + _isErrorPrinting + " на выход " + ErrorChannelNumber);
                    }
                }
                //Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
            }
        }

        private bool _inResetError = true;

        public bool InResetError
        {
            get { return _inResetError; }
            set
            {
                if (_inResetError == value) return;
                _inResetError = value;
                if (InResetError) return;
                IsErrorPrinting = false;
                //Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
            }
        }

        private bool _inStatus = true;

        public bool InStatus
        {
            get { return _inStatus; }
            set
            {
                if (_inStatus == value) return;
                _inStatus = value;
                if (InStatus) return;
                switch (RemotePrinterLabelId)
                {
                    case 1:
                        PrintLabelId1();
                        break;
                    case 2:
                        PrintLabelId2();
                        break;
                    case 3:
                        PrintLabelId3();
                        break;
                    default:
                        Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
                        break;
                }

            }
        }

        private void PrintLabelId1()
        {
            //Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Создана новая паллета: " + InStatus.ToString());
            var docId = Db.CreateNewPallet(PlaceId);
            if (docId == null) 
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Ошибка! Новая паллета не создана!");
                return;
            }
            Console.WriteLine(DateTime.Now + " :"+ PrinterName + " Создана новая паллета: " + docId.ToString());
            if (ReportManager.PrintReport("Амбалаж", PrinterName, "Pallet", docId, 2)) return;
            FaultIds.Enqueue((Guid)docId);
            if (!FaultPrintTaskIsRunning)
                Task.Factory.StartNew(PrintFromQueue);
            
        }

        private void PrintLabelId3()
        {
            Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Печать групповой этикетки через диспетчер печати: " + InStatus.ToString());
            if (PackageLabelPath != null)
                PrintLabel(PackageLabelPath);
            else
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При печати групповой этикетки произошла ошибка (путь до этикетки не указан)");
                IsErrorPrinting = true;
            }
        }

        private void PrintLabelId2()
        {
            Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Печать групповой этикетки напрямую в принтер: " + InStatus.ToString());
            //if (PackageLabelPath != null)
                PrintLabelZPL();
            //else
            //    Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При печати групповой этикетки произошла ошибка (путь до этикетки не указан)");
        }

        public bool SaveLabelToPrinterZPL(string pdfPath)
        {
            if (pdfPath == string.Empty)
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Пустое имя файла этикетки для загрузки");
                //isPrinting = false;
                return false;
            }
            /*var memStream = PdfPrint.PdfProcessingToPng(pdfPath);
            if (memStream == null)
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При загрузке этикетки " + pdfPath + " в принтер произошла ошибка");
                return false;
            }
            */
            SavePngToPrinterZPL(PdfPrint.PdfProcessingToPng(pdfPath));
            return true;
        }

        public bool SaveLabelToPrinterZPL(byte[] imagePNG)
        {
            if (imagePNG == null)
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Пустой файла этикетки для загрузки");
                //isPrinting = false;
                return false;
            }

            SavePngToPrinterZPL(new MemoryStream(imagePNG));
            return true;
        }
        public bool SavePngToPrinterZPL(MemoryStream memStream)
        {
            string zplImageData = string.Empty;
            //Make sure no transparency exists. I had some trouble with this. This PNG has a white background
            /* string filePath = PdfPrint.PdfProcessingToPngFile(pdfPath); 
             byte[] binaryData = System.IO.File.ReadAllBytes(filePath);
             foreach (Byte b in binaryData)
             {
                 string hexRep = String.Format("{0:X}", b);
                 if (hexRep.Length == 1)
                     hexRep = "0" + hexRep;
                 zplImageData += hexRep;
             }*/

            //var memStream = PdfPrint.PdfProcessingToPng(pdfPath);
            if (memStream == null)
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При загрузке этикетки  в принтер произошла ошибка");
                return false;
            }

            memStream.Seek(0, SeekOrigin.Begin);
            int count = 0;
            while (count < memStream.Length)
            {
                string hexRep = String.Format("{0:X}", Convert.ToByte(memStream.ReadByte()));
                if (hexRep.Length == 1)
                    hexRep = "0" + hexRep;
                zplImageData += hexRep;
                count++;
            }

            string zplToSend = "^XA" + /*"^MNN" + "^LL500" +*/ "~DYE:LABEL,P,P," + memStream.Length + ",," + zplImageData + "^XZ";
            //Console.WriteLine(DateTime.Now + " :" + PrinterName + " :"+zplToSend);
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, zplToSend))
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При загрузке этикетки в принтер произошла ошибка");
                //isPrinting = false;
                return false;
            }
            else
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Загрузке этикетки в принтер произведена успешно");
                //isPrinting = false;
            }
            return true;
        }

        public void SendCommandZPL(string s)
        {
            //isPrinting = true;
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, s))
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При отправке команды " + s + " произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Отправка команды " + s + " произведена успешно");
                IsErrorPrinting = false;
                //isPrinting = false;
            }
        }

        public void PrintLabelZPL()
        {
            //isPrinting = true;
            string printImage = "^XA^FO0,0^IME:LABEL.PNG^FS^XZ";
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, printImage))
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
                IsErrorPrinting = false;
                //isPrinting = false;
            }
            /*
            try
            {
                // Open connection
                System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
                client.Connect(PrinterName, 9100);

                // Write ZPL String to connection
                System.IO.StreamWriter writer = new System.IO.StreamWriter(client.GetStream(), Encoding.UTF8);
                writer.Write(printImage);
                writer.Flush();
                // Close Connection
                writer.Close();
                client.Close();
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
            }
            catch (Exception ex)
            {
                // Catch Exception
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
            }*/
        }

        public void PrintLabel(string pdfPath)
        {
            //isPrinting = true;
            if (pdfPath.Substring(pdfPath.Length - 3, 3).ToUpper() == "PDF" ? !PdfPrint.PrintPdfDocument(pdfPath, PrinterName) : !PrintImage.SendImageToPrinter(pdfPath, PrinterName))
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                Console.WriteLine(DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
                IsErrorPrinting = false;
                //isPrinting = false;
            }
        }

        private bool FaultPrintTaskIsRunning { get; set; }

        private void PrintFromQueue()
        {
            FaultPrintTaskIsRunning = true;
            while (FaultIds.Count > 0)
            {
                Guid id;
                if (!FaultIds.TryDequeue(out id)) continue;
                if (!ReportManager.PrintReport("Амбалаж", PrinterName, "Pallet", id, 2))
                {
                    FaultIds.Enqueue(id);
                }
            }
            FaultPrintTaskIsRunning = false;
        }

        
        public void OnModbusInputDataReceived(bool[] diData)
        {
            if (diData == null || diData.Length < Math.Max(SignalChannelNumber, (ResetErrorChannelNumber == null) ? 0 : (int)ResetErrorChannelNumber))
            {
                return;
            }
            //if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :"+ PrinterName + " :" + (inputPrint - 1).ToString() + ": " + !diData[inputPrint - 1] + "  " + (inputApplicatorReady - 1).ToString() + ": " + !diData[inputApplicatorReady - 1] + " Этикетка для печати: " + (labelReady ? "готова" : "не готова") + "  Аппликатор: " + (ApplicatorReady ? "готов" : "не готов"));
            if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :" + PrinterName + " :" + (SignalChannelNumber - 1).ToString() + ": " + diData[SignalChannelNumber - 1]);
            //for (int i = 0; i <= 5; i++)
            //{
            //    Console.WriteLine(i + " " + diData[i].ToString() + " " + DateTime.Now);
            //    using (StreamWriter sw = new StreamWriter(@"d:\cprojects\adam.txt", true, System.Text.Encoding.Default))
            //    {
            //        sw.WriteLine(i + " " + diData[i].ToString() + " "+DateTime.Now);
            //        sw.Close();
            //    }
            //}

            //if (PrintInputState != !diData[inputPrint - 1] || ApplicatorReady != !diData[inputApplicatorReady - 1])
            //{
            //    Console.WriteLine("0: " + !diData[inputPrint - 1]+"<-"+ PrintInputState+"/"+ inputPrint);
            //    Console.WriteLine("1: " + !diData[inputApplicatorReady - 1] + "<-" + ApplicatorReady+"/"+ inputApplicatorReady);
            //}
            //PrintInputState = !diData[inputPrint - 1];
            //ApplicatorReady = !diData[inputApplicatorReady - 1];
            InStatus = diData[SignalChannelNumber - 1];
            if (ResetErrorChannelNumber != null)
                InResetError = diData[(int)ResetErrorChannelNumber - 1];
        }

        public void SendSignal(Dictionary<int, bool> outData)
        {
            var iStart = 17 - m_iDiTotal;
            foreach (var signal in outData)
            {
                AdamModbus.Modbus().ForceSingleCoil(iStart + signal.Key, signal.Value);
                //Thread.Sleep(sendSignalPauseInMs);
                //AdamModbus.Modbus().ForceSingleCoil(iStart + signal.Key, !signal.Value);
            }
        }

        public void SendSignal(bool outData)
        {
            var iStart = 17 - m_iDiTotal;
            if (ResetErrorChannelNumber != null)
                AdamModbus.Modbus().ForceSingleCoil(iStart + (int)ResetErrorChannelNumber, outData);
        }

        public string LoadPackageLabelPath(int placeId, int remotePrinterLabelId)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    if ((remotePrinterLabelId == 2 || remotePrinterLabelId == 3) && (gammaBase.Places.First(p => p.PlaceID == placeId)
                            .UseApplicator ?? false))
                    {
                        var outPathLabels = gammaBase.LocalSettings
                            .FirstOrDefault()
                            .LabelPath;
                        var outPath = gammaBase.C1CCharacteristics
                            .First(c => c.ProductionTasks.Any(p => p.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId)))
                            .PackageLabelPath;
                        //var inPath = gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                        //    .ApplicatorLabelPath;
                        if (string.IsNullOrEmpty(outPathLabels + outPath))// || string.IsNullOrEmpty(inPath))
                        {
                            return string.Empty;
                        }
                        //File.Copy(outPath, inPath, true);
                        return outPathLabels + outPath;
                        
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return string.Empty;
            }
        }

        public bool? LoadPackageLabelPNG(int placeId, int remotePrinterLabelId)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    if ((remotePrinterLabelId == 2 || remotePrinterLabelId == 3) && (gammaBase.Places.First(p => p.PlaceID == placeId)
                            .UseApplicator ?? false))
                    {
                        var GroupPackageLabelPNG = 
                            gammaBase.ProductionTaskConverting
                                .FirstOrDefault(c => c.ProductionTasks.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId))
                                .GroupPackLabelPNG;

                        if (GroupPackageLabelPNG == null)
                        {
                            Console.WriteLine(DateTime.Now + " :" + PrinterName + " :В текущем задании нет сохраненной этикетки!");
                            return false;
                        }
                        PackageLabelPNG = GroupPackageLabelPNG;
                        return true;

                    }
                    else
                    {
                        return null;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
