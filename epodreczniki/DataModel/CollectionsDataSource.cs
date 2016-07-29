using epodreczniki.Common;
using epodreczniki.DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;

namespace epodreczniki.Data
{
    #region klasy pomocnicze

    public enum CollectionStatus { NotDownloaded, DownloadInProgress, CancelDownloadPending, Downloaded, UpdateRequired, UpdateInProgress, CancelUpdatePending }

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
        public ulong? Size { get; set; }

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
        
        private const string _tocFileName = "toc.json";
        private const string _pagesFileName = "pages.json";
        private const string _contentFolder = "content";
        private const string _jsFolder = "js";
        private const string _deviceFolder = "device";        
        private const string _jsFileName = "device.js";
        private const string _cssFileName = "mobile_app.css";
        private const string _cssPrefix = "W8_";
        private const string _defaultCover = "ms-appx:///Assets/DefaultCover.png";

        private HttpClient _httpClient = new HttpClient();
    
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
        public string CreatedDate {
            get {

                if(Created == null)
                    return String.Empty;

                return Created.ToString(); 
            }
            set {
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
                
        public string MainAuthor { get; set; }

        [DataMember(Name = "md_roles")]
        public RoleDataItem[] Roles { get; set; }         

        [DataMember(Name = "md_authors")]
        public AuthorDataItem[] Authors { get; set; }

        [DataMember(Name = "md_keywords")]
        public string[] Keywords { get; set; }

        public string Cover { get; set; }

        public string CoverThumb { get; set; }

        public string CoverImage { get; set; }
        
        public string CoverThumbImage { get; set; }

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

        [XmlIgnore]
        public int Order
        {
            get
            {
                try
                {
                    return (School != null ? School.Order : 0) * 100 + (Subject != null ? Subject.Id : 0 ) * 10 + (School != null ? (School.Class != null ? (int)School.Class : 0) : 0);
                }
                catch
                {
                    return 0;
                }
            }
        }

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

        public void ChooseCovers(int maxWidth)
        {
            if (this.Covers != null && this.Covers.Length > 0 && maxWidth > 0)
            {
                int maxCoverWidth = maxWidth / 2 * 3;
                int maxThumbWidth = maxWidth / 2;
                int selCoverWidth = -1;
                int selThumbWidth = -1;

                foreach(CoverDataItem item in Covers)
                {
                    if (item == null)
                        continue;
                    
                    int height = -1;
                    int width = -1;

                    if(!String.IsNullOrEmpty(item.Format))
                    {                        
                        string[] ar = item.Format.Split('-');
                        if(ar != null && ar.Length > 1)
                            height = Convert.ToInt32(ar[1]);                        
                    }

                    if (height < 0)
                        continue;       
   
                    width = height * 210 / 297;

                    if (String.IsNullOrEmpty(this.Cover))
                    {
                        this.Cover = item.Url;
                        selCoverWidth = width;
                    }
                    else
                    {                        
                        if(width < maxCoverWidth && width > selCoverWidth)
                        {
                            this.Cover = item.Url;
                            selCoverWidth = width;
                        }
                    }

                    if (String.IsNullOrEmpty(this.CoverThumb))
                    {
                        this.CoverThumb = item.Url;
                        selThumbWidth = width;
                    }
                    else
                    {                        
                        if (width < maxThumbWidth && width > selThumbWidth)
                        {
                            this.CoverThumb = item.Url;
                            selThumbWidth = width;
                        }
                    }                    
                }
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
                        selectedFormat = item;
                        selWidth = width;
                    }                    
                }
            }

            if (selectedFormat != null)
            {
                selectedFormat.IsSelectedFormat = true;
                if(selectedFormat.Size != null)
                    this.SetFolderSize((ulong)selectedFormat.Size);
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
            if(this.NewVersionItem != null)
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

        public async Task<String> DownloadCoverFile(bool thumb, bool getAnyway)
        {            
            if (String.IsNullOrEmpty(this.Cover) || this.Cover.LastIndexOf(".") <= 0)
                return _defaultCover;

            StringBuilder sbPath = new StringBuilder();

            try
            {
                string path = String.Empty;
                string name = String.Empty;
                bool sendRefreshImageEvent = false;
                StringBuilder sbImage = new StringBuilder();
                StorageFile coverFile = null;

                if (!getAnyway)
                {
                    if (thumb)
                        path = this.CoverThumbImage;
                    else
                        path = this.CoverImage;

                    if (!String.IsNullOrEmpty(path))
                    {
                        coverFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this.CoverThumbImage));

                        if (coverFile != null)
                            return path;
                    }
                }

                if (thumb)
                {
                    this.CoverThumb = GetIrrelativePath(this.CoverThumb);                    

                    path = this.CoverThumb;
                    name = CollectionsDataSource.ThumbImageName;
                }
                else
                {                    
                    this.Cover = GetIrrelativePath(this.Cover);

                    path = this.Cover;
                    name = CollectionsDataSource.CoverImageName;
                }

                sbImage.Append(name);
                sbImage.Append(path.Substring(path.LastIndexOf(".")));

                string fileName = sbImage.ToString();

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder itemFolder = await handbooksFolder.CreateFolderAsync(this.ContentId, CreationCollisionOption.OpenIfExists);

                coverFile = (StorageFile)await itemFolder.TryGetItemAsync(fileName);
                if (coverFile == null || getAnyway)
                {
                    byte[] arImage = await _httpClient.GetByteArrayAsync(path);
                    if (arImage != null)
                    {
                        StorageFile fileImage = await itemFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                        fileName = fileImage.Name;

                        using (Stream fileStream = await fileImage.OpenStreamForWriteAsync())
                        {
                            await fileStream.WriteAsync(arImage, 0, arImage.Length);
                            sendRefreshImageEvent = true;
                        }
                    }
                }

                sbPath.Append("ms-appdata:///local/");
                sbPath.Append(CollectionsDataSource.HandbooksFolderName);
                sbPath.Append("/");
                sbPath.Append(this.ContentId);
                sbPath.Append("/");
                sbPath.Append(fileName);

                if (thumb)
                {
                    this.CoverThumbImage = sbPath.ToString();
                    if (sendRefreshImageEvent)
                    {
                        EventHandler<ThumbEventArgs> handler = ThumbEvent;
                        if (handler != null)
                            handler(this, new ThumbEventArgs(this.ContentId));
                    }
                }
                else
                {
                    this.CoverImage = sbPath.ToString();
                    if (sendRefreshImageEvent)
                    {
                        EventHandler<CoverEventArgs> handler = CoverEvent;
                        if (handler != null)
                            handler(this, new CoverEventArgs(this.ContentId));
                    }
                }
            }
            catch
            {
                sbPath = null;
            }

            if (sbPath == null || sbPath.Length == 0)
                return _defaultCover;
            else
                return sbPath.ToString();
        }

        public async Task<bool> WriteMetadataFromFileAsync(bool replace = true)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CollectionDataItem));

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
            StorageFolder itemFolder = await handbooksFolder.CreateFolderAsync(this.ContentId, CreationCollisionOption.OpenIfExists);
            StorageFolder versionFolder = await itemFolder.CreateFolderAsync(this.Version, replace ? CreationCollisionOption.OpenIfExists : CreationCollisionOption.FailIfExists);

            if (versionFolder != null)
            {
                StorageFile fileCollections = await versionFolder.CreateFileAsync(CollectionsDataSource.HandbookFileName, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await fileCollections.OpenStreamForWriteAsync())
                {
                    serializer.Serialize(fileStream, this);
                    return true;
                }
            }

            return false;
        }

        public async Task<StorageFile> GetToCFileAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

            StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
            if (itemFolder == null)
                return null;

            StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
            if (versionFolder == null)
                return null;

            StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
            if (contentFolder == null)
                return null;

            return (StorageFile)await contentFolder.TryGetItemAsync(_tocFileName);        
        }

        public async Task<ToCDataItem[]> ReadToCFromFileAsync()
        {                   
            StorageFile fileToC = await GetToCFileAsync();
            if (fileToC != null)
            {
                if (fileToC != null)
                {
                    using (Stream fileStream = await fileToC.OpenStreamForReadAsync())
                    {
                        var reader = new StreamReader(fileStream);

                        return JsonConvert.DeserializeObject<ToCDataItem[]>(reader.ReadToEnd());                        
                    }
                }
            }

            return null;
        }

        public async Task<StorageFile> GetPagesFileAsync()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

            StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
            if (itemFolder == null)
                return null;

            StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
            if (versionFolder == null)
                return null;

            StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
            if (contentFolder == null)
                return null;

            return (StorageFile)await contentFolder.TryGetItemAsync(_pagesFileName);
        }

        public async Task<PageDataItem[]> ReadPagesFromFileAsync()
        {
            StorageFile filePages = await GetPagesFileAsync();
            if (filePages != null)
            {
                using (Stream fileStream = await filePages.OpenStreamForReadAsync())
                {
                    var reader = new StreamReader(fileStream);

                    return JsonConvert.DeserializeObject<PageDataItem[]>(reader.ReadToEnd());                    
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

        public async Task<CollectionDataItem> CompleteMetadata()
        {
            if (this.Authors != null && this.Authors.Count() > 0)
                this.MainAuthor = this.Authors[0].FullName;

            this.ChooseCovers(CollectionsDataSource.MaxWidth);

            this.ChooseFormat(CollectionsDataSource.MaxWidth);

            this.CoverThumb = GetIrrelativePath(this.CoverThumb);            
            this.Cover = GetIrrelativePath(this.Cover);

            foreach (FormatDataItem format in this.Formats)
            {
                if (format == null)
                    continue;

                format.Url = GetIrrelativePath(format.Url);

                if (!String.IsNullOrEmpty(format.Format) && format.IsZipFormat && format.IsSelectedFormat)
                {
                    try
                    {
                        HttpWebRequest webrequest = WebRequest.Create(format.Url) as HttpWebRequest;
                        webrequest.Method = "HEAD";
                        using (WebResponse response = await webrequest.GetResponseAsync())
                        {
                            if (response != null)
                                format.Zip = (ulong)response.ContentLength;
                        }
                    }
                    catch
                    {
                        format.Zip = 0;
                    }
                }
            }

            if (this.School != null && !String.IsNullOrEmpty(this.School.EducationLevel))
            {
                if (this.School.EducationLevel.Equals("I") || this.School.EducationLevel.Equals("II"))
                //if (this.School.EducationLevel.Equals("I"))
                //    this.School.EducationLevel = "Wczesnoszkolna";
                //else if (this.School.EducationLevel.Equals("II"))
                    this.School.EducationLevel = "Podstawowa";
                else if (this.School.EducationLevel.Equals("III"))
                    this.School.EducationLevel = "Gimnazjalna";
                else if (this.School.EducationLevel.Equals("IV"))
                    this.School.EducationLevel = "Ponadgimnazjalna";
            }

            return this;
        }

        public bool IsSufficientAppVersion()
        {
            var appVersion = Windows.ApplicationModel.Package.Current.Id.Version;

            if (String.IsNullOrEmpty(this.AppVersion))
                return true;

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

        #region funkcjonalność rozpakowywania paczki z podręcznikiem

        public async Task<bool> UnZipFile()
        {
            try
            {                
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder itemFolder = await handbooksFolder.CreateFolderAsync(this.ContentId, CreationCollisionOption.OpenIfExists);
                StorageFolder tempFolder = await itemFolder.CreateFolderAsync(CollectionsDataSource.TempFolderName, CreationCollisionOption.OpenIfExists);                
                ExtractedSize = 0;

                using (var zipStream = await downloadsFolder.OpenStreamForReadAsync(Path.GetFileName(this.DownloadPath)))
                {
                    using (MemoryStream zipMemoryStream = new MemoryStream((int)zipStream.Length))
                    {
                        await zipStream.CopyToAsync(zipMemoryStream);

                        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {

                                if (entry.Name == "")
                                {
                                    await CreateRecursiveFolder(tempFolder, entry);
                                }
                                else
                                {
                                    await ExtractFile(tempFolder, entry);

                                    if (TotalSize > 0)
                                    {
                                        lock(this)
                                        {
                                            ExtractedSize += (ulong)entry.Length;

                                            EventHandler<UnzipProgressEventArgs> handlerUnzipProgress = UnzipProgressEvent;
                                            if (handlerUnzipProgress != null)
                                            {
                                                int progress = (int)(ExtractedSize * 100 / TotalSize);
                                                if (progress > 100)
                                                    progress = 100;
                                            
                                                this.UnzipProgress = progress;

                                                handlerUnzipProgress(this, new UnzipProgressEventArgs(this.ContentId, progress));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                StorageFolder versionFolder = (StorageFolder)await itemFolder.TryGetItemAsync(this.Version);
                if (versionFolder != null)
                {
                    await versionFolder.DeleteAsync();
                }

                versionFolder = tempFolder;
                if (versionFolder != null)
                {
                    await versionFolder.RenameAsync(this.Version);

                    this.SetFolderSize(await GetFolderSize(versionFolder));
                }
                EventHandler<FolderSizeEventArgs> handler = FolderSizeEvent;
                if (handler != null)
                    handler(this, new FolderSizeEventArgs(this.ContentId, this.FolderSize));

                StorageFile zipFile = (StorageFile)await downloadsFolder.TryGetItemAsync(Path.GetFileName(this.DownloadPath));
                if (zipFile != null)
                    await zipFile.DeleteAsync();

                IReadOnlyList<StorageFolder> versionsFolders = await itemFolder.GetFoldersAsync();
                if (versionsFolders != null)                
                {
                    foreach (StorageFolder folder in versionsFolders)
                    {
                        if (folder != null && !folder.Name.Equals(this.Version))
                            await folder.DeleteAsync();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public async Task<bool> RemoveDownloadedAndUnZipedFile()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder itemFolder = await handbooksFolder.CreateFolderAsync(this.ContentId, CreationCollisionOption.OpenIfExists);

                StorageFile zipFile = (StorageFile)await downloadsFolder.TryGetItemAsync(Path.GetFileName(this.DownloadPath));
                if (zipFile != null)
                    await zipFile.DeleteAsync();

                StorageFolder versionFolder = (StorageFolder)await itemFolder.TryGetItemAsync(this.ContentId);
                if (versionFolder != null)
                {
                    await versionFolder.DeleteAsync();
                }
                else
                {
                    versionFolder = (StorageFolder)await itemFolder.TryGetItemAsync(this.Version);
                    if (versionFolder != null)
                        await versionFolder.DeleteAsync();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> FinishDownloadedFileToUse()
        {
            bool result = true;
            if (this.Status == CollectionStatus.DownloadInProgress || this.Status == CollectionStatus.UpdateInProgress)
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);                

                if (!String.IsNullOrEmpty(this.DownloadPath) && downloadsFolder != null)
                {
                    StorageFile zipFile = (StorageFile)await downloadsFolder.TryGetItemAsync(Path.GetFileName(this.DownloadPath));
                    if (zipFile != null)
                    {
                        BasicProperties properties = await zipFile.GetBasicPropertiesAsync();
                        FormatDataItem format = this.GetSelectedFormat();
                        if (properties != null && format != null && properties.Size == format.Zip)
                        {
                            this.DownloadProgress = 100;                            
                            EventHandler<DownloadEventArgs> handler = DownloadEvent;
                            if (handler != null)
                                handler(this, new DownloadEventArgs(this.ContentId, this.DownloadId, 100));

                            result = await this.PrepareDownloadedFileToUse();
                            
                            if (result)
                            {
                                if (this.Status == CollectionStatus.UpdateInProgress)
                                {
                                    this.OverwriteDataByNewVersion();
                                }
                                
                                if (this.NewVersionItem != null && !(this.Version.Equals(this.NewVersionItem.Version)))
                                {
                                    this.Status = CollectionStatus.UpdateRequired;
                                }
                            }                       
                        }
                    }
                }

                if (!result)
                {
                    if (this.Status == CollectionStatus.DownloadInProgress)
                        this.Status = CollectionStatus.NotDownloaded;
                    else if (this.Status == CollectionStatus.UpdateInProgress)
                        this.Status = CollectionStatus.UpdateRequired;

                    this.DownloadProgress = 0;
                    EventHandler<DownloadEventArgs> handler = DownloadEvent;
                    if (handler != null)
                        handler(this, new DownloadEventArgs(this.ContentId, this.DownloadId, 0));
                }                                
            }

            return result;
        }        

        public async Task<bool> PrepareDownloadedFileToUse()
        {
            string userId = Users.LoggedUserId;            

            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings != null && localSettings.Values.ContainsKey("contentUri_" + this.ContentId + userId))
                localSettings.Values.Remove("contentUri_" + this.ContentId + userId);

            if (localSettings != null && localSettings.Values.ContainsKey("Page_" + this.ContentId + userId))
                localSettings.Values.Remove("Page_" + this.ContentId + userId);

            if (localSettings != null && localSettings.Values.ContainsKey("ToCId_" + this.ContentId + userId))
                localSettings.Values.Remove("ToCId_" + this.ContentId + userId);

            if (localSettings != null && localSettings.Values.ContainsKey("History_" + this.ContentId + userId))
                localSettings.Values.Remove("History_" + this.ContentId + userId);

            if (localSettings != null && localSettings.Values.ContainsKey("HistoryIndex_" + this.ContentId + userId))
                localSettings.Values.Remove("HistoryIndex_" + this.ContentId + userId);
            
            bool isSufficientSpace = false;

            if (!String.IsNullOrEmpty(this.DownloadPath))
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);

                StorageFile zipFile = (StorageFile)await downloadsFolder.TryGetItemAsync(Path.GetFileName(this.DownloadPath));
                if (zipFile != null)
                {
                    BasicProperties properties = await zipFile.GetBasicPropertiesAsync();
                    
                    ulong freeDiskSpace = await CollectionsDataSource.GetDiskFreeSpace();

                    if (properties != null && freeDiskSpace > properties.Size * 2)
                    {
                        isSufficientSpace = true;
                    }
                }
            }
            
            bool isUnzipSuccessful = false;

            if (isSufficientSpace)
            {
                isUnzipSuccessful = await this.UnZipFile();
            }
            
            if (isUnzipSuccessful)
            {
                this.Status = CollectionStatus.Downloaded;

                this.AdjustCssFile();

                this.AdjustJsFile();

                this.CoverImage = await this.DownloadCoverFile(false, false);
                this.CoverThumbImage = await this.DownloadCoverFile(true, false);

                EventHandler<ThumbEventArgs> handlerThumbEvent = ThumbEvent;
                if (handlerThumbEvent != null)
                    handlerThumbEvent(this, new ThumbEventArgs(this.ContentId));

                EventHandler<CoverEventArgs> handlerCoverEvent = CoverEvent;
                if (handlerCoverEvent != null)
                    handlerCoverEvent(this, new CoverEventArgs(this.ContentId));
            }
            else
            {
                await this.RemoveDownloadedAndUnZipedFile();

                if (this.Status == CollectionStatus.DownloadInProgress)
                    this.Status = CollectionStatus.NotDownloaded;
                else if (this.Status == CollectionStatus.UpdateInProgress)
                    this.Status = CollectionStatus.UpdateRequired;
            }

            this.UnzipProgress = 0;

            await this.WriteMetadataFromFileAsync();

            sbyte result = 0;
            if (!isUnzipSuccessful)
            {
                if (isSufficientSpace)
                    result = -1;
                else
                    result = -2;
            }

            EventHandler<UnzipEventArgs> handlerUnzipEvent = UnzipEvent;
            if (handlerUnzipEvent != null)
                handlerUnzipEvent(this, new UnzipEventArgs(this.ContentId, result));

            return isUnzipSuccessful;
        }

        private async Task CreateRecursiveFolder(StorageFolder folder, ZipArchiveEntry entry)
        {
            var steps = entry.FullName.Split('/').ToList();

            steps.RemoveAt(steps.Count() - 1);

            foreach (var i in steps)
            {
                await folder.CreateFolderAsync(i, CreationCollisionOption.OpenIfExists);

                folder = await folder.GetFolderAsync(i);
            }
        }

        private async Task ExtractFile(StorageFolder folder, ZipArchiveEntry entry)
        {
            var steps = entry.FullName.Split('/').ToList();

            steps.RemoveAt(steps.Count() - 1);

            foreach (var i in steps)
            {
                await folder.CreateFolderAsync(i, CreationCollisionOption.OpenIfExists);

                folder = await folder.GetFolderAsync(i);
            }

            using (Stream fileData = entry.Open())
            {
                StorageFile outputFile = await folder.CreateFileAsync(entry.Name, CreationCollisionOption.ReplaceExisting);

                using (Stream outputFileStream = await outputFile.OpenStreamForWriteAsync())
                {                    
                    await fileData.CopyToAsync(outputFileStream);
                    await outputFileStream.FlushAsync();
                }
            }
        }

        #endregion


        #region metody pomocnicze

        private string GetIrrelativePath(string path)
        {
            if (!path.StartsWith("http"))
                return CollectionsDataSource.Host + path;

            return path;
        }

        private async Task<ulong> GetFolderSize(StorageFolder folder)
        {
            try
            {
                var folders = await folder.GetFoldersAsync();
                ulong result = 0;

                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync(CommonFileQuery.DefaultQuery);

                foreach (StorageFile file in fileList)
                {
                    BasicProperties properties = await file.GetBasicPropertiesAsync();
                    result += properties.Size;
                }

                foreach (var storageFolder in folders)
                {
                    result += await GetFolderSize(storageFolder);
                }

                return result;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> DownloadCssFile(string path)
        {
            try
            {                
                string contentCss = await _httpClient.GetStringAsync(path);

                if (!String.IsNullOrEmpty(contentCss))
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

                    StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
                    if (itemFolder == null)
                        return false;

                    StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
                    if (versionFolder == null)
                        return false;

                    StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
                    if (contentFolder == null)
                        return false;
                    
                    StorageFile fileCss = await contentFolder.CreateFileAsync(_cssFileName, CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fileCss, contentCss);

                    return true;                    
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task<bool> DownloadJsFile(string path)
        {
            try
            {
                string contentJs = await _httpClient.GetStringAsync(path);

                if (!String.IsNullOrEmpty(contentJs))
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

                    StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
                    if (itemFolder == null)
                        return false;

                    StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
                    if (versionFolder == null)
                        return false;

                    StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
                    if (contentFolder == null)
                        return false;

                    StorageFolder jsFolder = (StorageFolder)await contentFolder.TryGetItemAsync(_jsFolder);
                    if (jsFolder == null)
                        return false;

                    StorageFolder deviceFolder = (StorageFolder)await jsFolder.TryGetItemAsync(_deviceFolder);
                    if (deviceFolder == null)
                        return false;

                    
                    StorageFile fileJs = await deviceFolder.CreateFileAsync(_jsFileName, CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fileJs, contentJs);

                    return true;                                        
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task<bool> CheckTocOrIndexFile()
        {
            if (this.ContentId == null)
                return false;

            if (this.Version == null)
                return false;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

            StorageFolder itemFolder = (StorageFolder)await handbooksFolder.TryGetItemAsync(this.ContentId);
            if (itemFolder == null)
                return false;

            StorageFolder versionFolder = (StorageFolder)await itemFolder.TryGetItemAsync(this.Version);
            if (versionFolder == null)
                return false;

            StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
            if (contentFolder == null)
                return false;

            if (contentFolder != null)
            {
                StorageFile fileIndex = (StorageFile)await contentFolder.TryGetItemAsync(_tocFileName);
                if (fileIndex == null)
                    fileIndex = (StorageFile)await contentFolder.TryGetItemAsync("index.html");

                return (fileIndex != null);
            }

            return false;
        }

        private async void AdjustCssFile()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

            StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
            if (itemFolder == null)
                return;

            StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
            if (versionFolder == null)
                return;

            StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
            if (contentFolder == null)
                return;
            
            StorageFile fileCssW8 = (StorageFile)await contentFolder.TryGetItemAsync(_cssPrefix + _cssFileName);
            if (fileCssW8 != null)
            {
                StorageFile fileCssCommon = (StorageFile)await contentFolder.TryGetItemAsync(_cssFileName);
                if (fileCssCommon != null)
                    await fileCssCommon.DeleteAsync();

                await fileCssW8.RenameAsync(_cssFileName, NameCollisionOption.ReplaceExisting);                    
            }            
        }

        private async void AdjustJsFile()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

            StorageFolder itemFolder = await handbooksFolder.GetFolderAsync(this.ContentId);
            if (itemFolder == null)
                return;

            StorageFolder versionFolder = await itemFolder.GetFolderAsync(this.Version);
            if (versionFolder == null)
                return;

            StorageFolder contentFolder = (StorageFolder)await versionFolder.TryGetItemAsync(_contentFolder);
            if (contentFolder == null)
                return;

            StorageFolder jsFolder = (StorageFolder)await contentFolder.TryGetItemAsync(_jsFolder);
            if (jsFolder != null)
            {
                StorageFolder deviceFolder = (StorageFolder)await jsFolder.TryGetItemAsync(_deviceFolder);
                if (deviceFolder != null)
                {
                    StorageFile fileJsW8 = (StorageFile)await deviceFolder.TryGetItemAsync(_cssPrefix + _jsFileName);
                    if (fileJsW8 != null)
                    {
                        await fileJsW8.RenameAsync(_jsFileName, NameCollisionOption.ReplaceExisting);
                    }
                }
            }
        }        

        #endregion
    
    }

    [DataContract]
    public class RoleDataItem
    {
        public RoleDataItem()
        {
            Authors = new List<AuthorDataItem>();
        }
        
        [DataMember(Name = "type")]
        public string Type { get; set; }

        public List<AuthorDataItem> Authors { get; set; }                  
    }

    [DataContract]
    public class AuthorDataItem
    {            
        public AuthorDataItem()
        {            
        }                

        [DataMember(Name = "id")]
        public string Id { get; set; }

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

        [DataMember(Name = "role_type")]
        public string Role { get; set; }

        [DataMember(Name = "order")]
        public int Order { get; set; }
        
        public override string ToString()
        {
            return this.FullName;
        }

        public static IComparer<AuthorDataItem> SortAscending()
        {
            return (IComparer<AuthorDataItem>)new AuthorComparer();
        }
    }
    
    public class AuthorComparer : IComparer<AuthorDataItem>
    {
        public int Compare(AuthorDataItem authorA, AuthorDataItem authorB)
        {
            if (authorA.Order == authorB.Order)
                return 0;

            if (authorA.Order < authorB.Order)
                return -1;

            return 1;            
        }
    }

    [DataContract]
    public class SchoolDataItem
    {
        public SchoolDataItem()
        {
        }

        public SchoolDataItem(int id, string level, int? cla)
        {
            Id = id;
            EducationLevel = level;
            Class = cla;
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_education_level")]
        public string EducationLevel { get; set; }

        [DataMember(Name = "ep_class")]
        public int? Class { get; set; }


        public bool IsTheSameEducationLevel(int id)
        {
            if (id == 0)
            {
                return true;
            }
            else if (id <= 3)
            {
                return (this.Id > 0 && this.Id <= 3);
            }
            else if (id <= 9)
            {
                return (this.Id > 3 && this.Id <= 9);
            }
            else if (id <= 13)
            {
                return (this.Id > 9 && this.Id <= 13);
            }
            else
                return false;
        }

        [XmlIgnore]
        public int Order {
            get
            {
                //if (this.EducationLevel.Equals("I") || this.EducationLevel.Equals("Wczesnoszkolna"))
                //    return 1;
                //if (this.EducationLevel.Equals("II") || this.EducationLevel.Equals("Podstawowa"))
                //    return 2;
                //else if (this.EducationLevel.Equals("III") || this.EducationLevel.Equals("Gimnazjalna"))
                //    return 3;
                //else if (this.EducationLevel.Equals("IV") || this.EducationLevel.Equals("Ponadgimnazjalna"))
                //    return 4;

                return Id;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrEmpty(this.EducationLevel))
            {
                
                sb.Append("Edukacja ");
                if (this.EducationLevel.Equals("I") || this.EducationLevel.Equals("II"))                
                    sb.Append("podstawowa");
                else if (this.EducationLevel.Equals("III"))
                    sb.Append("gimnazjalna");
                else if (this.EducationLevel.Equals("IV"))
                    sb.Append("ponadgimnazjalna");
                else
                    sb.Append(this.EducationLevel);

                if (this.Class != null || this.Class == 0)
                {
                    sb.Append(", kl. ");
                    sb.Append(this.Class.ToString());
                }
            }
            else
            {
                sb.Append("Wszystkie podręczniki");
            }

            return sb.ToString();
        }
    }

    [DataContract]
    public class SubjectDataItem
    {
        public SubjectDataItem()
        {
        }

        public SubjectDataItem(int id, string subject, int order)
        {
            Id = id;
            Subject = subject;
            Order = order;
        }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "md_name")]
        public string Subject { get; set; }

        [DataMember(Name = "ordering")]
        public int Order { get; set; }

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
    public class ToCDataItem: INotifyPropertyChanged
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
        
        private bool _isSelected = false;

        public bool IsSelected {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("BackgroundColor");
                RaisePropertyChanged("ForegroundColor");                                
            }            
        }

        public SolidColorBrush BackgroundColor
        {
            get
            {
                if (IsSelected)
                {
                    return new SolidColorBrush(Color.FromArgb(143, 0, 34, 68));
                }
                else
                {
                    return new SolidColorBrush(Color.FromArgb(85, 179, 212, 252));
                }
            }
        }

        public SolidColorBrush ForegroundColor
        {
            get
            {
                if (IsSelected)
                {
                    return new SolidColorBrush(Colors.White);
                }
                else
                {
                    return new SolidColorBrush(Colors.Black);
                }
            }
        }

        public double Height
        {
            get {
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

        public string ChildrenVisibility
        {
            get
            {
                return (Children == null || Children.Length == 0) ? "Collapsed" : "Visible";
            }
        }

        public string BackVisibility
        {
            get
            {
                return (Parent == null) ? "Collapsed" : "Visible";
            }
        }        

        public string HeaderVisibility
        {
            get
            {
                return (String.IsNullOrEmpty(Title)) ? "Collapsed" : "Visible";
            }
        }   

        public ToCDataItem()
        {
            IsSelected = false;
        }

        public void UnselectAllChildren()
        {
            if (Children == null || Children.Length == 0)
                return;

            foreach (ToCDataItem item in Children)
            {
                if (item == null)
                    continue;

                item.IsSelected = false;                
            }
        }

        public void SelectChild(string id)
        {
            if (Children == null || Children.Length == 0)
                return;

            foreach (ToCDataItem item in Children)
            {
                if (item == null)
                    continue;

                if (item.Id.Equals(id))
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }

        public ToCDataItem FindItem(string id)
        {
            if (id.Equals(this.Id))
                return this;
            
            if (Children == null || Children.Length == 0)
                return null;

            ToCDataItem[] children = Children;

            if (!Users.IsTeacher)
                children = Children.Where(i => (i.IsTeacher == false)).ToArray();

            foreach (ToCDataItem item in children)
            {
                if (item == null)
                    continue;

                ToCDataItem result = item.FindItem(id);
                if (result != null)
                    return result;
            }

            return null;
        }

        public void PrintItem(string prefix, ref List<String> toc)
        {
            if (toc == null)
                toc = new List<string>();

            toc.Add(prefix + this.Title);

            if (Children == null || Children.Length == 0)
                return;

            ToCDataItem[] children = Children;

            if (!Users.IsTeacher)
                children = Children.Where(i => (i.IsTeacher == false)).ToArray();

            foreach (ToCDataItem item in children)
            {
                if (item == null)
                    continue;

                item.PrintItem(prefix + "      ", ref toc);                
            }
        }

        public void FetchParents()
        {
            if (Children == null || Children.Length == 0)
                return;

            if (!Users.IsTeacher)
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

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HandbookDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ToCDataItem _ToC;

        private List<NoteDataItem> _Notes;

        private List<NoteDataItem> _Bookmarks;

        public ToCDataItem ToC
        {
            get { return _ToC; }
            set
            {
                _ToC = value;
                RaisePropertyChanged("ToC");
                RaisePropertyChanged("Parent");
                RaisePropertyChanged("Title");
                RaisePropertyChanged("Children");
                RaisePropertyChanged("TotalHeight");
                RaisePropertyChanged("BackVisibility");
                RaisePropertyChanged("HeaderVisibility");
                RaisePropertyChanged("ChildrenVisibility");                   
            }
        }

        public List<NoteDataItem> Notes {
            get{
                return _Notes;
            }
            set
            {
                _Notes = value;
                RaisePropertyChanged("Notes");
            } 
        }

        public List<NoteDataItem> Bookmarks {
            get{
                return _Bookmarks;
            }
            set
            {
                _Bookmarks = value;
                RaisePropertyChanged("Bookmarks");
            } 
        }        

        public string Parent {
            get { return ToC.Parent; }
            set
            {
                ToC.Parent = value;
                RaisePropertyChanged("Parent");
            }
        }
        public double TotalHeight {
            get { return ToC.TotalHeight; }            
        }

        public string Title {
            get { return ToC.Title; }
            set
            {
                ToC.Title = value;
                RaisePropertyChanged("Title");
            }
        }

        public ToCDataItem[] Children {
            get { return ToC.Children; }
            set {
                ToC.Children = value;
                RaisePropertyChanged("Children");
            }
        }

        public string Id {
            get { return ToC.Id; }
        }

        public string Visibility {
            get { return ToC.ChildrenVisibility; }
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

        [DataMember(Name = "pageId")]
        public string PageId { get; set; }

        [DataMember(Name = "moduleId")]
        public string ModuleId { get; set; }

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

    #endregion
    
    public sealed class CollectionsDataSource : IDataCollecion
    {
        #region pola klasy

        public static CollectionsDataSource Source = new CollectionsDataSource();

        public static IAsyncOperation<IUICommand> MessageDialogCommand = null;

        public event EventHandler<DownloadEventArgs> DownloadEvent;
        public event EventHandler<FolderSizeEventArgs> FolderSizeEvent;
        public event EventHandler<ThumbEventArgs> ThumbEvent;
        public event EventHandler<CoverEventArgs> CoverEvent;
        public event EventHandler<CancelEventArgs> CancelEvent;
        public event EventHandler<UnzipEventArgs> UnzipEvent;
        public event EventHandler<UnzipProgressEventArgs> UnzipProgressEvent;        
        public event EventHandler<EventArgs> DownloaderInitializedEvent;
        public event EventHandler<ErrorEventArgs> ErrorEvent;

        private static PackageDownloader _downloader;

        private HttpClient _httpClient = new HttpClient();

        // wartość pola _collectionsMetadata powinna zawierać wspólną adres pod którym będą znajdowały się metadane oraz podręczników
        private const string _collectionsMetadata = "http://api.epodreczniki.pl/collections_mobile/";
        public static string Host = _collectionsMetadata;
        
        private const string _collectionsFileName = "Collections.xml";
                
        private const string _requiredAPIVersion = "2.1";

        public static string HandbookFileName = "data.xml";
        public static string HandbooksFolderName = "Handbooks";
        public static string DownloadsFolderName = "Downloads";
        public static string TempFolderName = "temp";

        public static string ThumbImageName = "thumb";
        public static string CoverImageName = "cover";

        public static int MaxWidth = 0;
        
        private ObservableCollection<CollectionDataItem> _collections = new ObservableCollection<CollectionDataItem>();        

        private List<SchoolDataItem> _schools = new List<SchoolDataItem>();
        private List<SubjectDataItem> _subjects = new List<SubjectDataItem>();

        #endregion

        #region propertisy

        public static PackageDownloader Downloader {
            get {
                if (_downloader == null)
                {                
                    _downloader = new PackageDownloader((IDataCollecion)CollectionsDataSource.Source);
                }
                return _downloader;
            }        
        }

        public ObservableCollection<CollectionDataItem> Collections
        {
            get { return this._collections; }
        }

        public ObservableCollection<CollectionDataItem> FilteredCollections
        {
            get
            {
                if (Users.BooksFilter >= 0)
                {
                    SchoolDataItem school = Schools.Where(s => s.Id == Users.BooksFilter).SingleOrDefault();
                    if (school != null)
                    {
                        ObservableCollection<CollectionDataItem> filteredCollection = new ObservableCollection<CollectionDataItem>();

                        if (this._collections != null && this._collections.Count > 0)
                        {
                            foreach ( CollectionDataItem col in this._collections)
                            {
                                if (col == null || col.School == null)
                                    continue;

                                if(col.School.Class == null || col.School.Class == 0)
                                {
                                    if (col.School.IsTheSameEducationLevel(Users.BooksFilter))
                                        filteredCollection.Add(col);
                                }
                                else
                                {
                                    if (col.School.Id == school.Id)
                                        filteredCollection.Add(col);
                                }

                            }
                        }

                        return new ObservableCollection<CollectionDataItem>(filteredCollection.OrderBy(col => col.Order).ToList<CollectionDataItem>());

                    }
                }

                return this._collections;             
            }
        }        

        public List<SchoolDataItem> Schools
        {
            get
            {
                this.FillSchoolsData();
                return this._schools; 
            }
        }

        public List<SubjectDataItem> Subjects
        {
            get
            {             
                this.FillSubjectsData();             
                return this._subjects;
            }
        }

        public bool CanPlayVideo {
            get {
                int connection = App.IsConnectedToInternet();

                return (connection > 0 && (connection < 2 || App.Use3GConnection));
            }
        }        
        
        #endregion

        #region konstruktor

        public CollectionsDataSource()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/psnc.epo.api-v" + _requiredAPIVersion));
        }

        #endregion

        #region statyczne metody publiczne

        public static async Task<IEnumerable<CollectionDataItem>> GetCollectionsAsync(bool update = false)
        {
            bool fetchSucceed = await Source.GetCollectionsDataAsync(update);

            if (fetchSucceed || !update)
                return Source.Collections;
            else
                return null;
        }                  

        public static void SetMaxWidth(Rect bounds)
        {
            MaxWidth = (int)Math.Max(bounds.Height, bounds.Width);
        }

        public static async Task<bool> ClearDownloadsFolder()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;                
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);

                if (downloadsFolder != null)
                {
                    IReadOnlyList<StorageFile> fileList = await downloadsFolder.GetFilesAsync();
                    foreach (StorageFile file in fileList)
                    {
                        if (file == null)
                            continue;
                                                                        
                        await file.DeleteAsync();                           
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> ClearHandbooksFolder()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);

                if (handbooksFolder != null)
                {
                    IReadOnlyList<StorageFolder> handbooksList = await handbooksFolder.GetFoldersAsync();
                    foreach (StorageFolder handbookFolder in handbooksList)
                    {
                        if (handbookFolder == null)
                            continue;
                        

                        IReadOnlyList<StorageFolder> folderList = await handbookFolder.GetFoldersAsync();
                        foreach (StorageFolder folder in folderList)
                        {
                            try
                            {
                                await folder.DeleteAsync();
                            }
                            catch { }
                        }

                        IReadOnlyList<StorageFile> fileList = await handbookFolder.GetFilesAsync();
                        foreach (StorageFile file in fileList)
                        {
                            try
                            {                         
                                await file.DeleteAsync();
                            }
                            catch {}
                        }

                        try
                        {
                            await handbookFolder.DeleteAsync();
                        }
                        catch { }
                    }
                }
                
                StorageFile fileCollections = (StorageFile)await localFolder.TryGetItemAsync(_collectionsFileName);
                if(fileCollections != null)
                {
                    await fileCollections.DeleteAsync();
                    CollectionsDataSource.Source.Collections.Clear();                    
                }
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> ClearFileStore(bool deleteOldHandbooks)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                if (localFolder == null)
                    return false;

                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                
                if (handbooksFolder != null)
                {
                    IReadOnlyList<StorageFolder> handbooksList = await handbooksFolder.GetFoldersAsync();
                    foreach (StorageFolder handbookFolder in handbooksList)
                    {
                        if (handbookFolder == null)
                            continue;

                        var items = await handbookFolder.GetItemsAsync(0,1);
                        if (items == null || items.Count == 0 ||
                            (deleteOldHandbooks &&
                            //usuwanie podręczników pobranych w poprzedniej wersji
                            (handbookFolder.Name.Equals("18131") ||
                            handbookFolder.Name.Equals("18148") ||
                            handbookFolder.Name.Equals("19886") ||
                            handbookFolder.Name.Equals("26546") ||
                            handbookFolder.Name.Equals("26762") ||
                            handbookFolder.Name.Equals("27644") ||
                            handbookFolder.Name.Equals("62352") )))
                        {
                            await handbookFolder.DeleteAsync();
                        }
                        else
                        {
                            IReadOnlyList<StorageFolder> versionsList = await handbookFolder.GetFoldersAsync();
                            foreach (StorageFolder versionFolder in versionsList)
                            {
                                if (versionFolder == null)
                                    continue;

                                StorageFile fileData = (StorageFile)await versionFolder.TryGetItemAsync(HandbookFileName);
                                if (fileData == null)
                                {
                                    await versionFolder.DeleteAsync();
                                }
                            }
                        }
                    }
                }

                if(deleteOldHandbooks)
                {
                    CollectionDataItem[] arCollections = null;

                    XmlSerializer deserializer = new XmlSerializer(typeof(CollectionDataItem[]));
                    StorageFile fileCollectionsToRead = (StorageFile)await localFolder.TryGetItemAsync(_collectionsFileName);
                    if (fileCollectionsToRead != null)
                    {
                        using (Stream fileStream = await fileCollectionsToRead.OpenStreamForReadAsync())
                        {
                            arCollections = (CollectionDataItem[])deserializer.Deserialize(fileStream);
                        }
                    }

                    if (arCollections != null && arCollections.Length > 0)
                    {
                        List<CollectionDataItem> lstCollections = new List<CollectionDataItem>(arCollections);

                        lstCollections.RemoveAll(i => (i.ContentId.Equals("18131") ||
                                                       i.ContentId.Equals("18148") ||
                                                       i.ContentId.Equals("19886") ||
                                                       i.ContentId.Equals("26546") ||
                                                       i.ContentId.Equals("26762") ||
                                                       i.ContentId.Equals("27644") ||
                                                       i.ContentId.Equals("62352")));                        

                        XmlSerializer serializer = new XmlSerializer(typeof(CollectionDataItem[]));
                        StorageFile fileCollectionsToWrite = await localFolder.CreateFileAsync(_collectionsFileName, CreationCollisionOption.ReplaceExisting);
                        if (fileCollectionsToWrite != null)
                        {
                            using (Stream fileStream = await fileCollectionsToWrite.OpenStreamForWriteAsync())
                            {
                                serializer.Serialize(fileStream, lstCollections.ToArray());
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }        

        public static async Task<bool> RemoveCollection(string contentId)
        {
            try
            {
                CollectionDataItem item = Source.Collections.Where(col => col.ContentId.Equals(contentId)).FirstOrDefault();

                if (item == null)
                    return false;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder itemFolder = await handbooksFolder.CreateFolderAsync(item.ContentId, CreationCollisionOption.OpenIfExists);

                if (itemFolder != null)
                {
                    IReadOnlyList<StorageFolder> folderList = await itemFolder.GetFoldersAsync();
                    foreach (StorageFolder folder in folderList)
                    {
                        await folder.DeleteAsync();
                    }

                    IReadOnlyList<StorageFile> fileList = await itemFolder.GetFilesAsync();
                    foreach (StorageFile file in fileList)
                    {
                        try
                        {
                            if (!file.Name.Equals(Path.GetFileName(item.CoverThumbImage)) &&
                                !file.Name.Equals(Path.GetFileName(item.CoverImage)) &&
                                (item.NewVersionItem == null || (!file.Name.Equals(Path.GetFileName(item.NewVersionItem.CoverThumbImage)) &&
                                !file.Name.Equals(Path.GetFileName(item.NewVersionItem.CoverImage)))))
                                await file.DeleteAsync();
                        }
                        catch
                        {
                        }
                    }
                }

                item.OverwriteDataByNewVersion();
                item.Status = CollectionStatus.NotDownloaded;
            }
            catch
            {
                return false;
            }

            return true;
        }        

        #endregion

        #region metody pomocnicze: pobieranie kolekcji

        private async Task<bool> GetCollectionsDataAsync(bool update = false)
        {
            if (this._collections.Count != 0 && !update)
                return true;

            _collections = new ObservableCollection<CollectionDataItem>();

            bool fetchSucceed = false;
            CollectionDataItem[] arCollections = null;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            try
            {
                int connection = App.IsConnectedToInternet();

                if (connection > 0 && (connection < 2 || App.Use3GConnection))
                {                                        
                    using (HttpResponseMessage response = await _httpClient.GetAsync(_collectionsMetadata))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            arCollections = JsonConvert.DeserializeObject<CollectionDataItem[]>(await response.Content.ReadAsStringAsync());                            
                        }
                    }
                }

                if (arCollections != null)
                {
                    arCollections = arCollections.Where(col => col.IsDummy == false).ToArray();

                    foreach (CollectionDataItem item in arCollections)
                    {
                        if (item == null)
                            continue;

                        item.Formats = item.Formats.Where(frm => frm.IsZipFormat).ToArray();
                    }
                }

                fetchSucceed = (arCollections != null && arCollections.Length > 0);                
            }
            catch (Exception exc)
            {
                fetchSucceed = false;
            }

            try
            {                
                if (!fetchSucceed && !update)
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(CollectionDataItem[]));
                    StorageFile fileCollectionsToRead = (StorageFile)await localFolder.TryGetItemAsync(_collectionsFileName);
                    if (fileCollectionsToRead != null)
                    {
                        using (Stream fileStream = await fileCollectionsToRead.OpenStreamForReadAsync())
                        {
                            arCollections = (CollectionDataItem[])deserializer.Deserialize(fileStream);
                        }
                    }
                }

                StorageFolder handbooksFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.HandbooksFolderName, CreationCollisionOption.OpenIfExists);
                StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(CollectionsDataSource.DownloadsFolderName, CreationCollisionOption.OpenIfExists);

                if (handbooksFolder != null)
                {
                    IReadOnlyList<StorageFolder> folders = await handbooksFolder.GetFoldersAsync();

                    foreach (StorageFolder folder in folders)
                    {
                        if (folder == null)
                            continue;

                        CollectionDataItem itemFromAPI = null;

                        if (arCollections != null && arCollections.Length > 0)
                        {
                            itemFromAPI = arCollections.FirstOrDefault(col => col.ContentId.Equals(folder.Name, StringComparison.OrdinalIgnoreCase));
                        }
                        if (itemFromAPI != null)
                        {
                            //CollectionDataItem itemDownload = await this.ReadDownloadFromFileAsync(folder);
                            //if (itemDownload != null)
                            //{
                            //    itemFromAPI.Status = itemDownload.Status;
                            //    itemFromAPI.DownloadId = itemDownload.DownloadId;
                            //    itemFromAPI.DownloadPath = itemDownload.DownloadPath;
                            //}

                            StorageFolder versionFolder = (StorageFolder)await folder.TryGetItemAsync(itemFromAPI.Version);

                            if (versionFolder == null)
                            {
                                IReadOnlyList<StorageFolder> versionsFolders = await folder.GetFoldersAsync();
                                if (versionsFolders != null && versionsFolders.Count > 0)                                
                                {
                                    StorageFolder tempFolder = (StorageFolder)await folder.TryGetItemAsync(TempFolderName);
                                                                        
                                    if (tempFolder != null)
                                    {
                                        await tempFolder.DeleteAsync();
                                        versionsFolders = await folder.GetFoldersAsync();
                                    }

                                    if (versionsFolders != null && versionsFolders.Count > 0 && versionsFolders[0] != null)
                                    {
                                        itemFromAPI.Status = CollectionStatus.UpdateRequired;
                                        versionFolder = versionsFolders[0];
                                    }
                                }
                            }
                            
                            if (versionFolder != null)
                            {
                                CollectionDataItem itemFromData = await ReadMetadataFromFileAsync(versionFolder);

                                if (itemFromData != null)
                                {                                    
                                    if (itemFromAPI.Status == CollectionStatus.UpdateRequired || !itemFromData.Version.Equals(itemFromAPI.Version))
                                    {
                                        if(itemFromData.Status != CollectionStatus.DownloadInProgress && itemFromData.Status != CollectionStatus.UpdateInProgress)
                                            itemFromData.Status = itemFromAPI.Status;

                                        if (itemFromData.NewVersionItem == null || itemFromData.NewVersionItem.Version != itemFromAPI.Version)
                                        {
                                            itemFromData.NewVersionItem = await itemFromAPI.CompleteMetadata();

                                            itemFromData.NewVersionItem.Status = CollectionStatus.NotDownloaded;
                                            itemFromData.NewVersionItem.CoverImage = await itemFromData.NewVersionItem.DownloadCoverFile(false, true);
                                            itemFromData.NewVersionItem.CoverThumbImage = await itemFromData.NewVersionItem.DownloadCoverFile(true, true);
                                        }
                                    }

                                    itemFromAPI = itemFromData;
                                    await itemFromAPI.WriteMetadataFromFileAsync();
                                }
                                else
                                {
                                    itemFromAPI.Status = CollectionStatus.NotDownloaded;

                                    itemFromAPI.CoverImage = await itemFromAPI.DownloadCoverFile(false, false);
                                    itemFromAPI.CoverThumbImage = await itemFromAPI.DownloadCoverFile(true, false);
                                }
                            }
                            else
                            {
                                itemFromAPI = await itemFromAPI.CompleteMetadata();
                                itemFromAPI.Status = CollectionStatus.NotDownloaded;

                                itemFromAPI.CoverImage = await itemFromAPI.DownloadCoverFile(false, false);
                                itemFromAPI.CoverThumbImage = await itemFromAPI.DownloadCoverFile(true, false);
                            }
                        }
                        else
                        {
                            CollectionDataItem itemFromData = null;

                            IReadOnlyList<StorageFolder> versionsFolders = await folder.GetFoldersAsync();
                            if (versionsFolders != null && versionsFolders.Count > 0 && versionsFolders[0] != null)
                            {
                                StorageFolder versionFolder = versionsFolders[0];

                                itemFromData = await ReadMetadataFromFileAsync(versionFolder);                                

                                if (itemFromData != null)
                                    itemFromAPI = itemFromData;
                            }
                        }

                        if (itemFromAPI != null)
                        {
                            itemFromAPI.SetHeightByStatus();

                            itemFromAPI.FolderSizeEvent += DataItem_FolderSizeEvent;
                            itemFromAPI.ThumbEvent += DataItem_ThumbEvent;
                            itemFromAPI.CoverEvent += DataItem_CoverEvent;
                            itemFromAPI.UnzipEvent += DataItem_UnzipEvent;
                            itemFromAPI.UnzipProgressEvent += DataItem_UnzipProgressEvent; 
                            itemFromAPI.DownloadEvent += DataItem_DownloadEvent;                            

                            this.Collections.Add(itemFromAPI);
                        }
                    }

                    if (arCollections != null && arCollections.Length > 0)
                    {
                        for (int idx = 0; idx < arCollections.Length; idx++)
                        {
                            CollectionDataItem apiItem = arCollections[idx];
                            if (apiItem == null)
                                continue;

                            if (this.Collections.Where(col => col.ContentId.Equals(apiItem.ContentId, StringComparison.OrdinalIgnoreCase)).Count() == 0)
                            {
                                apiItem = await apiItem.CompleteMetadata();                                

                                apiItem.FolderSizeEvent += DataItem_FolderSizeEvent;
                                apiItem.ThumbEvent += DataItem_ThumbEvent;
                                apiItem.CoverEvent += DataItem_CoverEvent;
                                apiItem.UnzipEvent += DataItem_UnzipEvent;
                                apiItem.UnzipProgressEvent += DataItem_UnzipProgressEvent; 
                                apiItem.DownloadEvent += DataItem_DownloadEvent;

                                apiItem.CoverImage = await apiItem.DownloadCoverFile(false, false);
                                apiItem.CoverThumbImage = await apiItem.DownloadCoverFile(true, false);

                                apiItem.SetHeightByStatus();

                                this.Collections.Add(apiItem);
                            }
                        }
                    }
                }

                this._collections = new ObservableCollection<CollectionDataItem>(this._collections.OrderBy(col => col.Order).ToList<CollectionDataItem>());

                EventHandler<EventArgs> handlerDownloaderInitializedEvent = DownloaderInitializedEvent;
                if (handlerDownloaderInitializedEvent != null)
                    handlerDownloaderInitializedEvent(this, new EventArgs());                

                XmlSerializer serializer = new XmlSerializer(typeof(CollectionDataItem[]));
                StorageFile fileCollectionsToWrite = await localFolder.CreateFileAsync(_collectionsFileName, CreationCollisionOption.ReplaceExisting);
                if (fileCollectionsToWrite != null)
                {
                    using (Stream fileStream = await fileCollectionsToWrite.OpenStreamForWriteAsync())
                    {
                        serializer.Serialize(fileStream, this.Collections.ToArray());
                    }
                }
            }
            catch
            {
                fetchSucceed = false;
            }


            bool allowClearDownloadsFolder = true;

            if(fetchSucceed)
            {                
                foreach (CollectionDataItem item in CollectionsDataSource.Source.Collections)
                {
                    if (item == null)
                        continue;

                    if (item.Status == CollectionStatus.DownloadInProgress || item.Status == CollectionStatus.UpdateInProgress)
                    {
                        allowClearDownloadsFolder = false;
                        Task.Run(() => item.FinishDownloadedFileToUse());
                    }

                    if (item.DownloadId != null && (item.Status == CollectionStatus.CancelDownloadPending || item.Status == CollectionStatus.CancelUpdatePending))
                    {
                        CollectionsDataSource.Downloader.CancelDownload(item.DownloadId);
                    }
                }
            }

            if(allowClearDownloadsFolder)
                Task.Run(() => CollectionsDataSource.ClearDownloadsFolder());
            
            return fetchSucceed;
        }
        
        private async Task<bool> GetDictionaryDataAsync<T>(List<T> dictionary, string apiAddress, string metadataFileName, bool update = false)
        {
            if (dictionary != null && dictionary.Count != 0 && !update)
                return true;

            dictionary.Clear();

            bool fetchSucceed = false;
            try
            {
                int connection = App.IsConnectedToInternet();

                if (!String.IsNullOrEmpty(apiAddress) && connection > 0 && (connection < 2 || App.Use3GConnection))
                {
                    using (HttpResponseMessage response = await _httpClient.GetAsync(apiAddress))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            dictionary.AddRange(JsonConvert.DeserializeObject<T[]>(await response.Content.ReadAsStringAsync()));

                            if (dictionary.Count > 0)
                            {
                                fetchSucceed = true;
                                XmlSerializer serializer = new XmlSerializer(typeof(T[]));
                                StorageFile fileData = await ApplicationData.Current.LocalFolder.CreateFileAsync(metadataFileName, CreationCollisionOption.ReplaceExisting);
                                if (fileData != null)
                                {
                                    using (Stream fileStream = await fileData.OpenStreamForWriteAsync())
                                    {
                                        serializer.Serialize(fileStream, dictionary.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                fetchSucceed = false;
            }

            if (!fetchSucceed && !update)
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(T[]));
                StorageFile fileData = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(metadataFileName);
                if (fileData != null)
                {
                    using (Stream fileStream = await fileData.OpenStreamForReadAsync())
                    {                        
                        dictionary.AddRange((T[])deserializer.Deserialize(fileStream));
                    }
                }
            }

            return fetchSucceed;
        }

        private void FillSchoolsData()
        {
            if(_schools == null)
                _schools = new List<SchoolDataItem>();

            if (_schools.Count > 0)
                return;

            _schools.Add(new SchoolDataItem(-1, "", 0));
            _schools.Add(new SchoolDataItem(1, "I", 1));
            _schools.Add(new SchoolDataItem(2, "I", 2));
            _schools.Add(new SchoolDataItem(3, "I", 3));
            _schools.Add(new SchoolDataItem(4, "II", 4));
            _schools.Add(new SchoolDataItem(5, "II", 5));
            _schools.Add(new SchoolDataItem(6, "II", 6));
            _schools.Add(new SchoolDataItem(7, "III", 1));
            _schools.Add(new SchoolDataItem(8, "III", 2));
            _schools.Add(new SchoolDataItem(9, "III", 3));
            _schools.Add(new SchoolDataItem(10, "IV", 1));
            _schools.Add(new SchoolDataItem(11, "IV", 2));
            _schools.Add(new SchoolDataItem(12, "IV", 3));
            _schools.Add(new SchoolDataItem(13, "IV", 4));
        }

        private void FillSubjectsData()
        {
            if (_subjects == null)
                _subjects = new List<SubjectDataItem>();

            if (_subjects.Count > 0)
                return;

            _subjects.Add(new SubjectDataItem(9, "Edukacja wczesnoszkolna", 1));
            _subjects.Add(new SubjectDataItem(5, "Język polski", 2));
            _subjects.Add(new SubjectDataItem(1, "Matematyka", 3));
            _subjects.Add(new SubjectDataItem(10, "Historia i społeczeństwo", 4));
            _subjects.Add(new SubjectDataItem(11, "Historia", 5));
            _subjects.Add(new SubjectDataItem(13, "Przyroda", 7));
            _subjects.Add(new SubjectDataItem(7, "Biologia", 8));
            _subjects.Add(new SubjectDataItem(8, "Geografia", 9));
            _subjects.Add(new SubjectDataItem(2, "Fizyka", 10));
            _subjects.Add(new SubjectDataItem(6, "Chemia", 11));
            _subjects.Add(new SubjectDataItem(12, "Wiedza o społeczeństwie", 16));
            _subjects.Add(new SubjectDataItem(3, "Informatyka", 17));
            _subjects.Add(new SubjectDataItem(4, "Zajęcia komputerowe", 18));
            _subjects.Add(new SubjectDataItem(14, "Edukacja dla bezpieczeństwa", 20));
        }

        private static async Task<CollectionDataItem> ReadMetadataFromFileAsync(StorageFolder versionFolder)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(CollectionDataItem));
            StorageFile fileData = (StorageFile)await versionFolder.TryGetItemAsync(HandbookFileName);
            if (fileData != null)
            {
                using (Stream fileStream = await fileData.OpenStreamForReadAsync())
                {
                    return (CollectionDataItem)deserializer.Deserialize(fileStream);
                }
            }
            return null;
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

        #region funkcjonalność rozpakowywania paczki z podręcznikiem        

        public static async Task<ulong> GetDiskFreeSpace()
        {
            IStorageItem sf = ApplicationData.Current.LocalFolder;
            var properties = await sf.GetBasicPropertiesAsync();
            var filteredProperties = await properties.RetrievePropertiesAsync(new[] { "System.FreeSpace" });
            var freeSpace = filteredProperties["System.FreeSpace"];

            return (ulong)freeSpace;
        }

        #endregion

        #region implementacja interfejsu: IDataCollecion

        public void SetDownloadProgress(Guid downloadId, int progress)
        {
            try
            {
                CollectionDataItem item = this.Collections.Where(col => col.DownloadId.Equals(downloadId)).FirstOrDefault();
                if (item != null)
                {
                    item.DownloadProgress = progress;

                    if (item.Status == CollectionStatus.NotDownloaded)
                        item.Status = CollectionStatus.DownloadInProgress;
                    else if (item.Status == CollectionStatus.UpdateRequired)
                        item.Status = CollectionStatus.UpdateInProgress;

                    EventHandler<DownloadEventArgs> handler = DownloadEvent;
                    if (handler != null)
                        handler(this, new DownloadEventArgs(item.ContentId, downloadId, progress));
                }
            }
            catch(Exception exc)
            {
                if(App.IsDebug())
                {                
                    EventHandler<ErrorEventArgs> handler = ErrorEvent;
                    if (handler != null)
                        handler(this, new ErrorEventArgs("Błąd podczas obsługi postępu pobierania podręcznika: " + exc.Message));
                }
            }
        }        

        public async void SetDownloadId(string contentId, Guid downloadId, string filePath)
        {
            string version = String.Empty;

            if(!String.IsNullOrEmpty(contentId))
            {                
                string[] arIds = contentId.Split('_');
                if(arIds != null && arIds.Length > 1)
                {
                    contentId = arIds[0];
                    version = arIds[1];
                }
            }

            CollectionDataItem item = this.Collections.Where(col => col.ContentId.Equals(contentId)).FirstOrDefault();

            if (item == null)
                return;

            item.DownloadId = downloadId;

            if (downloadId.Equals(Guid.Empty))
            {                                
                try
                {
                    if (!item.Version.Equals(version) && item.NewVersionItem != null && item.NewVersionItem.Version.Equals(version))
                    {
                        item.OverwriteDataByNewVersion();
                    }

                    if (item.Status == CollectionStatus.DownloadInProgress || item.Status == CollectionStatus.UpdateInProgress)
                    {
                        item.DownloadProgress = 100;
                        item.DownloadPath = filePath;

                        await item.WriteMetadataFromFileAsync();

                        EventHandler<DownloadEventArgs> handler = DownloadEvent;
                        if (handler != null)
                            handler(this, new DownloadEventArgs(contentId, downloadId, item.DownloadProgress));

                        await item.PrepareDownloadedFileToUse();
                    }
                    else
                    {
                        item.DownloadProgress = 0;                        

                        EventHandler<DownloadEventArgs> handler = DownloadEvent;
                        if (handler != null)
                            handler(this, new DownloadEventArgs(contentId, downloadId, item.DownloadProgress));
                    }
                }
                catch (Exception exc)
                {
                    StringBuilder sbMsg = new StringBuilder();
                    sbMsg.Append("Pobranie podręcznika zakończyło się błędem! Może to spowodować problemy w dostępie do podręcznika. Usuń podręcznik i pobierz go ponownie");
                    if (App.IsDebug())
                    {
                        sbMsg.Append(":");
                        sbMsg.Append(exc.Message);
                    }
                    else
                    {
                        sbMsg.Append(".");
                    }

                    EventHandler<ErrorEventArgs> handler = ErrorEvent;
                    if (handler != null)
                        handler(this, new ErrorEventArgs(sbMsg.ToString()));
                }
                
            }
            else
            {                     
                item.DownloadPath = filePath;

                if(item.Status == CollectionStatus.NotDownloaded)
                    item.Status = CollectionStatus.DownloadInProgress;
                else if (item.Status == CollectionStatus.UpdateRequired)
                    item.Status = CollectionStatus.UpdateInProgress;

                item.WriteMetadataFromFileAsync(false);
            }           
        }

        public async void CancelDownloadId(string contentId, Guid downloadId)
        {
            try
            {
                if (!String.IsNullOrEmpty(contentId))
                {
                    string[] arIds = contentId.Split('_');
                    if (arIds != null && arIds.Length > 1)
                    {
                        contentId = arIds[0];                        
                    }
                }

                CollectionDataItem item = null;

                if (String.IsNullOrEmpty(contentId))
                {
                    if (!downloadId.Equals(Guid.Empty))
                        item = this.Collections.Where(col => col.DownloadId.Equals(downloadId)).FirstOrDefault();
                }
                else
                {
                    item = this.Collections.Where(col => col.ContentId.Equals(contentId)).FirstOrDefault();
                }

                if (item == null)
                    return;

                if (item.Status == CollectionStatus.CancelUpdatePending || item.Status == CollectionStatus.CancelDownloadPending)
                {                    
                    await item.RemoveDownloadedAndUnZipedFile();

                    if (item.Status == CollectionStatus.CancelDownloadPending)
                    {
                        item.FolderSize = String.Empty;
                        item.Status = CollectionStatus.NotDownloaded;
                    }
                    else if (item.Status == CollectionStatus.CancelUpdatePending)
                    {
                        item.Status = CollectionStatus.UpdateRequired;
                    }

                    item.DownloadPath = String.Empty;

                    await item.WriteMetadataFromFileAsync();

                    EventHandler<CancelEventArgs> handler = CancelEvent;
                    if (handler != null)
                        handler(this, new CancelEventArgs(item.ContentId));
                }                
            }
            catch (Exception exc)
            {
                StringBuilder sbMsg = new StringBuilder();
                sbMsg.Append("Zatrzymanie pobierania podręcznika zakończyło się błędem! Usuń podręcznik i spróbuj pobrać go ponownie później");
                if (App.IsDebug())
                {
                    sbMsg.Append(":");
                    sbMsg.Append(exc.Message);                 
                }
                else
                {
                    sbMsg.Append(".");
                }

                EventHandler<ErrorEventArgs> handler = ErrorEvent;
                if (handler != null)
                    handler(this, new ErrorEventArgs(sbMsg.ToString()));
            }
        }

        #endregion
    }
}



