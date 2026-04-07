using Microsoft.UI.Xaml;

namespace WinUI3_Serial_Port_Communication
{
    public partial class App : Application
    {
        #region Fields

        private Window m_window;

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
            m_window = new MainWindow();
            m_window.Activate();
        }

        #endregion
    }
}
