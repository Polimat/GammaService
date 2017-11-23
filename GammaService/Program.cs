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
            List<ModbusDevice> modbuseDevices = new List<ModbusDevice>();
            using (var gammaBase = new GammaEntities())
            {
                var devices = gammaBase.PlaceRemotePrintingSettings.Include(ps => ps.ModbusDevices).Include(ps => ps.RemotePrinters);
                foreach (var device in devices)
                {
                    modbuseDevices.Add(new ModbusDevice((DeviceType)device.ModbusDevices.ModbusDeviceTypeID, 
                        device.ModbusDevices.IPAddress, device.PlaceID, device.RemotePrinters.PrinterName, 
                        (int)device.SignalChannelNumber, device.ConfirmChannelNumber, device.ModbusDevices.TimerTick));
                }
            }

            myServiceHost = new ServiceHost(typeof(PrinterService));
            //myServiceHost.AddDefaultEndpoints();
            myServiceHost.Open();
            const string message = "Press ESC to stop; F5 to reinitialize device; F2 to print status device; F6 to print status input";
            Console.WriteLine(message);
            ConsoleKey key;
            do
            {
                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.F5:
                        foreach (var device in modbuseDevices.Where(d => !d.IsConnected))
                        {
                            device.ReinitializeDevice();
                        }
                        break;
                    case ConsoleKey.F2:
                        foreach (var device in modbuseDevices)
                        {
                            Console.WriteLine("Printer: " + device.PrinterName + ", ADAM: " + device.IpAddress + ", " + (device.IsConnected ? "IsConnected" : "Disconnected"));
                            Console.WriteLine(message);
                        }
                        break;
                    case ConsoleKey.F6:
                        foreach (var device in modbuseDevices)
                        {
                            device.ChangePrintStatus();
                        }
                        break;
                }
            } while (key != ConsoleKey.Escape);

            



            /*
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new GammaService()
            };
            ServiceBase.Run(ServicesToRun);
            */
        }

        private static ServiceHost myServiceHost = null;
    }
}
