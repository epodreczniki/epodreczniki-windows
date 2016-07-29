using epodreczniki.Common;
using epodreczniki.Data;
using epodreczniki.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace epodreczniki
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ReaderPage : Page
    {
        private string _currentUri;
        private string _contentId;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }
        
        private string CurrentUriFromLocalSettings
        {
            get
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("contentUri_" + _contentId + Users.LoggedUserId))
                        return localSettings.Values["contentUri_" + _contentId + Users.LoggedUserId] as String;
                }

                return String.Empty;
            }

            set
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["contentUri_" + _contentId + Users.LoggedUserId] = value;
                }
            }
        }

        public ReaderPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;            
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            try
            {
                if (e.NavigationParameter != null)
                    _contentId = e.NavigationParameter as String;

                if (String.IsNullOrEmpty(_contentId))
                    return;

                StringBuilder path = new StringBuilder();

                _currentUri = CurrentUriFromLocalSettings;
                
                CollectionDataItem item = CollectionsDataSource.Source.Collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();
                if (item == null)
                {
                    var messageDialog = new MessageDialog("Otwarcie żądanego zasobu nie jest możliwe. Przejdź do listy podręczników i spróbuj otworzyć go ponownie.");
                    messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.BackCommandInvokedHandler)));
                    await messageDialog.ShowAsync();
                    return;
                }
                    
                //path.Append("ms-appdata:///local/");
                path.Append("/local/");
                path.Append(CollectionsDataSource.HandbooksFolderName);

                if (!String.IsNullOrEmpty(_currentUri))
                {
                    path.Append(_currentUri);
                }
                else
                {
                    path.Append("/");
                    path.Append(item.ContentId);
                    path.Append("/");
                    path.Append(item.Version);  
                    path.Append("/content/index.html");
                }

                //readerWebView.Navigate(new Uri(path.ToString()));

                Uri url = readerWebView.BuildLocalStreamUri("HandbookRootTag", path.ToString());
                StreamUriWinRTResolver myResolver = new StreamUriWinRTResolver();

                readerWebView.NavigateToLocalStreamUri(url, myResolver);

            }
            catch(Exception)
            {

            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (!String.IsNullOrEmpty(_currentUri))
                CurrentUriFromLocalSettings = _currentUri;
        }
        
        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void BackCommandInvokedHandler(IUICommand command)
        {
            this.Frame.GoBack();
        }

        private async void readerWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs e)
        {
            _currentUri = String.Empty;

            if (!e.IsSuccess)
            {                
                var messageDialog = new MessageDialog("Otwarcie żądanego zasobu nie jest możliwe. Usuń podręcznik i pobierz go ponownie.");
                messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.BackCommandInvokedHandler)));
                await messageDialog.ShowAsync();            
            }
            else if(e.Uri != null && !String.IsNullOrEmpty(e.Uri.AbsolutePath))
            {
                _currentUri = e.Uri.AbsolutePath;                
            }
        }

        private void CanPlayVideoInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is string && !String.IsNullOrEmpty(((string)command.Id)))
            {
                //CollectionsDataSource.Source.CanPlayVideo = true;                
            }
            else
            {
                //CollectionsDataSource.Source.CanPlayVideo = false;
            }
        }
    
        private async void readerWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            // z uwagi na brak obsługi notyfikacji z js do aplikacji
            // przy użyciu metody readerWebView.Navigate, funkcjonalność jest zablokowana
            return;

            try
            {
                string[] arData = null;
                if (!String.IsNullOrEmpty(e.Value))
                    arData = e.Value.Split('|');

                if (arData != null && arData.Length > 1 && arData[0].Equals("CanPlayVideo"))
                {
                    int connection = App.IsConnectedToInternet();

                    if (connection > 0)
                    {
                        if (connection > 1 && !App.Use3GConnection)
                        {
                            var messageDialog = new MessageDialog("Brak połączenia z siecią WiFi. Czy zezwalasz na użycie sieci komórkowej w celu odtworzenia materiału multimedialnego?");
                            messageDialog.Commands.Add(new UICommand("Tak", new UICommandInvokedHandler(CanPlayVideoInvokedHandler), arData[1]));
                            messageDialog.Commands.Add(new UICommand("Nie", null));

                            // Set the command that will be invoked by default
                            messageDialog.DefaultCommandIndex = 0;

                            // Set the command to be invoked when escape is pressed
                            messageDialog.CancelCommandIndex = 1;

                            // Show the message dialog
                            await messageDialog.ShowAsync();

                            if (CollectionsDataSource.Source.CanPlayVideo)
                                await readerWebView.InvokeScriptAsync("PlayVideoWin8", new string[] { arData[1] });
                        }
                        else
                        {
                            if (CollectionsDataSource.Source.CanPlayVideo)
                            {
                                await readerWebView.InvokeScriptAsync("PlayVideoWin8", new string[] { arData[1] });
                            }
                            else
                            {
                                var messageDialog = new MessageDialog("Czy rozpocząć odtwarzanie pliku multimedialnego?");

                                messageDialog.Commands.Add(new UICommand("Anuluj", new UICommandInvokedHandler(CanPlayVideoInvokedHandler), null));
                                messageDialog.Commands.Add(new UICommand("Odtwórz", new UICommandInvokedHandler(CanPlayVideoInvokedHandler), arData[1]));

                                // Set the command that will be invoked by default
                                messageDialog.DefaultCommandIndex = 1;

                                // Set the command to be invoked when escape is pressed
                                messageDialog.CancelCommandIndex = 0;

                                // Show the message dialog
                                await messageDialog.ShowAsync();

                                if (CollectionsDataSource.Source.CanPlayVideo)
                                    await readerWebView.InvokeScriptAsync("PlayVideoWin8", new string[] { arData[1] });
                            }
                        }
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("Brak połączenia z siecią Internet. Odtworzanie pliku mulimedialnego nie jest możliwe.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                    }                    
                }
                    
            }
            catch { }
        }

        private async void readerWebView_LoadCompleted(object sender, NavigationEventArgs e)
        {
            try
            {
                await readerWebView.InvokeScriptAsync("SetIsTeacherWin8", new string[] { Users.IsTeacher.ToString() });
            }
            catch { }
            try
            {
                await readerWebView.InvokeScriptAsync("SetCanPlayVideoWin8", new string[] { CollectionsDataSource.Source.CanPlayVideo.ToString() });
            }
            catch { }
        }        
    }
}
