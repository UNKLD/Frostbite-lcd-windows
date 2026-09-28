using System;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace FrostbiteMonitor;

class Program
{
    private const int BaudRate = 1_000_000;
    private const string TargetVid = "1F3A";
    private const string TargetPid = "0008";

    static void Main(string[] args)
    {
        Console.Title = "Dragos FrostBite 240 Standalone Monitor";
        Console.WriteLine("=============================================");
        Console.WriteLine(" Dragos FrostBite 240 - Standalone C# Daemon ");
        Console.WriteLine("=============================================\n");

        var computer = new Computer
        {
            IsCpuEnabled = true
        };

        try
        {
            computer.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Could not initialize driver: {ex.Message}");
            Console.WriteLine("Make sure you right-click the .exe and Run as Administrator.");
            Console.ReadLine();
            return;
        }

        SerialPort? serialPort = null;

        while (true)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                string? portName = FindFrostbitePort();
                if (string.IsNullOrEmpty(portName))
                {
                    Console.Write($"\r[Search] Device {TargetVid}:{TargetPid} not found. Retrying...   ");
                    Thread.Sleep(2000);
                    continue;
                }

                try
                {
                    Console.WriteLine($"\n[Serial] Found FrostBite on {portName}. Connecting...");
                    serialPort = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One)
                    {
                        WriteTimeout = 1000
                    };
                    serialPort.Open();
                    Console.WriteLine($"[Serial] Connected to {portName} at {BaudRate:N0} baud.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Serial Error] {ex.Message}");
                    serialPort?.Dispose();
                    serialPort = null;
                    Thread.Sleep(2000);
                    continue;
                }
            }

            int? temp = ReadCpuTemp(computer);

            if (temp.HasValue)
            {
                int clamped = Math.Clamp(temp.Value, 0, 99);
                byte encoded = (byte)(clamped ^ 0x02);
                byte[] packet = [0x74, 0x01, 0x02, encoded, 0x00, 0x02];

                try
                {
                    serialPort.Write(packet, 0, packet.Length);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] CPU: {clamped}°C | Sent: {BitConverter.ToString(packet).Replace("-", " ")}");
                }
                catch
                {
                    Console.WriteLine("\n[Serial] Connection lost.");
                    serialPort.Dispose();
                    serialPort = null;
                }
            }

            Thread.Sleep(1000);
        }
    }

    private static string? FindFrostbitePort()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, PNPDeviceID FROM Win32_SerialPort");
            foreach (ManagementObject port in searcher.Get())
            {
                string pnp = port["PNPDeviceID"]?.ToString() ?? "";
                if (pnp.Contains($"VID_{TargetVid}", StringComparison.OrdinalIgnoreCase) &&
                    pnp.Contains($"PID_{TargetPid}", StringComparison.OrdinalIgnoreCase))
                {
                    return port["DeviceID"]?.ToString();
                }
            }

            using var pnpSearcher = new ManagementObjectSearcher(
                $"SELECT Name FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_{TargetVid}&PID_{TargetPid}%'");
            foreach (ManagementObject entity in pnpSearcher.Get())
            {
                string name = entity["Name"]?.ToString() ?? "";
                var match = Regex.Match(name, @"\((COM\d+)\)");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }
        return null;
    }

    private static int? ReadCpuTemp(Computer computer)
    {
        foreach (var hardware in computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;
            hardware.Update();

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature)
                {
                    string n = sensor.Name.ToLower();
                    if (n.Contains("package") || n.Contains("average") || n.Contains("core max"))
                    {
                        if (sensor.Value.HasValue) return (int)Math.Round(sensor.Value.Value);
                    }
                }
            }

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                {
                    return (int)Math.Round(sensor.Value.Value);
                }
            }
        }
        return null;
    }
}