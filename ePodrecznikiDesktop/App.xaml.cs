using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace ePodrecznikiDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IISExpress IIS = null;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                IIS = IISExpress.Start(Configuration.Current.ContentFolder, Configuration.Current.Port);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       

        private void Application_Exit(object sender, ExitEventArgs e)
        {
        
            if(IIS != null)
            {
                IIS.Stop();
            }
        }
    }
}
