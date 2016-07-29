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
using System.Threading.Tasks;
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
    public sealed partial class ConfigPage : Page
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


        public ConfigPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            this.DefaultViewModel["Schools"] = CollectionsDataSource.Source.Schools;
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
            UserDataItem user = Users.LoggedUser;
            bool showAll = false;
            int selBooksFilter = -1;
            if(user == null || user.IsDefault)
            {
                switchIsTeacher.IsOn = Users.IsTeacher;
                selBooksFilter = Users.BooksFilter;
            }
            else
            {
                this.txtUserName.Text = user.Name;
                this.txtRecoveryQuestion.Text = user.RecoveryQuestion;
                this.txtEmail.Text = user.Email;
                
                switchIsTeacher.IsOn = user.IsTeacher;
                switchIsSecured.IsOn = user.IsSecured;

                selBooksFilter = user.BooksFilter;

                showAll = user.IsAdmin;                
            }
            
            cboxBooksFilter.SelectedItem = CollectionsDataSource.Source.Schools.Where(s => s.Id == selBooksFilter).SingleOrDefault();

            // dla użytkownika niezalogowanego oraz domyśnego użytkownika nie ma możliwości edycji nazwy użytkownika, oraz hasła
            lblUserName.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            txtUserName.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

            lblPassword.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            txtPassword.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

            lblRepeatPassword.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            txtRepeatPassword.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

            lblIsSecured.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            switchIsSecured.Visibility = (user == null || user.IsDefault) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;            

            // pytanie i odpowiedź do odzyskiwania hasła możliwa do edycji tylko dla alogowanego admina
            lblRecoveryQuestion.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            txtRecoveryQuestion.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            lblRecoveryAnswer.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            txtRecoveryAnswer.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            
            // email tymczasem nie będzie obsługiwany
            lblEmail.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            txtEmail.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            lblEmailMsg.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //lblEmail.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            //txtEmail.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            //lblEmailMsg.Visibility = showAll ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;            

            txtUserName.Focus(FocusState.Programmatic);
        }        

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            await this.Save();
        }

        private async Task Save()        
        {
            bool result = false;
            bool isValid = true;

            try
            {
                UserDataItem user = Users.LoggedUser;

                if (user == null)
                    return;

                Collection<UserDataItem> users = Users.GetUsers();

                lblUserNameMsg.Text = "";
                lblPasswordMsg.Text = "";
                lblRepeatPasswordMsg.Text = "";
                lblRecoveryQuestionMsg.Text = "";
                lblRecoveryAnswerMsg.Text = "";
                lblEmailMsg.Text = "";

                txtUserName.Text = txtUserName.Text.Trim();

                if (!String.IsNullOrEmpty(txtUserName.Text) && !txtUserName.Text.Equals(user.Name))
                {
                    if (Regex.IsMatch(txtUserName.Text, "[/\\\\[\\]\":;|<>+=,?%*@]", RegexOptions.IgnoreCase))
                    {
                        lblUserNameMsg.Text = "Nazwa użytkownika nie może zawierać znaków specjalnych: /\\[]\":;|<>+=,?*%@";
                        txtUserName.Focus(FocusState.Programmatic);
                        isValid = false;
                    }

                    if (isValid)
                    {
                        if (users != null && users.Where(u => u.Name.Equals(txtUserName.Text)).SingleOrDefault() != null)
                        {
                            lblUserNameMsg.Text = "Podana nazwa użytkownika już istnieje. Wybierz inną nazwę.";
                            txtUserName.Focus(FocusState.Programmatic);
                            isValid = false;
                        }
                    }
                }

                if (!String.IsNullOrEmpty(txtPassword.Password))
                {
                    if (txtPassword.Password.Length < 6)
                    {

                        lblPasswordMsg.Text = "Podane hasło jest zbyt krótkie: musi posiadać przynajmniej 6 znaków";
                        if (isValid)
                        {
                            txtPassword.Focus(FocusState.Programmatic);
                            isValid = false;
                        }
                    }

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
                }

                if (user.IsAdmin && !String.IsNullOrEmpty(txtRecoveryQuestion.Text) && !txtRecoveryQuestion.Text.Equals(user.RecoveryQuestion))
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
                
                if (user != null && !user.IsDefault)
                {
                    user.IsSecured = switchIsSecured.IsOn;
                }

                Users.IsTeacher = switchIsTeacher.IsOn;

                Users.BooksFilter = cboxBooksFilter.SelectedItem != null ? ((SchoolDataItem)cboxBooksFilter.SelectedItem).Id : -1;

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
                    if (!String.IsNullOrEmpty(txtUserName.Text) && !txtUserName.Text.Equals(user.Name))
                        user.Name = txtUserName.Text;

                    if (!String.IsNullOrEmpty(txtPassword.Password))
                        user.Password = DataDecoder.HashPassword(txtPassword.Password, user.Salt);

                    if (user.IsAdmin)
                    {
                        if (!String.IsNullOrEmpty(txtRecoveryQuestion.Text))
                            user.RecoveryQuestion = txtRecoveryQuestion.Text;

                        if (!String.IsNullOrEmpty(txtRecoveryAnswer.Text))
                            user.RecoveryAnswer = DataDecoder.HashPassword(txtRecoveryAnswer.Text.ToLower(), user.Salt);
                    }

                    result = await user.SaveToFile();
                    if( result )
                        result = user.SaveUserDataToSettings();
                }
            }
            catch(Exception)
            {
                result = false; 
            }

            if (result)
            {
                var messageDialog = new MessageDialog("Dane użytkownika zostały zapisane.");
                messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.SuccessCommandInvokedHandler), null));
                
                await messageDialog.ShowAsync();
            }
            else if (isValid)
            {
                var messageDialog = new MessageDialog("Błąd podczas zapisywania danych użytkownika.");
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
            if (bounds.Height > bounds.Width || Users.LoggedUser == null || Users.LoggedUser.IsDefault)
            {
                controlsGrid.ColumnDefinitions[0].Width = new GridLength(0);

                for (int idx = 0; idx < controlsGrid.RowDefinitions.Count; idx++)
                {
                    RowDefinition row = controlsGrid.RowDefinitions[idx];

                    if (row == null)
                        continue;

                    if (idx < 20 && (idx + 1) % 4 == 0)
                        row.Height = new GridLength(20);
                    else if (idx == 26 || idx == 29)
                        row.Height = new GridLength(20);
                    else if (idx != 32)
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

                lblBooksFilter.SetValue(Grid.RowProperty, 24);
                cboxBooksFilter.SetValue(Grid.RowProperty, 25);

                lblBooksFilter.SetValue(Grid.ColumnProperty, 1);
                cboxBooksFilter.SetValue(Grid.ColumnProperty, 1);

                lblIsTeacher.SetValue(Grid.RowProperty, 27);
                switchIsTeacher.SetValue(Grid.RowProperty, 28);

                lblIsTeacher.SetValue(Grid.ColumnProperty, 1);
                switchIsTeacher.SetValue(Grid.ColumnProperty, 1);

                lblIsSecured.SetValue(Grid.RowProperty, 30);
                switchIsSecured.SetValue(Grid.RowProperty, 31);

                lblIsSecured.SetValue(Grid.ColumnProperty, 1);
                switchIsSecured.SetValue(Grid.ColumnProperty, 1);
                
                btnSave.SetValue(Grid.ColumnProperty, 1);
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
                    else if (idx == 19 || idx == 21)
                        row.Height = new GridLength(20);
                    else if (idx != 32)
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
                    cboxBooksFilter.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;

                    txtUserName.Width = txtUserName.MaxWidth;
                    txtPassword.Width = txtPassword.MaxWidth;
                    txtRepeatPassword.Width = txtRepeatPassword.MaxWidth;
                    txtRecoveryQuestion.Width = txtRecoveryQuestion.MaxWidth;
                    txtRecoveryAnswer.Width = txtRecoveryAnswer.MaxWidth;
                    txtEmail.Width = txtEmail.MaxWidth;
                    cboxBooksFilter.Width = cboxBooksFilter.MaxWidth;
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

                lblBooksFilter.SetValue(Grid.RowProperty, 18);
                cboxBooksFilter.SetValue(Grid.RowProperty, 18);

                lblBooksFilter.SetValue(Grid.ColumnProperty, 0);
                cboxBooksFilter.SetValue(Grid.ColumnProperty, 1);

                lblIsTeacher.SetValue(Grid.RowProperty, 20);
                switchIsTeacher.SetValue(Grid.RowProperty, 20);

                lblIsTeacher.SetValue(Grid.ColumnProperty, 0);
                switchIsTeacher.SetValue(Grid.ColumnProperty, 1);

                lblIsSecured.SetValue(Grid.RowProperty, 22);
                switchIsSecured.SetValue(Grid.RowProperty, 22);

                lblIsSecured.SetValue(Grid.ColumnProperty, 0);
                switchIsSecured.SetValue(Grid.ColumnProperty, 1);

                btnSave.SetValue(Grid.ColumnProperty, 1);
            }
        }        

        private async void pageRoot_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await this.Save();
            }
        }        
    }
}
