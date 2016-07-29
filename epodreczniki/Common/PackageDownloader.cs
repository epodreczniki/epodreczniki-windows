using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Core;

namespace epodreczniki.Common
{
    public class PackageDownloader
    {
        struct DownloadInfo
        {
            public CancellationTokenSource Cts;
            public DownloadOperation Operation;


            public DownloadInfo(DownloadOperation download)
            {
                this.Cts = new CancellationTokenSource();
                this.Operation = download;
            }
        }

        IDataCollecion m_collecion;
        private List<DownloadInfo> m_activeDownloads;

        public async Task<bool> Initialize()
        {
            return (await DiscoverActiveDownloadsAsync());
        }

        public PackageDownloader(IDataCollecion collecion)
        {
            m_collecion = collecion;
            m_activeDownloads = new List<DownloadInfo>();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceUri">url źródłowy</param>
        /// <param name="contentId">unikalny identfikator kolekcji</param>
        /// <param name="downloadAlways">true - pobieranie działa zawsze niezalażnie w jakiej sieci jesteśmy</param>
        public async void QueueDownload(Uri sourceUri, string contentId, bool downloadAlways, string downloadFolderName)
        {
            // spawdzamy czy ktoś juz tego nie ściaga
            DownloadInfo di;
            if (TryGetById(contentId, out di) == true)
            {
                throw new Exception("Obiekt o podanym identyfikatorze jest już pobierany.");
            }

            string destination = CreateFileName(contentId);
            StorageFile destinationFile;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder downloadsFolder = await localFolder.CreateFolderAsync(downloadFolderName, CreationCollisionOption.OpenIfExists);
            try
            {
                destinationFile = await downloadsFolder.CreateFileAsync(destination, CreationCollisionOption.GenerateUniqueName);
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(sourceUri, destinationFile);

            if (downloadAlways)
                download.CostPolicy = BackgroundTransferCostPolicy.Always;
            else
                download.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;

            download.Priority = BackgroundTransferPriority.Default;


            await HandleDownloadAsync(download, true);
        }

        public async void CancelDownload(Guid downloadId)
        {
            DownloadInfo di;
            if (TryGetByGuid(downloadId, out di))
            {
                di.Cts.Cancel();
                string contentId = GetIdFromFile(di.Operation.ResultFile.Name);
                if (m_collecion != null)
                    m_collecion.CancelDownloadId(contentId, Guid.Empty);

                await di.Operation.ResultFile.DeleteAsync();
            }
            else
            {
                if (m_collecion != null)
                    m_collecion.CancelDownloadId(null, downloadId);
            }
        }


        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            DownloadInfo di = new DownloadInfo(download);
            string contentId = null;
            try
            {
                m_activeDownloads.Add(di);


                contentId = GetIdFromFile(download.ResultFile.Name);

                if (m_collecion != null)
                    m_collecion.SetDownloadId(contentId, download.Guid, download.ResultFile.Path);
                // Store the download so we can pause/resume.

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (start)
                {
                    // Start the download and attach a progress handler.
                    await download.StartAsync().AsTask(di.Cts.Token, progressCallback);
                }
                else
                {
                    // The download was already running when the application started, re-attach the progress handler.
                    await download.AttachAsync().AsTask(di.Cts.Token, progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();

                if (m_collecion != null)
                    m_collecion.SetDownloadId(contentId, Guid.Empty, download.ResultFile.Path);
            }
            catch (Exception)
            {
                if (m_collecion != null)
                    m_collecion.SetDownloadId(contentId, Guid.Empty, download.ResultFile.Path);
            }
            finally
            {
                m_activeDownloads.Remove(di);
            }
        }

        private async Task<bool> DiscoverActiveDownloadsAsync()
        {
            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception)
            {
                return false;
            }

            if (downloads.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    // Attach progress and completion handlers.
                    tasks.Add(HandleDownloadAsync(download, false));
                }

                await Task.WhenAll(tasks);                
            }

            return true;
        }

        private void DownloadProgress(DownloadOperation download)
        {
            try
            {
                double percent = 100;
                if (download.Progress.TotalBytesToReceive > 0)
                {
                    percent = download.Progress.BytesReceived * 100 / download.Progress.TotalBytesToReceive;
                }

                m_collecion.SetDownloadProgress(download.Guid, (int)percent);

                if (download.Progress.HasResponseChanged)
                {
                    download.GetResultStreamAt(0);
                }
            }
            catch
            {

            }
        }

        private string CreateFileName(string contentId)
        {
            return string.Format("{0}.zip", contentId);
        }

        private string GetIdFromFile(string fileName)
        {
            Regex regex = new Regex(@"^(\w+[0-9\.]+)( \(\d+\))?\.zip$");
            Match m = regex.Match(fileName);

            if (m.Groups[1].Success)
            {
                return m.Groups[1].Value;
            }

            return fileName;
        }

        private bool TryGetByGuid(Guid downloadId, out DownloadInfo di)
        {
            bool res = false;
            di.Operation = null;
            di.Cts = null;

            if (m_activeDownloads != null)
                foreach (DownloadInfo d in m_activeDownloads)
                {
                    if (d.Operation.Guid == downloadId)
                    {
                        di.Operation = d.Operation;
                        di.Cts = d.Cts;
                        res = true;
                        break;
                    }
                }

            return res;
        }

        private bool TryGetById(string contentId, out DownloadInfo di)
        {
            bool res = false;
            di.Operation = null;
            di.Cts = null;

            if (m_activeDownloads != null)
                foreach (DownloadInfo d in m_activeDownloads)
                {
                    string id = GetIdFromFile(d.Operation.ResultFile.Name);
                    if (id == contentId)
                    {
                        di.Operation = d.Operation;
                        di.Cts = d.Cts;
                        res = true;
                        break;
                    }
                }

            return res;
        }

    }
}
