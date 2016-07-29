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
    public sealed partial class SettingsPage : Page
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


        public SettingsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            this.DefaultViewModel["Accounts"] = Users.GetUsers().Where(usr => usr.IsDefault == false).ToArray<UserDataItem>();
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
            switchAllowUsersToCreateAccounts.IsOn = App.AllowUsersToCreateAccounts;
            switchUse3GConnection.IsOn = App.Use3GConnection;            
        }        

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag != null && ((Button)sender).Tag is Guid)
            {
                UserDataItem user = Users.FindUser((Guid)((Button)sender).Tag);
                if (user != null)
                {
                    var messageDialog = new MessageDialog("Czy chcesz usunąć konto użytkownika: " + user.Name + "?");

                    messageDialog.Commands.Add(new UICommand("Tak", new UICommandInvokedHandler(this.DeleteCommandInvokedHandler), (Guid)((Button)sender).Tag));
                    messageDialog.Commands.Add(new UICommand("Nie", new UICommandInvokedHandler(this.DeleteCommandInvokedHandler), null));

                    // Set the command that will be invoked by default
                    messageDialog.DefaultCommandIndex = 1;

                    // Set the command to be invoked when escape is pressed
                    messageDialog.CancelCommandIndex = 1;

                    // Show the message dialog
                    await messageDialog.ShowAsync();
                }
            }
        }        

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag != null && ((Button)sender).Tag is Guid)
            {
                UserDataItem user = Users.FindUser((Guid)((Button)sender).Tag);
                if (user != null)
                {
                    this.Frame.Navigate(typeof(PasswordRecoveryPage), user);
                }
            }
        }

        private void switchUse3GConnection_Toggled(object sender, RoutedEventArgs e)
        {
            App.Use3GConnection = switchUse3GConnection.IsOn;
        }

        private void switchAllowUsersToCreateAccounts_Toggled(object sender, RoutedEventArgs e)
        {
            App.AllowUsersToCreateAccounts = switchAllowUsersToCreateAccounts.IsOn;
        }

        private async void btnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Users.AnyUserExists)
            {
                this.Frame.Navigate(typeof(NewAccountPage), new NavigationPageParameter(false, "SettingsPage"));
            }
            else
            {
                var messageDialog = new MessageDialog("Pierwsze dodane konto będzie kontem z uprawnieniami administracyjnymi. Jednynie administrator będzie miał dostęp do ustawień programu.");

                messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.AddCommandInvokedHandler), 0));
                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;
                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                await messageDialog.ShowAsync();
            }
        }

        private void AddCommandInvokedHandler(IUICommand command)
        {
            if (command.Id is int && (int)command.Id == 0)
            {
                this.Frame.Navigate(typeof(NewAccountPage), new NavigationPageParameter(true, "SettingsPage"));            
            }
        }

        private async void DeleteCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is Guid)
            {
                if (await Users.DeleteUser((Guid)command.Id))
                {
                    this.DefaultViewModel["Accounts"] = Users.GetUsers();
                }
                else
                {
                    var messageDialog = new MessageDialog("Nie udało się usunąć użytkownika.");
                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                    await messageDialog.ShowAsync();
                }
            }
        }
        
        private async void ResetCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is int && ((int)command.Id) == 1)
            {
                this.ShowProgressIndicator(true, "resetowanie aplikacji w toku...");
                try
                {
                    // usunięcie użytkowników
                    if (await Users.DeleteAllUsers())
                    {
                        // usunięcie podręczników"                                        
                        if (await CollectionsDataSource.ClearHandbooksFolder())
                        {
                            if (await CollectionsDataSource.ClearDownloadsFolder())
                            {
                                if (App.ClearLocalSettings())
                                {
                                    var messageDialog = new MessageDialog("Aplikacja została zresetowana.");
                                    messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.SuccessCommandInvokedHandler), null));
                                    await messageDialog.ShowAsync();
                                }
                                else
                                {
                                    // Usunięcie ustawień lokalnych nie powiodło się. 
                                    var messageDialog = new MessageDialog("Usunięcie ustawień lokalnych nie powiodło się.");
                                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                                    await messageDialog.ShowAsync();
                                }
                            }
                            else
                            {
                                // Usunięcie pobranych plików nie powiodło się. 
                                var messageDialog = new MessageDialog("Usunięcie pobranych plików z podręcznikami nie powiodło się.");
                                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                                await messageDialog.ShowAsync();
                            }
                        }
                        else
                        {
                            // Usunięcie podręczników nie powiodło się. 
                            var messageDialog = new MessageDialog("Usunięcie podręczników nie powiodło się.");
                            messageDialog.Commands.Add(new UICommand("Zamknij", null));
                            await messageDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        // Usunięcie kont użytkowników nie powiodło się. 
                        var messageDialog = new MessageDialog("Usunięcie kont użytkowników nie powiodło się. Operacja resetowania aplikacji została przerwana.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                    }
                }
                catch
                {
                    this.ShowProgressIndicator(false);
                }
            }
        }

        private void SuccessCommandInvokedHandler(IUICommand command)
        {
            this.Frame.Navigate(typeof(PrivacyPolicyPage), null);  
        }

        private void backButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HandbooksListPage), null);  
        }

        private async void AllowToManageHandbooksButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag != null && ((Button)sender).Tag is Guid)
            {
                UserDataItem user = Users.FindUser((Guid)((Button)sender).Tag);
                if (user != null)
                {
                    user.AllowToManageHandbooks = true;
                    
                    if (user.SaveUserDataToSettings())
                    {
                        ((Button)sender).Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                        var button = ((Grid)(((Button)sender).Parent)).Children[3];
                        if (button != null && button is Button)
                        {
                            ((Button)button).Visibility = Windows.UI.Xaml.Visibility.Visible;
                        }
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("Nie udało się zapisać ustawień użytkownika do pliku.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }        

        private async void BlockToManageHandbooksButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag != null && ((Button)sender).Tag is Guid)
            {
                UserDataItem user = Users.FindUser((Guid)((Button)sender).Tag);
                if (user != null)
                {
                    user.AllowToManageHandbooks = false;

                    if (user.SaveUserDataToSettings())
                    {
                        ((Button)sender).Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                        var button = ((Grid)(((Button)sender).Parent)).Children[2];
                        if (button != null && button is Button)
                        {
                            ((Button)button).Visibility = Windows.UI.Xaml.Visibility.Visible;
                        }
                    }
                    else
                    {
                        var messageDialog = new MessageDialog("Nie udało się zapisać ustawień użytkownika do pliku.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        private async void btnResetApplication_Click(object sender, RoutedEventArgs e)
        {
            var messageDialog = new MessageDialog("Czy chcesz zresetować aplikację poprzez usunięcie wszystkich kont użytkowników, wszystkich pobranych podręczników oraz ustawień lokalnych?");

            messageDialog.Commands.Add(new UICommand("Tak", new UICommandInvokedHandler(this.ResetCommandInvokedHandler), 1));
            messageDialog.Commands.Add(new UICommand("Nie", new UICommandInvokedHandler(this.ResetCommandInvokedHandler), 0));

            messageDialog.DefaultCommandIndex = 1;
            messageDialog.CancelCommandIndex = 1;
            
            await messageDialog.ShowAsync();            
        }

        private void ShowProgressIndicator(bool show, string text = "")
        {
            progressTextBlock.Text = text;
            progressIndicator.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            progressTextBlock.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            switchAllowUsersToCreateAccounts.IsEnabled = !show;
            switchUse3GConnection.IsEnabled = !show;
            btnAddAccount.IsEnabled = !show;
            btnResetApplication.IsEnabled = !show;
        }

    }
}
