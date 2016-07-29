using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ePodrecznikiDesktop
{
    /// <summary>
    /// Interaction logic for Terms.xaml
    /// </summary>
    public partial class Terms : Page
    {
        private WebBrowserNotify _webNotify;
        public event TermsDelegate TermsAcceptedDelegate;

        public Terms()
        {
            InitializeComponent();

            if ((bool)Properties.Settings.Default["IsTermsAccepted"])
            {
                ui_TermsAccept.Visibility = System.Windows.Visibility.Collapsed;
                ui_WebBorder.BorderThickness = new Thickness(0, 3, 0, 0);
            }

            _webNotify = new WebBrowserNotify();
            _webNotify.TermsReadedDelegate += TermsReadedDelegate;

            ui_WebBrowser.ObjectForScripting = _webNotify;

            String directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            String myfile = System.IO.Path.Combine(directory, "Terms.html");
            
            ui_WebBrowser.Navigate(new Uri("file:///" + myfile));
        }

        void TermsReadedDelegate()
        {
            ui_TermsAccept.IsEnabled = true;
        }

        private void TermsAccept_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Czy jesteś pewien?", "Potwierdź akceptację", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Properties.Settings.Default["IsTermsAccepted"] = true;
                Properties.Settings.Default.Save();
                TermsAcceptedDelegate();
            }
        }
    }
}
