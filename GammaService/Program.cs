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
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Укажите номер устройства ModbusDevices для инициализации в параметры командной строки или -1 для всех устройств.");
            }
            else
            {
                int num;
                bool test;
                ModbusDevicesID = new List<int>(args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    test = int.TryParse(args[i], out num);
                    if (test == false)
                        System.Console.WriteLine("Параметр не цифра: " + args[i]);
                    else
                    {
                        ModbusDevicesID.Add(num);
                    }
                }

                if (ModbusDevicesID.Contains(-1))
                {
                    ModbusDevicesID.Clear();
                    ModbusDevicesID.Add(-1);
                }
                if (!InitializeDevices())
                {
                    System.Console.WriteLine("Устройства с такими ID не обнаружены.");
                }
                else
                {
                    //myServiceHost = new ServiceHost(typeof(PrinterService));
                    ////myServiceHost.AddDefaultEndpoints();
                    //myServiceHost.Open();
                    const string message = "Press ESC to stop; F5 to reinitialize devices; F2 to print status device; F8 to print message; F9 to print status input; F6 to print avilable printers";
                    Common.Console.WriteLine(message);
                    ConsoleKey key;
                    do
                    {
                        key = System.Console.ReadKey(true).Key;
                        switch (key)
                        {
                            case ConsoleKey.F4://F4 to restart devices; not worked
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
                                    Common.Console.WriteLine("Printer: " + device.PrinterName + ", ADAM: " + device.ModbusName + "(" + device.IpAddress + "), ServiceAddress: " + device.ServiceAddress + ", " + (device.IsConnected ? "IsConnected" : "Disconnected"));
                                }
                                Common.Console.WriteLine(message);
                                break;
                            case ConsoleKey.F8:
                                foreach (var device in modbuseDevices)
                                {
                                    device.ChangePrintMessageStatus();
                                }
                                break;
                            case ConsoleKey.F9:
                                foreach (var device in modbuseDevices)
                                {
                                    device.ChangePrintPortStatus();
                                }
                                break;
                            case ConsoleKey.F6:
                                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                                {
                                    Common.Console.WriteLine(printer);
                                }
                                break;

                        }
                    } while (key != ConsoleKey.Escape);

                    //if (myServiceHost != null && (myServiceHost as ICommunicationObject).State == CommunicationState.Opened)
                    //{
                    //    myServiceHost.Close();
                    //    myServiceHost = null;
                    //}

                    ///**/
                    ///*
                    //ServiceBase[] ServicesToRun;
                    //ServicesToRun = new ServiceBase[]
                    //{
                    //    new GammaService()
                    //};
                    //ServiceBase.Run(ServicesToRun);
                    //*/
                }
            }
        }

        public static List<ModbusDevice> modbuseDevices = null;
        public static List<int> ModbusDevicesID;
        public static List<ServiceHost> myServiceHosts;
        public static List<string> myServiceAddress;
        //private static ServiceHost myServiceHost = null;

        public static void CloseProgram()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public static bool InitializeDevices()
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
                var devices = gammaBase.PlaceRemotePrinters.Include(ps => ps.ModbusDevices).Include(ps => ps.RemotePrinters).Include(ps => ps.PlaceRemotePrinterSettings).Where(p => p.IsEnabled == true && (ModbusDevicesID.Contains(-1) || ModbusDevicesID.Contains(p.ModbusDeviceID)));
                foreach (var device in devices)
                {
                    var mDevice = new ModbusDevice((DeviceType)device.ModbusDevices.ModbusDeviceTypeID,
                        device.ModbusDevices.IPAddress, device.ModbusDevices.Name, device.PlaceID, device.RemotePrinters.PrinterName, device.RemotePrinters.RemotePrinterLabelID,
                        //(int)device.SignalChannelNumber, device.ConfirmChannelNumber, device.ModbusDevices.TimerTick));
                        device.PlaceRemotePrinterSettings.ToDictionary(p => p.SettingName, p => p.SettingValue), device.ModbusDevices.TimerTick, device.RemotePrinters.IpAdress, device.RemotePrinters.Port, device.ModbusDevices.ServiceAddress);
                    mDevice.OnDIDataReceived += mDevice.OnModbusInputDataReceived;
                    modbuseDevices.Add(mDevice);
                }
                if (devices.Count() == 0 && !ModbusDevicesID.Contains(-2))
                    return false;
                else
                {
                    if (myServiceAddress== null)
                        myServiceAddress = new List<string>();
                    foreach (var modbusDevice in modbuseDevices)
                    {
                        if (myServiceAddress.Count() == 0 || !myServiceAddress.Contains(modbusDevice.ServiceAddress))
                            myServiceAddress.Add(modbusDevice.ServiceAddress);
                    }
                    if (ModbusDevicesID.Contains(-2) || ModbusDevicesID.Contains(-1))
                    {
                        var mMailServiceAddress = gammaBase.LocalSettings.FirstOrDefault().MailServiceAddress;
                        if (mMailServiceAddress != String.Empty && (myServiceAddress.Count() == 0 || !myServiceAddress.Contains(mMailServiceAddress)))
                        {
                            myServiceAddress.Add(mMailServiceAddress);
                            Common.Console.WriteLine(DateTime.Now + " : Будет запущен mail сервис по адресу " + mMailServiceAddress);
                        }
                    }
                    if (myServiceAddress.Count() == 0)
                        return false;
                    else
                    {
                        if (myServiceHosts == null)
                            myServiceHosts = new List<ServiceHost>();
                        foreach (var serviceAddress in myServiceAddress)
                        {
                            //myServiceHost = new ServiceHost(typeof(PrinterService), new Uri(serviceAddress));
                            Uri baseAddress = new Uri(serviceAddress);
                            //string address = "";// serviceAddress + modbusName+"/";
                            var myServiceHost = new ServiceHost(typeof(PrinterService), baseAddress);

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
                                myServiceHosts.Add(myServiceHost);
                            }
                            catch (InvalidOperationException ieProblem)
                            {
                                //if (!(Program.ModbusDevicesID.Contains(-1)))
                                {
                                    Common.Console.WriteLine("There was a operation problem." + ieProblem.Message + ieProblem.StackTrace);
                                    Program.CloseProgram();
                                }
                            }

                            catch (CommunicationException commProblem)
                            {
                                //if (!(Program.ModbusDevicesID.Contains(-1)))
                                {
                                    Common.Console.WriteLine("There was a communication problem." + commProblem.Message + commProblem.StackTrace);
                                    Program.CloseProgram();
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            
        }
    }
}
