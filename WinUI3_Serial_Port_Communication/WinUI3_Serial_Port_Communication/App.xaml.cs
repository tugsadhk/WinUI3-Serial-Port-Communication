using Microsoft.UI.Xaml;

namespace WinUI3_Serial_Port_Communication
{
    public partial class App : Application
    {
        #region Fields

        private Window _window;

        #endregion

        #region Initialization

        public App()
        {
            this.InitializeComponent();
        }

        #endregion

        #region Application Events

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        #endregion
    }
}
