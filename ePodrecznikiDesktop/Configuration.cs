using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ePodrecznikiDesktop
{
    internal class Configuration
    {
        const int port = 8090;
        const string folderName = "ePodręczniki";

        internal static Configuration Current { get; private set; }

        internal int Port {
            get
            {
                return port;
            }
        }

        internal string ContentFolder { get; private set; }

        static Configuration()
        {
            Current = new Configuration();
        }

        internal Configuration()
        {
            this.ContentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), folderName);
            if (!Directory.Exists(this.ContentFolder))
                Directory.CreateDirectory(this.ContentFolder);
        }
    }
}
