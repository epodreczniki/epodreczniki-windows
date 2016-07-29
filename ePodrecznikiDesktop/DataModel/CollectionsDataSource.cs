using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace ePodrecznikiDesktop.DataModel
{
    #region klasy pomocnicze

    public enum CollectionStatus { NotDownloaded, DownloadInProgress, CancelDownloadPending, Downloaded, UpdateRequired, UpdateInProgress, CancelUpdatePending }
    public enum ErrorCode { OK, WrongFolderName, WrongCollectionId, WrongMetadata, RuntimeError}
    

    [DataContract]
    public class FormatDataItem
    {
        public FormatDataItem()
        {
            Size = 0;
            Zip = 0;
            IsSelectedFormat = false;
        }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "size")]
        public ulong Size { get; set; }

        [DataMember(Name = "format")]
        public string Format { get; set; }

        public ulong Zip { get; set; }

        public override string ToString()
        {
            return this.Format;
        }

        public bool IsZipFormat
        {
            get
            {
                if (!String.IsNullOrEmpty(Format))
                {
                    string[] ar = Format.Split('-');
                    if (ar != null && ar.Length > 0)
                        return (ar[0].Equals("ZIP", StringComparison.OrdinalIgnoreCase));
                }

                return false;
            }
        }

        public bool IsSelectedFormat { get; set; }
    }

    [DataContract]
    public class StateDataItem
    {
        public StateDataItem()
        {
            Comment = String.Empty;
            Ready = false;
            LastTransformed = String.Empty;
            LastModified = String.Empty;
        }

        [DataMember(Name = "comment")]
        public string Comment { get; set; }

        [DataMember(Name = "ready")]
        public bool Ready { get; set; }

        [DataMember(Name = "lasttransformed")]
        public string LastTransformed { get; set; }

        [DataMember(Name = "lastmodified")]
        public string LastModified { get; set; }

        private string ConvertDateTime(string datetime)
        {

            if (String.IsNullOrEmpty(datetime))
                return String.Empty;

            bool addAnHour = false;
            if (datetime.IndexOf("+UTC") > 0)
            {
                datetime = datetime.Replace("+UTC", "");
                addAnHour = true;
            }

            DateTime dt = DateTime.MinValue;

            if (DateTime.TryParse(datetime, out dt))
            {
                if (addAnHour)
                    dt = dt.AddHours(1);

                return dt.ToString("dd-MM-yyyy HH:mm");
            }

            return String.Empty;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrEmpty(this.Comment))
            {
                if (sb.Length == 0)
                    sb.Append("[");
                sb.Append(this.Comment);
            }

            if (!String.IsNullOrEmpty(this.LastModified))
            {
                if (sb.Length == 0)
                    sb.Append("[ ");
                else
                    sb.Append(". ");
                sb.Append("Ostatnia modyfikacja: ");
                sb.Append(this.ConvertDateTime(this.LastModified));
            }

            if (!String.IsNullOrEmpty(this.LastTransformed))
            {
                if (sb.Length == 0)
                    sb.Append("[ ");
                else
                    sb.Append(". ");
                sb.Append("Ostatnia transformacja: ");
                sb.Append(this.ConvertDateTime(this.LastTransformed));
            }

            if (sb.Length > 0)
                sb.Append("]");

            return sb.ToString();
        }
    }

    [DataContract]
    public class CollectionDataItem
    {
        #region pola klasy

        private const string _handbookFileName = "data.xml";
        private const string _tocFileName = "toc.json";
        private const string _indexFileName = "index.html";                
        private const string _pagesFileName = "pages.json";
        private const string _contentFolder = "content";
        private const string _jsFolder = "js";
        private const string _deviceFolder = "device";
        private const string _jsFileName = "device.js";
        private const string _cssFileName = "mobile_app.css";
        private const string _cssPrefix = "W8_";
        private const string _defaultCover = "Assets/DefaultCover.png"; 

        public event EventHandler<FolderSizeEventArgs> FolderSizeEvent;
        public event EventHandler<ThumbEventArgs> ThumbEvent;
        public event EventHandler<CoverEventArgs> CoverEvent;
        public event EventHandler<UnzipEventArgs> UnzipEvent;
        public event EventHandler<UnzipProgressEventArgs> UnzipProgressEvent;
        public event EventHandler<DownloadEventArgs> DownloadEvent;

        #endregion

        #region propertisy

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_content_id")]
        public string ContentId { get; set; }

        [DataMember(Name = "md_title")]
        public string Title { get; set; }

        [DataMember(Name = "md_subtitle")]
        public string Subtitle { get; set; }

        [DataMember(Name = "md_abstract")]
        public string Abstract { get; set; }

        [DataMember(Name = "md_school")]
        public SchoolDataItem School { get; set; }

        [DataMember(Name = "md_subject")]
        public SubjectDataItem Subject { get; set; }

        [DataMember(Name = "md_published")]
        public bool Published { get; set; }

        [DataMember(Name = "md_version")]
        public string Version { get; set; }

        [DataMember(Name = "md_language")]
        public string Language { get; set; }

        [DataMember(Name = "md_license")]
        public string License { get; set; }

        [DataMember(Name = "md_created")]
        public string CreatedDate
        {
            get
            {

                if (Created == null)
                    return String.Empty;

                return Created.ToString();
            }
            set
            {
                DateTime dt = DateTime.MinValue;

                if (DateTime.TryParse(value, out dt))
                    Created = dt;
                else
                    Created = null;
            }
        }

        [XmlIgnore]
        public DateTime? Created { get; set; }

        [DataMember(Name = "md_revised")]
        public string RevisedDate
        {
            get
            {
                if (Revised == null)
                    return String.Empty;

                return Revised.ToString();
            }
            set
            {

                DateTime dt = DateTime.MinValue;

                if (DateTime.TryParse(value, out dt))
                    Revised = dt;
                else
                    Revised = null;
            }
        }

        [XmlIgnore]
        public DateTime? Revised { get; set; }

        [DataMember(Name = "is_dummy")]
        public bool IsDummy { get; set; }

        public string Signature { get; set; }

        public string MainAuthor { get; set; }

        [DataMember(Name = "md_authors")]
        public Role[] Authors { get; set; }

        [DataMember(Name = "md_keywords")]
        public string[] Keywords { get; set; }

        [DataMember(Name = "cover")]
        public string Cover { get; set; }

        [DataMember(Name = "cover_thumb")]
        public string CoverThumb { get; set; }

        [XmlIgnoreAttribute]
        public BitmapImage CoverImage { get; set; }                

        [XmlIgnoreAttribute]
        public BitmapImage CoverThumbImage { get; set; }

        [XmlIgnoreAttribute]
        public string CoverVisibility
        {
            get
            {
                return (CoverImage == null) ? "Hidden" : "Visible";
            }
        }        

        [XmlIgnoreAttribute]
        public string ThumbVisibility
        {
            get
            {
                return (CoverThumbImage == null) ? "Hidden" : "Visible";
            }
        }        

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "formats")]
        public FormatDataItem[] Formats { get; set; }

        [DataMember(Name = "covers")]
        public CoverDataItem[] Covers { get; set; }

        [DataMember(Name = "app_version_win8")]
        public string AppVersion { get; set; }

        public Guid DownloadId { get; set; }

        public string DownloadPath { get; set; }

        public CollectionStatus Status { get; set; }

        public int DownloadProgress { get; set; }

        public int UnzipProgress { get; set; }

        public double CurtainHeight { get; set; }

        public string FolderSize { get; set; }

        public ulong TotalSize { get; set; }

        public ulong ExtractedSize { get; set; }

        [DataMember(Name = "state")]
        public StateDataItem State { get; set; }

        #endregion

        #region konstruktor

        public CollectionDataItem()
        {
            Status = CollectionStatus.NotDownloaded;
            DownloadProgress = 0;
            UnzipProgress = 0;
            UpdateConfirmed = false;
            TotalSize = 0;
            ExtractedSize = 0;
        }

        #endregion

        #region metody publiczne

        public string GetIndexPath()
        {
            return System.IO.Path.Combine(CollectionsDataSource.HandbooksFolderName, this.ContentId, _contentFolder, "m_i" + this.ContentId + "_second_page.html");
        }

        public void CompleteCoverPaths(DirectoryInfo folder)
        {
            string pathCover = String.Empty;
            string pathThumb = String.Empty;

            if (!String.IsNullOrEmpty(this.Cover))
            {
                String coverPath = System.IO.Path.Combine(folder.FullName, this.Cover);

                FileInfo info = new FileInfo(coverPath);
                if (info.Exists)
                    pathCover = "file:///" + coverPath;
            }

            if (!String.IsNullOrEmpty(this.CoverThumb))                
            {
                String thumbPath = System.IO.Path.Combine(folder.FullName, this.CoverThumb);

                FileInfo info = new FileInfo(thumbPath);
                if (info.Exists)
                    pathThumb = "file:///" + thumbPath;                
            }

            BitmapImage image = null;

            if (!String.IsNullOrEmpty(pathCover))
            {
                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(pathCover);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                this.CoverImage = image;
            }

            if (!String.IsNullOrEmpty(pathThumb))
            {
                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(pathThumb);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                this.CoverThumbImage = image;
            }
        }

        public void SetFolderSize(ulong size)
        {
            FolderSize = String.Empty;

            if (size >= (1024 * 1024 * 1024))
                FolderSize = String.Format("{0:N2}", (double)size / (1024 * 1024 * 1024)) + " GB";
            else if (size >= (1024 * 1024))
                FolderSize = String.Format("{0:N2}", (double)size / (1024 * 1024)) + " MB";
            else if (size >= 1024)
                FolderSize = String.Format("{0:N2}", (double)size / (1024)) + " KB";
            else
                FolderSize = String.Format("{0}", size) + " B";

            this.TotalSize = size;
        }

        public double UpdateCurtainHeight(double maxHeight)
        {
            try
            {
                if (this.Status == CollectionStatus.NotDownloaded)
                    this.CurtainHeight = maxHeight;
                else if (this.Status == CollectionStatus.Downloaded || this.Status == CollectionStatus.UpdateRequired)
                    this.CurtainHeight = 0;
                if (this.Status == CollectionStatus.DownloadInProgress || this.Status == CollectionStatus.UpdateInProgress)
                    this.CurtainHeight = maxHeight - maxHeight * this.DownloadProgress / 100;

                return this.CurtainHeight;
            }
            catch
            {
                return 0;
            }
        }

        public FormatDataItem ChooseFormat(int maxWidth)
        {
            FormatDataItem selectedFormat = null;

            if (this.Formats != null && this.Formats.Length > 0 && maxWidth > 0)
            {
                int selWidth = -1;

                foreach (FormatDataItem item in Formats)
                {
                    if (item == null)
                        continue;

                    int width = -1;

                    if (!String.IsNullOrEmpty(item.Format))
                    {
                        string[] ar = item.Format.Split('-');
                        if (ar != null && ar.Length > 1)
                            width = Convert.ToInt32(ar[1]);
                    }

                    if (width < 0)
                        continue;

                    if (selWidth < 0)
                    {
                        selectedFormat = item;
                        selWidth = width;
                    }
                    else if (width <= maxWidth && width > selWidth)
                    {
                        // należy wybrać format przygotowany na największą szerokość okna, nieprzekraczającą maxWidth
                        selectedFormat = item;
                        selWidth = width;
                    }
                }
            }

            if (selectedFormat != null)
            {
                selectedFormat.IsSelectedFormat = true;
                this.SetFolderSize(selectedFormat.Size);
            }
            return selectedFormat;
        }

        public FormatDataItem GetSelectedFormat()
        {
            if (this.Formats != null && this.Formats.Length > 0)
            {
                foreach (FormatDataItem item in Formats)
                {
                    if (item == null)
                        continue;

                    if (item.IsSelectedFormat)
                        return item;

                }
            }

            return null;
        }

        public string UpdateIndicatorVisiblity
        {
            get
            {
                return Status == CollectionStatus.UpdateRequired ? "Visible" : "Collapsed";
            }
        }

        public bool UpdateConfirmed { get; set; }

        public CollectionDataItem NewVersionItem { get; set; }

        public void OverwriteDataByNewVersion()
        {
            if (this.NewVersionItem != null)
            {
                Id = this.NewVersionItem.Id;
                ContentId = this.NewVersionItem.ContentId;
                Title = this.NewVersionItem.Title;
                Subtitle = this.NewVersionItem.Subtitle;
                Abstract = this.NewVersionItem.Abstract;
                School = this.NewVersionItem.School;
                Subject = this.NewVersionItem.Subject;
                Published = this.NewVersionItem.Published;
                Version = this.NewVersionItem.Version;
                Language = this.NewVersionItem.Language;
                License = this.NewVersionItem.License;
                Created = this.NewVersionItem.Created;
                Revised = this.NewVersionItem.Revised;
                Authors = this.NewVersionItem.Authors;
                MainAuthor = this.NewVersionItem.MainAuthor;
                Keywords = this.NewVersionItem.Keywords;
                Cover = this.NewVersionItem.Cover;
                CoverThumb = this.NewVersionItem.CoverThumb;
                CoverImage = this.NewVersionItem.CoverImage;
                CoverThumbImage = this.NewVersionItem.CoverThumbImage;
                Link = this.NewVersionItem.Link;
                Formats = this.NewVersionItem.Formats;
                DownloadId = this.NewVersionItem.DownloadId;
                DownloadPath = this.NewVersionItem.DownloadPath;
                DownloadProgress = this.NewVersionItem.DownloadProgress;
                UnzipProgress = this.NewVersionItem.UnzipProgress;
                CurtainHeight = this.NewVersionItem.CurtainHeight;
                FolderSize = this.NewVersionItem.FolderSize;
                IsDummy = this.NewVersionItem.IsDummy;
                NewVersionItem = null;
                UpdateConfirmed = false;
            }
        }

        public override string ToString()
        {
            return this.Title;
        }

        public FileInfo GetToCFile()
        {
            DirectoryInfo localFolder = null;
            DirectoryInfo handbooksFolder = null;
            DirectoryInfo itemFolder = null;
            DirectoryInfo versionFolder = null;
            DirectoryInfo contentFolder = null;

            localFolder = new DirectoryInfo(Directory.GetCurrentDirectory());

            if (localFolder != null)
            {
                handbooksFolder = localFolder.GetDirectories(CollectionsDataSource.HandbooksFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (handbooksFolder == null)
                    handbooksFolder = localFolder.CreateSubdirectory(CollectionsDataSource.HandbooksFolderName);
            }

            if (handbooksFolder != null)
            {
                itemFolder = handbooksFolder.GetDirectories(this.ContentId, SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            if (itemFolder == null)
                return null;

            versionFolder = itemFolder.GetDirectories(this.Version, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (versionFolder == null)
                return null;

            contentFolder = versionFolder.GetDirectories(_contentFolder, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (contentFolder == null)
                return null;

            return contentFolder.GetFiles(_tocFileName, SearchOption.TopDirectoryOnly).FirstOrDefault();
        }

        public ToCDataItem[] ReadToCFromFileAsync()
        {
            FileInfo fileToC = GetToCFile();
            if (fileToC != null)
            {
                using (Stream fileStream = fileToC.OpenRead())
                {
                    var reader = new StreamReader(fileStream);

                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ToCDataItem[]));

                    return (ToCDataItem[])ser.ReadObject(fileStream);                    
                }
            }

            return null;
        }        

        public void SetHeightByStatus()
        {
            if (this.Status == CollectionStatus.NotDownloaded)
                this.CurtainHeight = 354;
            else if (this.Status == CollectionStatus.Downloaded || this.Status == CollectionStatus.UpdateRequired)
                this.CurtainHeight = 0;
            if (this.Status == CollectionStatus.DownloadInProgress || this.Status == CollectionStatus.UpdateInProgress)
                this.CurtainHeight = 354 - 354 * this.DownloadProgress / 100;
        }

        public bool IsSufficientAppVersion()
        {                        
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (String.IsNullOrEmpty(this.AppVersion))
                return false;

            var versionParts = this.AppVersion.Split('.').ToList();

            if (versionParts.Count == 0)
                return false;

            ushort major = 0;
            if (!ushort.TryParse(versionParts[0], out major))
                return false;

            if (major > appVersion.Major)
                return false;

            if (major == appVersion.Major && versionParts.Count > 1)
            {
                ushort minor = 0;
                if (ushort.TryParse(versionParts[1], out minor))
                {
                    if (minor > appVersion.Minor)
                        return false;

                    if (minor == appVersion.Minor && versionParts.Count > 2)
                    {
                        ushort build = 0;
                        if (ushort.TryParse(versionParts[2], out build))
                        {
                            if (build > appVersion.Build)
                                return false;

                            if (build == appVersion.Build && versionParts.Count > 3)
                            {
                                ushort revision = 0;
                                if (ushort.TryParse(versionParts[3], out revision))
                                {
                                    if (revision > appVersion.Revision)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        #region metody pomocnicze

        private ulong GetFolderSize(DirectoryInfo folder)
        {
            try
            {                                
                ulong result = 0;                                

                foreach (FileInfo file in folder.EnumerateFiles())
                {
                    result += (ulong)file.Length;
                }

                foreach (var storageFolder in folder.EnumerateDirectories())
                {
                    result += GetFolderSize(storageFolder);
                }

                return result;
            }
            catch
            {
                return 0;
            }
        }        

        public bool CheckTocOrIndexFile()
        {
            if (this.ContentId == null)
                return false;

            if (this.Version == null)
                return false;                        
            
            DirectoryInfo localFolder = null;
            DirectoryInfo handbooksFolder = null;
            DirectoryInfo itemFolder = null;
            DirectoryInfo versionFolder = null;
            DirectoryInfo contentFolder = null;

            localFolder = new DirectoryInfo(Directory.GetCurrentDirectory());

            if (localFolder != null) 
            {
                handbooksFolder = localFolder.GetDirectories(CollectionsDataSource.HandbooksFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();

                if(handbooksFolder == null)
                    handbooksFolder = localFolder.CreateSubdirectory(CollectionsDataSource.HandbooksFolderName);
                
            }

            if (handbooksFolder != null)
            {
                itemFolder = handbooksFolder.GetDirectories(this.ContentId, SearchOption.TopDirectoryOnly).FirstOrDefault();                
            }
                        
            if (itemFolder == null)
                return false;

            versionFolder = itemFolder.GetDirectories(this.Version, SearchOption.TopDirectoryOnly).FirstOrDefault();                
            if (versionFolder == null)
                return false;

            contentFolder = versionFolder.GetDirectories(_contentFolder, SearchOption.TopDirectoryOnly).FirstOrDefault();                            
            if (contentFolder == null)
                return false;

            if (contentFolder != null)
            {
                FileInfo fileIndex = contentFolder.GetFiles(_tocFileName, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (fileIndex == null)
                    fileIndex = contentFolder.GetFiles(_indexFileName, SearchOption.TopDirectoryOnly).FirstOrDefault(); 

                return (fileIndex != null);
            }

            return false;
        }

        public bool WriteMetadataFromFile()
        {
            DirectoryInfo localFolder = null;
            DirectoryInfo handbooksFolder = null;
            DirectoryInfo itemFolder = null;

            localFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
            if(localFolder != null)
                handbooksFolder = localFolder.GetDirectories(CollectionsDataSource.HandbooksFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();

            if(handbooksFolder != null)
                itemFolder = handbooksFolder.GetDirectories(this.ContentId, SearchOption.TopDirectoryOnly).FirstOrDefault();
            
            if (itemFolder != null)
            {
                FileInfo fileData = itemFolder.GetFiles(_handbookFileName, SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (fileData == null)                
                    fileData = new FileInfo(Path.Combine(itemFolder.FullName, _handbookFileName));                

                if(fileData != null)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(CollectionDataItem));

                    XmlWriterSettings settings = new XmlWriterSettings();
                    //settings.Encoding = new UnicodeEncoding(false, false); // no BOM in a .NET string
                    settings.Encoding = new UTF8Encoding();
                    settings.Indent = true;
                    //settings.NewLineHandling = NewLineHandling.None;
                    settings.OmitXmlDeclaration = false;

                    using (Stream fileStream = fileData.OpenWrite())
                        using (XmlWriter xmlWriter = XmlWriter.Create(fileStream, settings))
                        {
                            serializer.Serialize(xmlWriter, this);
                            return true;
                        }
                }
            }

            return false;
        }
        
        #endregion
    }

    [DataContract]
    public class AuthorDataItem
    {
        public AuthorDataItem()
        {
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "md_surname")]
        public string Surname { get; set; }

        [DataMember(Name = "md_institution")]
        public string Institution { get; set; }

        [DataMember(Name = "md_email")]
        public string Email { get; set; }

        [DataMember(Name = "md_full_name")]
        public string FullName { get; set; }

        public override string ToString()
        {
            return this.FullName;
        }
    }

    [DataContract]
    public class Role
    {
        public Role()
        {
        }
        
        public int Ordering { get; set; }
        
        public string RoleName { get; set; }
        
        public string RoleAuthors { get; set; }

        public override string ToString()
        {
            return this.RoleAuthors;
        }
    }

    [DataContract]
    public class SchoolDataItem
    {
        public SchoolDataItem()
        {
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_education_level")]
        public string EducationLevel { get; set; }

        [DataMember(Name = "ep_class")]
        public int Class { get; set; }

        public override string ToString()
        {
            return this.EducationLevel + "/" + Class.ToString();
        }
    }

    [DataContract]
    public class SubjectDataItem
    {
        public SubjectDataItem()
        {
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_name")]
        public string Subject { get; set; }


        public override string ToString()
        {
            return this.Subject;
        }
    }

    [DataContract]
    public class CoverDataItem
    {
        public CoverDataItem()
        {
        }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "format")]
        public string Format { get; set; }

        public override string ToString()
        {
            return this.Format;
        }
    }

    [DataContract]
    public class ToCDataItem
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        public string Parent { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "isTeacher")]
        public bool IsTeacher { get; set; }

        [DataMember(Name = "numbering")]
        public string Numbering { get; set; }

        [DataMember(Name = "contentStatus")]
        public List<String> ContentStatus { get; set; }

        [DataMember(Name = "pathRef")]
        public string Path { get; set; }

        [DataMember(Name = "children")]
        public ToCDataItem[] Children { get; set; }

        public double Height
        {
            get
            {
                return 54;
            }
        }

        public double TotalHeight
        {
            get
            {
                double total = this.Height;

                if (Children == null || Children.Length == 0)
                    return total;

                foreach (ToCDataItem item in Children)
                {
                    if (item == null)
                        continue;

                    total += item.Height;
                }

                return total;
            }
        }

        public string Visibility
        {
            get
            {
                return (Children == null || Children.Length == 0) ? "Collapsed" : "Visible";
            }
        }

        public ToCDataItem()
        {

        }

        public ToCDataItem FindItem(string id)
        {
            if (id.Equals(this.Id))
                return this;

            if (Children == null || Children.Length == 0)
                return null;

            if (!(bool)Properties.Settings.Default["IsTeacher"])
                Children = Children.Where(i => (i.IsTeacher == false)).ToArray();

            foreach (ToCDataItem item in Children)
            {
                if (item == null)
                    continue;

                ToCDataItem result = item.FindItem(id);
                if (result != null)
                    return result;
            }

            return null;
        }

        public void FetchParents()
        {
            if (Children == null || Children.Length == 0)
                return;

            if (!(bool)Properties.Settings.Default["IsTeacher"])
                Children = Children.Where(i => (i.IsTeacher == false)).ToArray();

            foreach (ToCDataItem item in Children)
            {
                if (item == null)
                    continue;

                item.Parent = this.Id;

                if (!String.IsNullOrEmpty(item.Numbering))
                    item.Title = item.Numbering + " " + item.Title;

                item.FetchParents();
            }
        }
    }

    [DataContract]
    public class PageDataItem
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "idRef")]
        public string ToCId { get; set; }

        [DataMember(Name = "isTeacher")]
        public bool IsTeacher { get; set; }

        public PageDataItem()
        {

        }
    }

    [DataContract]
    public class ZipDataItem
    {
        [DataMember(Name = "link")]
        public string Path { get; set; }

        [DataMember(Name = "win8")]
        public string Version { get; set; }

        public ZipDataItem()
        {

        }
    }

    #endregion

    #region klasy argumentów dla zdarzeń

    public class DownloadEventArgs : EventArgs
    {
        private readonly string _contentId;
        private readonly Guid _downloadId;
        private readonly int _progress;

        public DownloadEventArgs(string contentId, Guid downloadId, int progress)
        {
            _contentId = contentId;
            _downloadId = downloadId;
            _progress = progress;
        }

        public string ContentId
        {
            get { return _contentId; }
        }

        public Guid DownloadId
        {
            get { return _downloadId; }
        }

        public int Progress
        {
            get { return _progress; }
        }
    }

    public class FolderSizeEventArgs : EventArgs
    {
        private readonly string _contentId;
        private readonly string _foldersize;

        public FolderSizeEventArgs(string contentId, string foldersize)
        {
            _contentId = contentId;
            _foldersize = foldersize;
        }

        public string ContentId
        {
            get { return _contentId; }
        }

        public string FolderSize
        {
            get { return _foldersize; }
        }
    }

    public class ThumbEventArgs : EventArgs
    {
        private readonly string _contentId;

        public ThumbEventArgs(string contentId)
        {
            _contentId = contentId;
        }

        public string ContentId
        {
            get { return _contentId; }
        }
    }

    public class CoverEventArgs : EventArgs
    {
        private readonly string _contentId;

        public CoverEventArgs(string contentId)
        {
            _contentId = contentId;
        }

        public string ContentId
        {
            get { return _contentId; }
        }
    }

    public class CancelEventArgs : EventArgs
    {
        private readonly string _contentId;

        public CancelEventArgs(string contentId)
        {
            _contentId = contentId;
        }

        public string ContentId
        {
            get { return _contentId; }
        }
    }

    public class UnzipEventArgs : EventArgs
    {
        private readonly string _contentId;
        private readonly sbyte _result;

        public UnzipEventArgs(string contentId, sbyte result)
        {
            _contentId = contentId;
            _result = result;
        }

        public string ContentId
        {
            get { return _contentId; }
        }

        public sbyte Result
        {
            get { return _result; }
        }
    }

    public class UnzipProgressEventArgs : EventArgs
    {
        private readonly string _contentId;
        private readonly int _progress;

        public UnzipProgressEventArgs(string contentId, int progress)
        {
            _contentId = contentId;
            _progress = progress;
        }

        public string ContentId
        {
            get { return _contentId; }
        }

        public int Progress
        {
            get { return _progress; }
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        private readonly string _message;

        public ErrorEventArgs(string message)
        {
            _message = message;
        }

        public string Message
        {
            get { return _message; }
        }
    }

    public class DeleteEventArgs : EventArgs
    {
        private readonly bool _result;
        private readonly CollectionDataItem _item;

        public DeleteEventArgs(bool result, CollectionDataItem item)
        {
            _result = result;
            _item = item;
        }

        public bool Result
        {
            get { return _result; }
        }

        public CollectionDataItem Item
        {
            get { return _item; }
        }
    }    

    #endregion

    public sealed class CollectionsDataSource
    {
        #region pola klasy

        public static CollectionsDataSource Source = new CollectionsDataSource();

        public event EventHandler<DownloadEventArgs> DownloadEvent;
        public event EventHandler<FolderSizeEventArgs> FolderSizeEvent;
        public event EventHandler<ThumbEventArgs> ThumbEvent;
        public event EventHandler<CoverEventArgs> CoverEvent;
        public event EventHandler<CancelEventArgs> CancelEvent;
        public event EventHandler<UnzipEventArgs> UnzipEvent;
        public event EventHandler<UnzipProgressEventArgs> UnzipProgressEvent;
        public event EventHandler<EventArgs> DownloaderInitializedEvent;
        public event EventHandler<ErrorEventArgs> ErrorEvent;
        public event EventHandler<DeleteEventArgs> DeleteEvent;
    
        private const string _handbookFileName = "data.xml";
        private const string _requiredAPIVersion = "1.0";
        private const string _contentFolderName = "content";
        private const string _collectionFolderName = "collection";
        private const string _womiFolderName = "womi";

        public static string HandbooksFolderName = "ePodręczniki";
        public static string DownloadsFolderName = "Downloads";        
        public static string TempFolderName = "temp";

        public static string ThumbImageName = "thumb";

        public static int MaxWidth = 0;

        private ObservableCollection<CollectionDataItem> _collections = new ObservableCollection<CollectionDataItem>();

        #endregion

        #region propertisy

        public ObservableCollection<CollectionDataItem> Collections
        {
            get { return this._collections; }
        }


        public static DirectoryInfo HandbooksFolder
        {
            get {
                string handbooksFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), CollectionsDataSource.HandbooksFolderName);

                return new DirectoryInfo(handbooksFolder); 
            }
        }

        #endregion

        #region konstruktor

        public CollectionsDataSource()
        {

        }

        #endregion

        #region statyczne metody publiczne

        public static IEnumerable<CollectionDataItem> GetCollectionsAsync()
        {
            ErrorCode errorCode;
            int id;

            bool fetchSucceed = Source.GetCollectionsDataAsync(out errorCode, out id);

            // jeśli udało się pobrać, lub nie udało się pobrać ale to nie jest update i kolekcja została odczytana z pliku xml - zwróć odczytaną kolekcję
            if (fetchSucceed)
                return Source.Collections;
            else
                // jeśli nie udało się pobrać kolekcji i jest to update: zwróć null
                return null;
        }

        public static IEnumerable<CollectionDataItem> RefreshCollectionsAsync(out ErrorCode errorCode, out int id)
        {
            bool fetchSucceed = Source.GetCollectionsDataAsync(out errorCode, out id, true);

            // jeśli udało się pobrać, lub nie udało się pobrać ale to nie jest update i kolekcja została odczytana z pliku xml - zwróć odczytaną kolekcję
            if (fetchSucceed)
                return Source.Collections;
            else
                // jeśli nie udało się pobrać kolekcji i jest to update: zwróć null
                return null;
        }

        public static void SetMaxWidth(Rect bounds)
        {
            MaxWidth = (int)Math.Max(bounds.Height, bounds.Width);
        }

        public static bool RemoveCollection(string contentId)
        {
            bool result = false;
            CollectionDataItem item = null;
            try
            {
                item = Source.Collections.Where(col => col.ContentId.Equals(contentId)).FirstOrDefault();

                if (item == null)
                    return false;

                DirectoryInfo itemFolder = null;
                DirectoryInfo contentFolder = null;

                if (HandbooksFolder != null)
                    itemFolder = HandbooksFolder.GetDirectories(item.ContentId, SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (itemFolder != null)
                {
                    item.CoverImage = null;
                    item.CoverThumbImage = null;                    
                    itemFolder.Delete(true);
                }


                if (HandbooksFolder != null)
                {
                    // usunięcie pliku startowego podręcznika
                    FileInfo file = HandbooksFolder.GetFiles(item.ContentId + ".html", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (file != null)
                    {
                        file.Delete();
                    }

                    contentFolder = HandbooksFolder.GetDirectories(_contentFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();
                }

                if (contentFolder != null)
                {
                    // jeśli wszystkie podrczniki zostały usunięte: usuń folder "content"
                    if (Source.Collections.Count == 0)
                    {
                        contentFolder.Delete(true);
                    }
                    else
                    {                        
                        // usunięcie folderu podręcznika w katalogu collection
                        DirectoryInfo collectionFolder = contentFolder.GetDirectories(_collectionFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if(collectionFolder != null)
                        {
                            itemFolder = collectionFolder.GetDirectories(item.ContentId, SearchOption.TopDirectoryOnly).FirstOrDefault();
                            if(itemFolder != null)
                            {
                                itemFolder.Delete(true);
                            }                            
                        }

                        // usunięcie pliku ".collection" z wszystkich podkatalogów w folderze womi
                        DirectoryInfo womiFolder = contentFolder.GetDirectories(_womiFolderName, SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if(womiFolder != null)
                        {
                            List<DirectoryInfo> lstFoldersToDelete = new List<DirectoryInfo>();
                            foreach (System.IO.DirectoryInfo subDirectory in womiFolder.GetDirectories())
                            {
                                if (subDirectory == null)
                                    continue;

                                FileInfo file = subDirectory.GetFiles(item.ContentId + ".collection", SearchOption.TopDirectoryOnly).FirstOrDefault();
                                if(file != null)
                                {
                                    file.Delete();
                                }

                                // jeśli nie ma już żadnych plików z rozszerzeniem ".collection",
                                // co oznacza, że womi nie jest już nigdzie wykorzystywane, 
                                // dodaj folder womi do kolekcji katalogów do usunięcia
                                if (subDirectory.GetFiles("*.collection", SearchOption.TopDirectoryOnly).Count() == 0)
                                {
                                    lstFoldersToDelete.Add(subDirectory);
                                }
                            }

                            foreach (System.IO.DirectoryInfo subDirectory in lstFoldersToDelete)
                            {
                                if (subDirectory == null)
                                    continue;

                                subDirectory.Delete(true);
                            }                            
                        }
                    }
                }

                result = true;
            }
            catch (Exception exc)
            {
                result = false;             
            }

            EventHandler<DeleteEventArgs> handlerDelete = CollectionsDataSource.Source.DeleteEvent;
            if (handlerDelete != null)
            {
                handlerDelete(CollectionsDataSource.Source, new DeleteEventArgs(result, item));
            }

            return result;
        }

        public static bool RemoveHandbook(string contentId)
        {
            try
            {
                if (!String.IsNullOrEmpty(contentId))
                {                                        
                    Task t = Task.Factory.StartNew(() =>
                    {                        
                        CollectionsDataSource.RemoveCollection(contentId);
                    });

                    return true;
                }

                return false;
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        public static bool RemoveFolder(string contentId)
        {
            try
            {
                DirectoryInfo handbooksFolder = HandbooksFolder;
                DirectoryInfo itemFolder = null;


                if (handbooksFolder != null)
                    itemFolder = handbooksFolder.GetDirectories(contentId, SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (itemFolder != null)
                {
                    itemFolder.Delete(true);
                }
            }
            catch (Exception exc)
            {
                return false;
            }

            return true;
        }        
        #endregion

        #region metody pomocnicze: pobieranie kolekcji

        private static CollectionDataItem ReadMetadataFromFile(DirectoryInfo folder)
        {
            try
            {
                // pobierz metadane podręcznika z pliku xml                                            
                XmlSerializer deserializer = new XmlSerializer(typeof(CollectionDataItem));

                FileInfo fileData = folder.GetFiles(_handbookFileName, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (fileData != null)
                {
                    using (Stream fileStream = fileData.OpenRead())
                    {
                        return (CollectionDataItem)deserializer.Deserialize(fileStream);
                    }
                }

                return null;
            }
            catch (Exception exc)
            {
                return null;
            }            
        }

        private bool GetCollectionsDataAsync(out ErrorCode errorCode, out int id, bool refresh = false)
        {
            errorCode = ErrorCode.OK;
            id = -1;
            // jeśli kolekcja już została pobrana i to nie jest uaktualnienie:
            if (this._collections.Count != 0 && !refresh)
                return true;

            if(!refresh)
                _collections = new ObservableCollection<CollectionDataItem>();

            bool fetchSucceed = false;
            
            try
            {
                // folder z wszystkimi podręcznikami na local storze
                DirectoryInfo handbooksFolder = HandbooksFolder;

                if (handbooksFolder != null)
                {
                    DirectoryInfo[] folders = handbooksFolder.GetDirectories();

                    // dla wszystkich folderów zawierających podręczniki
                    foreach (DirectoryInfo folder in folders)
                    {
                        this.AddMetadataToCollection(folder, ref id, ref errorCode);                        
                    }
                }

                fetchSucceed = true;
            }
            catch (Exception)
            {
                fetchSucceed = false;
            }

            return fetchSucceed;
        }
        
        private bool AddMetadataToCollection(DirectoryInfo folder, ref int id, ref ErrorCode errorCode)
        {
            int collectionId = -1;

            try
            {
                if (folder == null || String.IsNullOrEmpty(folder.Name))
                {
                    errorCode = ErrorCode.WrongFolderName;
                    return false;
                }
                
                // jeśli nazwa folderu nie jest ciągiem cyfr, nie dodawaj do kolekcji                
                if (!Int32.TryParse(folder.Name, out collectionId))
                {
                    return false;
                }

                // pobierz z kolekcji z API element o takiej samej nazwie co folder
                CollectionDataItem itemFromAPI = null;

                // pobierz metadane podręcznika z pliku xml
                CollectionDataItem itemFromData = CollectionsDataSource.ReadMetadataFromFile(folder);

                if (itemFromData == null)
                {
                    errorCode = ErrorCode.WrongMetadata;
                    id = collectionId;
                    return false;
                }
                else
                {
                    if (!itemFromData.ContentId.Equals(folder.Name))
                    {
                        itemFromData.ContentId = folder.Name;
                        itemFromData.WriteMetadataFromFile();
                    }

                    if (Source.Collections != null && Source.Collections.Count > 0)
                    {
                        itemFromAPI = Source.Collections.FirstOrDefault(col => col.ContentId.Equals(folder.Name, StringComparison.OrdinalIgnoreCase));
                    }

                    // jeśli w kolekcji są metadane podręcznika o takiej samej nazwie co folder na dysku
                    if (itemFromAPI != null)
                    {
                        itemFromAPI = itemFromData;
                    }
                    else
                    {
                        itemFromData.FolderSizeEvent += DataItem_FolderSizeEvent;
                        itemFromData.ThumbEvent += DataItem_ThumbEvent;
                        itemFromData.CoverEvent += DataItem_CoverEvent;
                        itemFromData.UnzipEvent += DataItem_UnzipEvent;
                        itemFromData.UnzipProgressEvent += DataItem_UnzipProgressEvent;
                        itemFromData.DownloadEvent += DataItem_DownloadEvent;

                        itemFromData.CompleteCoverPaths(folder);
                        itemFromData.SetHeightByStatus();

                        int index = Source.Collections.Count;
                        for (int idx = 0; idx < Source.Collections.Count; idx++)
                        {
                            if(Source.Collections[idx] != null && Source.Collections[idx].Title.CompareTo(itemFromData.Title) > 0)
                            {
                                index = idx;
                            }
                        }

                        Source.Collections.Insert(index, itemFromData);
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                errorCode = ErrorCode.RuntimeError;
                id = collectionId;
                return false;
            }
        }
        
        #endregion

        #region funkcjonalność rozpakowywania paczki z podręcznikiem

        public static bool CheckUniquenessOfNewCollections(string archiveFilenameIn)
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(archiveFilenameIn);
                zf = new ZipFile(fs);

                int zipCollectionId = 0;
                int handbookCollectionId = 0;

                foreach (ZipEntry zipEntry in zf)
                {
                    if (zipEntry.IsFile)
                        continue;
                    
                    if (Int32.TryParse(zipEntry.Name.Trim(new char[]{'/', '\\'}), out zipCollectionId))
                    {
                        foreach (var di in HandbooksFolder.EnumerateDirectories())
                        {
                            if (Int32.TryParse(di.Name, out handbookCollectionId) && zipCollectionId == handbookCollectionId)
                                return false;
                        }
                    }
                }

                return true;
            }
            catch {
                return true;
            }

        }
        private static void ExtractZipFile(string archiveFilenameIn, string outFolder)
        {
            ZipFile zf = null;
            sbyte result = 0;
            long totalFiles = 0;
            long progressCount = 0;

            try
            {
                FileStream fs = File.OpenRead(archiveFilenameIn);
                zf = new ZipFile(fs);

                totalFiles = zf.Count;
                
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        progressCount++;
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    progressCount++;
                    EventHandler<UnzipProgressEventArgs> handlerUnzipProgress = CollectionsDataSource.Source.UnzipProgressEvent;
                    if (handlerUnzipProgress != null)
                    {
                        int progress = (int)(progressCount * 100 / totalFiles);
                        if (progress > 100)
                            progress = 100;

                        handlerUnzipProgress(CollectionsDataSource.Source, new UnzipProgressEventArgs(null, progress));
                    }
                }

                result = (progressCount == totalFiles) ? (sbyte)100 : (sbyte)-1;

            }
            catch
            {
                result = -2;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }

                EventHandler<UnzipEventArgs> handlerUnzip = CollectionsDataSource.Source.UnzipEvent;
                if (handlerUnzip != null)
                {
                    handlerUnzip(CollectionsDataSource.Source, new UnzipEventArgs(Path.GetFileNameWithoutExtension(outFolder), result));
                }
            }
        }
                
        public static bool UnZipFile(String fileZipPath)
        {
            try
            {                
                DirectoryInfo handbooksFolder = HandbooksFolder;

                if (handbooksFolder != null)
                {                    
                    Task t = Task.Factory.StartNew(() =>
                    {
                        CollectionsDataSource.ExtractZipFile(fileZipPath, handbooksFolder.FullName);
                    });
                    

                    return true;
                }

                return false;
            }
            catch (Exception exc)
            {
                return false;
            }            
        }

        #endregion

        #region obsługa zdarzeń z CollectionDataItem

        void DataItem_FolderSizeEvent(object sender, FolderSizeEventArgs e)
        {
            EventHandler<FolderSizeEventArgs> handler = FolderSizeEvent;
            if (handler != null)
                handler(this, new FolderSizeEventArgs(e.ContentId, e.FolderSize));
        }

        void DataItem_ThumbEvent(object sender, ThumbEventArgs e)
        {
            EventHandler<ThumbEventArgs> handler = ThumbEvent;
            if (handler != null)
                handler(this, new ThumbEventArgs(e.ContentId));
        }

        void DataItem_CoverEvent(object sender, CoverEventArgs e)
        {
            EventHandler<CoverEventArgs> handler = CoverEvent;
            if (handler != null)
                handler(this, new CoverEventArgs(e.ContentId));
        }

        void DataItem_UnzipEvent(object sender, UnzipEventArgs e)
        {
            EventHandler<UnzipEventArgs> handler = UnzipEvent;
            if (handler != null)
                handler(this, new UnzipEventArgs(e.ContentId, e.Result));
        }

        void DataItem_UnzipProgressEvent(object sender, UnzipProgressEventArgs e)
        {
            EventHandler<UnzipProgressEventArgs> handler = UnzipProgressEvent;
            if (handler != null)
                handler(this, new UnzipProgressEventArgs(e.ContentId, e.Progress));
        }

        void DataItem_DownloadEvent(object sender, DownloadEventArgs e)
        {
            EventHandler<DownloadEventArgs> handler = DownloadEvent;
            if (handler != null)
                handler(this, new DownloadEventArgs(e.ContentId, e.DownloadId, e.Progress));

        }

        #endregion        
    }
}
