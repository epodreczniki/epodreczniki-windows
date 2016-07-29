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
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class PasswordRecoveryPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private UserDataItem _user; 

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


        public PasswordRecoveryPage()
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
            try
            {
                if (e.NavigationParameter != null)
                    _user = e.NavigationParameter as UserDataItem;                                

                // dla niezalogowanego użytkownika, 
                if (Users.LoggedUser == null)
                {
                    lblUserName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    txtUserName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    // można zmienić tylko hasło administratora, z podaniem odpowiedzi na pytanie:
                    if (_user != null && _user.IsAdmin)
                    {
                        lblRecoveryQuestion.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        txtRecoveryQuestion.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        txtRecoveryQuestion.Text = _user.RecoveryQuestion;

                        lblRecoveryAnswer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        txtRecoveryAnswer.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                }
                // dla użytkownika zalogowanego
                else
                {
                    lblUserName.Visibility = _user != null && Users.LoggedUser.IsAdmin ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                    txtUserName.Visibility = _user != null && Users.LoggedUser.IsAdmin ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

                    if(_user != null)
                    {
                        txtUserName.Text = _user.Name;
                    }

                    lblRecoveryQuestion.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    txtRecoveryQuestion.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    lblRecoveryAnswer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    txtRecoveryAnswer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                
            }
            catch (Exception)
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
            txtRecoveryQuestion.Focus(FocusState.Programmatic);            
        }

        private void btnSetPassword_Click(object sender, RoutedEventArgs e)
        {
            this.SetPassword();
        }

        private async void SetPassword()
        {
            bool result = false;
            bool isValid = true;

            try
            {
                lblRecoveryQuestionMsg.Text = "";
                lblRecoveryAnswerMsg.Text = "";
                lblPasswordMsg.Text = "";
                lblRepeatPasswordMsg.Text = "";

                if (Users.LoggedUser == null && _user != null && _user.IsAdmin)
                {
                    if (String.IsNullOrEmpty(txtRecoveryAnswer.Text))
                    {
                        lblRecoveryAnswerMsg.Text = "Podanie odpowiedzi jest wymagane";
                        txtRecoveryAnswer.Focus(FocusState.Programmatic);
                        isValid = false;
                    }

                    if (isValid)
                    {
                        isValid = DataDecoder.VerifyPassword(_user.RecoveryAnswer, txtRecoveryAnswer.Text, _user.Salt);
                        if (!isValid)
                        {
                            lblRecoveryAnswerMsg.Text = "Nieprawidłowa odpowiedź";
                            txtRecoveryAnswer.Focus(FocusState.Programmatic);
                        }
                    }
                }

                if (isValid)
                {
                    if (String.IsNullOrEmpty(txtPassword.Password))
                    {
                        lblPasswordMsg.Text = "Podanie hasła użytkownika jest wymagane";
                        txtPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                    else if (txtPassword.Password.Length < 6)
                    {

                        lblPasswordMsg.Text = "Podane hasło jest zbyt krótkie: musi posiadać przynajmniej 6 znaków";
                        txtPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }

                    if (String.IsNullOrEmpty(txtRepeatPassword.Password))
                    {
                        lblRepeatPasswordMsg.Text = "Podanie powtórzenia hasła jest wymagane";
                        txtRepeatPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                    else if (isValid && !txtPassword.Password.Equals(txtRepeatPassword.Password))
                    {
                        lblRepeatPasswordMsg.Text = "Podane hasło i jego powtórzenie muszą być takie same";
                        txtRepeatPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    // jeśli nie wskazano użytkownika, któremu ma zostać zmienione hasło
                    if (_user == null)
                    {
                        lblRepeatPasswordMsg.Text = "Zmiana hasła się nie powiodła: nieznany użytkownik.";
                        isValid = false;
                    }                
                    else if (Users.LoggedUser == null)
                    {
                        // jeśli nie ma zalogowanego użytkownika, wówczas można zmienić można tylko hasło dla administratora 
                        // o ile podano hasło i odpowiedź do odzyskiwania hasła
                        if(!_user.IsAdmin)
                        {
                            lblRepeatPasswordMsg.Text = "Zmiana hasła się nie powiodła: użytkownik nie ma uprawnień do zmiany hasła.";
                            isValid = false;
                        }                    
                    }                
                }

                lblRecoveryQuestionMsg.Visibility = String.IsNullOrEmpty(lblRecoveryQuestionMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblRecoveryAnswerMsg.Visibility = String.IsNullOrEmpty(lblRecoveryAnswerMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblPasswordMsg.Visibility = String.IsNullOrEmpty(lblPasswordMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblRepeatPasswordMsg.Visibility = String.IsNullOrEmpty(lblRepeatPasswordMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

                SolidColorBrush normalBrush = new SolidColorBrush(Colors.White);
                SolidColorBrush failBrush = new SolidColorBrush(Color.FromArgb(255, 255, 250, 200));
            
                txtRecoveryQuestion.Background = String.IsNullOrEmpty(lblRecoveryQuestionMsg.Text) ? normalBrush : failBrush;
                txtRecoveryAnswer.Background = String.IsNullOrEmpty(lblRecoveryAnswerMsg.Text) ? normalBrush : failBrush;
                txtPassword.Background = String.IsNullOrEmpty(lblPasswordMsg.Text) ? normalBrush : failBrush;
                txtRepeatPassword.Background = String.IsNullOrEmpty(lblRepeatPasswordMsg.Text) ? normalBrush : failBrush;

                if (isValid)
                {
                    result = _user.ChangePassword(txtPassword.Password);
                }
            }
            catch(Exception)
            {
                result = false; 
            }

            if (result)
            {
                var messageDialog = new MessageDialog("Hasło użytkownika zostało zmienione.");
                messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.SuccessCommandInvokedHandler), null));

                await messageDialog.ShowAsync();
            }
            else if (isValid)
            {
                var messageDialog = new MessageDialog("Błąd podczas zmiany hasła użytkownika.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));

                await messageDialog.ShowAsync();
            }
        }

        private void SuccessCommandInvokedHandler(IUICommand command)
        {
            this.Frame.GoBack();
        }
    
        private void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = Window.Current.Bounds;
            // układ wertykalny
            if(bounds.Height > bounds.Width)
            {
                controlsGrid.ColumnDefinitions[0].Width = new GridLength(0);

                for (int idx = 0; idx < controlsGrid.RowDefinitions.Count; idx++)
                {
                    RowDefinition row = controlsGrid.RowDefinitions[idx];

                    if (row == null)
                        continue;
                    
                    if (idx < 18 && (idx + 2) % 4 == 0)
                        row.Height = new GridLength(20);
                    else if (idx != 18)
                        row.Height = GridLength.Auto;
                }

                if (lblUserName.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtUserName.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[2].Height = GridLength.Auto;

                if(lblRecoveryQuestion.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtRecoveryQuestion.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[6].Height = GridLength.Auto;

                if(lblRecoveryAnswer.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtRecoveryAnswer.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[10].Height = GridLength.Auto;                

                lblUserName.SetValue(Grid.RowProperty, 0);
                txtUserName.SetValue(Grid.RowProperty, 1);

                lblUserName.SetValue(Grid.ColumnProperty, 1);
                txtUserName.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryQuestion.SetValue(Grid.RowProperty, 3);
                txtRecoveryQuestion.SetValue(Grid.RowProperty, 4);
                lblRecoveryQuestionMsg.SetValue(Grid.RowProperty, 5);

                lblRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                txtRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryQuestionMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryAnswer.SetValue(Grid.RowProperty, 7);
                txtRecoveryAnswer.SetValue(Grid.RowProperty, 8);
                lblRecoveryAnswerMsg.SetValue(Grid.RowProperty, 9);

                lblRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                txtRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryAnswerMsg.SetValue(Grid.ColumnProperty, 1);

                lblPassword.SetValue(Grid.RowProperty, 11);
                txtPassword.SetValue(Grid.RowProperty, 12);
                lblPasswordMsg.SetValue(Grid.RowProperty, 13);

                lblPassword.SetValue(Grid.ColumnProperty, 1);
                txtPassword.SetValue(Grid.ColumnProperty, 1);
                lblPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRepeatPassword.SetValue(Grid.RowProperty, 15);
                txtRepeatPassword.SetValue(Grid.RowProperty, 16);
                lblRepeatPasswordMsg.SetValue(Grid.RowProperty, 17);

                lblRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                txtRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                lblRepeatPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                btnSetPassword.SetValue(Grid.ColumnProperty, 1);
            }
            else
            {
                // układ horyzontalny
                controlsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);

                for (int idx = 0; idx < controlsGrid.RowDefinitions.Count; idx++)
                {
                    RowDefinition row = controlsGrid.RowDefinitions[idx];

                    if (row == null)
                        continue;

                    if (idx < 13 && (idx + 2) % 3 == 0)
                        row.Height = new GridLength(20);
                    else if (idx != 18)
                        row.Height = GridLength.Auto;
                }

                if (lblUserName.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtUserName.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[1].Height = GridLength.Auto;

                if (lblRecoveryQuestion.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtRecoveryQuestion.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[4].Height = GridLength.Auto;

                if (lblRecoveryAnswer.Visibility == Windows.UI.Xaml.Visibility.Collapsed && txtRecoveryAnswer.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                    controlsGrid.RowDefinitions[7].Height = GridLength.Auto;

                if (bounds.Width > 1280)
                {
                    controlsGrid.ColumnDefinitions[1].MaxWidth = Double.PositiveInfinity;

                    txtUserName.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtRecoveryQuestion.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtRecoveryAnswer.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;                    
                    txtRepeatPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;

                    txtUserName.Width = txtRecoveryQuestion.MaxWidth;
                    txtRecoveryQuestion.Width = txtRecoveryQuestion.MaxWidth;
                    txtRecoveryAnswer.Width = txtRecoveryAnswer.MaxWidth;
                    txtPassword.Width = txtPassword.MaxWidth;
                    txtRepeatPassword.Width = txtRepeatPassword.MaxWidth;                    
                }
                else
                {
                    controlsGrid.ColumnDefinitions[1].MaxWidth = 500;

                    txtUserName.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRecoveryQuestion.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRecoveryAnswer.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRepeatPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;

                    txtUserName.Width = Double.NaN;
                    txtRecoveryQuestion.Width = Double.NaN;
                    txtRecoveryAnswer.Width = Double.NaN;
                    txtPassword.Width = Double.NaN;
                    txtRepeatPassword.Width = Double.NaN;                    
                }

                lblUserName.SetValue(Grid.RowProperty, 0);
                txtUserName.SetValue(Grid.RowProperty, 0);

                lblUserName.SetValue(Grid.ColumnProperty, 0);
                txtUserName.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryQuestion.SetValue(Grid.RowProperty, 2);
                txtRecoveryQuestion.SetValue(Grid.RowProperty, 2);
                lblRecoveryQuestionMsg.SetValue(Grid.RowProperty, 3);

                lblRecoveryQuestion.SetValue(Grid.ColumnProperty, 0);
                txtRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryQuestionMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryAnswer.SetValue(Grid.RowProperty, 5);
                txtRecoveryAnswer.SetValue(Grid.RowProperty, 5);
                lblRecoveryAnswerMsg.SetValue(Grid.RowProperty, 6);

                lblRecoveryAnswer.SetValue(Grid.ColumnProperty, 0);
                txtRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryAnswerMsg.SetValue(Grid.ColumnProperty, 1);

                lblPassword.SetValue(Grid.RowProperty, 8);
                txtPassword.SetValue(Grid.RowProperty, 8);
                lblPasswordMsg.SetValue(Grid.RowProperty, 9);

                lblPassword.SetValue(Grid.ColumnProperty, 0);
                txtPassword.SetValue(Grid.ColumnProperty, 1);
                lblPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRepeatPassword.SetValue(Grid.RowProperty, 11);
                txtRepeatPassword.SetValue(Grid.RowProperty, 11);
                lblRepeatPasswordMsg.SetValue(Grid.RowProperty, 12);

                lblRepeatPassword.SetValue(Grid.ColumnProperty, 0);
                txtRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                lblRepeatPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                btnSetPassword.SetValue(Grid.ColumnProperty, 1);            
            }
        }

        private void pageRoot_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.SetPassword();
            }
        }        
    }
}
