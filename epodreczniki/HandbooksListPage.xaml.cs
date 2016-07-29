using epodreczniki.Common;
using epodreczniki.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media.Animation;
using Windows.Networking.BackgroundTransfer;
using Windows.UI.Popups;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using epodreczniki.DataModel;
using System.Threading;

namespace epodreczniki
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HandbooksListPage : Page
    {
        #region pola klasy

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private delegate void ActionDelegate(IUICommand target);
        static ManualResetEvent manualEvent = new ManualResetEvent(true);

        #endregion

        #region propertisy

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        #endregion

        #region konstruktor

        public HandbooksListPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;

            this.InitializeData();            
        }
                
        #endregion

        #region metody pomocnicze

        private async void InitializeData()
        {
            this.ShowProgressIndicator(true, "pobieranie danych w toku...");

            CollectionsDataSource.SetMaxWidth(Window.Current.Bounds);
            
            IEnumerable<CollectionDataItem> collections = await CollectionsDataSource.GetCollectionsAsync();

            NoBooksMessageInfoStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            
            if (collections == null)
            {
                var messageDialog = new MessageDialog("Pobranie danych o podręcznikach nie jest teraz możliwe: nie można połączyć się z serwerem. Spróbuj ponownie później.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }
            else
            {
                this.DefaultViewModel["Collections"] = CollectionsDataSource.Source.FilteredCollections;

                this.ShowEmptyListWarningInfo();                
            }

            this.ShowProgressIndicator(false);
        }

        private void ShowProgressIndicator(bool show, string text = "")
        {
            progressTextBlock.Text = text;
            progressIndicator.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            progressTextBlock.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void RefreshButtons(Canvas reverse, CollectionStatus status, int progress)
        {
            UserDataItem user = Users.LoggedUser;
            bool allowToManageHandbooks = user == null || user.AllowToManageHandbooks;

            FrameworkElement innerGrid = VisualTreeHelper.GetChild(reverse, 0) as FrameworkElement;

            // update button
            FrameworkElement element = VisualTreeHelper.GetChild(innerGrid, 1) as FrameworkElement;
            element.Visibility = (status != CollectionStatus.NotDownloaded && status != CollectionStatus.DownloadInProgress && status != CollectionStatus.UpdateInProgress
                && status != CollectionStatus.CancelUpdatePending && status != CollectionStatus.CancelDownloadPending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = status == CollectionStatus.UpdateRequired && allowToManageHandbooks;

            // cancel update button
            element = VisualTreeHelper.GetChild(innerGrid, 2) as FrameworkElement;
            element.Visibility = (status == CollectionStatus.UpdateInProgress || status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = (progress < 100 && status != CollectionStatus.CancelUpdatePending); // jeśli wartość postępu == 100, wówczas trwa rozpakowywanie: nie można już anulować pobierania

            // download button
            element = VisualTreeHelper.GetChild(innerGrid, 3) as FrameworkElement;
            element.Visibility = (status == CollectionStatus.NotDownloaded) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = status == CollectionStatus.NotDownloaded && allowToManageHandbooks;

            // cancel download button
            element = VisualTreeHelper.GetChild(innerGrid, 4) as FrameworkElement;
            element.Visibility = (status == CollectionStatus.DownloadInProgress || status == CollectionStatus.CancelDownloadPending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = (progress < 100 && status != CollectionStatus.CancelDownloadPending); // jeśli wartość postępu == 100, wówczas trwa rozpakowywanie: nie można już anulować pobierania

            // delete button
            element = VisualTreeHelper.GetChild(innerGrid, 5) as FrameworkElement;
            ((Button)element).IsEnabled = status == CollectionStatus.Downloaded || status == CollectionStatus.UpdateRequired && allowToManageHandbooks; ;

            // read button
            element = VisualTreeHelper.GetChild(innerGrid, 6) as FrameworkElement;
            element.Visibility = (status != CollectionStatus.DownloadInProgress && status != CollectionStatus.UpdateInProgress) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = (status == CollectionStatus.Downloaded || status == CollectionStatus.UpdateRequired);

            // progress circle
            element = VisualTreeHelper.GetChild(innerGrid, 7) as FrameworkElement;
            element.Visibility = (status == CollectionStatus.DownloadInProgress || status == CollectionStatus.UpdateInProgress || status == CollectionStatus.CancelDownloadPending || status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;            
            ((Button)element).IsEnabled = (element.Visibility == Windows.UI.Xaml.Visibility.Collapsed);
            bool isCircleVisible = (element.Visibility == Windows.UI.Xaml.Visibility.Visible);

            // progress value
            element = VisualTreeHelper.GetChild(innerGrid, 8) as FrameworkElement;
            TextBlock progressTextBlock = VisualTreeHelper.GetChild(element, 0) as TextBlock;
            if (progressTextBlock != null)
            {
                progressTextBlock.Text = progress.ToString();

                // progress value: jeśli wartość postępu == 100, wówczas jest schowana: pokazany jest jedynie kręcący się okrąg: wskazujący że rozpakowywanie jest w toku...
                if (progress == 100)
                    element.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                else
                    element.Visibility = (status == CollectionStatus.DownloadInProgress || status == CollectionStatus.UpdateInProgress) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }

            bool isValueVisible = (element.Visibility == Windows.UI.Xaml.Visibility.Visible);

            // unzip progress pie            
            element = VisualTreeHelper.GetChild(innerGrid, 11) as FrameworkElement;
            element.Visibility = (isCircleVisible && !isValueVisible) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            // read small button
            element = VisualTreeHelper.GetChild(innerGrid, 9) as FrameworkElement;
            element.Visibility = (status == CollectionStatus.UpdateInProgress || status == CollectionStatus.CancelUpdatePending) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            ((Button)element).IsEnabled = (progress < 100 && status != CollectionStatus.CancelUpdatePending); // jeśli wartość postępu == 100, wówczas trwa rozpakowywanie: nie można wówczas czytać podręcznika
        }

        private void SetProgressCurtain(Canvas obverse, double height)
        {
            if (obverse == null)
                return;

            FrameworkElement progressCanvas = VisualTreeHelper.GetChild(obverse, 2) as FrameworkElement;
            if (progressCanvas != null)
                progressCanvas.Height = height;
        }

        private void HideUpdateIndicator(Canvas obverse)
        {
            if (obverse == null)
                return;

            FrameworkElement updateIndicator = VisualTreeHelper.GetChild(obverse, 1) as FrameworkElement;
            if (updateIndicator != null)
                updateIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void HideAllUpdateIndicators()
        {
            for (int idx = 0; idx < CollectionsDataSource.Source.Collections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.Collections[idx];
                if (item == null || item.ContentId == null)
                    continue;
             
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Grid innerGrid = FindElementByContentId(item.ContentId);
                    if (innerGrid != null)
                    {
                        Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                        if (cover != null)
                        {
                            Canvas obverse = VisualTreeHelper.GetChild(cover, 0) as Canvas;
                            this.HideUpdateIndicator(obverse);                                    
                        }
                    }
                });
            }
        }

        private void SetStatusInfo(Canvas obverse, CollectionStatus status)
        {
            if (obverse == null)
                return;

            FrameworkElement statusStackPanel = VisualTreeHelper.GetChild(obverse, 3) as FrameworkElement;
            if (statusStackPanel != null)
            {
                FrameworkElement statusTextBlock = VisualTreeHelper.GetChild(statusStackPanel, 1) as FrameworkElement;
                if (statusTextBlock != null)
                    ((TextBlock)statusTextBlock).Text = status.ToString();
            }

            FrameworkElement updateIndicator = VisualTreeHelper.GetChild(obverse, 1) as FrameworkElement;
            if (updateIndicator != null)
            {
                ((Polygon)updateIndicator).Visibility = (status == CollectionStatus.UpdateRequired || status == CollectionStatus.UpdateInProgress) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async void ReloadThumbImage(CollectionDataItem item)
        {
            if (item == null)
                return;

            try
            {
                Grid innerGrid = FindElementByContentId(item.ContentId);
                if (innerGrid != null)
                {
                    Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                    if (cover != null)
                    {
                        Canvas obverse = VisualTreeHelper.GetChild(cover, 0) as Canvas;
                        if (obverse != null)
                        {
                            Image img = VisualTreeHelper.GetChild(obverse, 0) as Image;
                            if (img != null)
                            {
                                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(item.CoverThumbImage));
                                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                                {
                                    BitmapImage image = new BitmapImage();
                                    image.SetSource(fileStream);
                                    img.Source = image;
                                }

                                // usuń poprzedni obrazek(i):
                                StorageFolder itemFolder = await file.GetParentAsync();
                                if (itemFolder != null)
                                {
                                    IReadOnlyList<StorageFile> allFilesList = await itemFolder.GetFilesAsync();

                                    // jeśli w nazwie pliku jest fraza "thumb" oraz nie jest to używany w kolekcji plik
                                    IEnumerable<StorageFile> filesToDeleteList = allFilesList.Where(f => f.Name.IndexOf(CollectionsDataSource.ThumbImageName) > -1 && !f.Name.Equals(System.IO.Path.GetFileName(item.CoverThumbImage)));

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
                    }
                }
            }
            catch (Exception exc)
            {
                if (App.IsDebug())
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        // Close the previous one out
                        if (CollectionsDataSource.MessageDialogCommand != null)
                        {
                            CollectionsDataSource.MessageDialogCommand.Cancel();
                            CollectionsDataSource.MessageDialogCommand = null;
                        }

                        var messageDialog = new MessageDialog("Błąd podczas obsługi przeładowywania małej okładki: " + exc.Message);
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        CollectionsDataSource.MessageDialogCommand = messageDialog.ShowAsync();
                        return;
                    });                    
                }
            }
        }

        private void RotateElement(Canvas canvasToRotate, CollectionDataItem item = null)
        {
            if (canvasToRotate.Children == null || canvasToRotate.Children.Count < 2)
                return;

            Canvas canvasIn = (Canvas)canvasToRotate.Children[0];
            Canvas canvasOut = (Canvas)canvasToRotate.Children[1];

            bool showRevers = false;
            if (canvasIn.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                showRevers = true;
                Canvas canvasTemp = canvasOut;
                canvasOut = canvasIn;
                canvasIn = canvasTemp;
            }

            Duration duration = new Duration(TimeSpan.FromMilliseconds(200));

            DoubleAnimation rotateInAnimation = new DoubleAnimation();
            rotateInAnimation.Duration = duration;
            rotateInAnimation.From = 270;
            rotateInAnimation.To = 360;

            CubicEase easeIn = new CubicEase();
            easeIn.EasingMode = EasingMode.EaseIn;

            //rotateInAnimation.EasingFunction = easeIn;

            Storyboard rotateInStoryboard = new Storyboard();
            rotateInStoryboard.Duration = duration;
            rotateInStoryboard.Children.Add(rotateInAnimation);

            Storyboard.SetTarget(rotateInAnimation, canvasToRotate);
            Storyboard.SetTargetProperty(rotateInAnimation, "(UIElement.Projection).(PlaneProjection.RotationY)");

            DoubleAnimation rotateOutAnimation = new DoubleAnimation();

            rotateOutAnimation.Duration = duration;
            rotateOutAnimation.From = 0;
            rotateOutAnimation.To = 90;

            CubicEase easeOut = new CubicEase();
            easeOut.EasingMode = EasingMode.EaseOut;

            //rotateOutAnimation.EasingFunction = easeOut;

            Storyboard rotateOutStoryboard = new Storyboard();
            rotateOutStoryboard.Duration = duration;
            rotateOutStoryboard.Children.Add(rotateOutAnimation);

            rotateOutStoryboard.Completed += (o, s) =>
            {
                canvasOut.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                canvasIn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                rotateInStoryboard.Begin();
            };

            Storyboard.SetTarget(rotateOutAnimation, canvasToRotate);
            Storyboard.SetTargetProperty(rotateOutAnimation, "(UIElement.Projection).(PlaneProjection.RotationY)");

            if (item != null && canvasIn != null)
            {
                if (showRevers)
                {
                    this.RefreshButtons(canvasIn, item.Status, item.DownloadProgress);
                }
                else
                {
                    this.SetProgressCurtain(canvasIn, item.UpdateCurtainHeight(canvasIn.Height));
                    this.SetStatusInfo(canvasIn, item.Status);
                }
            }

            rotateOutStoryboard.Begin();
        }

        private Grid FindElementByContentId(string contentId)
        {
            FrameworkElement child0 = VisualTreeHelper.GetChild(this.itemGridView, 0) as FrameworkElement;
            FrameworkElement child1 = VisualTreeHelper.GetChild(child0, 0) as FrameworkElement;
            FrameworkElement child2 = VisualTreeHelper.GetChild(child1, 0) as FrameworkElement;
            FrameworkElement child3 = VisualTreeHelper.GetChild(child2, 0) as FrameworkElement;
            FrameworkElement child4 = VisualTreeHelper.GetChild(child3, 0) as FrameworkElement;
            FrameworkElement child5 = VisualTreeHelper.GetChild(child4, 0) as FrameworkElement;
            FrameworkElement child6 = VisualTreeHelper.GetChild(child5, 1) as FrameworkElement;

            FrameworkElement innerGrid = null;
            for (int idx = 0; idx < VisualTreeHelper.GetChildrenCount(child6); idx++)
            {
                FrameworkElement gridElement = VisualTreeHelper.GetChild(child6, idx) as FrameworkElement;
                FrameworkElement elementPresenter = VisualTreeHelper.GetChild(gridElement, 0) as FrameworkElement;
                innerGrid = VisualTreeHelper.GetChild(elementPresenter, 0) as FrameworkElement;

                if (innerGrid.Tag != null && contentId.Equals(innerGrid.Tag.ToString()))
                {
                    break;
                }
            }

            return (Grid)innerGrid;
        }

        private CollectionDataItem FindDataFromGridByContentId(string contentId)
        {
            if (!String.IsNullOrEmpty(contentId) && this.itemGridView.Items != null)
            {
                for (int idx = 0; idx < this.itemGridView.Items.Count; idx++)
                {
                    CollectionDataItem item = (CollectionDataItem)(this.itemGridView.Items[idx]);
                    if (item != null && item.ContentId.Equals(contentId))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private Canvas FindReverseByButton(Button button)
        {
            if (button == null)
                return null;

            Grid innerGrid = (Grid)button.Parent;
            if (innerGrid == null)
                return null;

            return (Canvas)innerGrid.Parent;
        }

        private CollectionDataItem FindDataFromGridByButton(Button button)
        {
            Canvas reverse = FindReverseByButton(button);
            if (reverse == null)
                return null;

            Canvas cover = (Canvas)reverse.Parent;
            if (cover == null)
                return null;

            return FindDataFromGridByContentId(((Grid)cover.Parent).Tag.ToString());
        }

        private void SetProgressPie(Canvas progressPie, int progress)
        {
            Double radius = (progressPie.ActualWidth < progressPie.ActualHeight ? progressPie.ActualWidth / 2 : progressPie.ActualHeight / 2) * 0.92;
            Double angle = progress * 360 / 100;

            if (angle == 360 || progress < 0)
                angle = 359.99;

            Windows.UI.Xaml.Shapes.Path progressPath = (Windows.UI.Xaml.Shapes.Path)progressPie.Children[0];
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

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {            
                        
        }

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {            
            UserDataItem user = Users.LoggedUser;
            SettingsPanel.Visibility = (!Users.AnyUserExists || user != null && user.IsAdmin) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            LogoutPanel.Visibility = (user != null && (user.IsSecured || Users.UsersAmount() > 1)) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            RefreshListPanel.Visibility = (user == null || user.AllowToManageHandbooks) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

            double coverHeight = -1;
            
            if (CollectionsDataSource.Source.FilteredCollections != null && CollectionsDataSource.Source.FilteredCollections.Count > 0)
            {
                if (CollectionsDataSource.Source.FilteredCollections[0] != null)
                {
                    Grid innerGrid = FindElementByContentId(CollectionsDataSource.Source.FilteredCollections[0].ContentId);
                    if (innerGrid != null)
                    {
                        Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                        if (cover != null)
                            coverHeight = cover.Height;
                    }
                }
            }
           
            foreach (CollectionDataItem item in CollectionsDataSource.Source.FilteredCollections)
            {
                if (item == null)
                    continue;

                if (coverHeight > 0)
                    item.UpdateCurtainHeight(coverHeight);
            }
          
            CollectionsDataSource.Source.DownloadEvent += Source_DownloadEvent;
            CollectionsDataSource.Source.ThumbEvent += Source_ThumbEvent;
            CollectionsDataSource.Source.UnzipEvent += Source_UnzipEvent;
            CollectionsDataSource.Source.UnzipProgressEvent += Source_UnzipProgressEvent;
            CollectionsDataSource.Source.ErrorEvent += Source_ErrorEvent;
            CollectionsDataSource.Source.CancelEvent += Source_CancelEvent;
        }

        private void reversProgressPie_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender != null && ((Canvas)sender).Children.Count > 0 && ((Canvas)sender).Children[0] != null)
            {
                Windows.UI.Xaml.Shapes.Path path = (Windows.UI.Xaml.Shapes.Path)((Canvas)sender).Children[0];
                Canvas.SetLeft(path, ((Canvas)sender).ActualWidth / 2);
                Canvas.SetTop(path, ((Canvas)sender).ActualHeight / 2);
            }

            Grid innerGrid = (Grid)((Canvas)sender).Parent;
            if (innerGrid == null)
                return;

            Canvas reverse = (Canvas)innerGrid.Parent;
            if (reverse == null)
                return;

            Canvas cover = (Canvas)reverse.Parent;
            if (cover == null)
                return;

            CollectionDataItem item = FindDataFromGridByContentId(((Grid)cover.Parent).Tag.ToString());

            if (item != null)
                this.SetProgressPie((Canvas)sender, item.UnzipProgress);
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

        private void Source_DownloadEvent(object sender, DownloadEventArgs e)
        {
            for (int idx = 0; idx < CollectionsDataSource.Source.FilteredCollections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.FilteredCollections[idx];
                if (item == null)
                    continue;

                if (item.ContentId != null && item.ContentId.Equals(e.ContentId))
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Grid innerGrid = FindElementByContentId(item.ContentId);
                        if (innerGrid != null)
                        {
                            Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                            if (cover != null)
                            {
                                Canvas obverse = VisualTreeHelper.GetChild(cover, 0) as Canvas;
                                if (obverse != null && obverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.SetProgressCurtain(obverse, item.UpdateCurtainHeight(obverse.Height));
                                    this.SetStatusInfo(obverse, item.Status);
                                }

                                Canvas reverse = VisualTreeHelper.GetChild(cover, 1) as Canvas;
                                if (reverse != null && reverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.RefreshButtons(reverse, item.Status, item.DownloadProgress);
                                }
                            }
                        }
                    });

                    break;
                }
            }
        }
        
        private void Source_UnzipEvent(object sender, UnzipEventArgs e)
        {
            if (e.Result < 0 && ((Frame)Window.Current.Content).SourcePageType.Name.Equals(this.GetType().Name))
            {
                var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
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
                    return;
                });
            }

            for (int idx = 0; idx < CollectionsDataSource.Source.FilteredCollections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.FilteredCollections[idx];
                if (item == null)
                    continue;

                if (item.ContentId != null && item.ContentId.Equals(e.ContentId))
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Grid innerGrid = FindElementByContentId(item.ContentId);
                        if (innerGrid != null)
                        {
                            Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                            if (cover != null)
                            {
                                Canvas obverse = VisualTreeHelper.GetChild(cover, 0) as Canvas;
                                if (obverse != null && obverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.SetProgressCurtain(obverse, 0);
                                    this.SetStatusInfo(obverse, item.Status);
                                }

                                Canvas reverse = VisualTreeHelper.GetChild(cover, 1) as Canvas;
                                if (reverse != null && reverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.RefreshButtons(reverse, item.Status, item.DownloadProgress);
                                }

                                StackPanel stack = VisualTreeHelper.GetChild(innerGrid, 1) as StackPanel;
                                if (stack != null)
                                {
                                    TextBlock title = VisualTreeHelper.GetChild(stack, 0) as TextBlock;
                                    if (title != null)
                                        title.Text = item.Title;

                                    TextBlock subtitle = VisualTreeHelper.GetChild(stack, 1) as TextBlock;
                                    if (item.Subtitle != null)
                                        subtitle.Text = String.IsNullOrEmpty(item.Subtitle) ? String.Empty : item.Subtitle;
                                }
                            }
                        }
                    });

                    break;
                }
            }
        }

        private void Source_ThumbEvent(object sender, ThumbEventArgs e)
        {
            for (int idx = 0; idx < CollectionsDataSource.Source.FilteredCollections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.FilteredCollections[idx];
                if (item == null)
                    continue;

                if (item.ContentId != null && item.ContentId.Equals(e.ContentId))
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.ReloadThumbImage(item);
                    });

                    break;
                }
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

        private void Source_CancelEvent(object sender, CancelEventArgs e)
        {
            for (int idx = 0; idx < CollectionsDataSource.Source.FilteredCollections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.FilteredCollections[idx];
                if (item == null)
                    continue;

                if (item.ContentId != null && item.ContentId.Equals(e.ContentId))
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Grid innerGrid = FindElementByContentId(item.ContentId);
                        if (innerGrid != null)
                        {
                            Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                            if (cover != null)
                            {
                                Canvas obverse = VisualTreeHelper.GetChild(cover, 0) as Canvas;
                                if (obverse != null && obverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.SetProgressCurtain(obverse, item.UpdateCurtainHeight(obverse.Height));
                                    this.SetStatusInfo(obverse, item.Status);
                                }

                                Canvas reverse = VisualTreeHelper.GetChild(cover, 1) as Canvas;
                                if (reverse != null && reverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    this.RefreshButtons(reverse, item.Status, item.DownloadProgress);
                                }
                            }
                        }
                    });

                    break;
                }
            }
        }

        private void Source_UnzipProgressEvent(object sender, UnzipProgressEventArgs e)
        {
            for (int idx = 0; idx < CollectionsDataSource.Source.FilteredCollections.Count; idx++)
            {
                CollectionDataItem item = CollectionsDataSource.Source.FilteredCollections[idx];
                if (item == null)
                    continue;

                if (item.ContentId != null && item.ContentId.Equals(e.ContentId))
                {
                    var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Grid innerGrid = FindElementByContentId(item.ContentId);
                        if (innerGrid != null)
                        {
                            Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
                            if (cover != null)
                            {
                                Canvas reverse = VisualTreeHelper.GetChild(cover, 1) as Canvas;
                                if (reverse != null && reverse.Visibility == Windows.UI.Xaml.Visibility.Visible)
                                {
                                    FrameworkElement innerReverseGrid = VisualTreeHelper.GetChild(reverse, 0) as FrameworkElement;
                                    FrameworkElement element = VisualTreeHelper.GetChild(innerReverseGrid, 11) as FrameworkElement;

                                    this.SetProgressPie((Canvas)element, item.UnzipProgress);
                                }
                            }
                        }
                    });

                    break;
                }
            }
        }

        #endregion 

        #region obsługa przycisków

        private async Task<bool> DownloadHandbook(Button button, bool isUpdate = false)
        {
            bool result = false;

            try
            {
                if (button == null)
                    return false;

                CollectionDataItem itemMain = FindDataFromGridByButton(button);
                CollectionDataItem itemToDownload = itemMain;

                if (isUpdate && itemMain.NewVersionItem != null)
                    itemToDownload = itemMain.NewVersionItem;

                // porównanie wersji                
                if (!itemToDownload.IsSufficientAppVersion())
                {
                    var messageDialog = new MessageDialog("Obsługa wybranego podręcznika wymaga nowszej wersji aplikacji. Aby pobrać podręcznik, zainstaluj najnowszą wersję aplikacji ze sklepu.");
                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                    messageDialog.Commands.Add(new UICommand("Pobierz aplikację", new UICommandInvokedHandler(this.LaunchStoreCommandInvokedHandler)));
                    await messageDialog.ShowAsync();
                    return false;
                }

                if (itemToDownload != null && itemToDownload.Formats != null)
                {
                    FormatDataItem format = itemToDownload.ChooseFormat(CollectionsDataSource.MaxWidth);

                    if (format == null)
                    {
                        var messageDialog = new MessageDialog("Korzystanie z podręcznika nie jest aktualnie możliwe, ponieważ odpowiedni format emisyjny nie jest dostępny.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                        return false;
                    }

                    string url = format.Url;

                    // sprawdzenie czy w systemie plików jest wystarczająco wolnej przestrzeni dyskowej do pobrania i rozpakowania podręcznika
                    ulong freeDiskSpace = await CollectionsDataSource.GetDiskFreeSpace();

                    if (format.Size > 0 && format.Zip > 0 && freeDiskSpace < format.Size + format.Zip)
                    {
                        var messageDialog = new MessageDialog(String.Format("Pobranie podręcznika nie jest możliwe z powodu niewystarczającej ilości wolnej przestrzeni na dysku. Aby pobrać i udpostępnić wybrany podręcznik konieczne jest minimum  {0} MB. Spróbuj zwolnić nieco miejsca i ponownie pobrać podręcznik później.", (format.Size + format.Zip) / 1024 / 1024));
                        messageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await messageDialog.ShowAsync();
                        return false;
                    }

                    if (!String.IsNullOrEmpty(url) && itemToDownload != null && itemToDownload.DownloadId != null)
                         CollectionsDataSource.Downloader.QueueDownload(new Uri(url), itemToDownload.ContentId + "_" + itemToDownload.Version, true, CollectionsDataSource.DownloadsFolderName);

                    itemMain.Status = isUpdate ? CollectionStatus.UpdateInProgress : CollectionStatus.DownloadInProgress;
                    itemMain.DownloadProgress = 0;
                    itemMain.CurtainHeight = 0;

                    await itemMain.WriteMetadataFromFileAsync();

                    Canvas reverse = FindReverseByButton(button);
                    if (reverse != null && itemMain != null)
                        this.RefreshButtons(reverse, itemMain.Status, itemMain.DownloadProgress);

                    result = true;
                }
            }
            catch
            {
                result = false;
            }

            if (!result)
            {
                var messageDialog = new MessageDialog(String.Format("Pobranie podręcznika zakończyło się błędem!"));
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }

            return result;
        }

        private void ShowEmptyListWarningInfo()
        {
            if (CollectionsDataSource.Source.FilteredCollections == null || CollectionsDataSource.Source.FilteredCollections.Count == 0)
            {
                String message = String.Empty;
                if (CollectionsDataSource.Source.Collections != null && CollectionsDataSource.Source.Collections.Count > 0)
                {
                    message = "Zmień filtr podręczników w konfiguracji konta użytkownika.";
                }
                else
                {
                    message = "Sprawdź połączenie z siecią Internet a następnie odśwież listę.";
                }

                lblNoBooksInfoOrder.Text = message;
                NoBooksMessageInfoStackPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async Task<bool> RefreshList()
        {
            bool result = false;
            this.RefreshListButton.IsEnabled = false;
            // sprawdzenie czy jest coś do pokazania: pobranie listy podręczników
            this.ShowProgressIndicator(true, "pobieranie danych w toku...");            
            this.HideAllUpdateIndicators();

            CollectionsDataSource.SetMaxWidth(Window.Current.Bounds);

            try
            {
                NoBooksMessageInfoStackPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                IEnumerable<CollectionDataItem> collections = await CollectionsDataSource.GetCollectionsAsync(true);

                if (collections == null)
                {
                    var messageDialog = new MessageDialog("Aktualizacja danych o podręcznikach nie jest teraz możliwa: nie można połączyć się z serwerem. Spróbuj ponownie później.");
                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                    await messageDialog.ShowAsync();
                }
                else
                {
                    this.DefaultViewModel["Collections"] = CollectionsDataSource.Source.FilteredCollections;

                    this.ShowEmptyListWarningInfo();
                }                

                result = true;
            }
            catch
            {
                result = false;
            }

            this.ShowProgressIndicator(false);
            this.RefreshListButton.IsEnabled = true;

            if (!result)
            {
                var messageDialog = new MessageDialog(String.Format("Aktualizacja danych o podręcznikach zakończyła się błędem!"));
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }

            return result;
        }

        private async void LaunchStoreCommandInvokedHandler(IUICommand command)
        {            
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:PDP?PFN=" + Windows.ApplicationModel.Package.Current.Id.FamilyName));
        }

        private async Task<bool> DeleteHandbook(Button button)
        {
            bool result = false;

            try
            {
                if (button == null)
                    return false;

                CollectionDataItem item = FindDataFromGridByButton(button);

                if (item != null)
                {
                    item.Status = CollectionStatus.NotDownloaded;
                    Canvas reverse = FindReverseByButton(button);
                    if (reverse != null)
                        this.RefreshButtons(reverse, item.Status, item.DownloadProgress);

                    if (await CollectionsDataSource.RemoveCollection(item.ContentId))
                    {                        
                        item.FolderSize = String.Empty;                        

                        this.ReloadThumbImage(item);

                        Grid innerGrid = FindElementByContentId(item.ContentId);
                        if (innerGrid != null)
                        {
                            StackPanel stack = VisualTreeHelper.GetChild(innerGrid, 1) as StackPanel;
                            if (stack != null)
                            {
                                TextBlock title = VisualTreeHelper.GetChild(stack, 0) as TextBlock;
                                if (title != null)
                                    title.Text = item.Title;

                                TextBlock subtitle = VisualTreeHelper.GetChild(stack, 1) as TextBlock;
                                if (item.Subtitle != null)
                                    subtitle.Text = String.IsNullOrEmpty(item.Subtitle) ? String.Empty : item.Subtitle;
                            }
                        }
                        result = true;
                    }                    
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

        private async Task<IUICommand> ShowMessageAndStartAction(ActionDelegate action, string message, Button button)
        {
            var messageDialog = new MessageDialog(message.ToString());

            messageDialog.Commands.Add(new UICommand("Tak", new UICommandInvokedHandler(action), button));
            messageDialog.Commands.Add(new UICommand("Nie", null));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            return await messageDialog.ShowAsync();
        }        

        private async void DownloadCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is Button)
            {
                await this.DownloadHandbook((Button)command.Id, false);
            }
        }

        private async void RefreshListCommandInvokedHandler(IUICommand command)
        {                        
            await this.RefreshList();            
        }
        
        private async void CancelCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is Button)
            {
                bool result = false;

                try
                {
                    Button button = (Button)command.Id;
                    if (button != null)
                    {

                        CollectionDataItem item = FindDataFromGridByButton(button);

                        if (item != null && item.DownloadId != null)
                        {
                            item.Status = item.Status == CollectionStatus.UpdateInProgress ? CollectionStatus.CancelUpdatePending : CollectionStatus.CancelDownloadPending;

                            CollectionsDataSource.Downloader.CancelDownload(item.DownloadId);

                            await item.WriteMetadataFromFileAsync();
                        }

                        result = true;
                    }
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

        private async void DeleteCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is Button)
            {
                await this.DeleteHandbook((Button)command.Id);
            }
        }

        private async void UpdateCommandInvokedHandler(IUICommand command)
        {
            if (command.Id != null && command.Id is Button)
            {
                await this.DownloadHandbook((Button)command.Id, true);
            }
        }

        private void obverseCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Canvas obverse = (Canvas)sender;
            if (obverse == null)
                return;

            Canvas cover = (Canvas)obverse.Parent;
            if (cover == null)
                return;

            Grid innerGrid = (Grid)cover.Parent;
            if (innerGrid == null)
                return;

            CollectionDataItem item = this.FindDataFromGridByContentId(innerGrid.Tag.ToString());

            if (item == null || String.IsNullOrEmpty(item.ContentId))
                return;

            this.RotateElement(cover, item);
        }

        private void titleStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StackPanel panel = (StackPanel)sender;
            if (panel == null)
                return;

            Grid innerGrid = (Grid)panel.Parent;
            if (innerGrid == null)
                return;

            Canvas cover = VisualTreeHelper.GetChild(innerGrid, 0) as Canvas;
            if (cover == null)
                return;

            CollectionDataItem item = this.FindDataFromGridByContentId(innerGrid.Tag.ToString());

            if (item == null || String.IsNullOrEmpty(item.ContentId))
                return;            

            this.RotateElement(cover, item);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Canvas reverse = FindReverseByButton((Button)sender);
            if (reverse == null)
                return;

            Canvas cover = (Canvas)reverse.Parent;
            if (cover == null)
                return;

            CollectionDataItem item = this.FindDataFromGridByContentId(((Grid)cover.Parent).Tag.ToString());
            
            if(item!= null)
                this.RotateElement(cover, item);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            //CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            //if (item != null && item.NewVersionItem != null)
            //    this.Frame.Navigate(typeof(UpdateConfirmationPage), item.NewVersionItem);  


            CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            if (item != null && item.NewVersionItem != null)
            {
                int connection = App.IsConnectedToInternet();

                if (connection > 0)
                {
                    StringBuilder message = new StringBuilder();
                    if (connection > 1 && !App.Use3GConnection)
                    {                        
                        message.Append("Brak połączenia z siecią WiFi. Czy zezwalasz na użycie sieci komórkowej w celu uaktualnienia podręcznika: ");
                        message.Append(item.Title);
                        message.Append(" do wersji: ");
                        message.Append(item.NewVersionItem.Version);
                        message.Append("?");                                                
                    }
                    else
                    {                        
                        message.Append("Czy chcesz uaktualnić podręcznik: ");
                        message.Append(item.Title);
                        message.Append(" do wersji: ");
                        message.Append(item.NewVersionItem.Version);
                        message.Append("?");                        
                    }

                    await this.ShowMessageAndStartAction(this.UpdateCommandInvokedHandler, message.ToString(), (Button)sender);
                }
                else
                {                    
                    string message = "Urządzenie nie ma połączenia z siecią Internet. Czy chcesz automatycznie rozpocząć pobieranie po przywróceniu połączenia?";
                    await this.ShowMessageAndStartAction(this.UpdateCommandInvokedHandler, message, (Button)sender);
                }                         
            }                                    
        }

        private async void CancelUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            if (item != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz przerwać pobieranie uaktualnienia podręcznika: ");
                message.Append(item.Title);
                message.Append(" i usunąć wszystkie skopiowane do tej pory pliki uaktualnienia?");

                await this.ShowMessageAndStartAction(this.CancelCommandInvokedHandler, message.ToString(), (Button)sender);
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
                    await this.ShowMessageAndStartAction(this.DownloadCommandInvokedHandler, message, (Button)sender);
                }
                else
                {
                    await this.DownloadHandbook((Button)sender, false);
                }
            }                        
            else
            {
                string message = "Urządzenie nie ma połączenia z siecią Internet. Czy chcesz automatycznie rozpocząć pobieranie po przywróceniu połączenia?";
                await this.ShowMessageAndStartAction(this.DownloadCommandInvokedHandler, message, (Button)sender);                             
            }
        }        

        private async void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            if (item != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz przerwać pobieranie podręcznika: ");
                message.Append(item.Title);
                message.Append(" i usunąć wszystkie skopiowane do tej pory pliki podręcznika?");

                await this.ShowMessageAndStartAction(this.CancelCommandInvokedHandler, message.ToString(), (Button)sender);                                         
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

            CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            if (item != null)
            {
                StringBuilder message = new StringBuilder();
                message.Append("Czy chcesz usunąć podręcznik: ");
                message.Append(item.Title);
                message.Append("?");

                await this.ShowMessageAndStartAction(this.DeleteCommandInvokedHandler, message.ToString(), (Button)sender);                                         
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionDataItem item = FindDataFromGridByButton((Button)sender);

            if (item != null && (item.Status == CollectionStatus.Downloaded || item.Status == CollectionStatus.UpdateRequired))
            {                
                if(item.GetToCFileAsync() != null && item.GetPagesFileAsync() != null)
                    this.Frame.Navigate(typeof(ToCReaderPage), item.ContentId);
                else
                    this.Frame.Navigate(typeof(ReaderPage), item.ContentId);
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionDataItem item = FindDataFromGridByButton((Button)sender);
            
            this.Frame.Navigate(typeof(HandbookDetailsPage), item.ContentId);
        }

        private void About_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            this.Frame.Navigate(typeof(AboutPage), null);
        }

        private void PrivacyPolicy_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivacyPolicyPage), false);
        }

        private async void RefreshList_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            // sprawdź czy jest dostęp do sieci
            int connection = App.IsConnectedToInternet();

            if (connection > 0)
            {
                if (connection > 1 && !App.Use3GConnection)
                {
                    string message = "Brak połączenia z siecią WiFi. Czy zezwalasz na użycie sieci komórkowej w celu aktualizacji danych o podręcznikach?";
                    await this.ShowMessageAndStartAction(this.RefreshListCommandInvokedHandler, message, null);
                }
                else
                {
                    await this.RefreshList();
                }
            }
            else
            {
                //pokaż stosowny komunikat
                var messageDialog = new MessageDialog("Aktualizacja danych o podręcznikach nie jest teraz możliwa: brak połączenia z siecią Internet. Spróbuj ponownie później.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }
        }

        private void ConfigButton_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            this.Frame.Navigate(typeof(ConfigPage), null);
        }        
        
        private void SettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UserDataItem user = Users.LoggedUser;
            if (!Users.AnyUserExists)
            {
                this.Frame.Navigate(typeof(SettingsPage), null);
            }
            else if (user != null && user.IsAdmin)
            {
                this.Frame.Navigate(typeof(LoginPage), new NavigationPageParameter(true, "SettingsPage"));                
            }
        }

        private void LogoutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Users.LoggedUser = null;
            this.Frame.Navigate(typeof(LoginPage), new NavigationPageParameter(false, null));
        }

        #endregion
    }
}