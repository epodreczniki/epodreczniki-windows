using ePodrecznikiDesktop.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;

namespace ePodrecznikiDesktop
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class WebBrowserNotify
    {
        public event TermsDelegate TermsReadedDelegate;

        public WebBrowserNotify()
        {
        }

        public void TermsReaded(string isAccepted)
        {
            TermsReadedDelegate();
        }

        public void BackToList()
        {
            try
            {
                MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
                if(mainWindow != null)
                {
                    mainWindow.LoadHandbooksList();
                }                
            }
            catch (Exception)
            { }
        }
        
        public void OpenPdf(string path)
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return;

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), CollectionsDataSource.HandbooksFolderName, path);
                process.Start();
                process.WaitForExit();
            }
            catch (Exception)
            {
                //MessageBox.Show("Błąd podczas próby otwarcia pliku pdf.");
            }
        }              
    }
}