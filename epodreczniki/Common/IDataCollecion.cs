using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epodreczniki.Common
{
    public interface IDataCollecion
    {
        void SetDownloadProgress(Guid downloadId, int progress);

        void SetDownloadId(string contentId, Guid downloadId, string filePath);

        void CancelDownloadId(string contentId, Guid downloadId);
    }
}
