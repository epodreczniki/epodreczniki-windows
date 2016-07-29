using epodreczniki.Common;
using epodreczniki.Data;
using epodreczniki.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace epodreczniki
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class HandbookDetailsPage : Page
    {
        #region pola klasy

        private NavigationHelper navigationHelper;
        private CollectionDataItem _handbook = null;
        private string _contentId;

        private SimpleOrientationSensor _simpleorientation;

        private delegate void ActionDelegate(IUICommand command);

        #endregion

        #region propertisy

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public CollectionDataItem Handbook
        {
            get { return this._handbook; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        #endregion
        
        #region konstruktor

        public HandbookDetailsPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            this.updateIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            _simpleorientation = SimpleOrientationSensor.GetDefault();
            SimpleOrientation orientation = SimpleOrientation.NotRotated;

            // Assign an event handler for the sensor orientation-changed event
            if (_simpleorientation != null)
            {
                _simpleorientation.OrientationChanged += OrientationChanged;

                orientation = _simpleorientation.GetCurrentOrientation();                
            }

            RedesignView(orientation);
        }        
                
        #endregion

        #region metody pomocnicze

        private async void InitializeData()
        {
            this.ShowProgressIndicator(true, "pobieranie danych w toku...");

            CollectionsDataSource.SetMaxWidth(Window.Current.Bounds);
            IEnumerable<CollectionDataItem> collections = await CollectionsDataSource.GetCollectionsAsync();

            if (collections == null)
            {
                var messageDialog = new MessageDialog("Pobranie danych o podręcznikach nie jest teraz możliwe: nie można połączyć się z serwerem. Spróbuj ponownie później.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }
            else if (!String.IsNullOrEmpty(_contentId))
            {
                this._handbook = CollectionsDataSource.Source.Collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();

                if (this._handbook != null)
                {
                    progressCanvas.Height = (double)this._handbook.UpdateCurtainHeight((double)coverImage.ActualHeight);

                    this.DataContext = this._handbook;
                }

                this.RefreshButtons();
            }

            this.ShowProgressIndicator(false);
        }
        
        private void ShowProgressIndicator(bool show, string text = "")
        {
            progressTextBlock.Text = text;
            progressIndicator.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            progressTextBlock.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void DetectScreenType()
        {
            double dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var bounds = Window.Current.Bounds;
            double h;
            switch (DisplayInformation.GetForCurrentView().CurrentOrientation)
            {
                case DisplayOrientations.PortraitFlipped:
                    h = bounds.Height;
                    break;

                case DisplayOrientations.Landscape:
                    h = bounds.Height;
                    break;

                case DisplayOrientations.LandscapeFlipped:
                    h = bounds.Height;
                    break;

                case DisplayOrientations.Portrait:
                    h = bounds.Width;
                    break;

                default:
                    return;
            }
            double inches = h / dpi;
            string screenType = "Slate";
            if (inches < 10)
            {
                screenType = "Slate";
            }
            else if (inches < 14)
            {
                screenType = "WorkHorsePC";
            }
            else
            {
                screenType = "FamilyHub";
            }
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["screenType"] = screenType;
        }

        private void RedesignView(SimpleOrientation orientation)
        {            
            // 16:9 = 1,77777777
            // 4:3 = 1,33333333
            double refferenceRatio = (double)(((double)16 / 9 + (double)4 / 3) / 2);            
            Rect bounds = Window.Current.Bounds;
            double ratio = bounds.Width > bounds.Height ? bounds.Width / bounds.Height : bounds.Height / bounds.Width;
            bool isHorizontal = true;

            if(orientation == SimpleOrientation.Facedown || orientation == SimpleOrientation.Faceup)
            {
                isHorizontal = bounds.Width > bounds.Height;
            }
            else
            {
                isHorizontal = orientation == SimpleOrientation.NotRotated || orientation == SimpleOrientation.Rotated180DegreesCounterclockwise;
            }

            double db = detailsGrid.RowDefinitions[3].ActualHeight;

            if (isHorizontal)
            {                
                progressValueTextBlock.FontSize = 70;
                progressLabelTextBlock.FontSize = 30;
                progressValueTextBlock.LineHeight = 48;
                

                if (ratio > refferenceRatio)
                {
                    // 16:9 landscape
                    detailsGrid.ColumnDefinitions[0].Width = new GridLength(20, GridUnitType.Star);
                    detailsGrid.RowDefinitions[3].Height = new GridLength(25, GridUnitType.Star);
                }
                else
                {
                    // 4:3 landscape
                    detailsGrid.ColumnDefinitions[0].Width = new GridLength(20, GridUnitType.Star);
                    detailsGrid.RowDefinitions[3].Height = new GridLength(40, GridUnitType.Star);
                }
            }
            else
            {
                progressValueTextBlock.FontSize = 50;
                progressLabelTextBlock.FontSize = 22;
                progressValueTextBlock.LineHeight = 33;

                if (ratio  > refferenceRatio)
                {
                    // 16:9 portrait
                    detailsGrid.ColumnDefinitions[0].Width = new GridLength(10, GridUnitType.Star);
                    detailsGrid.RowDefinitions[3].Height = new GridLength(90, GridUnitType.Star);
                }
                else
                {
                    // 4:3 portrait
                    detailsGrid.ColumnDefinitions[0].Width = new GridLength(10, GridUnitType.Star);
                    detailsGrid.RowDefinitions[3].Height = new GridLength(65, GridUnitType.Star);
                }
            }
        }

        private void SetPropertyText(TextBlock label, TextBlock text, String value)
        {
            if (String.IsNullOrEmpty(value))
            {
                if (label != null)
                    label.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                text.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                text.Text = String.Empty;
            }
            else
            {
                if(label != null)
                    label.Visibility = Windows.UI.Xaml.Visibility.Visible;
                
                text.Visibility = Windows.UI.Xaml.Visibility.Visible;
                text.Text = value;
            }
        }

        private async void ReloadCoverImage()
        {
            if (this._handbook == null)
                return;

            try
            {
                if (coverImage != null)
                {
                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this._handbook.CoverImage));
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        BitmapImage image = new BitmapImage();
                        image.SetSource(fileStream);
                        coverImage.Source = image;
                    }

                    StorageFolder itemFolder = await file.GetParentAsync();
                    if (itemFolder != null)
                    {
                        IReadOnlyList<StorageFile> allFilesList = await itemFolder.GetFilesAsync();

                        IEnumerable<StorageFile> filesToDeleteList = allFilesList.Where(f => f.Name.IndexOf(CollectionsDataSource.CoverImageName) > -1 && !f.Name.Equals(Path.GetFileName(this._handbook.CoverImage)));

                        if (filesToDeleteList != null)
                        {
                            foreach (StorageFile fileToDelete in filesToDeleteList)
                            {
                                if (fileToDelete == null)
                                    continue;
                                await fileToDelete.DeleteAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (App.IsDebug())
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (CollectionsDataSource.MessageDialogCommand != null)
                        {
                            CollectionsDataSource.MessageDialogCommand.Cancel();
                            CollectionsDataSource.MessageDialogCommand = null;
                        }

                        var messageDialog = new MessageDialog("Błąd podczas obsługi przeładowywania dużej okładki: " + exc.Message);
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        CollectionsDataSource.MessageDialogCommand = messageDialog.ShowAsync();
                        return;
                    });                        
                }
            }
        }

        private void RefreshButtons()
        {
            if (this._handbook != null)
            {
                UserDataItem user = Users.LoggedUser;
                bool allowToManageHandbooks = user == null || user.AllowToManageHandbooks;

                UpdateButton.Visibility = (this._handbook.Status != CollectionStatus.NotDownloaded && this._handbook.Status != CollectionStatus.DownloadInProgress && this._handbook.Status != CollectionStatus.UpdateInProgress
                    && this._handbook.Status != CollectionStatus.CancelUpdatePending && this._handbook.Status != CollectionStatus.CancelDownloadPending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                UpdateButton.IsEnabled = this._handbook.Status == CollectionStatus.UpdateRequired && allowToManageHandbooks;

                updateIndicator.Visibility = (this._handbook.Status == CollectionStatus.UpdateRequired || this._handbook.Status == CollectionStatus.UpdateInProgress || this._handbook.Status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

                CancelUpdateButton.IsEnabled = (this._handbook.DownloadProgress < 100 && this._handbook.Status != CollectionStatus.CancelUpdatePending); 
                CancelUpdateButton.Visibility = (this._handbook.Status == CollectionStatus.UpdateInProgress || this._handbook.Status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

                DownloadButton.Visibility = (this._handbook.Status == CollectionStatus.NotDownloaded ) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                DownloadButton.IsEnabled = (this._handbook.Status == CollectionStatus.NotDownloaded && allowToManageHandbooks);

                CancelDownloadButton.IsEnabled = (this._handbook.DownloadProgress < 100 && this._handbook.Status != CollectionStatus.CancelDownloadPending); 
                CancelDownloadButton.Visibility = (this._handbook.Status == CollectionStatus.DownloadInProgress || this._handbook.Status == CollectionStatus.CancelDownloadPending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

                DeleteButton.IsEnabled = (this._handbook.Status == CollectionStatus.Downloaded || this._handbook.Status == CollectionStatus.UpdateRequired) && allowToManageHandbooks;
                DeleteButton.SetValue(Grid.ColumnProperty, (this._handbook.Status == CollectionStatus.DownloadInProgress || this._handbook.Status == CollectionStatus.UpdateInProgress
                    || this._handbook.Status == CollectionStatus.CancelDownloadPending || this._handbook.Status == CollectionStatus.CancelUpdatePending) ? 6 : 4);

                ReadButton.IsEnabled = (this._handbook.Status == CollectionStatus.Downloaded || this._handbook.Status == CollectionStatus.UpdateRequired);
                ReadButton.SetValue(Grid.ColumnProperty, (this._handbook.Status == CollectionStatus.DownloadInProgress || this._handbook.Status == CollectionStatus.UpdateInProgress
                    || this._handbook.Status == CollectionStatus.CancelDownloadPending || this._handbook.Status == CollectionStatus.CancelUpdatePending) ? 8 : 6);

                progressCircle.Visibility = (this._handbook.Status == CollectionStatus.DownloadInProgress || this._handbook.Status == CollectionStatus.UpdateInProgress
                    || this._handbook.Status == CollectionStatus.CancelDownloadPending || this._handbook.Status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                ((Button)progressCircle).IsEnabled = (progressCircle.Visibility == Windows.UI.Xaml.Visibility.Collapsed);
                
                if (!String.IsNullOrEmpty(progressValueTextBlock.Text) && Convert.ToInt32(progressValueTextBlock.Text) == 100)
                {
                    progressValue.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    CancelUpdateButton.IsEnabled = false;
                    CancelDownloadButton.IsEnabled = false;
                }
                else
                {
                    progressValue.Visibility = progressCircle.Visibility;
                    CancelUpdateButton.IsEnabled = true;
                    CancelDownloadButton.IsEnabled = true;
                }

                progressPie.Visibility = (progressCircle.Visibility == Windows.UI.Xaml.Visibility.Visible && progressValue.Visibility == Windows.UI.Xaml.Visibility.Collapsed) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

                lblFolderSize.Visibility = String.IsNullOrEmpty(this._handbook.FolderSize) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                txtFolderSize.Visibility = lblFolderSize.Visibility;

                lblVersion.Visibility = String.IsNullOrEmpty(this._handbook.Version) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                txtVersion.Visibility = lblVersion.Visibility;

                coverImage.IsTapEnabled = (this._handbook.Status == CollectionStatus.Downloaded || this._handbook.Status == CollectionStatus.UpdateRequired) && this._handbook.UnzipProgress == 0;  
                overlayCanvas.Visibility = coverImage.IsTapEnabled ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

                if (this._handbook.Status == CollectionStatus.NotDownloaded)
                    progressCanvas.Height = coverImage.ActualHeight;
            }
        }

        private void SetProperties()
        {
            SetPropertyText(null, txtTitle, this._handbook.Title);

            txtSubtitle.Visibility = String.IsNullOrEmpty(this._handbook.Subtitle) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            txtSubtitle.Text = String.IsNullOrEmpty(this._handbook.Subtitle) ? String.Empty : this._handbook.Subtitle;

            SetPropertyText(lblEducationLevel, txtEducationLevel, this._handbook.School.EducationLevel);
            SetPropertyText(lblClass, txtClass, (this._handbook.School.Class != null && this._handbook.School.Class > 0) ? this._handbook.School.Class.ToString() : String.Empty);
            SetPropertyText(lblSubjectName, txtSubjectName, this._handbook.Subject.Subject);
            SetPropertyText(lblAbstract, txtAbstract, this._handbook.Abstract);

            lblLicense.Visibility = String.IsNullOrEmpty(this._handbook.License) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            linkLicense.Visibility = String.IsNullOrEmpty(this._handbook.License) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            if (!String.IsNullOrEmpty(this._handbook.License))
                linkLicense.NavigateUri = new Uri(this._handbook.License);

            SetPropertyText(lblFolderSize, txtFolderSize, this._handbook.FolderSize);
            SetPropertyText(lblVersion, txtVersion, this._handbook.Version);


            if (this._handbook.Authors != null)
            {
                List<RoleDataItem> lstAuthorsInRoles = new List<RoleDataItem>();
                foreach (AuthorDataItem author in this._handbook.Authors)
                {
                    if (author == null)
                        continue;

                    if (String.IsNullOrEmpty(author.Role))
                        author.Role = String.Empty;

                    RoleDataItem role = lstAuthorsInRoles.Where(rol => rol.Type.Equals(author.Role, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
                    if(role == null)
                    {
                        role = new RoleDataItem();
                        role.Type = author.Role;
                        lstAuthorsInRoles.Add(role);
                    }                    

                    role.Authors.Add(author);
                }

                
                foreach (RoleDataItem role in lstAuthorsInRoles)
                {
                    if (role == null || role.Authors == null || role.Authors.Count == 0)
                        continue;


                    StringBuilder sbAuthors = new StringBuilder();

                    if (role.Authors != null)
                    {
                        foreach (AuthorDataItem author in role.Authors)
                        {
                            if (sbAuthors.Length > 0)
                                sbAuthors.Append(", ");

                            sbAuthors.Append(author.FullName);

                            if(!String.IsNullOrEmpty(author.Institution))
                            {
                                sbAuthors.Append(" ");
                                sbAuthors.Append(author.Institution);                                
                            }                                                            
                        }
                    }

                    if (sbAuthors.Length == 0)
                        continue;

                    role.Authors.Sort(AuthorDataItem.SortAscending());

                    TextBlock txtRole = new TextBlock();

                    if (!String.IsNullOrEmpty(role.Type))
                    {
                        txtRole.Text = role.Type;
                    }
                    else
                    {
                        txtRole.Text = "Autorzy";
                    }
                    
                    txtRole.TextWrapping = TextWrapping.WrapWholeWords;
                    txtRole.Margin = new Thickness(0, 30, 0, 0);
                    txtRole.Style = this.Resources["HandbookSmallAttributeLabelBlockStyle"] as Style;

                    panelAuthors.Children.Add(txtRole);

                    TextBlock txtAuthors = new TextBlock();
                    txtAuthors.Text = sbAuthors.ToString();
                    txtAuthors.TextWrapping = TextWrapping.WrapWholeWords;
                    txtAuthors.Margin = new Thickness(0, 10, 2, 0);                    
                    txtAuthors.Style = this.Resources["HandbookSmallAttributeTextBlockStyle"] as Style;

                    panelAuthors.Children.Add(txtAuthors);                    
                }
            }
        }

        private void SetProgressPie(int progress)
        {
            Double radius = (progressPie.ActualWidth < progressPie.ActualHeight ? progressPie.ActualWidth / 2 : progressPie.ActualHeight / 2) * 0.9;
            Double angle = progress * 360 / 100;

            if (angle == 360 || progress < 0)
                angle = 359.99;

            LineSegment lineSegment = (LineSegment)((PathGeometry)progressPath.Data).Figures[0].Segments[0];
            lineSegment.Point = new Point(radius, 0);

            ArcSegment arcPie = (ArcSegment)((PathGeometry)progressPath.Data).Figures[0].Segments[1];
            arcPie.IsLargeArc = angle >= 180.0;
            arcPie.Point = new Point(Math.Cos(angle * Math.PI / 180) * radius, Math.Sin(angle * Math.PI / 180) * radius);
            arcPie.Size = new Size(radius, radius);
            arcPie.SweepDirection = SweepDirection.Clockwise;
        }

        #endregion

        #region obsługa zdarzeń

        private async void OrientationChanged(object sender, SimpleOrientationSensorOrientationChangedEventArgs e)
        {
            if (e.Orientation == SimpleOrientation.Faceup || e.Orientation == SimpleOrientation.Facedown)
                return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RedesignView(e.Orientation);
            });
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
            if (e.NavigationParameter != null)
                _contentId = e.NavigationParameter as String;
            else
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null && localSettings.Values.ContainsKey("detailsContentId"))
                    _contentId = localSettings.Values["detailsContentId"] as String;
            }

            if (!String.IsNullOrEmpty(_contentId))
            {                
                // jeśli kolekcja podręczników jest już dostępna: pobież właściwy podręcznik.
                // Jeśli natomiast kolekcja podręczników nie jest dostępna  (ponieważ widok szczegółów jest pokazany jako pierwszy 
                // po uruchomieniu aplikacji w konsekwencji odtworzenia stanu aplikacji) wówczas zleć pobranie kolekcji: 
                if (CollectionsDataSource.Source != null && CollectionsDataSource.Source.Collections != null && CollectionsDataSource.Source.Collections.Count > 0)
                    this._handbook = CollectionsDataSource.Source.Collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();
                else
                    this.InitializeData();                
            }

            if(this._handbook != null)
            {                
                this.DataContext = this._handbook;
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
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings != null)
                localSettings.Values["detailsContentId"] = _contentId;
        }

        private async void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {            
            if (this._handbook != null)
            {
                if(this._handbook.NewVersionItem != null && this._handbook.NewVersionItem.UpdateConfirmed)
                {
                    this._handbook.NewVersionItem.UpdateConfirmed = false;

                    int connection = App.IsConnectedToInternet();

                    if (connection > 0)
                    {
                        if (connection > 1 && !App.Use3GConnection)
                        {
                            string message = "Brak połączenia z siecią WiFi. Czy zezwalasz na użycie sieci komórkowej w celu pobrania podręcznika?";
                            await this.ShowMessageAndStartAction(this.UpdateCommandInvokedHandler, message);
                        }
                        else
                        {
                            await this.DownloadHandbook(true);
                        }
                    }
                    else
                    {                
                        string message = "Urządzenie nie ma połączenia z siecią Internet. Czy chcesz automatycznie rozpocząć pobieranie po przywróceniu połączenia?";
                        await this.ShowMessageAndStartAction(this.UpdateCommandInvokedHandler, message);
                    }
                }

                progressCanvas.Height = (double)this._handbook.UpdateCurtainHeight((double)coverImage.ActualHeight);

                SetProperties();
            }
            
            this.RefreshButtons();

            CollectionsDataSource.Source.DownloadEvent += Source_DownloadEvent;
            CollectionsDataSource.Source.FolderSizeEvent += Source_FolderSizeEvent;
            CollectionsDataSource.Source.CoverEvent += Source_CoverEvent;
            CollectionsDataSource.Source.UnzipEvent += Source_UnzipEvent;
            CollectionsDataSource.Source.UnzipProgressEvent += Source_UnzipProgressEvent;
            CollectionsDataSource.Source.ErrorEvent += Source_ErrorEvent;
            CollectionsDataSource.Source.CancelEvent += Source_CancelEvent;
        }        
                
        private void coverImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            progressCanvas.Width = coverImage.ActualWidth;
            progressCanvas.Height = (double)this._handbook.UpdateCurtainHeight((double)coverImage.ActualHeight);
        }

        private void progressPie_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(progressPath, progressPie.ActualWidth / 2);
            Canvas.SetTop(progressPath, progressPie.ActualHeight / 2);

            if (this._handbook != null)
                this.SetProgressPie(this._handbook.UnzipProgress);
        }

        #endregion

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
       
        #region obsługa zdarzeń z CollectionsDataSource

        private void Source_CoverEvent(object sender, CoverEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.ReloadCoverImage();
                });
            }                
        }

        private void Source_CancelEvent(object sender, CancelEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {                    
                    this.RefreshButtons();
                });
            }
        }

        private void Source_DownloadEvent(object sender, DownloadEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    progressCanvas.Height = (float)coverImage.ActualHeight - (float)coverImage.ActualHeight * e.Progress / 100;

                    progressValueTextBlock.Text = e.Progress.ToString();

                    this.RefreshButtons();
                });
            }
        }

        private void Source_UnzipEvent(object sender, UnzipEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (e.Result < 0)
                    {
                        if (((Frame)Window.Current.Content).SourcePageType.Name.Equals(this.GetType().Name))
                        {
                            string message = "Rozpakowanie pobranych plików podręcznika nie udało się. Spróbuj ponownie pobrać podręcznik później.";
                            if (e.Result < -1)
                            {
                                message = "Rozpakowanie pobranych plików podręcznika nie udało się z powodu niewystarczającej ilości wolnej przestrzeni na dysku. Spróbuj zwolnić nieco miejsca i ponownie pobrać podręcznik później.";
                            }

                            // Close the previous one out
                            if (CollectionsDataSource.MessageDialogCommand != null)
                            {
                                CollectionsDataSource.MessageDialogCommand.Cancel();
                                CollectionsDataSource.MessageDialogCommand = null;
                            }

                            var messageDialog = new MessageDialog(message);
                            messageDialog.Commands.Add(new UICommand("Zamknij", null));
                            CollectionsDataSource.MessageDialogCommand = messageDialog.ShowAsync();
                        }
                        RefreshButtons();
                        return;
                    }

                    progressCanvas.Height = 0;       
                    if(this._handbook != null)
                        this.DataContext = this._handbook;                    

                    SetProperties();

                    RefreshButtons();
                });
            }
        }

        void Source_UnzipProgressEvent(object sender, UnzipProgressEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.SetProgressPie(e.Progress);
                });
            }
        }        

        private void Source_FolderSizeEvent(object sender, FolderSizeEventArgs e)
        {
            if (this._handbook != null && !String.IsNullOrEmpty(this._handbook.ContentId) && !String.IsNullOrEmpty(e.ContentId) && this._handbook.ContentId.Equals(e.ContentId))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (e.FolderSize == null)
                        this._handbook.FolderSize = String.Empty;
                    else
                        this._handbook.FolderSize = e.FolderSize;

                    txtFolderSize.Text = this._handbook.FolderSize;

                    lblFolderSize.Visibility = String.IsNullOrEmpty(this._handbook.FolderSize) ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                    txtFolderSize.Visibility = lblFolderSize.Visibility;
                });
            }
        }

        private void Source_ErrorEvent(object sender, ErrorEventArgs e)
        {
            if (e != null && !String.IsNullOrEmpty(e.Message) && ((Frame)Window.Current.Content).SourcePageType.Name.Equals(this.GetType().Name))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    // Close the previous one out
                    if (CollectionsDataSource.MessageDialogCommand != null)
                    {
                        CollectionsDataSource.MessageDialogCommand.Cancel();
                        CollectionsDataSource.MessageDialogCommand = null;
                    }

                    var messageDialog = new MessageDialog(e.Message);
                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                    CollectionsDataSource.MessageDialogCommand = messageDialog.ShowAsync();
                    return;
                });
            }
        }
        
        #endregion

        #region obsługa przycisków

        private async Task<bool> DownloadHandbook(bool isUpdate = false)
        {
            bool result = false;

            try
            {
                CollectionDataItem item = isUpdate ? this._handbook.NewVersionItem : this._handbook;

                if (item != null && item.Formats != null)
                {
                    FormatDataItem format = item.ChooseFormat(CollectionsDataSource.MaxWidth);
                    
                    if (format == null)
                    {
                        var messageDialog = new MessageDialog("Pobranie podręcznika nie jest aktualnie możliwe, ponieważ odpowiedni format emisyjny nie jest dostępny.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                        return false;
                    }

                    string url = format.Url;

                    ulong freeDiskSpace = await CollectionsDataSource.GetDiskFreeSpace();

                    if (format.Size > 0 && format.Zip > 0 && freeDiskSpace < format.Size + format.Zip)
                    {
                        var messageDialog = new MessageDialog(String.Format("Pobranie podręcznika nie jest aktualnie możliwe z powodu niewystarczającej ilości wolnej przestrzeni na dysku. Aby pobrać i udpostępnić wybrany podręcznik konieczne jest minimum  {0} MB. Spróbuj zwolnić nieco miejsca i ponownie pobrać podręcznik później.", (format.Size + format.Zip) / 1024 / 1024));
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                        return false;
                    }

                    if (!String.IsNullOrEmpty(url) && item.DownloadId != null)
                        CollectionsDataSource.Downloader.QueueDownload(new Uri(url), item.ContentId + "_" + item.Version, true, CollectionsDataSource.DownloadsFolderName);

                    this._handbook.Status = isUpdate ? CollectionStatus.UpdateInProgress : CollectionStatus.DownloadInProgress;
                    this._handbook.DownloadProgress = 0;
                    this._handbook.CurtainHeight = 0;

                    await this._handbook.WriteMetadataFromFileAsync();

                    progressValueTextBlock.Text = item.DownloadProgress.ToString();
                }

                this.RefreshButtons();

                result = true;
            }
            catch
            {
                result = false;
            }

            if(!result)
            {
                var messageDialog = new MessageDialog(String.Format("Pobranie podręcznika zakończyło się błędem!"));
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }

            return result;
        }
        
        private async void CancelCommandInvokedHandler(IUICommand command)
        {
            if (command.Id is int && (int)command.Id == 1)
            {
                bool result = false;

                try
                {
                    if (this._handbook != null && this._handbook.DownloadId != null)
                    {
                        this._handbook.Status = this._handbook.Status == CollectionStatus.UpdateInProgress ? CollectionStatus.CancelUpdatePending : CollectionStatus.CancelDownloadPending;

                        CollectionsDataSource.Downloader.CancelDownload(Handbook.DownloadId);

                        await this._handbook.WriteMetadataFromFileAsync();
                    }

                    //this.RefreshButtons();

                    result = true;
                }
                catch
                {
                    result = false;
                }

                if (!result)
                {
                    var messageDialog = new MessageDialog(String.Format("Zatrzymanie pobrania podręcznika zakończyło się błędem!"));
                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                    await messageDialog.ShowAsync();
                }
            }
        }

        private async void CancelDownloadHandbook()
        {
            if (this._handbook != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz przerwać pobieranie podręcznika: ");
                message.Append(this._handbook.Title);
                message.Append(" i usunąć wszystkie skopiowane do tej pory pliki podręcznika?");                

                await this.ShowMessageAndStartAction(this.CancelCommandInvokedHandler, message.ToString());
            }
        }

        private async Task<bool> DeleteHandbook()
        {
            bool result = false;

            try
            {
                this._handbook.Status = CollectionStatus.NotDownloaded;

                RefreshButtons();

                if (await CollectionsDataSource.RemoveCollection(this._handbook.ContentId))
                {                    
                    this._handbook.FolderSize = String.Empty;

                    progressCanvas.Height = (float)coverImage.ActualHeight;

                    this.ReloadCoverImage();

                    SetProperties();                    

                    result = true;
                }                
            }
            catch
            {
                result = false;
            }

            if (!result)
            {
                var messageDialog = new MessageDialog(String.Format("Usunięcie podręcznika zakończyło się błędem!"));
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }

            return result;
        }

        private async Task<IUICommand> ShowMessageAndStartAction(ActionDelegate action, string message)
        {
            var messageDialog = new MessageDialog(message);

            messageDialog.Commands.Add(new UICommand("Tak", new UICommandInvokedHandler(action), 1));
            messageDialog.Commands.Add(new UICommand("Nie", new UICommandInvokedHandler(action), 0));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            return await messageDialog.ShowAsync();
        }
               
        private async void DeleteCommandInvokedHandler(IUICommand command)
        {
            if (command.Id is int && (int)command.Id == 1)
            {
                await this.DeleteHandbook();
            }
        }

        private async void DownloadCommandInvokedHandler(IUICommand command)
        {
            if (command.Id is int && (int)command.Id == 1)
            {
                await this.DownloadHandbook(false);
            }
        }

        private async void UpdateCommandInvokedHandler(IUICommand command)
        {
            if (command.Id is int && (int)command.Id == 1)
            {
                await this.DownloadHandbook(true);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._handbook != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz usunąć podręcznik: ");
                message.Append(this._handbook.Title);
                message.Append("?");                

                await this.ShowMessageAndStartAction(this.DeleteCommandInvokedHandler, message.ToString());                                                         
            }            
        }
        
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            int connection = App.IsConnectedToInternet();

            if (connection > 0)
            {
                if (connection > 1 && !App.Use3GConnection)
                {
                    string message = "Brak połączenia z siecią WiFi. Czy zezwalasz na użycie sieci komórkowej w celu pobrania podręcznika?";
                    await this.ShowMessageAndStartAction(this.DownloadCommandInvokedHandler, message);
                }
                else
                {
                    await this.DownloadHandbook(false);
                }
            }
            else
            {
                string message = "Urządzenie nie ma połączenia z siecią Internet. Czy chcesz automatycznie rozpocząć pobieranie po przywróceniu połączenia?";
                await this.ShowMessageAndStartAction(this.DownloadCommandInvokedHandler, message);
            }                     
        }
                        
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {            
            if (_handbook != null && _handbook.NewVersionItem != null)
                this.Frame.Navigate(typeof(UpdateConfirmationPage), _handbook.NewVersionItem.ContentId);            
        }

        private async void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
        {

            if (this._handbook != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz przerwać pobieranie podręcznika: ");
                message.Append(this._handbook.Title);
                message.Append(" i usunąć wszystkie skopiowane do tej pory pliki podręcznika?");

                await this.ShowMessageAndStartAction(this.CancelCommandInvokedHandler, message.ToString());
            }
        }

        private async void CancelUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._handbook != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz przerwać pobieranie uaktualnienia podręcznika: ");
                message.Append(this._handbook.Title);
                message.Append(" i usunąć wszystkie skopiowane do tej pory pliki uaktualnienia?");

                await this.ShowMessageAndStartAction(this.CancelCommandInvokedHandler, message.ToString());
            }
        }        

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._handbook != null && (this._handbook.Status == CollectionStatus.Downloaded || this._handbook.Status == CollectionStatus.UpdateRequired))
            {
                if (this._handbook.GetToCFileAsync() != null && this._handbook.GetPagesFileAsync() != null)
                    this.Frame.Navigate(typeof(ToCReaderPage), this._handbook.ContentId);
                else
                    this.Frame.Navigate(typeof(ReaderPage), this._handbook.ContentId);
            }                
        }

        private void coverImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this._handbook != null && (this._handbook.Status == CollectionStatus.Downloaded || this._handbook.Status == CollectionStatus.UpdateRequired))
            {
                if (this._handbook.GetToCFileAsync() != null && this._handbook.GetPagesFileAsync() != null)
                    this.Frame.Navigate(typeof(ToCReaderPage), this._handbook.ContentId);
                else
                this.Frame.Navigate(typeof(ReaderPage), this._handbook.ContentId);
            }
        }

        #endregion                
    }
}


 