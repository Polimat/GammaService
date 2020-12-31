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
using System.Drawing;
using System.Drawing.Printing;
using System.ServiceModel;
using System.ServiceModel.Description;
using GammaService.Services;
using GammaService.Interfaces;

namespace GammaService
{
    public class ModbusDevice
    {
        public ModbusDevice(DeviceType deviceType, string ipAddress, string modbusName, int placeId, string printerName, int remotePrinterLabelId, Dictionary<string, string> placeRemotePrinterSettings, int timerTickTime, string remotePrinterIpAdress, int? remotePrinterPort, string serviceAddress, bool isDefaultPrinterForGamma)
        {
            /*
            //myServiceHost = new ServiceHost(typeof(PrinterService), new Uri(serviceAddress));
            Uri baseAddress = new Uri(serviceAddress);
            //string address = "";// serviceAddress + modbusName+"/";

            myServiceHost = new ServiceHost(typeof(PrinterService), baseAddress);

            //// Check to see if the service host already has a ServiceMetadataBehavior
            //ServiceMetadataBehavior smb = myServiceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            //// If not, add one
            //if (smb == null)
            //    smb = new ServiceMetadataBehavior();
            //smb.HttpGetEnabled = true;
            //smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            //myServiceHost.Description.Behaviors.Add(smb);
            //// Add MEX endpoint
            //myServiceHost.AddServiceEndpoint(
            //  ServiceMetadataBehavior.MexContractName,
            //  MetadataExchangeBindings.CreateMexHttpBinding(),
            //  "mex"
            //);
            //myServiceHost.AddServiceEndpoint(typeof(IPrinterService), new BasicHttpBinding(), address);
            try
            {
                myServiceHost.Open();
            }
            catch (InvalidOperationException ieProblem)
            {
                //if (!(Program.ModbusDevicesID.Contains(0)))
                //{
                //    Common.Console.WriteLine("There was a operation problem." + ieProblem.Message + ieProblem.StackTrace);
                //    Program.CloseProgram();
                //}
            }
            
            catch (CommunicationException commProblem)
            {
                if (!(Program.ModbusDevicesID.Contains(0)))
                {
                    Common.Console.WriteLine("There was a communication problem." + commProblem.Message + commProblem.StackTrace);
                    Program.CloseProgram();
                }
            }
            */
            IpAddress = ipAddress;
            DeviceType = deviceType;
            ModbusName = modbusName;
            PrinterName = printerName;
            PlaceId = placeId;
            IsDefaultPrinterForGamma = isDefaultPrinterForGamma;
            RemotePrinterLabelId = remotePrinterLabelId;
            RemotePrinterIpAdress = remotePrinterIpAdress;
            RemotePrinterPort = remotePrinterPort;
            //SignalChannelNumber = signalChannelNumber;
            //ConfirmChannelNumber = confirmChannelNumber;
            PlaceRemotePrinterSettings = placeRemotePrinterSettings;
            ServiceAddress = serviceAddress;
            //ServiceAddress = ServiceAddress + "\" + printerName;
            string value;
            PlaceRemotePrinterSettings.TryGetValue("SignalChannelNumber", out value);
            if (value != null)
            {
                SignalChannelNumber = Convert.ToInt32(value);
                Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Параметр SignalChannelNumber установлен в " + SignalChannelNumber.ToString());
            }
            else
            {
                SignalChannelNumber = 1;
                Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Внимание!!!! Параметр SignalChannelNumber не указан! (установлен по умолчанию в 1)");
            }
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("ErrorChannelNumber", out value);
            if (value != null)
                ErrorChannelNumber = Convert.ToInt32(value);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("ResetErrorChannelNumber", out value);
            if (value != null)
                ResetErrorChannelNumber = Convert.ToInt32(value);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("ReprintChannelNumber", out value);
            if (value != null)
                ReprintChannelNumber = Convert.ToInt32(value);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("IsPrintPortStatus", out value);
            IsPrintPortStatus = value != null ? Convert.ToInt32(value) == 1 : false;
            Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Параметр IsPrintPortStatus установлен в " + IsPrintPortStatus);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("IsPrintLastPrintingLabel", out value);
            IsPrintLastPrintingLabel = value != null ? Convert.ToInt32(value) == 1 : true;
            Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Параметр IsPrintLastPrintingLabel установлен в " + IsPrintLastPrintingLabel);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("IsPrintPortRecived", out value);
            IsPrintPortRecived = value != null ? Convert.ToInt32(value) == 1 : false;
            Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Параметр IsPrintPortRecived установлен в " + IsPrintPortRecived);
            value = null;
            PlaceRemotePrinterSettings.TryGetValue("MinDelayBetweenPrintingLabels_ms_", out value);
            if (value != null)
            {
                MinDelayBetweenPrintingLabels = Convert.ToInt32(value);
                Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Параметр MinDelayBetweenPrintingLabels_ms_ установлен в " + MinDelayBetweenPrintingLabels.ToString() + " ms)");
            }
            else
            {
                MinDelayBetweenPrintingLabels = 200;
                Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :Внимание!!!! Параметр MinDelayBetweenPrintingLabels_ms_ не указан! (установлен по умолчанию в 200 ms)");
            }
            InitializeDevice(DeviceType, IpAddress, ModbusName);
            //PackageLabelPath = LoadPackageLabelPath(PlaceId, RemotePrinterLabelId);
            LoadPackageLabelPNG(PlaceId, RemotePrinterLabelId);
            if (IpAddress != "0.0.0.0")
                MainTimer = new Timer(ReadCoil, null, 0, timerTickTime);
        }

        private static ServiceHost myServiceHost = null;

        public string ServiceAddress { get; set; }
        public string IpAddress { get; set; }
        private DeviceType DeviceType { get; set; }
        public string ModbusName { get; set; }

        public bool IsConnected { get; private set; }

        public string PrinterName { get; set; }

        public int PlaceId { get; set; }

        public bool IsDefaultPrinterForGamma { get; private set; }

        //public byte[] LabelPNG { get; set; }
        private byte[] _labelPNG = null;

        public byte[] LabelPNG
        {
            get { return _labelPNG; }
            set
            {
                _labelPNG = value;
                Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + PrinterName + " :LabelPNG Обновлен: " + LabelPNG.Length.ToString());
            }
        }

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
                //    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Этикетка " + _packageLabelPath + " активирована");
            }
        }
        /*
        private byte[] _packageLabelPNG;

        public byte[] PackageLabelPNG
        {
            get { return _packageLabelPNG; }
            set
            {
                _packageLabelPNG = value;
                if (value != null && SaveLabelToPrinterPNG(value))
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Этикетка PNG активирована");
            }
        }

        private string _packageLabelZPL;

        public string PackageLabelZPL
        {
            get { return _packageLabelZPL; }
            set
            {
                _packageLabelZPL = value;
                if (value != null )//&& SaveLabelToPrinterZPL(value))
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Этикетка ZPL активирована");
            }
        }
        */
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
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Сервис " + (_isEnabledService ? "активирован" : " остановлен"));
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

        /// <summary>
        /// Номер входа с сигналом "Перепечатать"
        /// </summary>
        private int? ReprintChannelNumber { get; set; }

        /// <summary>
        /// Минимальный промежуток между печатью предыдущей и следующей этикетки
        /// </summary>
        private int? MinDelayBetweenPrintingLabels { get; set; }

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
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Связь восстановлена");
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
            else
            {
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
        }

        private bool IsPrintMessageStatus { get; set; }

        public bool ChangePrintMessageStatus()
        {
            Common.Console.WriteLine(ModbusName, "Print message status of " + ModbusName + " change to " + !IsPrintMessageStatus);
            return IsPrintMessageStatus = !IsPrintMessageStatus;
        }

        private bool IsPrintPortStatus { get; set; }

        public bool ChangePrintPortStatus()
        {
            Common.Console.WriteLine(ModbusName, "Print port status of " + ModbusName + " change to " + !IsPrintPortStatus);
            return IsPrintPortStatus = !IsPrintPortStatus;
        }

        private bool IsPrintLastPrintingLabel { get; set; }

        public bool ChangePrintLastPrintingLabel()
        {
            Common.Console.WriteLine(ModbusName, "Print last printing label of " + ModbusName + " change to " + !IsPrintLastPrintingLabel);
            return IsPrintLastPrintingLabel = !IsPrintLastPrintingLabel;
        }

        private bool IsPrintPortRecived { get; set; }

        public bool ChangePrintPortRecived()
        {
            Common.Console.WriteLine(ModbusName, "Print last printing label of " + ModbusName + " change to " + !IsPrintPortRecived);
            return IsPrintPortRecived = !IsPrintPortRecived;
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
                    if (AdamModbus.Connected && IsConnected && countReadCoilStatusFalse > 10)
                    {
                        try
                        {
                            AdamModbus.Disconnect();
                            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :Принудительная переинициализация ADAM (завис) " + IpAddress);
                        }
                        catch (Exception ex)
                        {
                            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :AdamModbus.Disconnect() выпал в ошибку " + IpAddress);
                            Common.Console.WriteLine(ModbusName, ex);
                        }
                    }
                    countReadCoilStatusFalse = 0;
                    IsConnected = false;
                    if (RestoreConnectTimer != null) return;
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :Пропала связь");
                    RestoreConnectTimer = new Timer(RestoreConnect, null, 0, 1000);
                    return;
                }
                int iDiStart = 1, iDoStart = 17;
                int iChTotal;
                bool[] bDiData, bDoData, bData;
                //            if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
                //                !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData)) return;
                if (IsPrintPortStatus) Common.Console.WriteLineWindow(10, ModbusName, DateTime.Now + " :" + PrinterName + " :Опрос адама " + IpAddress + " Статус: " + AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData).ToString() + "/" + AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData).ToString());
                if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
                !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData))
                {
                    countReadCoilStatusFalse++;
                    return;
                }
                countReadCoilStatusFalse = 0;
                //if (IsPrintPortStatus) Common.Console.WriteLine10(DateTime.Now + " :" + ModbusName + "                        :Опрос адама " + IpAddress + " после проверки статуса");
                OnDIDataReceived?.Invoke(bDiData);
                /*
                iChTotal = m_iDiTotal + m_iDoTotal;
                bData = new bool[iChTotal];
                if (bDiData == null || bDoData == null) return;
                Array.Copy(bDiData, 0, bData, 0, m_iDiTotal);
                Array.Copy(bDoData, 0, bData, m_iDiTotal, m_iDoTotal);
                InStatus = bData[SignalChannelNumber-1];
                if (IsPrintPortStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " Состояние на входе: " + InStatus.ToString());
                */
            }
        }

        private void InitializeDevice(DeviceType deviceType, string ipAddress, string modbusName)
        {
            //AdamModbus?.Disconnect();
            AdamModbus = new AdamSocket();
            if (IpAddress != "0.0.0.0")
            {
                AdamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP
                if (AdamModbus.Connect(ipAddress, ProtocolType.Tcp, 502))
                {
                    if (RestoreConnectTimer == null)
                        Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + modbusName + " :Инициализация ADAM прошла успешно.");
                    IsConnected = true;
                }
                else
                {
                    if (RestoreConnectTimer == null)
                        Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + modbusName + " :Не удалось инициализировать ADAM.");
                    IsConnected = false;
                }
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
            InitializeDevice(DeviceType, IpAddress, ModbusName);
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
            if (myServiceHost != null && (myServiceHost as ICommunicationObject).State == CommunicationState.Opened)
            {
                myServiceHost.Close();
                myServiceHost = null;
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
                        if (IsPrintPortStatus) Common.Console.WriteLineWindow(10, ModbusName,DateTime.Now + " :" + ModbusName + " Выход " + ErrorChannelNumber + "; Сигнал " + _isErrorPrinting);
                    }
                    catch (Exception)
                    {
                        Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + ":Ошибка! Не удалось отправить сигнал " + _isErrorPrinting + " на выход " + ErrorChannelNumber);
                    }
                }
                //Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
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
                //Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
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
                TimeSpan span = DateTime.Now - lastPrintingLabel;
                if (span.TotalMilliseconds > MinDelayBetweenPrintingLabels)
                {
                    switch (RemotePrinterLabelId)
                    {
                        case 1:
                            PrintLabelId1(2);
                            break;
                        case 2:
                            PrintLabelId2();
                            break;
                        case 3:
                            PrintLabelId3();
                            break;
                        case 4:
                            PrintLabelId4();
                            break;
                        case 5:
                            PrintLabelId5();
                            break;
                        case 6:
                            PrintLabelId1(1);
                            break;
                        default:
                            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " :Ошибка! Неопределенный вид этикетки!: " + InStatus.ToString());
                            break;
                    }
                    lastPrintingLabel = DateTime.Now;
                }
            }
        }

        private DateTime _lastPrintingLabel = DateTime.Now;
        private DateTime lastPrintingLabel
        {
            get { return _lastPrintingLabel; }
            set
            {
                _lastPrintingLabel = value;
                if (IsPrintLastPrintingLabel)
                    Common.Console.WriteLineWindow(4, ModbusName,DateTime.Now + " :" + ModbusName + " :" + PrinterName + " lastPrintingLabel: " + _lastPrintingLabel.ToString());

            }
        }

        private void PrintLabelId1(int numCopies = 2)
        {
            //Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Создана новая паллета: " + InStatus.ToString());
            //TimeSpan span = DateTime.Now - _lastPrintingLabel;
            //if (span.TotalMilliseconds > MinDelayBetweenPrintingLabels)
            {
                var docId = Db.CreateNewPallet(PlaceId, ModbusName);
                if (docId == null)
                {
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " :Ошибка! Новая паллета не создана!");
                    return;
                }
                Common.Console.WriteLineWindow(4, ModbusName,DateTime.Now + " :" + ModbusName + " :" + PrinterName + " Создана новая паллета: " + docId.ToString());
                //_lastPrintingLabel = DateTime.Now;
                if (ReportManager.PrintReport("Амбалаж", PrinterName, "Pallet", docId, numCopies)) return;
                FaultIds.Enqueue((Guid)docId);
                if (!FaultPrintTaskIsRunning)
                {
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " PrintReport из очереди печати");
                    Task.Factory.StartNew(PrintFromQueue);
                }
            }
        }

        private void PrintLabelId5()
        {
            //Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Создана новая паллета: " + InStatus.ToString());
            //TimeSpan span = DateTime.Now - _lastPrintingLabel;
            //if (span.TotalMilliseconds > MinDelayBetweenPrintingLabels)
            {
                var docId = Db.CreateNewPallet(PlaceId,ModbusName);
                if (docId == null)
                {
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " :Ошибка! Новая паллета не создана!");
                    return;
                }
                Common.Console.WriteLineWindow(4, ModbusName,DateTime.Now + " :" + ModbusName + " :" + PrinterName + " Создана новая паллета: " + docId.ToString());
                //_lastPrintingLabel = DateTime.Now;
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " LabelPNG: " + LabelPNG.Length.ToString());
                if (LabelPNG != null)
                {
                    using (Image image = Image.FromStream(new MemoryStream(LabelPNG)))
                    {
                        if (ReportManager.PrintReport("Амбалаж новый", PrinterName, "Pallet", docId, 2, image)) return;
                        FaultIds.Enqueue((Guid)docId);
                        if (!FaultPrintTaskIsRunning)
                            Task.Factory.StartNew(PrintFromQueue);
                    };
                }
                else
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " Нет этикетки в формате PNG. Ошибка при печати паллеты: " + docId.ToString());
            }
        }

        private void PrintLabelId4()
        {
            //Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Создана новая паллета: " + InStatus.ToString());
            //TimeSpan span = DateTime.Now - _lastPrintingLabel;
            //if (span.TotalMilliseconds > MinDelayBetweenPrintingLabels)
            {
                var docId = Db.CreateNewPallet(PlaceId, ModbusName);
                if (docId == null)
                {
                    Common.Console.WriteLine(ModbusName,DateTime.Now + " :" + ModbusName + " :" + PrinterName + " :Ошибка! Новая паллета не создана!");
                    return;
                }
                Common.Console.WriteLineWindow(4, ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " Создана новая паллета: " + docId.ToString());
                //_lastPrintingLabel = DateTime.Now;
                //PrintLabelZPL(); 
                if (LabelPNG != null)
                {
                    using (Image image = Image.FromStream(new MemoryStream(LabelPNG)))
                    {
                        //image.Save(@"D:\testpng.png", System.Drawing.Imaging.ImageFormat.Png);
                        ReportManager.PrintReport("Амбалаж новый", PrinterName, "Pallet", docId, 2, image);
                    };
                }
                else
                    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + ModbusName + " :" + PrinterName + " Нет этикетки в формате PNG. Ошибка при печати паллеты: " + docId.ToString());

            }
        }

        private void PrintLabelId3()
        {
            if (IsPrintMessageStatus) Common.Console.WriteLineWindow(4, ModbusName,DateTime.Now + " :" + PrinterName + " :Печать групповой этикетки через диспетчер печати: " + InStatus.ToString());
            PrintLabelPNG();
            /*if (PackageLabelPath != null)
                PrintLabel(PackageLabelPath);
            else
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати групповой этикетки произошла ошибка (путь до этикетки не указан)");
                IsErrorPrinting = true;
            }*/
        }

        private void PrintLabelId2()
        {
            if (IsPrintMessageStatus) Common.Console.WriteLineWindow(4, ModbusName,DateTime.Now + " :" + PrinterName + " :Печать групповой этикетки напрямую в принтер: " + InStatus.ToString());
            //if (PackageLabelPath != null)
            PrintLabelZPL();
            //else
            //    Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати групповой этикетки произошла ошибка (путь до этикетки не указан)");
        }

        public bool SaveLabelToPrinterPNG(string pdfPath)
        {
            if (pdfPath == string.Empty)
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Пустое имя файла этикетки для загрузки");
                //isPrinting = false;
                return false;
            }
            /*var memStream = PdfPrint.PdfProcessingToPng(pdfPath);
            if (memStream == null)
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При загрузке этикетки " + pdfPath + " в принтер произошла ошибка");
                return false;
            }
            */
            SavePngToPrinterZPL(PdfPrint.PdfProcessingToPng(pdfPath));
            return true;
        }

        public bool SaveLabelToPrinterPNG(byte[] imagePNG)
        {
            if (imagePNG == null)
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Пустой файла этикетки для загрузки");
                //isPrinting = false;
                return false;
            }

            SavePngToPrinterZPL(new MemoryStream(imagePNG));
            return true;
        }
        public bool SavePngToPrinterZPL(MemoryStream memStream)
        {
            string zplImageData = string.Empty;
            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Начата загрузке этикетки в принтер");
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
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При загрузке этикетки  в принтер произошла ошибка");
                return false;
            }

            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Начато конвертирование этикетки для загрузки в принтер");
            memStream.Seek(0, SeekOrigin.Begin);
            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Спозиционировано на начало этикетки для загрузки в принтер");
            int count = 0;
            while (count < memStream.Length)
            {
                string hexRep = String.Format("{0:X}", Convert.ToByte(memStream.ReadByte()));
                if (hexRep.Length == 1)
                    hexRep = "0" + hexRep;
                zplImageData += hexRep;
                count++;
            }
            //Common.Console.WriteLine(zplImageData);
            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Закончено конвертирование этикетки для загрузки в принтер");
            return SavePngToPrinterZPL(zplImageData, memStream.Length);
        }

        public bool SavePngToPrinterZPL(string zplImageData, long ImageZize)
        {
            string zplToSend = "^XA" + /*"^MNN" + "^LL500" +*/ "~DYE:LABEL,P,P," + ImageZize.ToString() + ",," + zplImageData + "^XZ";
            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :" + ImageZize.ToString());
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, zplToSend))
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При загрузке этикетки в принтер произошла ошибка");
                //isPrinting = false;
                return false;
            }
            else
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Загрузке этикетки в принтер произведена успешно");
                //isPrinting = false;
            }
            return true;
        }

        public void SendCommandZPL(string s)
        {
            //isPrinting = true;
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, s))
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При отправке команды " + s + " произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Отправка команды " + s + " произведена успешно");
                IsErrorPrinting = false;
                //isPrinting = false;
            }
        }

        public void PrintLabelWithBarCodeZPL(string barCode)
        {
            //isPrinting = true;
            string printImage = "^XA^FO0,0^IME:LABEL.PNG^FS^XZ";
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, printImage))
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
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
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
            }
            catch (Exception ex)
            {
                // Catch Exception
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
            }*/
        }

        public void PrintLabelZPL()
        {
            //isPrinting = true;
            string printImage = "^XA^FO0,0^IME:LABEL.PNG^FS^XZ";
            if (!RawPrinterHelper.SendStringToPrinter(RemotePrinterIpAdress, RemotePrinterPort, printImage))
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
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
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
            }
            catch (Exception ex)
            {
                // Catch Exception
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
            }*/
        }

        public void PrintLabel(string pdfPath)
        {
            //isPrinting = true;
            if (pdfPath.Substring(pdfPath.Length - 3, 3).ToUpper() == "PDF" ? !PdfPrint.PrintPdfDocument(pdfPath, PrinterName, ModbusName) : !PrintImage.SendImageToPrinter(pdfPath, PrinterName, ModbusName))
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :При печати произошла ошибка");
                IsErrorPrinting = true;
                //isPrinting = false;
                return;
            }
            else
            {
                if (IsPrintMessageStatus) Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Печать произведена успешно");
                IsErrorPrinting = false;
                //isPrinting = false;
            }
        }

        public void PrintLabelPNG()
        {
            if (LabelPNG == null)
            {
                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :В текущем задании нет сохраненной этикетки!");
                //return false;
            }
            if (RemotePrinterLabelId == 3)
            {
                PdfPrint.PrintPNGDocument(LabelPNG, PrinterName, ModbusName);
            }
        }

        private void PrintPagePNG(object o, PrintPageEventArgs e)
        {
            try
            {
                if (LabelPNG != null)
                {
                    //Load the image from the file
                    System.Drawing.Image img = System.Drawing.Image.FromStream(new MemoryStream(LabelPNG));

                    //Adjust the size of the image to the page to print the full image without loosing any part of it
                    Rectangle m = e.MarginBounds;

                    if ((double)img.Width / (double)img.Height > (double)m.Width / (double)m.Height) // image is wider
                    {
                        m.Height = (int)((double)img.Height / (double)img.Width * (double)m.Width);
                    }
                    else
                    {
                        m.Width = (int)((double)img.Width / (double)img.Height * (double)m.Height);
                    }
                    e.Graphics.DrawImage(img, m);
                }
            }
            catch (Exception)
            {

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
            if (IsPrintPortRecived) Common.Console.WriteLineWindow(10, ModbusName,DateTime.Now + " :" + PrinterName + " : Вход " + (SignalChannelNumber - 1).ToString() + ": Значение " + diData[SignalChannelNumber - 1]+"                                 ");
            
            //for (int i = 0; i <= 5; i++)
            //{
            //    Common.Console.WriteLine(i + " " + diData[i].ToString() + " " + DateTime.Now);
            //    using (StreamWriter sw = new StreamWriter(@"d:\cprojects\adam.txt", true, System.Text.Encoding.Default))
            //    {
            //        sw.WriteLine(i + " " + diData[i].ToString() + " "+DateTime.Now);
            //        sw.Close();
            //    }
            //}

            //if (PrintInputState != !diData[inputPrint - 1] || ApplicatorReady != !diData[inputApplicatorReady - 1])
            //{
            //    Common.Console.WriteLine("0: " + !diData[inputPrint - 1]+"<-"+ PrintInputState+"/"+ inputPrint);
            //    Common.Console.WriteLine("1: " + !diData[inputApplicatorReady - 1] + "<-" + ApplicatorReady+"/"+ inputApplicatorReady);
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

        public bool? LoadPackageLabelPath(int placeId, int remotePrinterLabelId)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    if (((remotePrinterLabelId == 3) && (gammaBase.Places.First(p => p.PlaceID == placeId)
                            .UseApplicator ?? false)) || remotePrinterLabelId == 4 || remotePrinterLabelId == 5)
                    {
                        var outPathLabels = gammaBase.LocalSettings
                            .FirstOrDefault()
                            .LabelPath;
                        var characteristic =
                        gammaBase.C1CCharacteristics
                            .FirstOrDefault(c => c.ProductionTasks.Any(p => p.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId)));
                        string outPath = null;
                        if (characteristic != null && characteristic.PackageLabelPath != null)
                            if (File.Exists(outPathLabels + characteristic.PackageLabelPath))
                            {
                                outPath = characteristic.PackageLabelPath;
                            }
                            else
                            {
                                outPath = characteristic.C1COldCharacteristicID == null ? null : gammaBase.C1CCharacteristics.Where(p => p.C1CCharacteristicID == characteristic.C1COldCharacteristicID)
                                    .Select(p => p.PackageLabelPath)
                                    .FirstOrDefault();
                            }
                        //var inPath = gammaBase.Places.First(p => p.ProductionTasks.Any(pt => pt.ProductionTaskID == productionTaskId))
                        //    .ApplicatorLabelPath;
                        if (string.IsNullOrEmpty(outPathLabels + outPath))// || string.IsNullOrEmpty(inPath))
                        {
                            return false;
                        }
                        //File.Copy(outPath, inPath, true);
                        PackageLabelPath = outPathLabels + outPath;
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
                Common.Console.WriteLine(ModbusName, e);
                return false;
            }
        }

        public bool? LoadPackageLabelPNG(int placeId, int remotePrinterLabelId)
        {
            try
            {
                using (var gammaBase = new GammaEntities())
                {
                    if (((remotePrinterLabelId == 2 || remotePrinterLabelId == 3) && (gammaBase.Places.First(p => p.PlaceID == placeId)
                            .UseApplicator ?? false)) || remotePrinterLabelId == 4 || remotePrinterLabelId == 5)
                    {
                        string LabelZPL;
                        if (remotePrinterLabelId == 4 || remotePrinterLabelId == 5)
                        {
                            LabelPNG =
                                gammaBase.ProductionTaskConverting
                                    .FirstOrDefault(c => c.ProductionTasks.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId))
                                    .TransportPackLabelPNG;

                            LabelZPL =
                                gammaBase.ProductionTaskConverting
                                    .FirstOrDefault(c => c.ProductionTasks.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId))
                                    .TransportPackLabelZPL;
                        }
                        else
                        {
                            LabelPNG =
                                gammaBase.ProductionTaskConverting
                                    .FirstOrDefault(c => c.ProductionTasks.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId))
                                    .GroupPackLabelPNG;

                            LabelZPL =
                                gammaBase.ProductionTaskConverting
                                    .FirstOrDefault(c => c.ProductionTasks.ActiveProductionTasks.Any(pt => pt.PlaceID == placeId))
                                    .GroupPackLabelZPL;
                        }
                        if (LabelPNG == null)
                        {
                            Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :В текущем задании нет сохраненной этикетки!");
                            return false;
                        }
                        if (remotePrinterLabelId == 2 || remotePrinterLabelId == 4)
                        {
                            if (LabelZPL != String.Empty & LabelZPL != null)
                            {
                                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Вариант ZPL!");
                                SavePngToPrinterZPL(LabelZPL, LabelPNG.Length);
                            }
                            else
                            {
                                //PackageLabelPNG = LabelPNG;
                                Common.Console.WriteLine(ModbusName, DateTime.Now + " :" + PrinterName + " :Вариант PNG!");
                                SaveLabelToPrinterPNG(LabelPNG);
                            }
                        }
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
                Common.Console.WriteLine(ModbusName, e);
                return false;
            }
        }
    }
}
