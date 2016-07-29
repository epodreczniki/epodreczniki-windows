using ePodrecznikiDesktop.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ePodrecznikiDesktop
{
    /// <summary>
    /// Interaction logic for HandbooksListPage.xaml
    /// </summary>
    public partial class HandbooksListPage : Page
    {
        private bool _resizeList = false;
        private bool _operationInProgress = false;

        public event HandbookDelegate HandbookDelegate;

        private static ObservableCollection<CollectionDataItem> _collections;

        public HandbooksListPage(int? handbookId)
        {
            InitializeComponent();

            this.DataContext = this;

            CollectionsDataSource.SetMaxWidth(new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight));
            _collections = (ObservableCollection<CollectionDataItem>)CollectionsDataSource.GetCollectionsAsync();

            SetSelectedHandbook(handbookId);
            
            CollectionsDataSource.Source.UnzipProgressEvent += Source_UnzipProgressEvent;
            CollectionsDataSource.Source.UnzipEvent += Source_UnzipEvent;            
            CollectionsDataSource.Source.DeleteEvent += Source_DeleteEvent;
        }

        public void SetSelectedHandbook(int? handbookId)
        {
            if (handbookId == null)
            {
                handbookId = (int)Properties.Settings.Default["SelectedHandbook"];

                if (handbookId < 0)
                    handbookId = null;
            }

            if (handbookId == null)
                ui_listData.SelectedIndex = 0;
            else
            {
                CollectionDataItem selectedItem = _collections.Where(item => item.Id == handbookId).FirstOrDefault();
                if (selectedItem == null)
                    ui_listData.SelectedIndex = 0;
                else
                    ui_listData.SelectedItem = selectedItem;

                ui_listData.ScrollIntoView(selectedItem);
            }
        }

        void Source_DeleteEvent(object sender, DeleteEventArgs e)
        {            
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                (System.Windows.Forms.MethodInvoker)delegate()
                {
                    int selectedIndex = ui_listData.SelectedIndex;

                    if (e.Item != null)
                        _collections.Remove(e.Item);

                    _operationInProgress = false;

                    ui_AddButton.IsEnabled = true;
                    ui_DeleteButton.IsEnabled = ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0;
                    ui_DetailsButton.IsEnabled = ui_DeleteButton.IsEnabled;
                    ui_ReadButton.IsEnabled = ui_DeleteButton.IsEnabled;

                    ui_ProgressTextBlock.Visibility = System.Windows.Visibility.Collapsed;

                    if (e.Result)
                    {
                        MessageBox.Show("Podręcznik został usunięty.");
                    }
                    else
                    {
                        MessageBox.Show("Podczas usuwania podręcznika wystąpił błąd.");
                    }                    

                    if (selectedIndex < ui_listData.Items.Count)
                        ui_listData.SelectedIndex = selectedIndex;
                    else if (selectedIndex > 0)
                        ui_listData.SelectedIndex = selectedIndex - 1;
                }
            );            
        }

        void Source_UnzipEvent(object sender, UnzipEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UnzipCompleteDelegate(UnzipComplete), e.ContentId, e.Result);            
        }

        private delegate void UpdateProgressDelegate(int progress);
        private delegate void UnzipCompleteDelegate(string folderName, sbyte result);
        

        private void UpdateProgress(int progress)
        {
            ui_ProgressTextBlock.Visibility = System.Windows.Visibility.Visible;
            ui_ProgressTextBlock.Text = "Kopiowanie i rozpakowanie podręcznika w toku: " + progress.ToString() + "%";
        }

        private void UnzipComplete(string folderName, sbyte result)
        {
            ui_ProgressTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            ErrorCode errorCode;
            int id;

            _collections = (ObservableCollection<CollectionDataItem>)CollectionsDataSource.RefreshCollectionsAsync(out errorCode, out id);

            if(errorCode  != ErrorCode.OK)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Wystąpił problem z dodaniem podręcznika o identyfikatorze: ");
                sb.Append(id.ToString());
                switch (errorCode)
                {
                    case ErrorCode.WrongFolderName:
                        sb.Append(": nie znaleziono folderu podręcznika.");
                        break;
                    case ErrorCode.WrongCollectionId:
                        sb.Append(": nazwa kolekcji podręcznika jest nieprawidłowa.");
                        break;
                    case ErrorCode.WrongMetadata:
                        sb.Append(": metadane kolekcji podręcznika zawierają błędy.");
                        break;
                    case ErrorCode.RuntimeError:
                        sb.Append(".");
                        break;
                    default:
                        sb.Append(".");
                        break;
                }

                MessageBox.Show(sb.ToString());                            
            }

            _operationInProgress = false;

            ui_AddButton.IsEnabled = true;
            ui_DeleteButton.IsEnabled = ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0;
            ui_DetailsButton.IsEnabled = ui_DeleteButton.IsEnabled;
            ui_ReadButton.IsEnabled = ui_DeleteButton.IsEnabled;

            _resizeList = true;
        }

        void Source_UnzipProgressEvent(object sender, UnzipProgressEventArgs e)
        {
            int i = e.Progress;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateProgressDelegate(UpdateProgress), e.Progress );            
        }

        public static ObservableCollection<CollectionDataItem> Collections
        {
            get
            {
                return _collections;
            }
        }

        public static CollectionDataItem GetCollectionDataItemById(int id)
        {            
            return (CollectionDataItem)Collections.FirstOrDefault(m => m.Id == id);
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            int? id = null;
            if (ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0)
                id = (ui_listData.SelectedItems[0] as CollectionDataItem).Id;

            HandbookDelegate(PageType.About, id);
        }

        private void PrivacyPolicyButton_Click(object sender, RoutedEventArgs e)
        {
            int? id = null;
            if (ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0)
                id = (ui_listData.SelectedItems[0] as CollectionDataItem).Id;

            HandbookDelegate(PageType.Terms, id);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
                {
                    dlg.Title = "Wybierz plik z podręcznikiem";
                    dlg.DefaultExt = ".zip";
                    dlg.Filter = "Skompresowane pliki (*.zip)|*.zip|Wszystkie pliki|*.*";
                    //dlg.SelectedPath = Text;

                    System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {

                        if(!CollectionsDataSource.CheckUniquenessOfNewCollections(dlg.FileName))
                        {
                            if (MessageBox.Show("W programie istnieją już podręczniki o identyfikatorach równych do tych jakie są zawarte we wskazanym archiwum.\r\n Czy chcesz nadpisać istniejące podręczniki?", "Dodawanie podręcznika", MessageBoxButton.YesNo) == MessageBoxResult.No)
                                return;
                        }

                        ui_AddButton.IsEnabled = false;
                        ui_DeleteButton.IsEnabled = false;

                        _operationInProgress = true;

                        if(!CollectionsDataSource.UnZipFile(dlg.FileName))
                        {
                            MessageBox.Show("Rozpakowanie plików podręcznika nie powiodło się!");
                        }
                        else
                        {
                            _resizeList = true;
                        }
                    }
                }
            }
            catch
            {

            }            
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CollectionDataItem item = null;

                if (ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0)
                    item = ui_listData.SelectedItems[0] as CollectionDataItem;

                if (item != null)
                {                    
                    StringBuilder message = new StringBuilder();
                    message.Append("Czy chcesz usunąć podręcznik: ");
                    message.Append(item.Title);
                    message.Append("?");

                    if (MessageBox.Show(message.ToString(), "Usuwanie podręcznika", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        ui_AddButton.IsEnabled = false;
                        ui_DeleteButton.IsEnabled = false;
                        ui_DetailsButton.IsEnabled = false;
                        ui_ReadButton.IsEnabled = false;

                        _operationInProgress = true;

                        ui_ProgressTextBlock.Visibility = System.Windows.Visibility.Visible;
                        ui_ProgressTextBlock.Text = "Usuwanie podręcznika w toku...";

                        CollectionsDataSource.RemoveHandbook((ui_listData.SelectedItems[0] as CollectionDataItem).ContentId);
                    }                    
                }
            }
            catch
            {
                MessageBox.Show("Błąd podczas usuwania podręcznika.");
            }            
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0)
                HandbookDelegate(PageType.HandbookDetails, (ui_listData.SelectedItems[0] as CollectionDataItem).Id);
        }

        private void listData_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ResizeHandbooksList();
        }

        private void ResizeHandbooksList()
        {
            FrameworkElement child0 = VisualTreeHelper.GetChild(this.ui_listData, 0) as FrameworkElement;
            FrameworkElement child1 = VisualTreeHelper.GetChild(child0, 0) as FrameworkElement;
            FrameworkElement child2 = VisualTreeHelper.GetChild(child1, 0) as FrameworkElement;
            FrameworkElement child3 = VisualTreeHelper.GetChild(child2, 0) as FrameworkElement;

            FrameworkElement scrollContentPresenter = VisualTreeHelper.GetChild(child2, 1) as FrameworkElement;
            FrameworkElement itemsPresenter = VisualTreeHelper.GetChild(scrollContentPresenter, 0) as FrameworkElement;
            FrameworkElement wrapPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as FrameworkElement;
            if (VisualTreeHelper.GetChildrenCount(wrapPanel) > 0)
            {
                for (int idx = 0; idx < VisualTreeHelper.GetChildrenCount(wrapPanel); idx++)
                {
                    FrameworkElement item = VisualTreeHelper.GetChild(wrapPanel, idx) as FrameworkElement;
                    if (item == null)
                        continue;

                    FrameworkElement border = null;
                    if (VisualTreeHelper.GetChildrenCount(item) == 0)
                        continue;

                    border = VisualTreeHelper.GetChild(item, 0) as FrameworkElement;

                    // schowanie ramki dla wyselekcjonowanego elementu:
                    if(border != null)
                        ((Border)border).BorderBrush = new SolidColorBrush(Colors.Transparent);

                    FrameworkElement contentPresenter = VisualTreeHelper.GetChild(border, 0) as FrameworkElement;
                    FrameworkElement grid = VisualTreeHelper.GetChild(contentPresenter, 0) as FrameworkElement;

                    Canvas cover = (Canvas)((Grid)grid).Children[0];
                    StackPanel titleStack = (StackPanel)((Grid)grid).Children[1];
                    TextBlock textTitle = (TextBlock)titleStack.Children[0];
                    TextBlock textSubtitle = (TextBlock)titleStack.Children[1];

                    Image imageDefault = (Image)cover.Children[0];
                    Image imageCover = (Image)cover.Children[1];
                    Canvas curtain = (Canvas)cover.Children[2];

                    double scale = (scrollContentPresenter.ActualHeight - 4) / 800;
                    try
                    {
                        grid.Width = 480 * scale;
                        cover.Height = 680 * scale;

                        imageDefault.Height = cover.Height;
                        imageCover.Height = cover.Height;
                        curtain.Width = grid.Width;

                        titleStack.Height = 120 * scale;
                        textTitle.FontSize = 32 * scale;
                        textTitle.LineHeight = 32 * scale;
                        textSubtitle.FontSize = textTitle.FontSize;
                        textSubtitle.LineHeight = textTitle.LineHeight;

                        //((ScaleTransform)((Canvas)cover).RenderTransform).ScaleX = scale;
                        //((ScaleTransform)((Canvas)cover).RenderTransform).ScaleY = scale;
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.ResizeHandbooksList();
        }

        private void listData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_operationInProgress)
                return;

            CollectionDataItem selectedItem = ((ListView)e.Source).SelectedItem as CollectionDataItem;

            ui_DeleteButton.IsEnabled = selectedItem != null;
            ui_DetailsButton.IsEnabled = selectedItem != null;
            ui_ReadButton.IsEnabled = selectedItem != null;

            if (selectedItem != null)
            {
                Properties.Settings.Default["SelectedHandbook"] = selectedItem.Id;
                Properties.Settings.Default.Save();
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            this.ReadSelectedHandbook();
        }

        private void coverGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                this.ReadSelectedHandbook();
            }
        }

        private void ReadSelectedHandbook()
        {
            if (ui_listData.SelectedItems != null && ui_listData.SelectedItems.Count > 0)
                HandbookDelegate(PageType.HandbookRead, (ui_listData.SelectedItems[0] as CollectionDataItem).Id);
        }

        private void listData_LayoutUpdated(object sender, EventArgs e)
        {
            if(_resizeList)
            {
                this.ResizeHandbooksList();
                _resizeList = false;
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            String fileName = "pomoc.pdf";
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fileName;
                process.Start();
                process.WaitForExit();
            }
            catch(Exception exc)
            {
                //MessageBox.Show("Błąd podczas próby otwarcia pliku pomocy.");
            }
        }        
    }
}
