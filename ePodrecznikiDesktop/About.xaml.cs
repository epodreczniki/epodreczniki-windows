using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Page
    {
        public About()
        {
            InitializeComponent();

            String directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            String file = System.IO.Path.Combine(directory, "About.html");

            ui_WebBrowser.Navigate(new Uri("file:///" + file));            
        }        
    }
}
