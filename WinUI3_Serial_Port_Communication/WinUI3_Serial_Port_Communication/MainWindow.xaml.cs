using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Numerics;
using System.Text;
using System.Threading;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI3_Serial_Port_Communication
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool connectionStarted = false;
        private SerialPort _serialPort;
        private int baudRate = 0;
        private int dataBits = 0;
        private string parity;

        public MainWindow()
        {
            this.InitializeComponent();

            findConnectedPorts();
            //Debug.WriteLine("Height  " + this.Bounds.Height + "   width " + this.Bounds.Width);

            Main_Canvas.Height = this.Bounds.Height;
            Main_Canvas.Width = this.Bounds.Width;
            Example1Grid.Width = this.Bounds.Width;
            Example1Grid.Height = this.Bounds.Height;

            this.SizeChanged += MainWindow_SizeChanged;

            //this.ExtendsContentIntoTitleBar = true;
            this.Title = "Serial Communication WinUI 3";

        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            //Debug.WriteLine("Size Changing");
            Main_Canvas.Height = this.Bounds.Height;
            Main_Canvas.Width = this.Bounds.Width;
            Example1Grid.Width = this.Bounds.Width;
            Example1Grid.Height = this.Bounds.Height;


            Send_TxtBox.VerticalAlignment = VerticalAlignment.Center;
            Send_TxtBox.HorizontalAlignment = HorizontalAlignment.Center;



        }

        private void findConnectedPorts()
        {
            Port_CmbBox.Items.Clear();

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                Port_CmbBox.Items.Add(port);
            }
        }



        private void SerialPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                string itemName = e.AddedItems[0].ToString();
                //Debug.WriteLine("Item Name  " + itemName);
            }
            catch (Exception ex)
            {
            }

        }


        //Compositor _compositor = App.CurrentWindow.Compositor;
        SpringVector3NaturalMotionAnimation _springAnimation;

        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = this.Compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";
            }

            _springAnimation.FinalValue = new Vector3(finalValue);
        }



        private void element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Button button = sender as Button;
            // Scale up to 1.5
            switch (button.Name)
            {
                case "Connect_Btn":
                    CreateOrUpdateSpringAnimation(1.3f);
                    break;
                case "Disconnect_Btn":
                    CreateOrUpdateSpringAnimation(0.85f);
                    break;
            }


            (sender as UIElement).StartAnimation(_springAnimation);

        }

        private void element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).StartAnimation(_springAnimation);

        }

        private void Refresh_Btn_Click(object sender, RoutedEventArgs e)
        {
            findConnectedPorts();
        }


        private async void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Port_CmbBox.SelectedIndex == -1 || baudRate == 0)
            {
                var cd = new ContentDialog
                {
                    Title = "Mising Info",
                    Content = "Check Serial Port Info Section",
                    CloseButtonText = "Ok"
                };

                cd.XamlRoot = this.Content.XamlRoot;
                var result = await cd.ShowAsync();

            }
            else
            {
                ////Start Connection
                if (!connectionStarted)
                {

                    _serialPort = new SerialPort();
                    _serialPort.PortName = Port_CmbBox.SelectedItem.ToString();
                    _serialPort.BaudRate = baudRate;
                    _serialPort.Parity = Parity.None;
                    _serialPort.DataBits = 8;

                    if ((bool)Dtr_ChcBox.IsChecked)
                    {
                        _serialPort.DtrEnable = true;
                    }
                    else
                    {
                        _serialPort.DtrEnable = false;
                    }

                    if ((bool)Rts_ChcBox.IsChecked)
                    {
                        _serialPort.RtsEnable = true;
                    }
                    else
                    {
                        _serialPort.RtsEnable = false;
                    }

                    if (dataBits != 0)
                    {
                        _serialPort.DataBits = dataBits;
                    }

                    try
                    {
                        _serialPort.Open();
                    }
                    catch (Exception ex)
                    {

                        Connection_ProgressBar.IsIndeterminate = true;
                        Connection_ProgressBar.ShowError = true;
                        logError(ex.Message);
                    }

                    Thread.Sleep(30);

                    if (_serialPort.IsOpen)
                    {
                        connectionStarted = true;

                        Connection_ProgressBar.ShowError = false;
                        Connection_ProgressBar.IsIndeterminate = false;
                        Connection_ProgressBar.Value = 100;

                    }
                    else
                    {
                        Connection_ProgressBar.IsIndeterminate = true;
                        Connection_ProgressBar.IsIndeterminate = true;
                        Connection_ProgressBar.ShowError = true;
                    }


                    _serialPort.DataReceived += _serialPort_DataReceived;
                }
            }
        }

        private int txtBoxCounter = 0;
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {


            SerialPort spL = (SerialPort)sender;
            byte[] buf = new byte[spL.BytesToRead];
            spL.Read(buf, 0, buf.Length);
            foreach (Byte b in buf)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    Receive_TxtBox.Text += (char)b;

                    if (!Connection_ProgressBar.IsIndeterminate)
                    {
                        Connection_ProgressBar.IsIndeterminate = true;
                    }
                });
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                Receive_TxtBox.Text += "\n";
                txtBoxCounter++;
                if (txtBoxCounter == 10)
                {
                    txtBoxCounter = 0;
                    Receive_TxtBox.Text = "";
                }
            });

            //switch (e.EventType)
            //{
            //    case SerialData.Chars:
            //        break;
            //    case SerialData.Eof:
            //        break;
            //    default:
            //        break;
            //}
            GC.Collect();
        }

        private void Disconnect_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                connectionStarted = false;

            }

            Connection_ProgressBar.IsIndeterminate = false;
            Connection_ProgressBar.Value = 0;
            GC.Collect();
        }

        private async void Send_Button_Click(object sender, RoutedEventArgs e)
        {

            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.WriteLine(Send_TxtBox.Text);
            }
            else
            {
                var cd = new ContentDialog
                {
                    Title = "Fail",
                    Content = "You need to open serial port first!",
                    CloseButtonText = "Ok"
                };

                cd.XamlRoot = this.Content.XamlRoot;
                var result = await cd.ShowAsync();
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                Send_TxtBox.Text = "";
            });

            GC.Collect();
        }

        private void BaudR_CmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var container = sender as ComboBox;
            var selected = container.SelectedItem as ComboBoxItem;
            baudRate = Int32.Parse(selected.Content.ToString());
        }

        private void Data_Bits_CmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var container = sender as ComboBox;
            var selected = container.SelectedItem as ComboBoxItem;
            dataBits = Int32.Parse(selected.Content.ToString());
        }

        private void Parity_CmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var container = sender as ComboBox;
            var selected = container.SelectedItem as ComboBoxItem;
            parity = selected.Content.ToString();
        }



        private int errorLogCounter = 0;
        private void logError(string errorMessage)
        {
            errorLogCounter++;

            if (errorLogCounter == 7)
            {
                errorLogCounter = 0;
                DispatcherQueue.TryEnqueue(() =>
                {
                    Error_TxtBox.Text = "";
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    Error_TxtBox.Text += errorMessage + "\n";
                });
            }


        }



    }
}
