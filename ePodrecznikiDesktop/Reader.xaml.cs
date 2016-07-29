using ePodrecznikiDesktop.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
    /// Interaction logic for Reader.xaml
    /// </summary>
    public partial class Reader : Page
    {
        // change the UA string
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
        const int URLMON_OPTION_USERAGENT = 0x10000001;

        public void ChangeUserAgent(String Agent)
        {
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, Agent, Agent.Length, 0);
        }

        public event ShowHideTitlebarDelegate ShowHideTitlebarDelegate;

        private CollectionDataItem _handbook;
        private bool _isTitlebarShown = false;

        public CollectionDataItem Handbook
        {
            set
            {                                
                _handbook = value;
            }

            get
            {
                return _handbook;
            }
        }

        public void DisposeBrowser()
        {
            try
            {                
                ui_WebBrowser.Dispose();                
            }
            catch (Exception)
            { }                    
        }
            
        public Reader(int handbookId, int port)
        {
            InitializeComponent();

            dynamic activeX = this.ui_WebBrowser.GetType().InvokeMember("ActiveXInstance",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, this.ui_WebBrowser, new object[] { });

            activeX.Silent = true;

            Handbook = HandbooksListPage.GetCollectionDataItemById(handbookId);
            
            ChangeUserAgent("Mozilla/5.0 (Windows NT 6.3; Win64, x64; Trident/7.0; Touch; rv:11.0) like Gecko");
            
            ui_WebBrowser.ObjectForScripting = new WebBrowserNotify();
            ui_WebBrowser.Navigate(new Uri(string.Format(@"http://localhost:{0}/{1}.html", port, Handbook.ContentId)));
        }

        private void ui_WebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var doc = (mshtml.HTMLDocumentEvents2_Event)ui_WebBrowser.Document;            
            doc.oncontextmenu += Document_OnContextmenu;            
        }

        bool Document_OnContextmenu(mshtml.IHTMLEventObj pEvtObj)
        {
            _isTitlebarShown = !_isTitlebarShown;
            ShowHideTitlebarDelegate(_isTitlebarShown);
            return false;
        }

        private void ui_WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            //SetSilent(ui_WebBrowser, true); // make it silent
        }

    }
}
