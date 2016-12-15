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
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
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
