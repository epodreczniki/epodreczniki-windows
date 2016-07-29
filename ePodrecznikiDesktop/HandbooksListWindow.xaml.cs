using ePodrecznikiDesktop.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ePodrecznikiDesktop
{
    /// <summary>
    /// Interaction logic for HandbooksListWindow.xaml
    /// </summary>
    public partial class HandbooksListWindow : Window
    {        
        private ObservableCollection<CollectionDataItem> _collections;
        
        public HandbooksListWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            CollectionsDataSource.SetMaxWidth(new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight));
            _collections = (ObservableCollection<CollectionDataItem>)CollectionsDataSource.GetCollectionsAsync();
        }

        public ObservableCollection<CollectionDataItem> Collections
        {
            get
            {
                return _collections;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PrivacyPolicyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void listData_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ResizeHandbooksList();
        }


        private void ResizeHandbooksList()
        {
            FrameworkElement child0 = VisualTreeHelper.GetChild(this.listData, 0) as FrameworkElement;
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

                    FrameworkElement border = VisualTreeHelper.GetChild(item, 0) as FrameworkElement;
                    FrameworkElement contentPresenter = VisualTreeHelper.GetChild(border, 0) as FrameworkElement;
                    FrameworkElement grid = VisualTreeHelper.GetChild(contentPresenter, 0) as FrameworkElement;

                    Canvas cover = (Canvas)((Grid)grid).Children[0];
                    StackPanel titleStack = (StackPanel)((Grid)grid).Children[1];
                    TextBlock textTitle = (TextBlock)titleStack.Children[0];
                    TextBlock textSubtitle = (TextBlock)titleStack.Children[1];
                    Image image = (Image)cover.Children[0];
                    Canvas curtain = (Canvas)cover.Children[1];

                    double scale = (scrollContentPresenter.ActualHeight - 4) / 800;
                    try
                    {
                        grid.Width = 480 * scale;
                        cover.Height = 680 * scale;

                        image.Height = cover.Height;
                        curtain.Width = grid.Width;

                        titleStack.Height = 120 * scale;
                        textTitle.FontSize = 26 * scale;
                        textTitle.LineHeight = 30 * scale;
                        textSubtitle.FontSize = textTitle.FontSize;
                        textSubtitle.LineHeight = textTitle.LineHeight;

                        //((ScaleTransform)((Canvas)cover).RenderTransform).ScaleX = scale;
                        //((ScaleTransform)((Canvas)cover).RenderTransform).ScaleY = scale;
                    }
                    catch (Exception exc)
                    {

                    }
                }
            }            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ResizeHandbooksList();
        }
    }
}
