using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using GammaService.Common;
using GammaService.Services;

namespace GammaService
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            InitializeDevices();
            /*modbuseDevices = new List<ModbusDevice>();
            using (var gammaBase = new GammaEntities())
            {
                var devices = gammaBase.PlaceRemotePrinters.Include(ps => ps.ModbusDevices).Include(ps => ps.RemotePrinters).Include(ps => ps.PlaceRemotePrinterSettings).Where(p => p.IsEnabled == true);
                foreach (var device in devices)
                {
                    var mDevice = new ModbusDevice((DeviceType)device.ModbusDevices.ModbusDeviceTypeID, 
                        device.ModbusDevices.IPAddress, device.PlaceID, device.RemotePrinters.PrinterName, device.RemotePrinters.RemotePrinterLabelID,
                        //(int)device.SignalChannelNumber, device.ConfirmChannelNumber, device.ModbusDevices.TimerTick));
                        device.PlaceRemotePrinterSettings.ToDictionary(p => p.SettingName, p => p.SettingValue), device.ModbusDevices.TimerTick, device.RemotePrinters.IpAdress, device.RemotePrinters.Port);
                    mDevice.OnDIDataReceived += mDevice.OnModbusInputDataReceived;
                    modbuseDevices.Add(mDevice);
                }
            }
            */
            myServiceHost = new ServiceHost(typeof(PrinterService));
            //myServiceHost.AddDefaultEndpoints();
            myServiceHost.Open();
            const string message = "Press ESC to stop; F4 to restart devices; F5 to reinitialize devices; F2 to print status device; F9 to print status input";
            Console.WriteLine(message);
            ConsoleKey key;
            do
            {
                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.F4:
                        InitializeDevices();
                        break;
                    case ConsoleKey.F5:
                        foreach (var device in modbuseDevices)//.Where(d => !d.IsConnected))
                        {
                            device.ReinitializeADAM();
                        }
                        break;
                    case ConsoleKey.F2:
                        foreach (var device in modbuseDevices)
                        {
                            Console.WriteLine("Printer: " + device.PrinterName + ", ADAM: " + device.IpAddress + ", " + (device.IsConnected ? "IsConnected" : "Disconnected"));
                        }
                        Console.WriteLine(message);
                        break;
                    case ConsoleKey.F9:
                        foreach (var device in modbuseDevices)
                        {
                            device.ChangePrintPortStatus();
                        }
                        break;
                }
            } while (key != ConsoleKey.Escape);

            if (myServiceHost != null && (myServiceHost as ICommunicationObject).State == CommunicationState.Opened)
            {
                myServiceHost.Close();
                myServiceHost = null;
            }

            /**/
            /*
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new GammaService()
            };
            ServiceBase.Run(ServicesToRun);
            */
        }

        public static List<ModbusDevice> modbuseDevices = null;
        private static ServiceHost myServiceHost = null;

        public static void InitializeDevices()
        {
            //modbuseDevices = new List<ModbusDevice>();
            if (modbuseDevices == null)
                modbuseDevices = new List<ModbusDevice>();
            else
            {
                foreach (var device in modbuseDevices)
                {
                    device.DestroyDevice();
                }
            }
            using (var gammaBase = new GammaEntities())
            {
                var devices = gammaBase.PlaceRemotePrinters.Include(ps => ps.ModbusDevices).Include(ps => ps.RemotePrinters).Include(ps => ps.PlaceRemotePrinterSettings).Where(p => p.IsEnabled == true);
                foreach (var device in devices)
                {
                    var mDevice = new ModbusDevice((DeviceType)device.ModbusDevices.ModbusDeviceTypeID,
                        device.ModbusDevices.IPAddress, device.PlaceID, device.RemotePrinters.PrinterName, device.RemotePrinters.RemotePrinterLabelID,
                        //(int)device.SignalChannelNumber, device.ConfirmChannelNumber, device.ModbusDevices.TimerTick));
                        device.PlaceRemotePrinterSettings.ToDictionary(p => p.SettingName, p => p.SettingValue), device.ModbusDevices.TimerTick, device.RemotePrinters.IpAdress, device.RemotePrinters.Port);
                    mDevice.OnDIDataReceived += mDevice.OnModbusInputDataReceived;
                    modbuseDevices.Add(mDevice);
                }
            }
        }
    }
}
