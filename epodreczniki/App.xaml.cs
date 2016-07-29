using epodreczniki.Common;
using epodreczniki.Data;
using epodreczniki.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Grid App template is documented at http://go.microsoft.com/fwlink/?LinkId=234226

namespace epodreczniki
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }
        
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;
            string currentPageTypeName = String.Empty;
            bool firstLaunch = false;   

            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
            

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active

                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();
                    //Associate the frame with a SuspensionManager key                                
                    SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                    // Set the default language
                    rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        // Restore the saved session state only when appropriate
                        try
                        {
                            Users.GetUsers();
                            Users.LoggedUser = Users.GetLastLoggedUser();

                            Object value = localSettings.Values["CurrentSourcePageType"];
                            if (value != null)
                                currentPageTypeName = (string)value;

                            await SuspensionManager.RestoreAsync();
                        }
                        catch (SuspensionManagerException)
                        {
                            //Something went wrong restoring state.
                            //Assume there is no state and continue
                        }
                    }

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }
            
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter

                    bool accepted = false;                                
                    if (localSettings != null)
                    {                    
                        Object value = localSettings.Values["PrivacyPolicyAccepted" + "_" + App.AppVersion()];
                        if (value != null)
                            accepted = (bool)value;                    
                    }

                    ObservableCollection<UserDataItem> users = Users.GetUsers();
                    UserDataItem admin = null;
                    if (users != null)
                        admin = users.Where(u => u.IsAdmin).SingleOrDefault();                

                    if (accepted)
                    {                    
                        bool loginRequired = false;
                        if(users != null)
                            loginRequired = (users.Count > 1 || admin != null && admin.IsSecured);

                        if (loginRequired)
                        {
                            rootFrame.Navigate(typeof(LoginPage), e.Arguments);
                        }
                        else
                        {
                            // jeśli nie trzeba się logować bo jest tylko jeden użytkownik: ustaw admina jako zalogowanego użytkownika
                            if (admin != null)
                            {
                                Users.LoggedUser = admin;
                                await UserDataItem.LoadFromFile(Users.LoggedUser.Id, Users.LoggedUser);
                            }

                            if (String.IsNullOrEmpty(currentPageTypeName))
                            {
                                rootFrame.Navigate(typeof(HandbooksListPage), e.Arguments);
                            }
                            else
                            {
                                rootFrame.Navigate(Type.GetType("epodreczniki." + currentPageTypeName), e.Arguments);
                            }
                        }
                    }
                    else
                    {
                        firstLaunch = true;
                        // jeśli jest to pierwsze uruchomienie: jest tylko jeden domyślny użytkownik - admin: ustaw go jako zalogowanego użytkownika
                        if (admin != null)
                            Users.LoggedUser = admin;

                        rootFrame.Navigate(typeof(PrivacyPolicyPage), e.Arguments);
                    }
                }
            }
            catch (Exception)
            {

            }

            // Ensure the current window is active
            Window.Current.Activate();

            CollectionsDataSource.Source.DownloaderInitializedEvent += Source_DownloaderInitializedEvent;

            if (!await CollectionsDataSource.ClearFileStore(firstLaunch))
            {
                var messageDialog = new MessageDialog("Program wykrył problemy z dostępem do lokalnego systemu plików. Może spowodować to nieprawidłowe działanie aplikacji.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }
        }

        async void Source_DownloaderInitializedEvent(object sender, EventArgs e)
        {
            if(!await CollectionsDataSource.Downloader.Initialize())
            {
                var messageDialog = new MessageDialog("Program wykrył problemy z dostępem do lokalnego systemu plików. Może spowodować to nieprawidłowe działanie aplikacji.");
                messageDialog.Commands.Add(new UICommand("Zamknij", null));
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame != null)
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && !(rootFrame.CurrentSourcePageType == typeof(LoginPage)))
                    {
                        localSettings.Values["CurrentSourcePageType"] = rootFrame.CurrentSourcePageType.Name;
                    }
                }

                var deferral = e.SuspendingOperation.GetDeferral();
                await SuspensionManager.SaveAsync();
                deferral.Complete();
            }
            catch(Exception)
            {

            }
        }

        public static int IsConnectedToInternet()
        {
            try
            {
                ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

                if (InternetConnectionProfile == null)
                {
                    return -1;
                }                
                
                var level = InternetConnectionProfile.GetNetworkConnectivityLevel();

                if(level == NetworkConnectivityLevel.InternetAccess)
                {
                    var interfaceType = InternetConnectionProfile.NetworkAdapter.IanaInterfaceType;

                    // 71 is WiFi & 6 is Ethernet(LAN)
                    if (interfaceType == 71 || interfaceType == 6)
                    {
                        return 1;
                    }
                    // 243 & 244 is 3G/Mobile
                    else if (interfaceType == 243 || interfaceType == 244)
                    {
                        return 2;
                    } 
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        public static bool Use3GConnection
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null && localSettings.Values.ContainsKey("Use3GConnection"))
                {
                    bool valBool = false;
                    String valString = localSettings.Values["Use3GConnection"] as String;
                    if (!String.IsNullOrEmpty(valString) && Boolean.TryParse(valString, out valBool))
                        return valBool;
                }

                return false;
            }

            set
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null)
                    localSettings.Values["Use3GConnection"] = value.ToString();
            }
        }

        public static bool AllowUsersToCreateAccounts
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null && localSettings.Values.ContainsKey("AllowUsersToCreateAccounts"))
                {
                    bool valBool = false;
                    String valString = localSettings.Values["AllowUsersToCreateAccounts"] as String;
                    if (!String.IsNullOrEmpty(valString) && Boolean.TryParse(valString, out valBool))
                        return valBool;
                }

                return false;
            }

            set
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null)
                    localSettings.Values["AllowUsersToCreateAccounts"] = value.ToString();
            }
        }

        public static string AppVersion()
        {
            try
            {
                var ver = Windows.ApplicationModel.Package.Current.Id.Version;
                StringBuilder sbApp = new StringBuilder();
                sbApp.Append(ver.Major.ToString());
                sbApp.Append(".");
                sbApp.Append(ver.Minor.ToString());
                sbApp.Append(".");
                sbApp.Append(ver.Build.ToString());
                sbApp.Append(".");
                sbApp.Append(ver.Revision.ToString());

                return sbApp.ToString();
            }
            catch
            {
                return String.Empty;
            }
        }

        public static bool IsDebug()
        {
            return false;
        }

        public static bool ClearLocalSettings()
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

                if (localSettings == null)
                    return false;
                
                //localSettings.Values.Remove("PrivacyPolicyAccepted" + "_" + App.AppVersion());
                //localSettings.Values.Remove("CurrentSourcePageType");
                //localSettings.Values.Remove("Use3GConnection");
                //localSettings.Values.Remove("AllowUsersToCreateAccounts");
                //localSettings.Values.Remove("Last_Logged_User");
                //localSettings.Values.Remove("User_IsTeacher");

                localSettings.Values.Clear();
                
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
