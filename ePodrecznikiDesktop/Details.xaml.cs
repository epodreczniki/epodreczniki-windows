using ePodrecznikiDesktop.DataModel;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Details.xaml
    /// </summary>
    public partial class Details : Page
    {
        public event HandbookDelegate HandbookDelegate;

        private CollectionDataItem _handbook;

        public CollectionDataItem Handbook
        {
            set
            {
                if(value.CoverThumbImage != null)
                    ui_Cover.Source = value.CoverThumbImage;
                ui_Title.Text = value.Title;
                ui_School.Text = value.School.EducationLevel;
                ui_Class.Text = value.School.Class.ToString();
                ui_Subject.Text = value.Subject.Subject;                

                if (!string.IsNullOrEmpty(value.MainAuthor))
                {
                    ui_Abstract.Text = value.Abstract;
                }
                else
                {
                    ui_PanelAbstract.Visibility = Visibility.Collapsed;
                }


                if (!string.IsNullOrEmpty(value.Signature))
                {
                    byte[] buffer = Convert.FromBase64String(value.Signature);                    
                    ui_WebBrowserSignature.NavigateToString(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
                }
                else
                {
                    ui_PanelSignature.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(value.MainAuthor))
                {
                    ui_MainAuthor.Text = value.MainAuthor;
                }
                else
                {
                    ui_PanelMainAuthor.Visibility = Visibility.Collapsed;
                }
                
                if (value.Authors != null && value.Authors.Count() > 0)
                {
                    foreach(Role role in value.Authors)
                    {                        
                        TextBlock label = new TextBlock();
                        label.Foreground = new SolidColorBrush(Colors.White);
                        label.Style = Resources["HandbookSmallAttributeLabelBlockStyle"] as Style;
                        ui_PanelAuthors.Children.Add(label);

                        TextBlock authors = new TextBlock();
                        authors.Foreground = new SolidColorBrush(Colors.White);
                        authors.TextWrapping = TextWrapping.Wrap;
                        authors.Style = Resources["HandbookSmallAttributeTextBlockStyle"] as Style;
                        authors.Margin = new Thickness(0,5,0,0);
                        ui_PanelAuthors.Children.Add(authors);
                    }                    
                }
                else
                {
                    ui_PanelAuthors.Visibility = Visibility.Collapsed;
                }
                             
                _handbook = value;
            }
        }

        public Details(int handbookId)
        {
            InitializeComponent();
            
            Handbook = HandbooksListPage.GetCollectionDataItemById(handbookId);
        }

        public string GetAuthorsString(AuthorDataItem[] authors)
        {
            StringBuilder output = new StringBuilder();
            if (authors != null)
            {
                foreach(AuthorDataItem author in authors)
                {
                    output.Append(author.FullName + ", ");
                }
            }
            return output.ToString().Trim(new char[]{' ', ','});
        }

        private void ui_Cover_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((ScaleTransform)((TransformGroup)ui_CoverReadPath.RenderTransform).Children[0]).ScaleX = ui_Cover.ActualWidth * 0.5 / 260;
            ((ScaleTransform)((TransformGroup)ui_CoverReadPath.RenderTransform).Children[0]).ScaleY = ui_Cover.ActualWidth * 0.5 / 260;
        }

        private void ui_CoverReadMask_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_handbook != null)
                HandbookDelegate(PageType.HandbookRead, _handbook.Id);
        }

        private void ui_CoverReadMask_MouseEnter(object sender, MouseEventArgs e)
        {                        
            ui_CoverReadPath.Opacity = 1;
            ui_CoverMaskBackground.Visibility = System.Windows.Visibility.Visible;
        }

        private void ui_CoverReadMask_MouseLeave(object sender, MouseEventArgs e)
        {                     
            ui_CoverReadPath.Opacity = 0;
            ui_CoverMaskBackground.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
