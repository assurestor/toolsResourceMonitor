using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using CommandLine;

namespace assurestor
{
    class ResourceMonitor
    {
        private struct Counters
        {
            internal int totalCpu;
            internal int totalMemory;
            internal Dictionary<string, long> driveSpace;
        }

        private static String BytesToString(long byteCount)
        {
            try
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0" + suf[0];
                long mbytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(mbytes, 1024)));
                double num = Math.Round(mbytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static String MBytesToString(long byteCount)
        {
            try
            {
                string[] suf = { "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0" + suf[0];
                long mbytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(mbytes, 1024)));
                double num = Math.Round(mbytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static Counters GetResourceUsage(string processName = null, string drive = null)
        {
            try
            {
                var totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var totalMemory = new PerformanceCounter("Memory", "Available MBytes");
                var driveSpace = new Dictionary<string, long>();
                if (drive != null) driveSpace = GetDriveSpace(drive);
                totalCpu.NextValue();
                totalMemory.NextValue();

                System.Threading.Thread.Sleep(1000);

                Counters result = new Counters
                {
                    totalCpu = (int)totalCpu.NextValue(),
                    totalMemory = (int)totalMemory.NextValue(),
                    driveSpace = (Dictionary<string, long>)driveSpace
                };

                return result;
            }
            catch (Exception e)
            {
                Counters result = new Counters();
                return result;
            }
        }

        private static Dictionary<string,long> GetDriveSpace(string drive = null)
        {
            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                Dictionary<string, long> driveSpace = new Dictionary<string, long>();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady)
                    {
                        if (drive.ToUpper() == "ALL" || d.Name == drive.ToUpper() + ":\\")
                        {
                            driveSpace.Add(d.Name, d.TotalFreeSpace);
                        }
                    }
                }
                return driveSpace;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public class Options
        {
            [Option('d', "drivespace", Default = false, Required = false, HelpText = "include drive space statistics")]
            public bool diskspace { get; set; }

            [Option('l', "driveletter", Required = false, HelpText = "only log drive space statistics for specified drive letter (C, D, etc.)")]
            public string diskletter { get; set; }

            [Option('t', "timeout", Default = 1440, Required = false, HelpText = "specify the number of mintues to run for")]
            public int Timeout { get; set; }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Clear();
                Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
                {
                    DateTime startTime = DateTime.Now;
                    int timeout = o.Timeout;
                    do
                    {
                        if (o.diskspace == true && o.diskletter == null) o.diskletter = "ALL";
                        var resourceUsage = GetResourceUsage(null, o.diskletter);

                        int usageCPU = resourceUsage.totalCpu;
                        int usageMemory = resourceUsage.totalMemory;
                        Dictionary<string, long> usageDisk = resourceUsage.driveSpace;

                        //CSV Output


                        //Console Output
                        Console.SetCursorPosition(0, 0);
                        Console.WriteLine("AssureStor Resource Monitor Logger");
                        if (o.diskspace == true) Console.WriteLine("Including disk space statistics");
                        Console.WriteLine("Timeout after {0} minutes", timeout);
                        Console.WriteLine();
                        Console.WriteLine("Date         Time        CPU    Memory    Disk Usage");
                        if (usageDisk.Count == 0)
                        {
                            Console.WriteLine(DateTime.Now.ToShortDateString().ToString().PadLeft(10) + " " + DateTime.Now.ToLongTimeString().PadLeft(10) + "   " + usageCPU.ToString().PadLeft(3) + "%  " + MBytesToString(usageMemory).PadLeft(8));
                        }
                        else
                        {
                            string usageDiskString = null;
                            foreach (var disk in usageDisk)
                            {
                                usageDiskString += disk.Key.PadLeft(3) + BytesToString(disk.Value).PadLeft(8) + " | ";
                            }
                            Console.WriteLine(DateTime.Now.ToShortDateString().ToString().PadLeft(10) + " " + DateTime.Now.ToLongTimeString().PadLeft(10) + "   " + usageCPU.ToString().PadLeft(3) + "%  " + MBytesToString(usageMemory).PadLeft(8) + "    " + usageDiskString);
                        }
                    } while (DateTime.Now < startTime.AddMinutes(timeout));
                });
                Console.WriteLine();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("EXCEPTION ALERT!, program will now terminate...");
                Console.WriteLine();
                Console.WriteLine(e.ToString());
            }
        }
    }
}
