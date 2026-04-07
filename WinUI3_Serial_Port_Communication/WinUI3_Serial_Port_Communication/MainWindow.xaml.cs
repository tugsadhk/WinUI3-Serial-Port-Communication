using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;

namespace WinUI3_Serial_Port_Communication
{
    public sealed partial class MainWindow : Window
    {
        #region Fields

        private readonly SerialPortService _service = new();

        // Settings (written from UI thread, read from DataReceived thread → volatile)
        private volatile bool _timestampEnabled = false;
        private volatile bool _hexViewEnabled   = false;
        private volatile bool _autoScroll       = true;

        private string            _lineEnding = "\r\n";
        private volatile Encoding _encoding   = Encoding.UTF8;
        private int      _txBytes    = 0;
        private int      _rxBytes    = 0;
        private int      _errorCount = 0;

        // Send history
        private readonly List<string> _sendHistory = new();
        private int    _historyIndex = -1;
        private string _historyDraft = string.Empty;
        private const int MaxHistorySize = 50;

        // Receive buffer limit (line count)
        private const int MaxReceiveLines = 500;

        private enum ConnectionState { Disconnected, Connected, Error }

        #endregion

        #region Initialization

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title  = "Serial Communication  ·  WinUI 3";
            this.Closed += MainWindow_Closed;

            // Mica backdrop (Win 11), graceful fallback to Acrylic (Win 10)
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            else
                this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();

            // Wire service events
            _service.DataReceived  += Service_DataReceived;
            _service.ErrorOccurred += Service_ErrorOccurred;
            _service.ConnectionLost += Service_ConnectionLost;

            FindConnectedPorts();
            LoadSettings();
        }

        #endregion

        #region Window Events

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            SaveSettings();
            _service.Dispose();
        }

        #endregion

        #region Serial Port – Connect / Disconnect

        private async void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Port_CmbBox.SelectedIndex == -1 || BaudR_CmbBox.SelectedIndex == -1)
            {
                await new ContentDialog
                {
                    Title         = "Missing Configuration",
                    Content       = "Please select a port and baud rate.",
                    CloseButtonText = "OK",
                    XamlRoot      = this.Content.XamlRoot
                }.ShowAsync();
                return;
            }

            var cfg = new SerialPortConfig
            {
                PortName  = Port_CmbBox.SelectedItem.ToString(),
                BaudRate  = int.Parse(((ComboBoxItem)BaudR_CmbBox.SelectedItem).Content.ToString()),
                DataBits  = DataBits_CmbBox.SelectedIndex >= 0
                                ? int.Parse(((ComboBoxItem)DataBits_CmbBox.SelectedItem).Content.ToString())
                                : 8,
                StopBits  = StopBits_CmbBox.SelectedIndex switch
                {
                    1     => StopBits.OnePointFive,
                    2     => StopBits.Two,
                    _     => StopBits.One
                },
                Parity    = Parity_CmbBox.SelectedIndex >= 0 && Parity_CmbBox.SelectedItem is ComboBoxItem pi
                                ? Enum.TryParse(pi.Content.ToString(), out Parity p) ? p : Parity.None
                                : Parity.None,
                DtrEnable = Dtr_ChcBox.IsChecked == true,
                RtsEnable = Rts_ChcBox.IsChecked == true
            };

            try
            {
                _service.Connect(cfg);
                SetConnectionState(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                // Connect failed — go back to Disconnected (not Error, which is reserved
                // for unexpected disconnections while the port was already open).
                SetConnectionState(ConnectionState.Disconnected);
                LogError($"Failed to connect: {ex.Message}");
            }
        }

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
            _service.Disconnect();
            SetConnectionState(ConnectionState.Disconnected);
        }

        private void FindConnectedPorts()
        {
            string selected = Port_CmbBox.SelectedItem?.ToString();
            Port_CmbBox.Items.Clear();
            foreach (string port in SerialPort.GetPortNames())
                Port_CmbBox.Items.Add(port);

            // Restore previous selection when possible
            if (selected != null)
                for (int i = 0; i < Port_CmbBox.Items.Count; i++)
                    if (Port_CmbBox.Items[i].ToString() == selected)
                    { Port_CmbBox.SelectedIndex = i; break; }
        }

        #endregion

        #region Serial Port – Send / Receive

        private async void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_service.IsConnected)
            {
                await new ContentDialog
                {
                    Title           = "Not Connected",
                    Content         = "Open the serial port first.",
                    CloseButtonText = "OK",
                    XamlRoot        = this.Content.XamlRoot
                }.ShowAsync();
                return;
            }

            string text = Send_TxtBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                byte[] data = _encoding.GetBytes(text + _lineEnding);
                _service.Write(data);
                _txBytes += data.Length;

                AddToHistory(text);
                Send_TxtBox.Text = string.Empty;
                TxBytes_Txt.Text = $"TX: {SerialTerminalHelpers.FormatBytes(_txBytes)}";
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private void Service_DataReceived(object sender, byte[] buffer)
        {
            bool   hex       = _hexViewEnabled;
            bool   ts        = _timestampEnabled;
            bool   scroll    = _autoScroll;
            int    len       = buffer.Length;

            string text = hex
                ? BitConverter.ToString(buffer).Replace("-", " ")
                : _encoding.GetString(buffer);

            string prefix = ts ? $"[{DateTime.Now:HH:mm:ss.fff}] " : string.Empty;

            DispatcherQueue.TryEnqueue(() =>
            {
                _rxBytes += len;

                string current = Receive_TxtBox.Text;
                string appended = current + prefix + text;
                Receive_TxtBox.Text = SerialTerminalHelpers.TrimToMaxLines(appended, MaxReceiveLines);

                if (scroll)
                    Receive_TxtBox.SelectionStart = Receive_TxtBox.Text.Length;

                RxBytes_Txt.Text = $"RX: {SerialTerminalHelpers.FormatBytes(_rxBytes)}";
            });
        }

        private void Service_ErrorOccurred(object sender, string message)
        {
            LogError(message);
        }

        private void Service_ConnectionLost(object sender, EventArgs e)
        {
            // Disconnect first (thread-safe) so the port is fully closed before
            // the user tries to reconnect; otherwise Connect() would throw "Already connected".
            _service.Disconnect();

            DispatcherQueue.TryEnqueue(() =>
            {
                FindConnectedPorts(); // device may have been removed; refresh the list
                SetConnectionState(ConnectionState.Error);
                LogError("Connection lost – device may have been disconnected.");
            });
        }

        #endregion

        #region Send History

        private void AddToHistory(string text)
        {
            // Avoid consecutive duplicates
            if (_sendHistory.Count == 0 || _sendHistory[^1] != text)
                _sendHistory.Add(text);

            if (_sendHistory.Count > MaxHistorySize)
                _sendHistory.RemoveAt(0);

            _historyIndex = -1;
            _historyDraft = string.Empty;
        }

        private void NavigateHistory(int direction)
        {
            if (_sendHistory.Count == 0) return;

            if (direction == -1) // older
            {
                if (_historyIndex == -1)
                {
                    _historyDraft = Send_TxtBox.Text;
                    _historyIndex = _sendHistory.Count - 1;
                }
                else if (_historyIndex > 0)
                {
                    _historyIndex--;
                }
                Send_TxtBox.Text = _sendHistory[_historyIndex];
            }
            else // newer
            {
                if (_historyIndex == -1) return;

                if (_historyIndex < _sendHistory.Count - 1)
                {
                    _historyIndex++;
                    Send_TxtBox.Text = _sendHistory[_historyIndex];
                }
                else
                {
                    _historyIndex    = -1;
                    Send_TxtBox.Text = _historyDraft;
                }
            }

            // Move cursor to end
            Send_TxtBox.SelectionStart = Send_TxtBox.Text.Length;
        }

        #endregion

        #region UI Events

        private void Refresh_Btn_Click(object sender, RoutedEventArgs e)
            => FindConnectedPorts();

        private void Encoding_CmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _encoding = Encoding_CmbBox.SelectedIndex switch
            {
                1     => Encoding.ASCII,
                2     => Encoding.Latin1,
                _     => Encoding.UTF8
            };
        }

        private void LineEnding_CmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _lineEnding = LineEnding_CmbBox.SelectedIndex switch
            {
                0     => "",
                1     => "\r",
                2     => "\n",
                3     => "\r\n",
                _     => "\r\n"
            };
        }

        private void AutoScroll_Btn_Click(object sender, RoutedEventArgs e)
            => _autoScroll = AutoScroll_Btn.IsChecked == true;

        private void Timestamp_ChcBox_Click(object sender, RoutedEventArgs e)
            => _timestampEnabled = Timestamp_ChcBox.IsChecked == true;

        private void HexView_ChcBox_Click(object sender, RoutedEventArgs e)
        {
            _hexViewEnabled = HexView_ChcBox.IsChecked == true;
            // Clear the buffer so old text (in the previous format) doesn't mix with new data.
            Receive_TxtBox.Text = string.Empty;
        }

        private void ClearReceive_Btn_Click(object sender, RoutedEventArgs e)
        {
            Receive_TxtBox.Text = string.Empty;
            _rxBytes            = 0;
            RxBytes_Txt.Text    = "RX: 0 B";
        }

        private void ClearError_Btn_Click(object sender, RoutedEventArgs e)
        {
            Error_TxtBox.Text          = string.Empty;
            _errorCount                = 0;
            ErrorCount_Badge.Visibility = Visibility.Collapsed;
        }

        private async void SaveReceive_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Receive_TxtBox.Text)) return;

            try
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });
                picker.SuggestedFileName = $"received_{DateTime.Now:yyyyMMdd_HHmmss}";

                WinRT.Interop.InitializeWithWindow.Initialize(picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(this));

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                    await Windows.Storage.FileIO.WriteTextAsync(file, Receive_TxtBox.Text);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private void Send_TxtBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                {
                    bool shift = (Microsoft.UI.Input.InputKeyboardSource
                        .GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0;

                    if (shift)
                    {
                        // Insert newline at cursor position
                        int pos = Send_TxtBox.SelectionStart;
                        Send_TxtBox.Text = Send_TxtBox.Text.Insert(pos, "\n");
                        Send_TxtBox.SelectionStart = pos + 1;
                    }
                    else
                    {
                        Send_Button_Click(sender, e);
                    }
                    e.Handled = true;
                    break;
                }
                case VirtualKey.Up:
                    NavigateHistory(-1);
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    NavigateHistory(1);
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Connection State

        private void SetConnectionState(ConnectionState state)
        {
            bool connected    = state == ConnectionState.Connected;
            bool notConnected = !connected;

            Connect_Btn.IsEnabled      = notConnected;
            Disconnect_Btn.IsEnabled   = connected;
            Send_Btn.IsEnabled         = connected;
            Port_CmbBox.IsEnabled      = notConnected;
            Refresh_Btn.IsEnabled      = notConnected;
            BaudR_CmbBox.IsEnabled     = notConnected;
            DataBits_CmbBox.IsEnabled  = notConnected;
            StopBits_CmbBox.IsEnabled  = notConnected;
            Parity_CmbBox.IsEnabled    = notConnected;
            Encoding_CmbBox.IsEnabled  = notConnected;
            Dtr_ChcBox.IsEnabled       = notConnected;
            Rts_ChcBox.IsEnabled       = notConnected;

            Status_Txt.Text = state switch
            {
                ConnectionState.Connected    => "Connected",
                ConnectionState.Error        => "Error",
                _                            => "Disconnected"
            };

            string brushKey = state switch
            {
                ConnectionState.Connected => "SystemFillColorSuccessBrush",
                ConnectionState.Error     => "SystemFillColorCriticalBrush",
                _                         => "SystemFillColorNeutralBrush"
            };

            if (Application.Current.Resources.TryGetValue(brushKey, out object brush))
                Status_Dot.Fill = (SolidColorBrush)brush;
        }

        #endregion

        #region Settings Persistence

        private void SaveSettings()
        {
            var s = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            s["Port"]        = Port_CmbBox.SelectedItem?.ToString() ?? string.Empty;
            s["BaudRate"]    = BaudR_CmbBox.SelectedIndex;
            s["DataBits"]    = DataBits_CmbBox.SelectedIndex;
            s["StopBits"]    = StopBits_CmbBox.SelectedIndex;
            s["Parity"]      = Parity_CmbBox.SelectedIndex;
            s["Encoding"]    = Encoding_CmbBox.SelectedIndex;
            s["LineEnding"]  = LineEnding_CmbBox.SelectedIndex;
            s["DtrEnable"]   = Dtr_ChcBox.IsChecked == true;
            s["RtsEnable"]   = Rts_ChcBox.IsChecked == true;
            s["Timestamp"]   = Timestamp_ChcBox.IsChecked == true;
            s["HexView"]     = HexView_ChcBox.IsChecked == true;
            s["AutoScroll"]  = AutoScroll_Btn.IsChecked == true;
        }

        private void LoadSettings()
        {
            var s = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

            // Restore port selection after populating the list
            if (s.TryGetValue("Port", out object port) && port is string portName)
                for (int i = 0; i < Port_CmbBox.Items.Count; i++)
                    if (Port_CmbBox.Items[i].ToString() == portName)
                    { Port_CmbBox.SelectedIndex = i; break; }

            if (s.TryGetValue("BaudRate",   out object br)  && br  is int brIdx)  BaudR_CmbBox.SelectedIndex    = brIdx;
            if (s.TryGetValue("DataBits",   out object db)  && db  is int dbIdx)  DataBits_CmbBox.SelectedIndex  = dbIdx;
            if (s.TryGetValue("StopBits",   out object sb)  && sb  is int sbIdx)  StopBits_CmbBox.SelectedIndex  = sbIdx;
            if (s.TryGetValue("Parity",     out object pa)  && pa  is int paIdx)  Parity_CmbBox.SelectedIndex    = paIdx;
            if (s.TryGetValue("Encoding",   out object enc) && enc is int encIdx) Encoding_CmbBox.SelectedIndex  = encIdx;
            if (s.TryGetValue("LineEnding", out object le)  && le  is int leIdx)  LineEnding_CmbBox.SelectedIndex = leIdx;

            if (s.TryGetValue("DtrEnable",  out object dtr) && dtr is bool dtrVal) Dtr_ChcBox.IsChecked = dtrVal;
            if (s.TryGetValue("RtsEnable",  out object rts) && rts is bool rtsVal) Rts_ChcBox.IsChecked = rtsVal;
            if (s.TryGetValue("Timestamp",  out object ts)  && ts  is bool tsVal)  { Timestamp_ChcBox.IsChecked = tsVal; _timestampEnabled = tsVal; }
            if (s.TryGetValue("HexView",    out object hv)  && hv  is bool hvVal)  { HexView_ChcBox.IsChecked   = hvVal; _hexViewEnabled   = hvVal; }
            if (s.TryGetValue("AutoScroll", out object au)  && au  is bool auVal)  { AutoScroll_Btn.IsChecked   = auVal; _autoScroll       = auVal; }
        }

        #endregion

        #region Helpers

        private void LogError(string message)
        {
            // Capture timestamp on the calling thread (may be a background thread).
            // _errorCount is incremented inside TryEnqueue so it stays on the UI thread,
            // avoiding a data race with the read that follows immediately after.
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            DispatcherQueue.TryEnqueue(() =>
            {
                _errorCount++;
                Error_TxtBox.Text          += $"[{timestamp}] {message}\n";
                ErrorCount_Txt.Text         = _errorCount.ToString();
                ErrorCount_Badge.Visibility = Visibility.Visible;
            });
        }

        #endregion
    }
}
