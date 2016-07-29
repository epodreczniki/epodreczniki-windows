using epodreczniki.Common;
using epodreczniki.Data;
using epodreczniki.DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace epodreczniki
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ToCReaderPage : Page
    {
        #region pola klasy

        private enum ListsTab { ToCTab, BookmarksTab, NotesTab };
        private List<Button> _lstListsHeaders = new List<Button>();
        private List<RadioButton> _lstNoteTypes = new List<RadioButton>();
        private List<ScrollViewer> _lstListsViewers = new List<ScrollViewer>();        

        private string _contentId;
        private string _path;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private ToCDataItem _root;
        private string _currentToCId;
        private bool _isToCShown = false;
        private bool _isNoteShown = false;
        private bool _isAddBookmarkPressed = false;

        private NewNoteData _newNote = null;
        private NoteDataItem _shownNote = null;

        private int _idxPage = 0;
        private string _noteIdToJump = String.Empty;

        private int _idxFirstPage = -1;
        private int _idxLastPage = -1;

        private int _idxView = 0;
        private bool _preventHandleSelectionChange = true;
        private bool _changeAfterTocItemSelection = false;
        private PageDataItem[] _pages;
        private bool _selectionCompletionInProgress = false;

        private StreamUriWinRTResolver _resolver = new StreamUriWinRTResolver();

        private event EventHandler<EventArgs> SelectionChangeCompleted;
        private List<Int32> _lstHistory = new List<Int32>();
        private int _idxHistory = -1;

        private ListsTab _selectedListsTab = ListsTab.ToCTab;

        #endregion

        #region propertisy

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

        private string CurrentToCIdFromLocalSettings
        {
            get
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("ToCId_" + _contentId + Users.LoggedUserId))
                        return localSettings.Values["ToCId_" + _contentId + Users.LoggedUserId] as String;
                }

                return String.Empty;
            }

            set
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["ToCId_" + _contentId + Users.LoggedUserId] = value;
                }
            }
        }

        private string CurrentPageFromLocalSettings
        {
            get
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("Page_" + _contentId + Users.LoggedUserId))
                        return localSettings.Values["Page_" + _contentId + Users.LoggedUserId] as String;
                }

                return String.Empty;
            }

            set
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["Page_" + _contentId + Users.LoggedUserId] = value;
                }
            }
        }

        private string HistoryFromLocalSettings
        {
            get
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("History_" + _contentId + Users.LoggedUserId))
                        return localSettings.Values["History_" + _contentId + Users.LoggedUserId] as String;
                }

                return String.Empty;
            }

            set
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["History_" + _contentId + Users.LoggedUserId] = value;
                }
            }
        }

        private string HistoryIndexFromLocalSettings
        {
            get
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("HistoryIndex_" + _contentId + Users.LoggedUserId))
                        return localSettings.Values["HistoryIndex_" + _contentId + Users.LoggedUserId] as String;
                }

                return String.Empty;
            }

            set
            {
                if (!String.IsNullOrEmpty(_contentId))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["HistoryIndex_" + _contentId + Users.LoggedUserId] = value;
                }
            }
        }

        #endregion

        #region konstruktor

        public ToCReaderPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            this.SelectionChangeCompleted += ToCReaderPage_SelectionChangeCompleted;

            this.tocHeaderButton.Tag = ListsTab.ToCTab;
            _lstListsHeaders.Add(this.tocHeaderButton);
            this.bookmarksHeaderButton.Tag = ListsTab.BookmarksTab;
            _lstListsHeaders.Add(this.bookmarksHeaderButton);
            this.notesHeaderButton.Tag = ListsTab.NotesTab;
            _lstListsHeaders.Add(this.notesHeaderButton);
            
            _lstNoteTypes.Add(this.type0Button);
            _lstNoteTypes.Add(this.type1Button);
            _lstNoteTypes.Add(this.type2Button);
            _lstNoteTypes.Add(this.type3Button);
            _lstNoteTypes.Add(this.type4Button);
            _lstNoteTypes.Add(this.type5Button);
            _lstNoteTypes.Add(this.type6Button);

            foreach (RadioButton button in _lstNoteTypes)
            {
                if (button == null)
                    continue;

                SetTypeButtonColor(button);                
            }

            this.tocScrollViewer.Tag = ListsTab.ToCTab;
            _lstListsViewers.Add(this.tocScrollViewer);
            this.bookmarksScrollViewer.Tag = ListsTab.BookmarksTab;
            _lstListsViewers.Add(this.bookmarksScrollViewer);
            this.notesScrollViewer.Tag = ListsTab.NotesTab;
            _lstListsViewers.Add(this.notesScrollViewer);
        }

        private void SetTypeButtonColor(RadioButton button)
        {
            if (button == null || String.IsNullOrEmpty(button.Tag.ToString()))
                return;

            NoteType type;
            if(Enum.TryParse<NoteType>(button.Tag.ToString(), out type))
            {
                button.Foreground = NoteDataItem.ConvertToColorBrush(NoteDataItem.GetTypeColor(type));
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selType">typ notatki/zakładki: przycisk związany z tym typem ma zostać wyróżniony</param>
        /// <param name="setEnabled">jeśli true, wówczas pokazane są wszystkie przyciski (dla notatki lub zakładki),
        /// jeśli false wówczas wszystkie przyciski są nieaktywne, dodatkowo widoczny jest tylko przycisk zwązany
        /// z typem wskazanym przez pierwszy parametr</param>
        /// <param name="isNote">true: jeśli dotyczy notatek, false, jeśli zakładek</param>
        private void SetTypeButtonsState(NoteType selType, bool setEnabled, bool isNote)
        {
            foreach (RadioButton button in _lstNoteTypes)
            {
                if (button == null || String.IsNullOrEmpty(button.Tag.ToString()))
                    continue;                

                NoteType type;
                if (Enum.TryParse<NoteType>(button.Tag.ToString(), out type))
                {                    
                    button.Foreground = NoteDataItem.ConvertToColorBrush(NoteDataItem.GetTypeColor(type));
                    button.IsChecked = type.Equals(selType);
                    button.IsEnabled = setEnabled;
                    button.Visibility = button.IsChecked == true || (setEnabled && ((isNote && button.Tag.ToString().StartsWith("Note")) || (!isNote && button.Tag.ToString().StartsWith("Bookmark")))) ? Visibility.Visible : Visibility.Collapsed;

                    typesGrid.ColumnDefinitions[(int)type].Width = (button.Visibility == Visibility.Visible) ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Star);                    
                    
                    button.HorizontalAlignment = setEnabled ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                }
            }
        }

        private NoteType GetNoteTypeFromButtons()
        {
            foreach (RadioButton button in _lstNoteTypes)
            {
                if (button == null || String.IsNullOrEmpty(button.Tag.ToString()))
                    continue;

                if (button.IsChecked == true)
                {
                    NoteType type;
                    if (Enum.TryParse<NoteType>(button.Tag.ToString(), out type))
                        return type;                                    
                }                                    
            }

            return NoteType.Note_0;            
        }

        #endregion

        #region metody pomocnicze: informacje debugowe

        private string GetViewInfo(int idxView)
        {
            string info = String.Empty;
            string html = String.Empty;
            int idxPage = -1;

            WebView view = (WebView)this.htmlFlipView.Items[idxView];
            if (view != null && view.Source != null)
            {
                html = view.Source.Segments.Last();
            }

            if (_pages != null)
            {
                for (int idx = 0; idx < _pages.Length; idx++)
                {
                    PageDataItem pageItem = _pages[idx];

                    if (pageItem == null)
                        continue;

                    if (pageItem.Path.Equals(html))
                    {
                        idxPage = idx;
                        break;
                    }
                }
            }

            return String.Format("view: {0}, page: {1}, {2}", idxView.ToString(), idxPage.ToString(), html);

        }

        private void UpdateNavigationInfo()
        {
#if DEBUG_INFO
            TextBlock txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = "current view: " + _idxView.ToString();
            infoStackPanel.Children.Add(txtInfo);

            txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = "current page: " + _idxPage.ToString();
            infoStackPanel.Children.Add(txtInfo);

            txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = GetViewInfo(0);
            infoStackPanel.Children.Add(txtInfo);

            txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = GetViewInfo(1);
            infoStackPanel.Children.Add(txtInfo);

            txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = GetViewInfo(2);
            infoStackPanel.Children.Add(txtInfo);

            infoScrollViewer.ChangeView(infoStackPanel.ActualHeight, null, null);            
#endif
        }

        private void AddDebugInfo(string info)
        {
#if DEBUG_INFO
            TextBlock txtInfo = new TextBlock();
            txtInfo.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            txtInfo.FontSize = 15;
            txtInfo.Text = info;
            infoStackPanel.Children.Add(txtInfo);

            infoScrollViewer.ChangeView(infoStackPanel.ActualHeight, null, null);
#endif
        }

        #endregion

        #region metody pomocnicze: operacje na widokach

        private WebView CreateView()
        {
            WebView view = new WebView();
            //view.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            //view.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            //view.Margin = new Thickness(0);
            view.ScriptNotify += readerWebView_ScriptNotify;
            view.NavigationStarting += readerWebView_NavigationStarting;
            view.NavigationCompleted += readerWebView_NavigationCompleted;            
            return view;
        }        

        private void AddView(string path, StreamUriWinRTResolver resolver)
        {
            WebView view = this.CreateView();

            view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", path), resolver);

            this.htmlFlipView.Items.Add(view);
        }

        private void InsertView(string path, StreamUriWinRTResolver resolver)
        {
            WebView view = this.CreateView();

            view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", path), resolver);

            this.htmlFlipView.Items.Insert(0, view);
        }

        private void ReloadView(int idx, string path, StreamUriWinRTResolver resolver)
        {
            WebView view = null;

            while (this.htmlFlipView.Items.Count < idx)
            {
                this.htmlFlipView.Items.Add(this.CreateView());
            }

            view = (WebView)this.htmlFlipView.Items[idx];

            view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", path), resolver);
        }

        private bool IsAnySourceDuplicated()
        {
            WebView v0 = (WebView)this.htmlFlipView.Items[0];
            WebView v1 = (WebView)this.htmlFlipView.Items[1];
            WebView v2 = (WebView)this.htmlFlipView.Items[2];

            if ((v1 != null && v0 != null && v0.Source != null && v1.Source != null && v1.Source.Equals(v0.Source)) ||
                (v1 != null && v2 != null && v1.Source != null && v2.Source != null && v1.Source.Equals(v2.Source)) ||
                (v0 != null && v2 != null && v2.Source != null && v0.Source != null && v0.Source.Equals(v2.Source)))
            {
                return true;
            }
            return false;
        }

        private bool SourceAlreadyExists(string source)
        {
            WebView v0 = (WebView)this.htmlFlipView.Items[0];
            WebView v1 = (WebView)this.htmlFlipView.Items[1];
            WebView v2 = (WebView)this.htmlFlipView.Items[2];

            if ((v0 != null && v0.Source != null && source.Equals(v0.Source.ToString())) ||
                (v1 != null && v1.Source != null && source.Equals(v1.Source.ToString())) ||
                (v2 != null && v2.Source != null && source.Equals(v2.Source.ToString())))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region metody pomocnicze: pozostałe

        private void UpdateHistory()
        {
            if (_idxHistory < 0 || _lstHistory.Count == 0)
            {
                _lstHistory.Add(_idxPage);
                _idxHistory = 0;
            }
            else
            {
                if (_lstHistory[_idxHistory] != _idxPage)
                {
                    if (_idxHistory < _lstHistory.Count - 1)
                        _lstHistory.RemoveRange(_idxHistory + 1, _lstHistory.Count - _idxHistory - 1);

                    _lstHistory.Add(_idxPage);
                    _idxHistory = _lstHistory.Count - 1;
                }
            }

            BackPageButton.IsEnabled = _idxHistory > 0;
            ForwardPageButton.IsEnabled = _idxHistory < _lstHistory.Count - 1;

#if DEBUG_INFO
            StringBuilder sb = new StringBuilder();
            foreach (int idx in _lstHistory)
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.Append(idx.ToString());
            }

            this.AddDebugInfo("historia przeglądania: " + sb.ToString());
            this.AddDebugInfo("indeks historii: " + _idxHistory.ToString());
#endif
        }

        private void EnablePrevNextButtons()
        {
            PrevPageButton.IsEnabled = _idxPage > _idxFirstPage;
            NextPageButton.IsEnabled = _idxPage < _idxLastPage;
        }

        private void ShowProgressIndicator(bool show, string text = "")
        {
            progressTextBlock.Text = text;
            progressIndicator.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            progressTextBlock.Visibility = show ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void BackCommandInvokedHandler(IUICommand command)
        {
            this.Frame.GoBack();
        }

        private async Task InitializeData()
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

            this.ShowProgressIndicator(false);
        }

        private async void JumpToAnchor(string anchor)
        {
            if (String.IsNullOrEmpty(anchor))
                return;

            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];                                
                if (view != null)
                {
                    this.AddDebugInfo("JumpToAnchor: " + anchor + " dla widoku: " + view.Source.Segments.Last());
                    await view.InvokeScriptAsync("JumpToAnchorWin8", new string[] { anchor });
                    this.readerBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed;                    
                }
            }
            catch { }
        }

        private async void JumpToNote(string noteId)
        {
            if (String.IsNullOrEmpty(noteId))
                return;

            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                {
                    this.AddDebugInfo("JumpToNote: " + noteId + " dla widoku: " + view.Source.Segments.Last());
                    await view.InvokeScriptAsync("JumpToNoteWin8", new string[] { noteId });
                    this.readerBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            catch { }
        }

        private async void StopPlaybackInAllViews()
        {
            for (int idx = 0; idx < this.htmlFlipView.Items.Count; idx++)
            {
                WebView view = (WebView)this.htmlFlipView.Items[idx];
                if (view != null)
                {
                    try
                    {
                        await view.InvokeScriptAsync("StopPlaybackWin8", null);
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        #endregion

        #region obsługa spisu treści

        private void ShowToC()
        {
            if (_isNoteShown)
                this.HideNote();

            if (!_isToCShown)
            {
                ReloadToCByCurrentPage();
                listsBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                EnterToCStoryboard.Begin();
            }
        }

        private void HideToC()
        {
            ExitToCStoryboard.Begin();
            this.tocBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        
        private void ExitToCStoryboard_Completed(object sender, object e)
        {            
            listsBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;            
            _isToCShown = false;
        }

        private void EnterToCStoryboard_Completed(object sender, object e)
        {            
            this.tocBackground.Visibility = Windows.UI.Xaml.Visibility.Visible;

            _isToCShown = true;
        }

        private void ToCButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isToCShown)
                this.HideToC();
            else
                this.ShowToC();
        }

        private void tocCanvasBack_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            string id = ((Canvas)sender).Tag as String;

            if (String.IsNullOrEmpty(id))
            {
                this.Frame.GoBack();
            }
            else
            {
                ToCDataItem item = _root.FindItem(id);

                _currentToCId = id;
                
                ((HandbookDataItem)this.DataContext).ToC = item;

                SelectItemInToC();
            }
        }
        
        private void TabHeaderLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _currentToCId = _root.Id;            

            ((HandbookDataItem)this.DataContext).ToC = _root;

            SelectItemInToC();

            if (sender is Button && ((Button)sender).Tag != null && ((Button)sender).Tag is ListsTab)
                SelectListsTab((ListsTab)((Button)sender).Tag);
        }

        private void SelectListsTab(ListsTab tab)
        {          
            _selectedListsTab = tab;

            SolidColorBrush normalBackBrush = new SolidColorBrush(Colors.LightGray);
            SolidColorBrush selectedBackBrush = new SolidColorBrush(Colors.White);

            SolidColorBrush normalForeBrush = new SolidColorBrush(Colors.Gray);
            SolidColorBrush selectedForeBrush = new SolidColorBrush(Colors.Black);
            
            foreach (Button button in _lstListsHeaders)
            {
                if (button == null)
                    continue;

                button.Style = button.Tag.Equals(tab) ? (Style)Resources["SelectedTabButtonStyle"] : (Style)Resources["TabButtonStyle"];
                button.Background = button.Tag.Equals(tab) ? selectedBackBrush : normalBackBrush;
                button.Foreground = button.Tag.Equals(tab) ? selectedForeBrush : normalForeBrush;                                
            }

            foreach (ScrollViewer viewer in _lstListsViewers)
            {
                if (viewer == null)
                    continue;

                viewer.Visibility = viewer.Tag.Equals(tab) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }            
        }

        private void tocHeaderContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((TextBlock)((Canvas)sender).Children[0]).Width = ((Canvas)sender).ActualWidth - 50;
        }

        private void tocChildContainer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.AddDebugInfo("tocChildContainer_Tapped ----------------------------------");
            e.Handled = true;
            if (_selectionCompletionInProgress)
                return;

            string id = ((Canvas)sender).Tag as String;
            int page = -1;

            if (String.IsNullOrEmpty(id))
                return;

            ToCDataItem item = ((Grid)((Canvas)sender).Parent).DataContext as ToCDataItem;

            if (item != null && _pages != null && _pages.Length > 0)
            {
                for (int idx = 0; idx < _pages.Length; idx++)
                {
                    PageDataItem itemPage = _pages[idx];

                    if (itemPage == null)
                        continue;

                    if (itemPage.Path.Equals(item.Path))
                    {
                        page = idx;
                        break;
                    }
                }

                this.GoToPage(page);
            }
        }

        private void tocCanvasContent_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            string id = ((Canvas)sender).Tag as String;

            ToCDataItem item = _root.FindItem(id);

            _currentToCId = id;
            
            ((HandbookDataItem)this.DataContext).ToC = item;

            SelectItemInToC();
        }

        private void tocScrollViewer_Tapped(object sender, TappedRoutedEventArgs e)
        {
#if DEBUG_INFO
            if (this.infoScrollViewer.Visibility == Windows.UI.Xaml.Visibility.Visible)
                this.infoScrollViewer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            else
                this.infoScrollViewer.Visibility = Windows.UI.Xaml.Visibility.Visible;
#else
                this.HideToC();
#endif
        }

        #endregion

        #region obsługa nawogacji pomiędzy widokami

        private void MovePageFromLeft()
        {            
            WebView view = (WebView)this.htmlFlipView.Items.Last();

            this.htmlFlipView.UseTouchAnimationsForAllNavigation = false;

            if (_idxPage > _idxFirstPage && _idxView < 2)
            {
                if (this.htmlFlipView.Items.Count < 3)
                {
                    view = this.CreateView();
                }
                else
                {
                    this.htmlFlipView.Items.Remove(view);
                }

                view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", _path + _pages[_idxPage - 1].Path), _resolver);
                this.htmlFlipView.Items.Insert(0, view);
            }

            this.htmlFlipView.UseTouchAnimationsForAllNavigation = true;
        }

        private void MovePageFromRight()
        {            
            WebView view = (WebView)this.htmlFlipView.Items.First();

            this.htmlFlipView.UseTouchAnimationsForAllNavigation = false;

            if (_idxPage < _idxLastPage && _idxView > 0)
            {
                if (this.htmlFlipView.Items.Count < 3)
                {
                    view = this.CreateView();
                }
                else
                {
                    this.htmlFlipView.Items.Remove(view);
                }

                view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", _path + _pages[_idxPage + 1].Path), _resolver);
                this.htmlFlipView.Items.Add(view);
            }

            this.htmlFlipView.UseTouchAnimationsForAllNavigation = true;
        }

        private void GoToPage(int page, string anchor = "", string noteId ="")
        {
            this.AddDebugInfo("index strony do pokazania: " + page.ToString());

            if (page > -1 && page != _idxPage)
            {
                if (page + 1 == _idxPage) 
                {
                    this.AddDebugInfo("już załadowana, po lewej stronie");
                    if (_idxPage > _idxFirstPage)
                    {
                        _idxPage--;

                        this.MovePageFromLeft();
                        this.UpdateHistory();
                    }

                    if (_idxPage == _idxFirstPage)
                        htmlFlipView.SelectedIndex = _idxView = 0;
                    else
                        htmlFlipView.SelectedIndex = _idxView = 1;

                    this.JumpToAnchor(anchor);

                    this.JumpToNote(noteId); 

                    this.UpdateNavigationInfo();
                }
                else if (page - 1 == _idxPage) 
                {
                    this.AddDebugInfo("już załadowana, po prawej stronie");
                    if (_idxPage < _idxLastPage)
                    {
                        _idxPage++;

                        this.MovePageFromRight();
                        this.UpdateHistory();
                    }

                    if (_idxPage == _idxLastPage)
                        htmlFlipView.SelectedIndex = _idxView = 2;
                    else
                        htmlFlipView.SelectedIndex = _idxView = 1;

                    this.JumpToAnchor(anchor);

                    this.JumpToNote(noteId); 

                    this.UpdateNavigationInfo();
                }
                else if (page < _idxPage)
                {
                    this.AddDebugInfo("ma pojawić się z lewej strony");

                    _idxPage = page;

                    string path = _pages[page].Path;
                    if (!String.IsNullOrEmpty(anchor))
                        path += anchor;

                    WebView view = this.CreateView();

                    view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", _path + path), _resolver);

                    int newIdx = _idxView - 1;
                    if (newIdx < 0)
                        newIdx = 0;

                    this.htmlFlipView.Items[newIdx] = view;

                    _changeAfterTocItemSelection = true;

                    _noteIdToJump = noteId;

                    htmlFlipView.SelectedIndex = newIdx;
                }
                else if (page > _idxPage) 
                {
                    this.AddDebugInfo("ma pojawić się z prawej strony");

                    _idxPage = page;

                    string path = _pages[page].Path;
                    if (!String.IsNullOrEmpty(anchor))
                        path += anchor;

                    WebView view = this.CreateView();

                    view.NavigateToLocalStreamUri(view.BuildLocalStreamUri("contentUri", _path + path), _resolver);

                    int newIdx = _idxView + 1;

                    if (newIdx >= this.htmlFlipView.Items.Count)
                        newIdx = this.htmlFlipView.Items.Count - 1;

                    this.htmlFlipView.Items[newIdx] = view;

                    _changeAfterTocItemSelection = true;

                    _noteIdToJump = noteId;

                    htmlFlipView.SelectedIndex = newIdx;
                }
            }
            else if (page == _idxPage)
            {
                this.JumpToAnchor(anchor);

                //jeśli jest id notatki:
                this.JumpToNote(noteId); 
            }

            ReloadToCByCurrentPage();

            this.EnablePrevNextButtons();
        }

        private PageDataItem GetPage(ref int index, bool next = true)
        {
            PageDataItem item = null;

            if (index < 0)
                return null;

            if (_pages != null && _pages.Length > index)
                item = _pages[index];

            if (item == null)
            {
                index = 0;
                return GetPage(ref index, true);
            }

            if (!Users.IsTeacher && item.IsTeacher)
            {                
                index = next ? index + 1 : index - 1;
                return GetPage(ref index, next);    
            }
            else
                return item;
        }

        private int GetFirstPageIndex()
        {
            if (_pages == null)
                return -1;

            int index = 0;

            if (Users.IsTeacher)
                return index;
            
            PageDataItem item = GetPage(ref index, true);

            return index;
        }

        private int GetLastPageIndex()
        {
            if (_pages == null )
                return -1;

            int index = _pages.Length - 1;

            if (Users.IsTeacher)
                return index;
                                              
            PageDataItem item = GetPage(ref index, false);

            return index;
        }                

        private async void ToCReaderPage_SelectionChangeCompleted(object sender, EventArgs e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { this.AddDebugInfo("ToCReaderPage_SelectionChangeCompleted - start"); });

            _selectionCompletionInProgress = true;
            await Task.Delay(TimeSpan.FromMilliseconds(750));

            this.htmlFlipView.UseTouchAnimationsForAllNavigation = false;
            WebView view = null;
            if (htmlFlipView.SelectedIndex > 1)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { this.AddDebugInfo("htmlFlipView.SelectedIndex > 1; przesuń pierwszy na koniec"); });

                view = (WebView)this.htmlFlipView.Items.First();
                this.htmlFlipView.Items.Remove(view);
                this.htmlFlipView.Items.Add(view);

                _idxView = 1;

                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { this.UpdateNavigationInfo(); });
            }
            else if (htmlFlipView.SelectedIndex < 1)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { this.AddDebugInfo("htmlFlipView.SelectedIndex < 1; przesuń ostatni na początek"); });

                view = (WebView)this.htmlFlipView.Items.Last();
                this.htmlFlipView.Items.Remove(view);
                this.htmlFlipView.Items.Insert(0, view);

                _idxView = 1;

                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { this.UpdateNavigationInfo(); });
            }
            this.htmlFlipView.UseTouchAnimationsForAllNavigation = true;

            _selectionCompletionInProgress = false;

            var ignore = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.AddDebugInfo("ToCReaderPage_SelectionChangeCompleted - end");

                this.EnablePrevNextButtons();
            });
        }

        #endregion

        #region obsługa zdarzeń: przyciski i spis treści

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void tocBackground_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.HideToC();
        }
        
        private void htmlFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_preventHandleSelectionChange)
                return;

            this.AddDebugInfo("htmlFlipView_SelectionChanged");

            if (_idxView != htmlFlipView.SelectedIndex)            
            {
                
                if (_changeAfterTocItemSelection)
                {
                    PageDataItem pageLeft = null;
                    PageDataItem pageMiddle = null;
                    PageDataItem pageRight = null;

                    if (_idxPage == _idxFirstPage) 
                    {
                        this.AddDebugInfo("po przesunięciu pokazywana jest pierwsza strona");

                        pageLeft = this.GetPage(ref _idxPage, true);

                        int idxNext = _idxPage + 1;
                        pageMiddle = this.GetPage(ref idxNext, true);

                        int idxNextNext = idxNext + 1;
                        pageRight = this.GetPage(ref idxNextNext, true);

                        this.ReloadView(1, _path + pageMiddle.Path, _resolver);
                        this.ReloadView(2, _path + pageRight.Path, _resolver);

                        this.UpdateNavigationInfo();
                    }
                    else if (_idxPage == _idxLastPage)
                    {
                        this.AddDebugInfo("po przesunięciu pokazywana jest ostatnia strona");

                        pageRight = this.GetPage(ref _idxPage, false);

                        int idxPrev = _idxPage - 1;
                        pageMiddle = this.GetPage(ref idxPrev, false);

                        int idxPrevPrev = idxPrev - 1;
                        pageLeft = this.GetPage(ref idxPrevPrev, false);

                        this.ReloadView(0, _path + pageLeft.Path, _resolver);
                        this.ReloadView(1, _path + pageMiddle.Path, _resolver);

                        this.UpdateNavigationInfo();
                    }
                    else 
                    {
                        pageMiddle = this.GetPage(ref _idxPage, true);

                        int idxNext = _idxPage + 1;
                        pageRight = this.GetPage(ref idxNext, true);

                        int idxPrev = _idxPage - 1;
                        pageLeft = this.GetPage(ref idxPrev, false);
                        
                        if (htmlFlipView.SelectedIndex == 1)
                        {
                            this.AddDebugInfo("idxView = 1;  przeładuj widoki: 0 i 2");

                            this.ReloadView(0, _path + pageLeft.Path, _resolver);
                            this.ReloadView(2, _path + pageRight.Path, _resolver);

                            this.UpdateNavigationInfo();
                        }
                        else if (htmlFlipView.SelectedIndex > 1)
                        {
                            this.AddDebugInfo("idxView > 1;  przeładuj widoki: 0 i 1");

                            this.ReloadView(0, _path + pageRight.Path, _resolver);
                            this.ReloadView(1, _path + pageLeft.Path, _resolver);

                            _idxView = htmlFlipView.SelectedIndex;

                            EventHandler<EventArgs> handlerEvent = SelectionChangeCompleted;
                            if (handlerEvent != null)
                                handlerEvent(this, new EventArgs());
                        }
                        else if (htmlFlipView.SelectedIndex < 1)
                        {
                            this.AddDebugInfo("idxView < 1;  przeładuj widoki: 1 i 2");

                            this.ReloadView(1, _path + pageRight.Path, _resolver);
                            this.ReloadView(2, _path + pageLeft.Path, _resolver);

                            _idxView = htmlFlipView.SelectedIndex;

                            EventHandler<EventArgs> handlerEvent = SelectionChangeCompleted;
                            if (handlerEvent != null)
                                handlerEvent(this, new EventArgs());
                        }
                    }

                    _changeAfterTocItemSelection = false;
                }
                else
                {
                    if (_idxView < htmlFlipView.SelectedIndex)
                    {
                        if (_idxPage < _idxLastPage)
                        {
                            _idxPage++;

                            MovePageFromRight();
                        }

                    }
                    else
                    {
                        if (_idxPage > _idxFirstPage)
                        {
                            _idxPage--;

                            this.MovePageFromLeft();
                        }
                    }

                    this.UpdateNavigationInfo();
                }

                _idxView = htmlFlipView.SelectedIndex;
                
                this.EnablePrevNextButtons();
            }

            this.StopPlaybackInAllViews();
        }

        private void PrevPageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectionCompletionInProgress)
                return;

            if (_pages != null && _pages.Length > 0)
            {
                if (_idxPage > _idxFirstPage)
                {
                    _idxPage--;
                    this.GetPage(ref _idxPage, false);

                    this.MovePageFromLeft();
                    this.UpdateHistory();
                }

                if (_idxPage == _idxFirstPage)
                    htmlFlipView.SelectedIndex = _idxView = 0;
                else
                    htmlFlipView.SelectedIndex = _idxView = 1;
            }

            ReloadToCByCurrentPage();

            this.EnablePrevNextButtons();
        }

        private void NextPageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectionCompletionInProgress)
                return;

            if (_pages != null && _pages.Length > 0)
            {
                if (_idxPage < _idxLastPage)
                {
                    _idxPage++;
                    this.GetPage(ref _idxPage, true);

                    this.MovePageFromRight();
                    this.UpdateHistory();
                }

                if (_idxPage == _idxLastPage)
                    htmlFlipView.SelectedIndex = _idxView = 2;
                else
                    htmlFlipView.SelectedIndex = _idxView = 1;
            }

            ReloadToCByCurrentPage();
       
            this.EnablePrevNextButtons();
        }
        
        private void SelectItemInToC()
        {
            ToCDataItem parent = ((HandbookDataItem)this.DataContext).ToC;
            PageDataItem page = _pages[_idxPage];

            if (parent != null && page != null && !String.IsNullOrEmpty(page.ToCId))
            {
                parent.UnselectAllChildren();
                parent.SelectChild(page.ToCId);                
            }
        }

        private void ReloadToCByCurrentPage()
        {
            PageDataItem page = _pages[_idxPage];
            if (page != null)
            {
                ToCDataItem item = _root.FindItem(page.ToCId);
                if (item != null)
                {
                    if (!String.IsNullOrEmpty(item.Parent))
                    {
                        ToCDataItem parent = _root.FindItem(item.Parent);
                        if (parent != null)
                        {
                            parent.UnselectAllChildren();
                            item.IsSelected = true;
                            item = parent;
                        }
                        else
                        {
                            item.IsSelected = true;
                        }
                    }

                    if (item != null)
                    {
                        _currentToCId = item.Id;

#if DEBUG_INFO
                        List<String> toc = new List<string>();
                        item.PrintItem("", ref toc);
                        foreach (string i in toc)
                        {
                            AddDebugInfo(i);
                        }
#endif

                        ((HandbookDataItem)this.DataContext).ToC = item;
                    }
                }
            }
        }
        
        private void BackPageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_idxHistory <= 0 || _selectionCompletionInProgress)
                return;

            if (_idxHistory > 0)
                _idxHistory--;

            this.GoToPage(_lstHistory[_idxHistory]);
        }

        private void ForwardPageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            if (_idxHistory >= _lstHistory.Count - 1 || _selectionCompletionInProgress)
                return;

            if (_idxHistory < _lstHistory.Count - 1)
                _idxHistory++;

            this.GoToPage(_lstHistory[_idxHistory]);
        }

        #endregion

        #region obsługa pozostałych zdarzeń

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
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            try
            {
                if (e.NavigationParameter != null)
                    _contentId = e.NavigationParameter as String;

                if (String.IsNullOrEmpty(_contentId))
                    return;

                double width = Window.Current.Bounds.Width / 2;

                tocGrid.Width = width - tocGrid.Margin.Left - tocGrid.Margin.Right - tocScrollViewer.Margin.Left - tocScrollViewer.Margin.Right - listsBorder.Margin.Left - listsBorder.Margin.Right - (2 * listsBorder.BorderThickness.Right);
                bookmarksGrid.Width = width - bookmarksGrid.Margin.Left - bookmarksGrid.Margin.Right - bookmarksScrollViewer.Margin.Left - bookmarksScrollViewer.Margin.Right - listsBorder.Margin.Left - listsBorder.Margin.Right - (2 * listsBorder.BorderThickness.Right);
                notesGrid.Width = width - notesGrid.Margin.Left - notesGrid.Margin.Right - notesScrollViewer.Margin.Left - notesScrollViewer.Margin.Right - listsBorder.Margin.Left - listsBorder.Margin.Right - (2 * listsBorder.BorderThickness.Right);

                noteGrid.Width = width - noteBorder.Margin.Left - noteBorder.Margin.Right - (2 * noteBorder.BorderThickness.Left);

                infoStackPanel.Width = width - listsGrid.Margin.Left - listsGrid.Margin.Right - infoStackPanel.Margin.Left - infoStackPanel.Margin.Right - 6;
                infoScrollViewer.Height = Window.Current.Bounds.Height - 6;
                infoScrollViewer.SetValue(Canvas.MarginProperty, new Thickness(width + listsGrid.Margin.Left + listsGrid.Margin.Right + infoStackPanel.Margin.Left, 3, 3, 0));

                ((PopInThemeAnimation)EnterToCStoryboard.Children[0]).FromHorizontalOffset = Window.Current.Bounds.Width / (-2);                
                this.HideToC();

                ((PopInThemeAnimation)EnterNoteStoryboard.Children[0]).FromHorizontalOffset = Window.Current.Bounds.Width;
                this.HideNote();

                await this.InitializeData();

                CollectionDataItem item = CollectionsDataSource.Source.Collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();
                if (item == null)
                {
                    var messageDialog = new MessageDialog("Otwarcie żądanego zasobu nie jest możliwe. Przejdź do listy podręczników i spróbuj otworzyć go ponownie.");
                    messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.BackCommandInvokedHandler)));
                    await messageDialog.ShowAsync();
                    return;
                }

                ToCDataItem[] arToC = await item.ReadToCFromFileAsync();
                
                if (arToC == null || arToC.Length == 0)
                {
                    this.Frame.Navigate(typeof(ReaderPage), _contentId);
                    return;
                }

                _root = new ToCDataItem();
                _root.Id = "root";
                _root.Title = item.Title;
                _root.Children = arToC;
                _root.FetchParents();

                ToCDataItem current = _root;

#if DEBUG_INFO
                List<String> toc = new List<string>();
                _root.PrintItem("", ref toc);
                foreach(string i in toc)
                {
                    AddDebugInfo(i);
                }
#endif

                _currentToCId = CurrentToCIdFromLocalSettings;

                if (!String.IsNullOrEmpty(_currentToCId))
                    current = _root.FindItem(_currentToCId);

                if (current == null)
                    current = _root;

                HandbookDataItem handbookItem = new HandbookDataItem();
                handbookItem.ToC = current;
                
                UserDataItem user = Users.LoggedUser;
                if (user != null)
                {                    
                    IEnumerable<NoteDataItem> notes = user.GetNotesForHandbook(_contentId);
                    if (notes != null)
                    {
                        handbookItem.Notes = notes.ToList<NoteDataItem>();
                    }
                    
                    IEnumerable<NoteDataItem> bookmarks = user.GetBookmarksForHandbook(_contentId);
                    if (bookmarks != null)
                    {
                        handbookItem.Bookmarks = bookmarks.ToList<NoteDataItem>();
                    }
                }

                this.DataContext = handbookItem;

                _pages = await item.ReadPagesFromFileAsync();

                if (_pages == null || _pages.Length == 0)
                {
                    this.Frame.Navigate(typeof(ReaderPage), _contentId);
                    return;
                }

                string currentPage = CurrentPageFromLocalSettings;
                int idx = -1;
                if (!String.IsNullOrEmpty(currentPage) && Int32.TryParse(currentPage, out idx))
                    _idxPage = idx;

                _idxFirstPage = this.GetFirstPageIndex();
                _idxLastPage = this.GetLastPageIndex();

                PageDataItem pageLeft = null;
                PageDataItem pageMiddle = null;
                PageDataItem pageRight = null;

                if (_idxPage == _idxFirstPage)
                {
                    pageLeft = this.GetPage(ref _idxPage, true);

                    int idxNext = _idxPage + 1;
                    pageMiddle = this.GetPage(ref idxNext, true);

                    int idxNextNext = idxNext + 1;
                    pageRight = this.GetPage(ref idxNextNext, true);

                    _idxView = 0;
                }
                else if (_idxPage == _idxLastPage)
                {
                    pageRight = this.GetPage(ref _idxPage, false);

                    int idxPrev = _idxPage - 1;
                    pageMiddle = this.GetPage(ref idxPrev, false);

                    int idxPrevPrev = idxPrev - 1;
                    pageLeft = this.GetPage(ref idxPrevPrev, false);

                    _idxView = 2;

                    if (pageLeft == null)
                        _idxView--;

                    if (pageMiddle == null)
                        _idxView--;
                }
                else
                {
                    pageMiddle = this.GetPage(ref _idxPage, true);

                    int idxNext = _idxPage + 1;
                    pageRight = this.GetPage(ref idxNext, true);

                    int idxPrev = _idxPage - 1;
                    pageLeft = this.GetPage(ref idxPrev, false);

                    _idxView = 1;                    

                    if (pageMiddle == null)
                        _idxView--;
                }

                _preventHandleSelectionChange = true;

                StringBuilder  path = new StringBuilder();
                path.Append("/local/");
                path.Append(CollectionsDataSource.HandbooksFolderName);

                {
                    path.Append("/");
                    path.Append(item.ContentId);
                    path.Append("/");
                    path.Append(item.Version);
                    path.Append("/content/");
                }

                _path = path.ToString();

                if (pageLeft != null)
                    this.AddView(_path + pageLeft.Path, _resolver);

                if (pageMiddle != null)
                    this.AddView(_path + pageMiddle.Path, _resolver);

                if (pageRight != null)
                    this.AddView(_path + pageRight.Path, _resolver);

                string idxHistory = HistoryIndexFromLocalSettings;
                idx = -1;
                if (!String.IsNullOrEmpty(idxHistory) && Int32.TryParse(idxHistory, out idx))
                    _idxHistory = idx;

                string history = HistoryFromLocalSettings;
                if (!String.IsNullOrEmpty(history))
                {
                    string[] ar = history.Split(' ');
                    if(ar != null && ar.Length > 0)
                    {
                        foreach(string page in ar)
                        {
                            if(String.IsNullOrEmpty(page))
                                continue;
                            _lstHistory.Add(Convert.ToInt32(page));
                        }
                    }
                }

                if (_lstHistory.Count == 0 || _idxHistory > _lstHistory.Count - 1 )
                    _idxHistory = _lstHistory.Count - 1;

                _preventHandleSelectionChange = false;

                htmlFlipView.SelectedIndex = _idxView;

                this.EnablePrevNextButtons();

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
            this.StopPlaybackInAllViews();

            CurrentPageFromLocalSettings = this._idxPage.ToString();

            if (!String.IsNullOrEmpty(_currentToCId))
                CurrentToCIdFromLocalSettings = this._currentToCId;

            string idxHistory = HistoryIndexFromLocalSettings = this._idxHistory.ToString();

            StringBuilder sb = new StringBuilder();
            foreach (int idx in _lstHistory)
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.Append(idx.ToString());
            }

            HistoryFromLocalSettings = sb.ToString();
        }

        private void readerWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            this.AddDebugInfo("readerWebView_NavigationStarting, current page=" + _idxPage + ", " + args.Uri.Segments.Last());

            if (_idxPage >= _idxFirstPage && _pages != null && _idxPage <= _idxLastPage)
            {
                string pageUri = args.Uri.Segments.Last();
                if (!String.IsNullOrEmpty(pageUri) && _pages[_idxPage].Path.Equals(pageUri))
                {
                    this.AddDebugInfo("show progress canvas for page=" + _idxPage);
                }
            }
        }

        private async void readerWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            WebView view = (WebView)sender;

            if (view == null)
                return;

            try
            {                
                await view.InvokeScriptAsync("SetIsTeacherWin8", new string[] { Users.IsTeacher.ToString() });                    
            }
            catch (Exception)
            { }
            try
            {                
                await view.InvokeScriptAsync("SetCanPlayVideoWin8", new string[] { CollectionsDataSource.Source.CanPlayVideo.ToString() });
            }
            catch (Exception)
            { }

            try
            {
                //string pageUri = args.Uri.Segments.Last();
                //PageDataItem itemPage = _pages.Where(p => p.Path.Equals(pageUri)).SingleOrDefault();
                //if (itemPage != null)
                //{
                //    UserDataItem user = Users.LoggedUser;
                //    if (user != null)
                //    {
                //        IEnumerable<NoteDataItem> notes = user.GetNotesForPage(itemPage.ModuleId, itemPage.PageId);
                //        if(notes != null)
                //            await view.InvokeScriptAsync("ShowAllNotes", new string[] { JsonConvert.SerializeObject(notes) });
                //        else
                //            await view.InvokeScriptAsync("ShowAllNotes", null);
                //    }
                //}
            }
            catch (Exception exc)
            { 
            }
        }        

        private async void readerWebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            try
            {
                if (e.Value.Equals("notifyEverythingWasLoaded"))
                {
                    this.AddDebugInfo("notifyEverythingWasLoaded, current page=" + _idxPage + ", " + e.CallingUri.Segments.Last());

                    if (_idxPage >= 0 && _pages != null && _idxPage < _pages.Length)
                    {
                        string anchor = String.Empty;
                        string pageUri = e.CallingUri.Segments.Last();

                        string[] ar = pageUri.Split('#');
                        if (ar != null)
                        {
                            if (ar.Length > 0)
                                pageUri = ar[0];

                            if (ar.Length > 1)
                                anchor = ar[1];
                        }

                        if (!String.IsNullOrEmpty(pageUri) && _pages[_idxPage].Path.Equals(pageUri))
                        {
                            this.readerBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                            this.JumpToAnchor(anchor);

                            this.JumpToNote(_noteIdToJump);

                            _noteIdToJump = String.Empty;

                            this.UpdateHistory();
                        }
                    }
                }
                else if (e.Value.Equals("notifyModalWindowVisible"))
                {
                    htmlFlipView.IsEnabled = false;
                    CloseExternalButton.IsEnabled = true;
                }
                else if (e.Value.Equals("notifyModalWindowHidden"))
                {
                    htmlFlipView.IsEnabled = true;
                    CloseExternalButton.IsEnabled = false;
                }
                else if (e.Value.StartsWith("notifyFontButtonsEnabled"))
                {
                    string aminus = String.Empty;
                    string aplus = String.Empty;
                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            aminus = ar[1];

                        if (ar.Length > 2)
                            aplus = ar[2];
                    }

                    bool val = false;
                    if (!String.IsNullOrEmpty(aminus) && Boolean.TryParse(aminus, out val))
                        FontSizeDecButton.IsEnabled = val;

                    if (!String.IsNullOrEmpty(aplus) && Boolean.TryParse(aplus, out val))
                        FontSizeIncButton.IsEnabled = val;
                }
                else if (e.Value.StartsWith("openPageLink"))
                {
                    string anchor = String.Empty;
                    string path = String.Empty;
                    string[] ar = e.Value.Split(';');
                    int page = -1;

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            path = ar[1];

                        if (ar.Length > 2)
                            anchor = ar[2];

                        if (_pages != null)
                        {
                            for (int idx = 0; idx < _pages.Length; idx++)
                            {
                                PageDataItem itemPage = _pages[idx];

                                if (itemPage == null)
                                    continue;

                                if (itemPage.Path.Equals(path))
                                {
                                    page = idx;
                                    break;
                                }
                            }

                            this.AddDebugInfo("openPageLink, page=" + page + ", anchor=" + anchor);

                            this.GoToPage(page, anchor);
                        }
                    }
                }
                else if (e.Value.StartsWith("openExternalWindow"))
                {
                    string url = String.Empty;
                    string[] ar = e.Value.Split(';');                    
                    bool showOverlay = false;

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            url = ar[1];

                        if (ar.Length > 2 && Boolean.TryParse(ar[2], out showOverlay))
                        {
                            if(showOverlay)
                                this.readerBackground.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        }                        
                    }

                    CollectionDataItem item = CollectionsDataSource.Source.Collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();
                    if (item == null)
                    {
                        var messageDialog = new MessageDialog("Otwarcie żądanego zasobu nie jest możliwe. Przejdź do listy podręczników i spróbuj otworzyć go ponownie.");
                        messageDialog.Commands.Add(new UICommand("Zamknij", new UICommandInvokedHandler(this.BackCommandInvokedHandler)));
                        await messageDialog.ShowAsync();
                        return;
                    }

                    StringBuilder path = new StringBuilder();
                    path.Append("ms-appdata:///local/");
                    path.Append(CollectionsDataSource.HandbooksFolderName);

                    path.Append("/");
                    path.Append(item.ContentId);
                    path.Append("/");
                    path.Append(item.Version);
                    path.Append("/");
                    path.Append(url);                    
                    
                    externalWebView.Navigate(new Uri(path.ToString()));

                    externalWebView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    CloseExternalButton.IsEnabled = true;

                    PrevPageButton.IsEnabled = false;
                    ToCButton.IsEnabled = false;
                    NextPageButton.IsEnabled = false;
                    BackPageButton.IsEnabled = false;
                    ForwardPageButton.IsEnabled = false;
                }
                else if (e.Value.StartsWith("showNoteCreate"))
                {
                    string Note = e.Value.Substring(e.Value.IndexOf(';') + 1);

                    _newNote = JsonConvert.DeserializeObject<NewNoteData>(Note);

                    //dodawanie notatki
                    _shownNote = new NoteDataItem(_newNote);

                    if (_isAddBookmarkPressed)
                        _shownNote.Type = NoteType.Bookmark_0;
                    else
                        _shownNote.Type = NoteType.Note_0;

                    UserDataItem user = Users.LoggedUser;
                    if (user != null)
                    {
                        _shownNote.Value = user.GetMargedNotes(_newNote.ToMarge);
                    }

                    if (_shownNote != null)
                    {
                        this.ShowNote(true, true);
                    }
                }
                else if (e.Value.StartsWith("handleNoteClick"))
                {
                    string id = String.Empty;
                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            id = ar[1];
                    }

                    if (!String.IsNullOrEmpty(id))
                    {
                        UserDataItem user = Users.LoggedUser;
                        if (user != null)
                        {
                            _shownNote = user.GetNote(id);
                            if (_shownNote != null)
                            {
                                this.ShowNote(false, true);
                            }
                        }
                    }
                }
                else if (e.Value.StartsWith("getNotesForCurrentView"))
                {
                    try
                    {
                        WebView view = (WebView)sender;

                        if (view != null)
                        {
                            string pageUri = e.CallingUri.Segments.Last();
                            PageDataItem itemPage = _pages.Where(p => p.Path.Equals(pageUri)).SingleOrDefault();
                            if (itemPage != null)
                            {
                                UserDataItem user = Users.LoggedUser;
                                if (user != null)
                                {
                                    IEnumerable<NoteDataItem> notes = user.GetNotesAndBookmarksForPage(itemPage.ModuleId, itemPage.PageId);
                                    await view.InvokeScriptAsync("ShowNotes", new string[] { JsonConvert.SerializeObject(notes) });
                                }
                            }
                        }
                    }
                    catch (Exception)
                    { }
                }
                else if (e.Value.StartsWith("notifyPlayerFullScreen"))
                {
                    bool isFullScreen = false;
                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                        {
                            if (Boolean.TryParse(ar[1], out isFullScreen))
                            {
                                this.BottomAppBar.Visibility = isFullScreen ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
                            }
                        }
                    }
                }
                else if (e.Value.StartsWith("handleNoteClick"))
                {
                    string id = String.Empty;
                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            id = ar[1];
                    }

                    if (!String.IsNullOrEmpty(id))
                    {
                        UserDataItem user = Users.LoggedUser;
                        if (user != null)
                        {
                            _shownNote = user.GetNote(id);
                            if (_shownNote != null)
                            {
                                this.ShowNote(false, true);
                            }
                        }
                    }
                }
                else if (e.Value.Contains("setStateForWomi") || e.Value.Contains("setStateForOpenQuestion"))
                {
                    string womiId = String.Empty;
                    string base64 = String.Empty;

                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            womiId = ar[1];

                        if (ar.Length > 2)
                            base64 = ar[2];
                    }

                    if (!String.IsNullOrEmpty(womiId) && !String.IsNullOrEmpty(base64))
                    {
                        UserDataItem user = Users.LoggedUser;
                        if (user != null)
                        {
                            user.SetWomiState(_contentId, womiId, base64);
                        }
                    }
                }
                else if (e.Value.Contains("getStateForWomi"))
                {
                    string womiId = String.Empty;
                    string base64 = String.Empty;

                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            womiId = ar[1];
                    }

                    if (!String.IsNullOrEmpty(womiId))
                    {
                        UserDataItem user = Users.LoggedUser;
                        if (user != null)
                        {
                            WomiStateItem womi = user.GetWomiState(_contentId, womiId);
                            WebView view = (WebView)sender;
                            if (view != null)
                            {
                                await view.InvokeScriptAsync("updateWomiState", new string[] { base64 });
                            }
                        }
                    }
                }
                else if (e.Value.Contains("getStateForOpenQuestions"))
                {
                    string ids = String.Empty;
                    string base64 = String.Empty;

                    string[] ar = e.Value.Split(';');

                    if (ar != null)
                    {
                        if (ar.Length > 1)
                            ids = ar[1];
                    }

                    if (!String.IsNullOrEmpty(ids))
                    {
                        UserDataItem user = Users.LoggedUser;
                        ar = JsonConvert.DeserializeObject<string[]>(ids);
                        StringBuilder sb = new StringBuilder();

                        if (user != null && ar != null && ar.Length > 0)
                        {
                            foreach (string id in ar)
                            {
                                if (String.IsNullOrEmpty(id))
                                    continue;

                                WomiStateItem womi = user.GetWomiState(_contentId, id);

                                if (sb.Length > 0)
                                    sb.Append(",");

                                sb.Append("\"");
                                sb.Append(id);
                                sb.Append("\"");
                                sb.Append(": ");
                                if (womi != null && !String.IsNullOrEmpty(womi.Base64))
                                {
                                    sb.Append("\"");
                                    sb.Append(womi.Base64);
                                    sb.Append("\"");
                                } 
                                else
                                    sb.Append("null");
                            }

                            var bytes = Encoding.UTF8.GetBytes("{}");

                            if (sb.Length > 0)
                            {
                                bytes = Encoding.UTF8.GetBytes("{" + sb.ToString() + "}");
                           
                                WebView view = (WebView)sender;
                                if (view != null)
                                {
                                    await view.InvokeScriptAsync("updateOpenQuestionsStates", new string[] { Convert.ToBase64String(bytes) });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        private async void FontSizeIncButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                {
                    await view.InvokeScriptAsync("IncreaseSizeWin8", null);
                }

                for (int idx = 0; idx < this.htmlFlipView.Items.Count; idx++)
                {
                    if(this.htmlFlipView.Items[idx] != null && idx != _idxView)
                    {
                        view = (WebView)this.htmlFlipView.Items[idx];
                        if(view != null)
                            await view.InvokeScriptAsync("UpdateSizeWin8", null);
                    }
                }
            }
            catch (Exception)
            { }
        }

        private async void FontSizeDecButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                    await view.InvokeScriptAsync("DecreaseSizeWin8", null);

                for (int idx = 0; idx < this.htmlFlipView.Items.Count; idx++)
                {
                    if (this.htmlFlipView.Items[idx] != null && idx != _idxView)
                    {
                        view = (WebView)this.htmlFlipView.Items[idx];
                        if (view != null)
                            await view.InvokeScriptAsync("UpdateSizeWin8", null);
                    }
                }
            }
            catch (Exception)
            { }
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

        private void externalWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            this.AddDebugInfo("externalWebView_NavigationStarting, current page=" + args.Uri.Segments.Last());            
        }

        private async void externalWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            this.readerBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed; 
            try
            {
                WebView view = (WebView)sender;
                if (view != null)
                    await view.InvokeScriptAsync("SetIsTeacherWin8", new string[] { Users.IsTeacher.ToString() });
            }
            catch (Exception)
            { }
            try
            {
                WebView view = (WebView)sender;
                if (view != null)
                    await view.InvokeScriptAsync("SetCanPlayVideoWin8", new string[] { CollectionsDataSource.Source.CanPlayVideo.ToString() });
            }
            catch (Exception)
            { }
        }

        private async void CloseExternalButton_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            PrevPageButton.IsEnabled = true;
            ToCButton.IsEnabled = true;
            NextPageButton.IsEnabled = true;
            BackPageButton.IsEnabled = true;
            ForwardPageButton.IsEnabled = true;

            CloseExternalButton.IsEnabled = false;

            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                {
                    this.AddDebugInfo("CloseWindow: dla widoku: " + view.Source.Segments.Last());
                    await view.InvokeScriptAsync("CloseWindowWin8", null);
                }
            }
            catch { }

            externalWebView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        #region obsługa notatek i zakładek
        
        private void ShowNote(bool edit, bool hideToC = true)
        {
            if (_shownNote == null)
                return;

            if (hideToC)
                this.HideToC();

            lblNoteSubject.Text = _shownNote.IsBookmark ? "Nazwa zakładki" : "Nazwa notatki";
            txtNoteSubject.Text = _shownNote.Subject;
            
            txtNoteValue.Text = _shownNote.Value;


            txtNoteSubject.IsEnabled = edit;
            txtNoteValue.IsEnabled = edit;

            txtNoteValue.Visibility = _shownNote.IsBookmark ? Visibility.Collapsed : Visibility.Visible;
            lblNoteValue.Visibility = _shownNote.IsBookmark ? Visibility.Collapsed : Visibility.Visible;

            if(_shownNote.IsBookmark)
                txtNoteValue.Focus(FocusState.Programmatic);
            else
                txtNoteSubject.Focus(FocusState.Programmatic);

            lblTypeValue.Text = _shownNote.IsBookmark ? "Typ zakładki" : "Typ notatki";

            SaveNoteButton.Visibility = edit ? Visibility.Visible : Visibility.Collapsed;
            EditNoteButton.Visibility = edit ? Visibility.Collapsed : Visibility.Visible;
            DeleteNoteButton.Visibility = edit ? Visibility.Collapsed : Visibility.Visible;
            CancelNoteButton.Content = edit ? "Anuluj" : "Zamknij";

            typesGrid.ColumnDefinitions[0].Width = new GridLength(_shownNote.IsBookmark ? 0 : 1, GridUnitType.Star);
            typesGrid.ColumnDefinitions[1].Width = new GridLength(_shownNote.IsBookmark ? 0 : 1, GridUnitType.Star);
            typesGrid.ColumnDefinitions[2].Width = new GridLength(_shownNote.IsBookmark ? 0 : 1, GridUnitType.Star);
            typesGrid.ColumnDefinitions[3].Width = new GridLength(_shownNote.IsBookmark ? 0 : 1, GridUnitType.Star);

            typesGrid.ColumnDefinitions[4].Width = new GridLength(_shownNote.IsBookmark ? 1 : 0, GridUnitType.Star);
            typesGrid.ColumnDefinitions[5].Width = new GridLength(_shownNote.IsBookmark ? 1 : 0, GridUnitType.Star);
            typesGrid.ColumnDefinitions[6].Width = new GridLength(_shownNote.IsBookmark ? 1 : 0, GridUnitType.Star);
            
            noteGrid.RowDefinitions[4].Height = new GridLength(_shownNote.IsBookmark ? 0 : 200, GridUnitType.Pixel);

            SetTypeButtonsState(_shownNote.Type, edit, !_shownNote.IsBookmark);

            if (!_isNoteShown)
            {
                noteBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                EnterNoteStoryboard.Begin();
            }
        }

        private void HideNote()
        {
            ExitNoteStoryboard.Begin();
            this.noteBackground.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            _shownNote = null;
            _newNote = null;
        }        

        private async void AddNoteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {                
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                {
                    _isAddBookmarkPressed = false;
                    bool shouldStringify = false;
                    await view.InvokeScriptAsync("StartAddNote", new string[] { shouldStringify.ToString() });
                }
            }
            catch { }            
        }
                
        private void noteBackground_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.HideNote();
        }

        private void ExitNoteStoryboard_Completed(object sender, object e)
        {
            noteBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            
            _isNoteShown = false;
        }

        private void EnterNoteStoryboard_Completed(object sender, object e)
        {
            this.noteBackground.Visibility = Windows.UI.Xaml.Visibility.Visible;

            _isNoteShown = true;
        }

        private void CancelNoteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.HideNote();
        }

        private void EditNoteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            txtNoteSubject.IsEnabled = true;
            txtNoteValue.IsEnabled = true;

            SaveNoteButton.Visibility = Visibility.Visible;
            EditNoteButton.Visibility = Visibility.Collapsed;
            CancelNoteButton.Content = "Anuluj";

            SetTypeButtonsState((_shownNote != null) ? _shownNote.Type : NoteType.Note_0, true, (_shownNote != null) ?  !_shownNote.IsBookmark : true);
        }

        private async void DeleteNoteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            if (_shownNote != null)
            {
                StringBuilder message = new StringBuilder();
                if(_shownNote.IsBookmark)
                    message.Append("Czy chcesz usunąć zakłądkę: ");
                else
                    message.Append("Czy chcesz usunąć notatkę: ");
                message.Append("'");
                message.Append(_shownNote.Subject);
                message.Append("'?");

                var messageDialog = new MessageDialog(message.ToString());

                messageDialog.Commands.Add(new UICommand("Tak", null, _shownNote.LocalNoteId));
                messageDialog.Commands.Add(new UICommand("Nie", null));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                if(!String.IsNullOrEmpty((await messageDialog.ShowAsync()).Id as String))
                {
                    UserDataItem user = Users.LoggedUser;
                    if (user == null)
                        return;

                    bool result = false;
                    if (user.DeleteNote(_shownNote))
                    {
                        WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                        if (view != null)
                        {
                            await view.InvokeScriptAsync("NoteDeleteCallback", new string[] { _shownNote.LocalNoteId });

                            result = await user.SaveToFile();
                            if (!result)
                            {
                                var errorMessageDialog = new MessageDialog("Nie udało się zapisać danych użytkownika do pliku.");
                                errorMessageDialog.Commands.Add(new UICommand("Zamknij", null));
                                await errorMessageDialog.ShowAsync();
                            }
                        }

                        if (result)
                        {
                            this.UpdateNotesBookmarksList(user, !_shownNote.IsBookmark);

                            this.HideNote();                            
                        }
                    }
                    else
                    {
                        var errorMessageDialog = new MessageDialog("Usunięciue notatki zakończyło się błędem.");
                        errorMessageDialog.Commands.Add(new UICommand("Zamknij", null));
                        await errorMessageDialog.ShowAsync();
                    }
                }
            }
        }

        private void UpdateNotesBookmarksList(UserDataItem user, bool isNote)
        {
            if (isNote)
            {
                IEnumerable<NoteDataItem> notes = user.GetNotesForHandbook(_contentId);
                if (notes != null)
                {
                    ((HandbookDataItem)this.DataContext).Notes = notes.ToList<NoteDataItem>();
                }                
            }
            else
            {
                IEnumerable<NoteDataItem> bookmarks = user.GetBookmarksForHandbook(_contentId);
                if (bookmarks != null)
                {
                    ((HandbookDataItem)this.DataContext).Bookmarks = bookmarks.ToList<NoteDataItem>();
                }                
            }
        }
        
        private async void SaveNoteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                UserDataItem user = Users.LoggedUser;
                if (user != null)
                {
                    string version = String.Empty;
                    IEnumerable<CollectionDataItem> collections = await CollectionsDataSource.GetCollectionsAsync();
                    if (collections != null)
                    {
                        CollectionDataItem item = collections.Where(col => col.ContentId.Equals(_contentId)).FirstOrDefault();

                        if (item != null)
                            version = item.Version;
                    }

                    if (_newNote != null)
                    {
                        NoteDataItem newNote = user.AddNote(_contentId, version, _pages[_idxPage].ModuleId, _pages[_idxPage].PageId, _idxPage, txtNoteSubject.Text, txtNoteValue.Text, this.GetNoteTypeFromButtons(), _newNote);
                        if (newNote != null)
                        {
                            WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                            if (view != null)
                            {
                                string note = String.Empty;
                                string ids = String.Empty;
                                try
                                {
                                    note = JsonConvert.SerializeObject(newNote);
                                    if (_newNote.ToMarge != null && _newNote.ToMarge.Length > 0)
                                        ids = JsonConvert.SerializeObject(_newNote.ToMarge);
                                }
                                catch
                                {
                                    note = String.Empty;
                                }

                                await view.InvokeScriptAsync("NoteCreateCallback", new string[] { note, ids });

                                if (await user.SaveToFile())
                                {
                                    PageDataItem itemPage = _pages[_idxPage];
                                    if (itemPage != null)
                                    {
                                        IEnumerable<NoteDataItem> notes = user.GetNotesAndBookmarksForPage(itemPage.ModuleId, itemPage.PageId);
                                        await view.InvokeScriptAsync("ShowNotes", new string[] { JsonConvert.SerializeObject(notes) });
                                    }

                                    this.UpdateNotesBookmarksList(user, !newNote.IsBookmark);
                                    this.UpdateNotesBookmarksList(user, newNote.IsBookmark);

                                    this.HideNote();
                                }
                                else
                                {
                                    var messageDialog = new MessageDialog("Nie udało się zapisać danych użytkownika do pliku.");
                                    messageDialog.Commands.Add(new UICommand("Zamknij", null));
                                    await messageDialog.ShowAsync();
                                }
                            }
                        }
                    }
                    else if (_shownNote != null)
                    {
                        _shownNote.Subject = txtNoteSubject.Text;
                        _shownNote.Value = txtNoteValue.Text;
                        _shownNote.Type = GetNoteTypeFromButtons();

                        WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                        if (view != null)
                        {
                            string note = String.Empty;
                            try
                            {
                                note = JsonConvert.SerializeObject(_shownNote);
                            }
                            catch
                            {
                                note = String.Empty;
                            }

                            await view.InvokeScriptAsync("NoteEditCallback", new string[] { note });

                            if (await user.SaveToFile())
                            {
                                this.UpdateNotesBookmarksList(user, !_shownNote.IsBookmark);

                                this.HideNote();
                            }
                            else
                            {
                                var messageDialog = new MessageDialog("Nie udało się zapisać danych użytkownika do pliku.");
                                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                                await messageDialog.ShowAsync();
                            }
                        }
                    }
                }
            }
            catch { }
        }        

        private void noteSubjectCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((TextBlock)((Canvas)sender).Children[0]).Width = ((Canvas)sender).ActualWidth - 50;
        }

        private void bookmarksSubjectCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((TextBlock)((Canvas)sender).Children[0]).Width = ((Canvas)sender).ActualWidth - 50;
        }

        private void tocCanvasContent_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ((Canvas)sender).Background = new SolidColorBrush(Colors.Red);
        }

        private void tocCanvasContent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ((Canvas)sender).Background = new SolidColorBrush(Colors.White);
        }

        private void noteSubjectCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            string id = ((Canvas)sender).Tag as String;

            if (!String.IsNullOrEmpty(id))
            {
                UserDataItem user = Users.LoggedUser;
                if (user != null)
                {
                    _shownNote = user.GetNote(id);
                    if (_shownNote != null)
                    {                        
                        this.ShowNote(false, false);
                    }
                }
            }
        }

        private void noteContentCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isNoteShown)
                this.HideNote();

            string id = ((Canvas)sender).Tag as String;

            if (!String.IsNullOrEmpty(id))
            {
                UserDataItem user = Users.LoggedUser;
                if (user != null)
                {
                    NoteDataItem note = user.GetNote(id);
                    if (note != null && _pages != null && _pages.Length > 0)
                    {                                    
                        for (int idx = 0; idx < _pages.Length; idx++)
                        {
                            PageDataItem itemPage = _pages[idx];

                            if (itemPage == null)
                                continue;

                            if (itemPage.PageId.Equals(note.PageId))
                            {
                                this.GoToPage(idx, "", note.LocalNoteId.ToString());
                                break;
                            }
                        }                        
                    }
                }
            }
        }

        private async void AddBookmarkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                WebView view = (WebView)this.htmlFlipView.Items[_idxView];
                if (view != null)
                {
                    _isAddBookmarkPressed = true;
                    bool shouldStringify = false;
                    await view.InvokeScriptAsync("StartAddNote", new string[] { shouldStringify.ToString() });
                }
            }
            catch { }
        }

        #endregion        
    }
}
