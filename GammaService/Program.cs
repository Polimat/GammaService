using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using GammaService.Common;

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
            Console.WriteLine("Press ESC to stop");
            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
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
                            Console.WriteLine(device.IsConnected + " " + (device.IsConnected ? "IsConnected" : "Disconnected"));
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

        
    }
}
