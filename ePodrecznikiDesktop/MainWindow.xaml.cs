using ePodrecznikiDesktop.DataModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ePodrecznikiDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int? _handbookId = null;
        HandbooksListPage _pageHandbooksList = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SplashScreen splash = new SplashScreen();
            ui_MainFrame.Content = splash;
            ui_MainFrame.Navigated += MainFrame_Navigated;

            Timer timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            timer.Start();
        }

        void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ui_MainFrame.NavigationService.RemoveBackEntry();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
            {
                if (!(bool)Properties.Settings.Default["IsTermsAccepted"])
                {
                    LoadPage(PageType.Terms, null);
                }
                else
                {
                    LoadPage(PageType.HandbooksList, null);
                }              
            });
        }

        private void LoadPage(PageType pageType, int? handbookId)
        {
            DisposeBrowserInReader();

            switch (pageType)
            {
                case PageType.About:
                    About about = new About();
                    ui_MainFrame.Content = about;
                    ui_Menu.Visibility = Visibility.Visible;
                    ui_ReturnButton.Visibility = Visibility.Visible;
                    break;
                case PageType.Terms:
                    Terms terms = new Terms();
                    terms.TermsAcceptedDelegate += TermsAcceptedDelegate;
                    ui_MainFrame.Content = terms;
                    ui_Menu.Visibility = Visibility.Visible;
                    ui_ReturnButton.Visibility = (bool)Properties.Settings.Default["IsTermsAccepted"] ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case PageType.HandbooksList:
                    if (_pageHandbooksList != null)
                    {
                        _pageHandbooksList.SetSelectedHandbook(handbookId);
                    }
                    else
                    {
                        _pageHandbooksList = new HandbooksListPage(handbookId);
                        _pageHandbooksList.HandbookDelegate += HandbooksList_HandbookDelegate;
                    }
                    ui_MainFrame.Content = _pageHandbooksList;
                    ui_Menu.Visibility = Visibility.Collapsed;
                    break;
                case PageType.HandbookDetails:                    
                    //Details details = new Details(handbookId.GetValueOrDefault(0));
                    //details.HandbookDelegate += HandbooksList_HandbookDelegate;
                    DetailsHtml details = new DetailsHtml(handbookId.GetValueOrDefault(0));
                    ui_MainFrame.Content = details;
                    ui_Menu.Visibility = Visibility.Visible;
                    ui_ReturnButton.Visibility = Visibility.Visible;
                    break;
                case PageType.HandbookRead:
                    String version = (String)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer", "svcVersion", String.Empty);
                    bool isVersionValid = false;
                    if (!String.IsNullOrEmpty(version))
                    {
                        string[] ar = version.Split(new char[] { '.' });
                        if (ar != null && ar.Length > 0)
                        {   
                            int ver = 0;
                            if (Int32.TryParse(ar[0], out ver) && ver >= 10)
                                isVersionValid = true;
                        }
                    }

                    if (isVersionValid)
                    {
                        int port = (Application.Current as App).IIS.Port;
                        Reader reader = new Reader(handbookId.GetValueOrDefault(0), port);
                        reader.ShowHideTitlebarDelegate += reader_ShowHideTitlebarDelegate;
                        ui_MainFrame.Content = reader;
                        ui_Menu.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show("Zawartość podręcznika nie może zostać wyświetlona z uwagi na brak odpowiedniej przeglądarki internetowej.\r\nDo prawidłowego działania aplikacji niezbędne jest zainstalowanie przeglądarki Internet Explorer w wersji 10 lub nowszej.", "Błąd");
                    }
                    
                    break;
                    
                default:
                    break;
            }
        }

        void reader_ShowHideTitlebarDelegate(bool show)
        {
            ui_Menu.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        void TermsAcceptedDelegate()
        {
            LoadPage(PageType.HandbooksList, null);
        }

        void HandbooksList_HandbookDelegate(PageType pageType, int? handbookId)
        {                        
            _handbookId = handbookId;
            LoadPage(pageType, handbookId);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            LoadHandbooksList();
        }

        public void LoadHandbooksList()
        {
            LoadPage(PageType.HandbooksList, _handbookId);
        }


        public void DisposeBrowserInReader()
        {
            if(ui_MainFrame.Content != null && ui_MainFrame.Content is Reader)
            {
                ((Reader)ui_MainFrame.Content).DisposeBrowser();     
            }
        }
    }
}