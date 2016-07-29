using epodreczniki.Common;
using epodreczniki.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class OfflineInfoPage : Page
    {

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


        public OfflineInfoPage()
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
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            txtMessage1.Text = "Nie udało się uzyskać dostępu do sieci.";
            txtMessage2.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            txtMessage4.Text = "Połącz się z siecią Internet i spróbuj ponownie.";
            txtMessage5.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            int connection = App.IsConnectedToInternet();

            if (connection > 0)
            {

                if (connection > 1 && !App.Use3GConnection)
                {
                    txtMessage1.Text = "Nie udało się połączyć z siecią WiFi.";
                    txtMessage2.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    txtMessage4.Text = "Połącz się z siecią Internet poprzez WiFi";
                    txtMessage5.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }                
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
        }

        private void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage), null);
        }

        private void PrivacyPolicy_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivacyPolicyPage), false);
        }

        private void ConfigButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConfigPage), null);
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

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            CheckCollectionsAvailability();
        }

        private void RefreshListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CheckCollectionsAvailability();
        }

        private async void CheckCollectionsAvailability()
        {
            IEnumerable<CollectionDataItem> collections = await CollectionsDataSource.GetCollectionsAsync();

            // jeśli są już pobrane jakieś podręczniki:
            if (collections != null && collections.Count() > 0)
            {
                this.Frame.Navigate(typeof(HandbooksListPage), null);
            }
            else
            {
                // sprawdzenie czy jest dostęp do sieci...
                int connection = App.IsConnectedToInternet();

                // jeśli jest połączenie WiFi lub jest połączenie 3G i jest na nie zgoda
                if (connection > 0 && (connection < 2 || App.Use3GConnection))
                {
                    this.Frame.Navigate(typeof(HandbooksListPage), null);
                }
            }
        }
    }
}
