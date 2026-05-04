using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;

namespace WinUI3_Serial_Port_Communication
{
    internal sealed class PortItem
    {
        public string PortName     { get; init; }
        public string FriendlyName { get; init; }

        public override string ToString()
            => FriendlyName.Length > 0 ? $"{PortName}  —  {FriendlyName}" : PortName;
    }

    internal static class SerialPortDetector
    {
        private static readonly Regex _comPattern =
            new(@"\(COM(\d+)\)$", RegexOptions.Compiled);

        public static IReadOnlyList<PortItem> GetPorts()
        {
            var friendly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%)'");

                foreach (ManagementObject obj in searcher.Get())
                using (obj)
                {
                    string caption = obj["Caption"]?.ToString() ?? string.Empty;
                    var m = _comPattern.Match(caption);
                    if (!m.Success) continue;

                    string comName = $"COM{m.Groups[1].Value}";
                    string fname   = caption[..caption.LastIndexOf('(')].Trim();
                    friendly[comName] = fname;
                }
            }
            catch { /* WMI kullanılamıyor — salt port adıyla devam et */ }

            var ports = SerialPort.GetPortNames();
            Array.Sort(ports, (a, b) =>
            {
                int na = int.TryParse(a.AsSpan(3), out int x) ? x : 0;
                int nb = int.TryParse(b.AsSpan(3), out int y) ? y : 0;
                return na.CompareTo(nb);
            });

            var result = new List<PortItem>();
            foreach (string port in ports)
            {
                friendly.TryGetValue(port, out string fname);
                result.Add(new PortItem { PortName = port, FriendlyName = fname ?? string.Empty });
            }
            return result;
        }

        // Cihaz adına göre en olası baud rate'i tahmin eder.
        // Data bits / stop bits / parity hemen her zaman 8-N-1 olduğundan bunları önermiyoruz.
        public static int SuggestBaudRate(string friendlyName)
        {
            string n = friendlyName.ToLowerInvariant();

            if (ContainsAny(n, "gps", "u-blox", "nmea", "sim28", "neo-6", "neo-7", "neo-8", "neo-m"))
                return 9600;

            if (ContainsAny(n, "arduino uno", "arduino mega", "arduino nano",
                               "arduino leonardo", "arduino micro"))
                return 9600;

            if (ContainsAny(n, "arduino", "esp", "cp210", "ch340", "ch341",
                               "ft232", "ft231", "ft2232", "prolific", "pl2303",
                               "silicon labs", "xr21"))
                return 115200;

            return 115200;
        }

        private static bool ContainsAny(string source, params string[] terms)
        {
            foreach (string t in terms)
                if (source.Contains(t)) return true;
            return false;
        }
    }
}
