#nullable enable
using System;
using System.IO;
using System.IO.Ports;

namespace WinUI3_Serial_Port_Communication
{
    public sealed class SerialPortConfig
    {
        public required string   PortName        { get; init; }
        public required int      BaudRate        { get; init; }
        public int               DataBits        { get; init; } = 8;
        public StopBits          StopBits        { get; init; } = StopBits.One;
        public Parity            Parity          { get; init; } = Parity.None;
        public bool              DtrEnable       { get; init; } = true;
        public bool              RtsEnable       { get; init; } = true;
        public int               ReadTimeoutMs   { get; init; } = 500;
        public int               WriteTimeoutMs  { get; init; } = 500;
    }

    public sealed class SerialPortService : IDisposable
    {
        private SerialPort? _port;
        private readonly object _lock = new();

        public bool IsConnected { get { lock (_lock) { return _port?.IsOpen == true; } } }

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler?         ConnectionLost;

        // ------------------------------------------------------------------ //
        //  Connect / Disconnect                                               //
        // ------------------------------------------------------------------ //

        public void Connect(SerialPortConfig cfg)
        {
            lock (_lock)
            {
                if (_port?.IsOpen == true)
                    throw new InvalidOperationException("Already connected. Call Disconnect first.");

                _port = new SerialPort
                {
                    PortName     = cfg.PortName,
                    BaudRate     = cfg.BaudRate,
                    DataBits     = cfg.DataBits,
                    StopBits     = cfg.StopBits,
                    Parity       = cfg.Parity,
                    DtrEnable    = cfg.DtrEnable,
                    RtsEnable    = cfg.RtsEnable,
                    ReadTimeout  = cfg.ReadTimeoutMs,
                    WriteTimeout = cfg.WriteTimeoutMs
                };

                _port.DataReceived  += Port_DataReceived;
                _port.ErrorReceived += Port_ErrorReceived;
                try
                {
                    _port.Open();
                    _port.DiscardInBuffer();
                }
                catch
                {
                    // Open failed — clean up so the next Connect() doesn't leak this instance
                    _port.DataReceived  -= Port_DataReceived;
                    _port.ErrorReceived -= Port_ErrorReceived;
                    _port.Dispose();
                    _port = null;
                    throw;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_port is null) return;
                _port.DataReceived  -= Port_DataReceived;
                _port.ErrorReceived -= Port_ErrorReceived;
                try { _port.Dispose(); } catch { }
                _port = null;
            }
        }

        // ------------------------------------------------------------------ //
        //  Write                                                              //
        // ------------------------------------------------------------------ //

        public void Write(byte[] data)
        {
            lock (_lock)
            {
                if (_port?.IsOpen != true)
                    throw new InvalidOperationException("Port is not open.");
                _port.Write(data, 0, data.Length);
            }
        }

        // ------------------------------------------------------------------ //
        //  Private handlers                                                   //
        // ------------------------------------------------------------------ //

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort? port;
                lock (_lock) { port = _port; }
                if (port?.IsOpen != true) return;

                int count = port.BytesToRead;
                if (count <= 0) return;

                byte[] buf  = new byte[count];
                int    read = port.Read(buf, 0, count);
                if (read <= 0) return;

                if (read < count) Array.Resize(ref buf, read);
                DataReceived?.Invoke(this, buf);
            }
            catch (IOException ex)
            {
                // Device physically disconnected or cable pulled
                ErrorOccurred?.Invoke(this, ex.Message);
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                // Port closed unexpectedly (e.g., driver removed)
                ErrorOccurred?.Invoke(this, ex.Message);
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Other errors (timeout, encoding) — report but don't signal ConnectionLost
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorOccurred?.Invoke(this, $"Serial error: {e.EventType}");
        }

        // ------------------------------------------------------------------ //
        //  Dispose                                                            //
        // ------------------------------------------------------------------ //

        public void Dispose()
        {
            Disconnect(); // unsubscribes, disposes, and nulls _port
        }
    }
}
