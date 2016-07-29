using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics; 
using System.Runtime.InteropServices;
using System.Runtime;
using System.Net;

namespace ePodrecznikiDesktop
{
    public class IISExpress
    {
        public int Port { get; set; }

        internal class NativeMethods
        {
            // Methods
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetTopWindow(IntPtr hWnd);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }

        public static void SendStopMessageToProcess(int PID)
        {
            try
            {
                for (IntPtr ptr = NativeMethods.GetTopWindow(IntPtr.Zero); ptr != IntPtr.Zero; ptr = NativeMethods.GetWindow(ptr, 2))
                {
                    uint num;
                    NativeMethods.GetWindowThreadProcessId(ptr, out num);
                    if (PID == num)
                    {
                        HandleRef hWnd = new HandleRef(null, ptr);
                        NativeMethods.PostMessage(hWnd, 0x12, IntPtr.Zero, IntPtr.Zero);
                        return;
                    }
                }
            }
            catch (ArgumentException)
            {
            }
        }

        const string IIS_EXPRESS = @"IIS Express\iisexpress.exe";
        const string PATH = "path";
        const string PORT = "port";

        Process process;

        IISExpress(string path, int startport)
        {
            bool alreadyOpened;
            int port = GetFreePort(startport, out alreadyOpened);
            string iisExpressPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), IIS_EXPRESS);

            if (!File.Exists(iisExpressPath))                
                throw new ApplicationException("Program IIS Expres nie jest zainstalowany na tym kompuerze!\n Czytanie podręcznika nie jest możliwe.");

            this.Port = port;

            if (alreadyOpened == false)
            {
                StringBuilder arguments = new StringBuilder();
                if (!string.IsNullOrEmpty(path))
                    arguments.AppendFormat("/{0}:{1} ", PATH, path);

                if (port > 0)
                    arguments.AppendFormat("/{0}:{1} ", PORT, port);

                process = Process.Start(new ProcessStartInfo()
                {
                    FileName = iisExpressPath,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
        }

        public static IISExpress Start(string path, int startport)
        {
            return new IISExpress(path, startport);
        }

        public void Stop()
        {
            if (process != null)
            {
                SendStopMessageToProcess(process.Id);
                process.Close();
            }
        }


        private int GetFreePort(int proposedPort, out bool alreadyOpened)
        {
            alreadyOpened = false;
            bool ok = false;
            int port = proposedPort;
            
            string url = string.Format("http://localhost:{0}", port);

            while (!ok)
            {
                try
                {
                    HttpWebRequest webrequest = WebRequest.Create(url) as HttpWebRequest;
                    webrequest.Method = "HEAD";
                    webrequest.Timeout = 400;
                    WebResponse response = webrequest.GetResponse();

                    for (int i = 0; i < response.Headers.Count; i++)
                    {
                        String header = response.Headers.GetKey(i);
                        if (header == "X-EP")
                        {
                            ok = true;
                            alreadyOpened = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ok = true;
                    // jezeli na tym porcie nic nie ma to znaczy ze jest wolny
                }


                if (ok == false)
                {
                    port++;
                }
            }


            return port;
        }
    }
}
