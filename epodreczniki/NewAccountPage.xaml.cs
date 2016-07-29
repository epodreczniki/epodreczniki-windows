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
    public sealed partial class NewAccountPage : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private string _backPage;
        private bool _showAll = true;

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


        public NewAccountPage()
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
                _showAll = ((NavigationPageParameter)e.NavigationParameter).ShowAllAttributes;
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
            lblRecoveryQuestion.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            txtRecoveryQuestion.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            lblRecoveryAnswer.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            txtRecoveryAnswer.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;            

            // email tymczasem nie będzie obsługiwany
            lblEmail.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            txtEmail.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            lblEmailMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //lblEmail.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            //txtEmail.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            //lblEmailMsg.Visibility = _showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            txtUserName.Focus(FocusState.Programmatic);
        }

        private void btnCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            this.CreateAccount();
        }

        private async void CreateAccount()
        {
            bool result = false;
            bool isValid = true;            
            UserDataItem user = null;
            Collection<UserDataItem> users = Users.GetUsers();
            bool isDefaultAccount = users == null || !Users.AnyUserExists;

            try
            {
                lblUserNameMsg.Text = "";
                lblPasswordMsg.Text = "";
                lblRepeatPasswordMsg.Text = "";
                lblRecoveryQuestionMsg.Text = "";
                lblRecoveryAnswerMsg.Text = "";
                lblEmailMsg.Text = "";

                txtUserName.Text = txtUserName.Text.Trim();

                if (String.IsNullOrEmpty(txtUserName.Text))
                {
                    lblUserNameMsg.Text = "Podanie nazwy użytkownika jest wymagane";
                    if (isValid)
                    {
                        txtUserName.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }
                else if (Regex.IsMatch(txtUserName.Text, "[/\\\\[\\]\":;|<>+=,?%*@]", RegexOptions.IgnoreCase))
                {
                    lblUserNameMsg.Text = "Nazwa użytkownika nie może zawierać znaków specjalnych: /\\[]\":;|<>+=,?*%@";
                    if (isValid)
                    {
                        txtUserName.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    if (users != null && users.Where(u => (!String.IsNullOrEmpty(u.Name) && u.Name.Equals(txtUserName.Text))).SingleOrDefault() != null)
                    {
                        lblUserNameMsg.Text = "Podana nazwa użytkownika już istnieje. Wybierz inną nazwę.";
                        if (isValid)
                        {
                            txtUserName.Focus(FocusState.Programmatic);
                            isValid = false;
                        }
                    }
                }

                if (String.IsNullOrEmpty(txtPassword.Password))
                {
                    lblPasswordMsg.Text = "Podanie hasła użytkownika jest wymagane";
                    if (isValid)
                    {
                        txtPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }
                else if (txtPassword.Password.Length < 6)
                {

                    lblPasswordMsg.Text = "Podane hasło jest zbyt krótkie: musi posiadać przynajmniej 6 znaków";
                    if (isValid)
                    {
                        txtPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }
                //else if (txtPassword.Password.Length > 12)
                //{

                //    lblPasswordMsg.Text = "Podane hasło jest zbyt długie: może posiadać maksymalnie 12 znaków";
                //    isValid = false;            
                //}            
                //else if (!txtPassword.Password.Any(c => char.IsDigit(c)))
                //{

                //    lblPasswordMsg.Text = "Podane hasło jest zbyt słabe: musi zawierać przynajmniej jedną liczbę";
                //    isValid = false;            
                //}
                //else if (!txtPassword.Password.Any(c => !char.IsLetter(c) && !char.IsDigit(c)))
                //{
                //    lblPasswordMsg.Text = "Podane hasło jest zbyt słabe: musi zawierać przynajmniej jeden znak nie będący ani literą ani cyfrą";
                //    isValid = false;            
                //}
                //else
                //{                
                //    var repeatCount = 0;
                //    var lastChar = '\0';
                //    foreach (var c in txtPassword.Password)
                //    {
                //        if (c == lastChar)
                //            repeatCount++;
                //        else
                //            repeatCount = 0;

                //        if (repeatCount == 2)
                //        {
                //            lblPasswordMsg.Text = "Podane hasło zawiera więcej niż dwa powtarzające się znaki";
                //            isValid = false;
                //            break;
                //        }                        
                //        lastChar = c;
                //    }
                //}

                if (String.IsNullOrEmpty(txtRepeatPassword.Password))
                {
                    lblRepeatPasswordMsg.Text = "Podanie powtórzenia hasła jest wymagane";
                    if (isValid)
                    {
                        txtRepeatPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }
                else if (isValid && !txtPassword.Password.Equals(txtRepeatPassword.Password))
                {
                    lblRepeatPasswordMsg.Text = "Podane hasło i jego powtórzenie muszą być takie same";
                    if (isValid)
                    {
                        txtRepeatPassword.Focus(FocusState.Programmatic);
                        isValid = false;
                    }
                }

                if (isDefaultAccount)
                {
                    if (String.IsNullOrEmpty(txtRecoveryQuestion.Text))
                    {
                        lblRecoveryQuestionMsg.Text = "Podanie pytania do odzyskiwania hasła jest wymagane";
                        if (isValid)
                        {
                            txtRecoveryQuestion.Focus(FocusState.Programmatic);
                            isValid = false;
                        }
                    }

                    if (String.IsNullOrEmpty(txtRecoveryAnswer.Text))
                    {
                        lblRecoveryAnswerMsg.Text = "Podanie odpowiedzi do pytania do odzyskiwania hasła jest wymagane";
                        if (isValid)
                        {
                            txtRecoveryAnswer.Focus(FocusState.Programmatic);
                            isValid = false;
                        }
                    }
                }

                // tymczasem email jest pominięty
                //if (Regex.IsMatch(txtEmail.Text, @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.([a-zA-Z0-9-.]|[a-zA-Z0-9-.]\.[a-zA-Z0-9-.])$", RegexOptions.IgnoreCase))                
                //{
                //    lblEmailMsg.Text = "Podany adres e-mail jest ma nieprawidłowy format";
                //    isValid = false;
                //}            

                lblUserNameMsg.Visibility = String.IsNullOrEmpty(lblUserNameMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblPasswordMsg.Visibility = String.IsNullOrEmpty(lblPasswordMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblRepeatPasswordMsg.Visibility = String.IsNullOrEmpty(lblRepeatPasswordMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblRecoveryQuestionMsg.Visibility = String.IsNullOrEmpty(lblRecoveryQuestionMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblRecoveryAnswerMsg.Visibility = String.IsNullOrEmpty(lblRecoveryAnswerMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                lblEmailMsg.Visibility = String.IsNullOrEmpty(lblEmailMsg.Text) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

                SolidColorBrush normalBrush = new SolidColorBrush(Colors.White);
                SolidColorBrush failBrush = new SolidColorBrush(Color.FromArgb(255, 255, 250, 200));

                txtUserName.Background = String.IsNullOrEmpty(lblUserNameMsg.Text) ? normalBrush : failBrush;
                txtPassword.Background = String.IsNullOrEmpty(lblPasswordMsg.Text) ? normalBrush : failBrush;
                txtRepeatPassword.Background = String.IsNullOrEmpty(lblRepeatPasswordMsg.Text) ? normalBrush : failBrush;
                txtRecoveryQuestion.Background = String.IsNullOrEmpty(lblRecoveryQuestionMsg.Text) ? normalBrush : failBrush;
                txtRecoveryAnswer.Background = String.IsNullOrEmpty(lblRecoveryAnswerMsg.Text) ? normalBrush : failBrush;
                txtEmail.Background = String.IsNullOrEmpty(lblEmailMsg.Text) ? normalBrush : failBrush;

                if (isValid)
                {
                    
                    if(isDefaultAccount)
                    {
                        user = users.Where(usr => usr.IsDefault == true).SingleOrDefault();
                        if(user != null)
                        {
                            user.CompleteData(txtUserName.Text, txtPassword.Password, txtRecoveryQuestion.Text, txtRecoveryAnswer.Text, txtEmail.Text);                            
                        }
                    }

                    if(user == null)
                        user = Users.AddUser(txtUserName.Text, txtPassword.Password, txtRecoveryQuestion.Text, txtRecoveryAnswer.Text, txtEmail.Text, isDefaultAccount);


                    if (user != null)
                    {
                        result = await user.SaveToFile();
                        if (result)
                            result = user.SaveUserDataToSettings();

                        // jeśli nie było użytkowników, ustaw nowo dodanego użytkownika jako zalogowanego:
                        if (isDefaultAccount)
                            Users.LoggedUser = user;
                    }
                }
            }
            catch(Exception)
            {
                result = false; 
            }

            if (result)
            {
                var messageDialog = new MessageDialog("Konto zostało utworzone.");
                messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.SuccessCommandInvokedHandler), null));

                await messageDialog.ShowAsync();
            }

            else
            {
                var messageDialog = new MessageDialog("Błąd podczas tworzenia konta.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));

                await messageDialog.ShowAsync();
            }
        }

        private void SuccessCommandInvokedHandler(IUICommand command)
        {            
            if (String.IsNullOrEmpty(this._backPage))
                this.Frame.GoBack();                
            else
                this.Frame.Navigate(Type.GetType("epodreczniki." + _backPage), null);                            
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
                    
                    if (idx < 20 && (idx + 1) % 4 == 0)
                        row.Height = new GridLength(20);
                    else if (idx != 23)
                        row.Height = GridLength.Auto;                    
                }               
                
                lblUserName.SetValue(Grid.RowProperty, 0);
                txtUserName.SetValue(Grid.RowProperty, 1);
                lblUserNameMsg.SetValue(Grid.RowProperty, 2);

                lblUserName.SetValue(Grid.ColumnProperty, 1);
                txtUserName.SetValue(Grid.ColumnProperty, 1);
                lblUserNameMsg.SetValue(Grid.ColumnProperty, 1);

                lblPassword.SetValue(Grid.RowProperty, 4);
                txtPassword.SetValue(Grid.RowProperty, 5);
                lblPasswordMsg.SetValue(Grid.RowProperty, 6);

                lblPassword.SetValue(Grid.ColumnProperty, 1);
                txtPassword.SetValue(Grid.ColumnProperty, 1);
                lblPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRepeatPassword.SetValue(Grid.RowProperty, 8);
                txtRepeatPassword.SetValue(Grid.RowProperty, 9);
                lblRepeatPasswordMsg.SetValue(Grid.RowProperty, 10);

                lblRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                txtRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                lblRepeatPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryQuestion.SetValue(Grid.RowProperty, 12);
                txtRecoveryQuestion.SetValue(Grid.RowProperty, 13);
                lblRecoveryQuestionMsg.SetValue(Grid.RowProperty, 14);

                lblRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                txtRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryQuestionMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryAnswer.SetValue(Grid.RowProperty, 16);
                txtRecoveryAnswer.SetValue(Grid.RowProperty, 17);
                lblRecoveryAnswerMsg.SetValue(Grid.RowProperty, 18);

                lblRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                txtRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryAnswerMsg.SetValue(Grid.ColumnProperty, 1);

                lblEmail.SetValue(Grid.RowProperty, 20);
                txtEmail.SetValue(Grid.RowProperty, 21);
                lblEmailMsg.SetValue(Grid.RowProperty, 22);

                lblEmail.SetValue(Grid.ColumnProperty, 1);
                txtEmail.SetValue(Grid.ColumnProperty, 1);
                lblEmailMsg.SetValue(Grid.ColumnProperty, 1);

                btnCreateAccount.SetValue(Grid.ColumnProperty, 1);
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

                    if (idx < 15 && (idx + 1) % 3 == 0)
                        row.Height = new GridLength(20);
                    else if (idx != 23)
                        row.Height = GridLength.Auto;
                }                  
                
                if (bounds.Width > 1280)
                {
                    controlsGrid.ColumnDefinitions[1].MaxWidth = Double.PositiveInfinity;

                    txtUserName.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;                    
                    txtPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;                    
                    txtRepeatPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtRecoveryQuestion.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtRecoveryAnswer.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                    txtEmail.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;

                    txtUserName.Width = txtUserName.MaxWidth;                    
                    txtPassword.Width = txtPassword.MaxWidth;
                    txtRepeatPassword.Width = txtRepeatPassword.MaxWidth;
                    txtRecoveryQuestion.Width = txtRecoveryQuestion.MaxWidth;
                    txtRecoveryAnswer.Width = txtRecoveryAnswer.MaxWidth;
                    txtEmail.Width = txtEmail.MaxWidth;
                }
                else
                {
                    controlsGrid.ColumnDefinitions[1].MaxWidth = 500;

                    txtUserName.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRepeatPassword.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRecoveryQuestion.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtRecoveryAnswer.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                    txtEmail.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;

                    txtUserName.Width = Double.NaN;
                    txtPassword.Width = Double.NaN;
                    txtRepeatPassword.Width = Double.NaN;
                    txtRecoveryQuestion.Width = Double.NaN;
                    txtRecoveryAnswer.Width = Double.NaN;
                    txtEmail.Width = Double.NaN;
                }

                lblUserName.SetValue(Grid.RowProperty, 0);
                txtUserName.SetValue(Grid.RowProperty, 0);
                lblUserNameMsg.SetValue(Grid.RowProperty, 1);

                lblUserName.SetValue(Grid.ColumnProperty, 0);
                txtUserName.SetValue(Grid.ColumnProperty, 1);
                lblUserNameMsg.SetValue(Grid.ColumnProperty, 1);

                lblPassword.SetValue(Grid.RowProperty, 3);
                txtPassword.SetValue(Grid.RowProperty, 3);
                lblPasswordMsg.SetValue(Grid.RowProperty, 4);

                lblPassword.SetValue(Grid.ColumnProperty, 0);
                txtPassword.SetValue(Grid.ColumnProperty, 1);
                lblPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRepeatPassword.SetValue(Grid.RowProperty, 6);
                txtRepeatPassword.SetValue(Grid.RowProperty, 6);
                lblRepeatPasswordMsg.SetValue(Grid.RowProperty, 7);

                lblRepeatPassword.SetValue(Grid.ColumnProperty, 0);
                txtRepeatPassword.SetValue(Grid.ColumnProperty, 1);
                lblRepeatPasswordMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryQuestion.SetValue(Grid.RowProperty, 9);
                txtRecoveryQuestion.SetValue(Grid.RowProperty, 9);
                lblRecoveryQuestionMsg.SetValue(Grid.RowProperty, 10);

                lblRecoveryQuestion.SetValue(Grid.ColumnProperty, 0);
                txtRecoveryQuestion.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryQuestionMsg.SetValue(Grid.ColumnProperty, 1);

                lblRecoveryAnswer.SetValue(Grid.RowProperty, 12);
                txtRecoveryAnswer.SetValue(Grid.RowProperty, 12);
                lblRecoveryAnswerMsg.SetValue(Grid.RowProperty, 13);

                lblRecoveryAnswer.SetValue(Grid.ColumnProperty, 0);
                txtRecoveryAnswer.SetValue(Grid.ColumnProperty, 1);
                lblRecoveryAnswerMsg.SetValue(Grid.ColumnProperty, 1);

                lblEmail.SetValue(Grid.RowProperty, 15);
                txtEmail.SetValue(Grid.RowProperty, 15);
                lblEmailMsg.SetValue(Grid.RowProperty, 16);

                lblEmail.SetValue(Grid.ColumnProperty, 0);
                txtEmail.SetValue(Grid.ColumnProperty, 1);
                lblEmailMsg.SetValue(Grid.ColumnProperty, 1);

                btnCreateAccount.SetValue(Grid.ColumnProperty, 1);            
            }
        }

        private void pageRoot_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.CreateAccount();
            }
        }
    }
}
