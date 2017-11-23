using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Advantech.Adam;
using GammaService.Common;

namespace GammaService
{
    public class ModbusDevice
    {
        public ModbusDevice(DeviceType deviceType, string ipAddress, int placeId, string printerName, int signalChannelNumber, int? confirmChannelNumber, int timerTickTime)
        {
            IpAddress = ipAddress;
            DeviceType = deviceType;
            PrinterName = printerName;
            PlaceId = placeId;
            SignalChannelNumber = signalChannelNumber;
            ConfirmChannelNumber = confirmChannelNumber;
            InitializeDevice(deviceType, ipAddress);
            MainTimer = new Timer(ReadCoil, null, 0, timerTickTime);
        }

        public string IpAddress { get; set; }
        private DeviceType DeviceType { get; set; }

        public bool IsConnected { get; private set; }

        public string PrinterName { get; set; }

        private int PlaceId { get; set; }

        private AdamSocket AdamModbus { get; set; }

        private int m_iDiTotal;
        private int m_iDoTotal;

        private int SignalChannelNumber { get; set; }
        private int? ConfirmChannelNumber { get; set; }

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
                ReinitializeDevice();
                if (!AdamModbus.Connected) return;
                IsConnected = true;
                Console.WriteLine(DateTime.Now + ": Связь с " + PrinterName + " восстановлена");
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

        public void ChangePrintStatus()
        {
            IsPrintPortStatus = !IsPrintPortStatus;
        }


        private void ReadCoil(object obj)
        {
            if (!AdamModbus.Connected || countReadCoilStatusFalse > 10)
            {
                if (IsConnected && countReadCoilStatusFalse > 10)
                {
                    AdamModbus.Disconnect();
                    Console.WriteLine(DateTime.Now + " :Принудительная переинициализация (завис) " + IpAddress);
                }
                countReadCoilStatusFalse = 0;
                IsConnected = false;
                if (RestoreConnectTimer != null) return;
                Console.WriteLine(DateTime.Now + " :Пропала связь с " + PrinterName);
                RestoreConnectTimer = new Timer(RestoreConnect, null, 0, 1000);
                return;
            }
            int iDiStart = 1, iDoStart = 17;
            int iChTotal;
            bool[] bDiData, bDoData, bData;
            //            if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
            //                !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData)) return;
            if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " :Опрос адама " + IpAddress + " Статус: " + AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData).ToString()+"/"+ AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData).ToString());
            if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
            !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData))
            {
                countReadCoilStatusFalse++;
                return;
            }
            countReadCoilStatusFalse = 0;
            if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + "                        :Опрос адама " + IpAddress + " после проверки статуса");
            iChTotal = m_iDiTotal + m_iDoTotal;
            bData = new bool[iChTotal];
            if (bDiData == null || bDoData == null) return;
            Array.Copy(bDiData, 0, bData, 0, m_iDiTotal);
            Array.Copy(bDoData, 0, bData, m_iDiTotal, m_iDoTotal);
            InStatus = bData[SignalChannelNumber-1];
            if (IsPrintPortStatus) Console.WriteLine(DateTime.Now + " Состояние на входе: " + InStatus.ToString());
        }

        private void InitializeDevice(DeviceType deviceType, string ipAddress)
        {
            //AdamModbus?.Disconnect();
            AdamModbus = new AdamSocket();
            AdamModbus.SetTimeout(1000, 1000, 1000); // set timeout for TCP
            if (AdamModbus.Connect(ipAddress, ProtocolType.Tcp, 502))
            {
                if (RestoreConnectTimer == null)
                    Console.WriteLine(DateTime.Now + "Инициализация прошла успешно: " + PrinterName);
                IsConnected = true;
            }
            else
            {
                if (RestoreConnectTimer == null)
                    Console.WriteLine(DateTime.Now + "Не удалось инициализировать: " + PrinterName);
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

        public void ReinitializeDevice()
        {
            InitializeDevice(DeviceType, IpAddress);
        }

        private ConcurrentQueue<Guid> FaultIds { get; } = new ConcurrentQueue<Guid>();

        private bool _inStatus = true;

        private bool InStatus
        {
            get { return _inStatus; }
            set
            {
                if (_inStatus == value) return;
                _inStatus = value;
                if (InStatus) return;
                var docId = Db.CreateNewPallet(PlaceId);
                if (docId == null) return;
                if (ReportManager.PrintReport("Амбалаж", PrinterName, "Pallet", docId, 2)) return;
                FaultIds.Enqueue((Guid)docId);
                if (!FaultPrintTaskIsRunning)
                    Task.Factory.StartNew(PrintFromQueue);
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
    }
}
