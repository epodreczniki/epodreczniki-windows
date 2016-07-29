using epodreczniki.Common;
using epodreczniki.Data;
using epodreczniki.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
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
    public sealed partial class LoginPage : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private string _backPage;
        private bool _forceLogin = true;

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


        public LoginPage()
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
            if (e.NavigationParameter != null && e.NavigationParameter is NavigationPageParameter)
            {
                _backPage = ((NavigationPageParameter)e.NavigationParameter).BackPageTypeName;
                _forceLogin = ((NavigationPageParameter)e.NavigationParameter).ShowAllAttributes;
            }           
            else
            {
                _backPage = String.Empty;
                _forceLogin = false;
            }

            this.backButton.Visibility = String.IsNullOrEmpty(_backPage) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            this.btnCreateAccount.Visibility = !_forceLogin ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
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
            try
            { 
                Collection<UserDataItem> users = Users.GetUsers();

                if (users != null)
                {
                    List<UserDataItem> list = new List<UserDataItem>(users);
                    list.Sort(Users.SortAscending());

                    cboxUserName.DataContext = list;
                    cboxUserName.DisplayMemberPath = "Name";

                    UserDataItem selectedUser = Users.GetLastLoggedUser();

                    if (selectedUser != null)
                        cboxUserName.SelectedItem = selectedUser;
                    else
                        cboxUserName.SelectedIndex = 0;

                    lblPassword.Visibility = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                    txtPassword.Visibility = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                    btnLogin.Content = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? "Zaloguj" : "Wejdź";

                    cboxUserName.IsEnabled = !_forceLogin;

                    if(cboxUserName.IsEnabled)
                        cboxUserName.Focus(FocusState.Programmatic);
                    else
                        txtPassword.Focus(FocusState.Programmatic);

                    lblPasswordRecovery.Visibility = Users.LoggedUser == null && selectedUser != null && selectedUser.IsAdmin && selectedUser.IsSecured ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;            
                }

                btnCreateAccount.Visibility = App.AllowUsersToCreateAccounts && !_forceLogin ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
            catch (Exception)
            {

            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            this.Login();
        }

        private async void Login()
        {            
            bool isValid = false;
            lblUserNameMsg.Text = "";
            lblPasswordMsg.Text = "";

            UserDataItem selectedUser = cboxUserName.SelectedItem as UserDataItem;

            if (selectedUser == null)
            {
                lblUserNameMsg.Text = "Wybierz użytkownika";
                cboxUserName.Focus(FocusState.Programmatic);
            }
            else if (!selectedUser.IsSecured)
            {
                isValid = true;
            }
            else if(String.IsNullOrEmpty(selectedUser.Password) || String.IsNullOrEmpty(selectedUser.Salt))
            {
                lblUserNameMsg.Text = "Brak informacji o użytkowniku";
                cboxUserName.Focus(FocusState.Programmatic);   
            }
            else
            {
                isValid = DataDecoder.VerifyPassword(selectedUser.Password, txtPassword.Password, selectedUser.Salt);
                if(!isValid)
                {
                    lblPasswordMsg.Text = "Nieprawidłowe hasło";
                    txtPassword.Focus(FocusState.Programmatic);
                }
            }

            lblUserNameMsg.Visibility = String.IsNullOrEmpty(lblUserNameMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            lblPasswordMsg.Visibility = String.IsNullOrEmpty(lblPasswordMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

            SolidColorBrush normalBrush = new SolidColorBrush(Colors.White);
            SolidColorBrush failBrush = new SolidColorBrush(Color.FromArgb(255, 255, 250, 200));

            cboxUserName.Background = String.IsNullOrEmpty(lblUserNameMsg.Text) ? normalBrush : failBrush;
            txtPassword.Background = String.IsNullOrEmpty(lblPasswordMsg.Text) ? normalBrush : failBrush;

            if (isValid)
            {
                string currentPageTypeName = String.Empty;

                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null)
                {                    
                    currentPageTypeName = localSettings.Values["CurrentSourcePageType"] as String;

                    Users.LoggedUser = (UserDataItem)cboxUserName.SelectedItem;

                    if (String.IsNullOrEmpty(this._backPage))
                        this.Frame.Navigate(typeof(HandbooksListPage), null);
                    else
                        this.Frame.Navigate(Type.GetType("epodreczniki." + this._backPage), null);

                    await UserDataItem.LoadFromFile(Users.LoggedUser.Id, Users.LoggedUser);
                }
            }            
        }

        private void btnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NewAccountPage), new NavigationPageParameter(false, null));
        }

        private void lblPasswordRecovery_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UserDataItem selectedUser = cboxUserName.SelectedItem as UserDataItem;

            if (selectedUser != null)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null)                
                    localSettings.Values["Last_Logged_User"] = selectedUser.Id.ToString();                

                this.Frame.Navigate(typeof(PasswordRecoveryPage), selectedUser);
            }
        }

        private void cboxUserName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserDataItem selectedUser = cboxUserName.SelectedItem as UserDataItem;

            lblPasswordRecovery.Visibility = selectedUser != null && selectedUser.IsAdmin && selectedUser.IsSecured ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            lblPassword.Visibility = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            txtPassword.Visibility = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            btnLogin.Content = (selectedUser == null || selectedUser.IsSecured || _forceLogin) ? "Zaloguj" : "Wejdź";            
        }

        private void pageRoot_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Login();
            }
        }        
    }
}
