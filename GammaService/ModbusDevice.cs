using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        private string IpAddress { get; set; }
        private DeviceType DeviceType { get; set; }

        public bool IsConnected { get; private set; }

        public string PrinterName { get; set; }

        private int PlaceId { get; set; }

        private AdamSocket AdamModbus { get; set; }

        private int m_iDiTotal;
        private int m_iDoTotal;

        private int SignalChannelNumber { get; set; }
        private int? ConfirmChannelNumber { get; set; }

        private Timer MainTimer { get; set; }
        private Timer RestoreConnectTimer { get; set; }

        private void RestoreConnect(object obj)
        {
            if (!AdamModbus.Connected)
            {
                ReinitializeDevice();
                if (AdamModbus.Connected)
                {
                    IsConnected = true;
                    Console.WriteLine(DateTime.Now + ": Связь с " + PrinterName + " восстановлена");
                }
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
            else
            {
                RestoreConnectTimer?.Dispose();
                RestoreConnectTimer = null;
            }
        }

        private void ReadCoil(object obj)
        {
            if (!AdamModbus.Connected)
            {
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
            if (!AdamModbus.Modbus().ReadCoilStatus(iDiStart, m_iDiTotal, out bDiData) ||
            !AdamModbus.Modbus().ReadCoilStatus(iDoStart, m_iDoTotal, out bDoData)) return;
            iChTotal = m_iDiTotal + m_iDoTotal;
            bData = new bool[iChTotal];
            if (bDiData == null || bDoData == null) return;
            Array.Copy(bDiData, 0, bData, 0, m_iDiTotal);
            Array.Copy(bDoData, 0, bData, m_iDiTotal, m_iDoTotal);
            InStatus = bData[SignalChannelNumber-1];
        }

        private void InitializeDevice(DeviceType deviceType, string ipAddress)
        {
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
                if (docId != null)
                {
                    ReportManager.PrintReport("Амбалаж", PrinterName, "Pallet", docId, 2);
                }
            }
        }
    }
}
