using epodreczniki.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;

namespace epodreczniki.DataModel
{    
    
    [DataContract]
    public class ProgressDataItem
    {             
        #region propertisy

        [DataMember(Name = "md_content_id")]
        public string ContentId { get; set; }

        [DataMember(Name = "exercise_id")]
        public string ExerciseId { get; set; }

        [DataMember(Name = "progress")]
        public int Progress { get; set; }

        #endregion        
    }

    [DataContract]
    public class WomiStateItem
    {
        #region propertisy

        [DataMember(Name = "md_content_id")]
        public string ContentId { get; set; }

        [DataMember(Name = "md_womi_id")]
        public string WomiId { get; set; }

        [DataMember(Name = "md_base64")]
        public string Base64 { get; set; }                

        #endregion

        public WomiStateItem(string contentId, string womiId, string base64)
        {
            this.ContentId = contentId;
            this.WomiId = womiId;
            this.Base64 = base64;
        }
    }

    [DataContract]
    public class UserDataItem
    {
        #region pola klasy

        public static string UsersFolderName = "Users";

        #endregion        

        #region propertisy

        [DataMember(Name = "id")]
        public Guid Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        [DataMember(Name = "salt")]
        public string Salt { get; set; }

        [DataMember(Name = "question")]
        public string RecoveryQuestion { get; set; }

        [DataMember(Name = "answer")]
        public string RecoveryAnswer { get; set; }

        [DataMember(Name = "secured")]
        public bool IsSecured { get; set; }

        [DataMember(Name = "admin")]
        public bool IsAdmin { get; set; }   

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "teacher")]
        public bool IsTeacher { get; set; }   

        [DataMember(Name = "notes")]
        public List<NoteDataItem> Notes { get; set; }

        [DataMember(Name = "womistates")]
        public List<WomiStateItem> WomiStates { get; set; }

        [DataMember(Name = "progress")]
        public ProgressDataItem[] Progress { get; set; }
                
        [DataMember(Name = "allow_to_manage")]
        public bool AllowToManageHandbooks { get; set; }   

        [DataMember(Name = "books_filter")]
        public int BooksFilter { get; set; }           

        public bool IsDefault
        {
            get
            {
                return (!Id.Equals(Guid.Empty) 
                    && String.IsNullOrEmpty(Name) 
                    && String.IsNullOrEmpty(Password)
                    && String.IsNullOrEmpty(Salt)
                    && String.IsNullOrEmpty(RecoveryQuestion)
                    && String.IsNullOrEmpty(RecoveryAnswer)
                    && !IsSecured
                    && IsAdmin);
            }
        }

        public bool CompleteData(string name, string pass, string question, string answer, string email)
        {
            try
            {            
                this.Name = name;                
                this.Salt = DataDecoder.CreateSalt();
                this.Password = DataDecoder.HashPassword(pass, this.Salt);
                this.RecoveryQuestion = question;
                this.RecoveryAnswer = DataDecoder.HashPassword(answer.ToLower(), this.Salt);
                this.Email = email;

                var localSettings = ApplicationData.Current.LocalSettings;

                return this.SaveUserDataToSettings(localSettings);
            }
            catch
            {
                return false;
            }
        }

        public string CommandsVisibility
        {
            get
            {
                return this.IsAdmin ? "Collapsed" : "Visible";
            }
        }

        public string AllowToManageHandbooksVisibility
        {
            get
            {
                return this.AllowToManageHandbooks || this.IsAdmin ? "Collapsed" : "Visible";
            }
        }

        public string BlockToManageHandbooksVisibility
        {
            get
            {
                return !this.AllowToManageHandbooks || this.IsAdmin ? "Collapsed" : "Visible";
            }
        }
        
        #endregion

        #region konstruktor

        public UserDataItem()
        {
            IsTeacher = false;
            IsSecured = true;
            IsAdmin = false;
            AllowToManageHandbooks = false;
            Email = String.Empty;
            Notes = new List<NoteDataItem>();
            WomiStates = new List<WomiStateItem>();
            BooksFilter = -1;
        }        

        public UserDataItem(String guid)
        {
            IsTeacher = false;
            IsSecured = true;
            IsAdmin = false;
            AllowToManageHandbooks = false;
            Email = String.Empty;
            Notes = new List<NoteDataItem>();
            WomiStates = new List<WomiStateItem>();
            BooksFilter = -1;

            Guid id = Guid.Empty;
            if (Guid.TryParse(guid, out id))
                this.Id = id;
        }        

        #endregion

        #region metody publiczne


        public string GetMargedNotes(string[] ids)
        {
            StringBuilder sb = new StringBuilder();

            if (ids != null && ids.Length > 0)
            {
                foreach (string id in ids)
                {
                    NoteDataItem noteToMerge = this.Notes.Where(n => n.LocalNoteId.Equals(id)).FirstOrDefault();
                    if (noteToMerge == null)
                        continue;

                    if (sb.Length > 0)
                        sb.Append("\r\n");

                    sb.Append(noteToMerge.Value);
                }
            }

            return sb.ToString();
        }

        public IEnumerable<NoteDataItem> GetNotesAndBookmarksForPage(string moduleId, string pageId)
        {
            return this.Notes.Where(n => (n.PageId.Equals(pageId) && n.ModuleId.Equals(moduleId)));
        }

        public IEnumerable<NoteDataItem> GetNotesForPage(string moduleId, string pageId)
        {
            return this.Notes.Where(n => (n.PageId.Equals(pageId) && n.ModuleId.Equals(moduleId) && n.Type <= NoteType.Note_3));
        }

        public IEnumerable<NoteDataItem> GetNotesForHandbook(string contentId)
        {
            return this.Notes.Where(n => (n.ContentId.Equals(contentId) && n.Type <= NoteType.Note_3));
        }

        public IEnumerable<NoteDataItem> GetBookmarksForPage(string moduleId, string pageId)
        {
            return this.Notes.Where(n => (n.PageId.Equals(pageId) && n.ModuleId.Equals(moduleId) && n.Type >= NoteType.Bookmark_0));
        }

        public IEnumerable<NoteDataItem> GetBookmarksForHandbook(string contentId)
        {
            return this.Notes.Where(n => (n.ContentId.Equals(contentId) && n.Type >= NoteType.Bookmark_0));
        }        

        public NoteDataItem GetNote(string id)
        {
            try
            {
                return this.Notes.Where(n => n.LocalNoteId.Equals(id)).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }        

        public NoteDataItem AddNote(string contentId, string version, string moduleId, string pageId, int index, string subject, string value, NoteType type,  NewNoteData note)
        {
            try
            {
                NoteDataItem newNote = new NoteDataItem(this.Id.ToString(), contentId, version, moduleId, pageId, index, subject, value, type, note);

                if (note.ToMarge != null && note.ToMarge.Length > 0)
                {
                    foreach (string id in note.ToMarge)
                    {
                        NoteDataItem noteToRemove = this.Notes.Where(n => n.LocalNoteId.Equals(id)).FirstOrDefault();
                        if (noteToRemove == null)
                            continue;

                        Notes.Remove(noteToRemove);
                    }
                }

                int insert = -1;
                for (int idx = 0; idx < Notes.Count; idx++)
                {
                    NoteDataItem item = Notes[idx];
                    if (item == null)
                        continue;

                    if(item.Index > index)
                    {
                        insert = idx;
                        break;
                    }
                }
                if (insert < 0)
                    Notes.Add(newNote);
                else
                    Notes.Insert(insert, newNote);

                return newNote;
            }
            catch
            {
                return null;
            }
        }

        public bool DeleteNote(NoteDataItem note)
        {
            try
            {                
                if (note != null)
                    return Notes.Remove(note);

                return false;
            }
            catch
            {
                return false;
            }
        }

        public WomiStateItem GetWomiState(string contentId, string womiId)
        {
            try
            {
                return this.WomiStates.Where(s => (s.ContentId.Equals(contentId) && s.WomiId.Equals(womiId))).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public WomiStateItem SetWomiState(string contentId, string womiId, string base64)
        {
            try
            {
                WomiStateItem newWomi = GetWomiState(contentId, womiId);

                if (newWomi == null)
                {
                    newWomi = new WomiStateItem(contentId, womiId, base64);
                    WomiStates.Add(newWomi);
                }
                else
                {
                    newWomi.Base64 = base64;
                }

                return newWomi;
            }
            catch
            {
                return null;
            }
        }
        
        
        public bool SaveUserDataToSettings(ApplicationDataContainer localSettings = null)
        {
            try
            {
                if (localSettings == null)
                    localSettings = ApplicationData.Current.LocalSettings;

                if (localSettings == null)
                    return false;

                localSettings.Values[String.Format("User_{0}_Name", this.Id.ToString())] = this.Name;
                localSettings.Values[String.Format("User_{0}_Salt", this.Id.ToString())] = this.Salt;
                localSettings.Values[String.Format("User_{0}_Password", this.Id.ToString())] = this.Password;
                localSettings.Values[String.Format("User_{0}_RecoveryQuestion", this.Id.ToString())] = this.RecoveryQuestion;
                localSettings.Values[String.Format("User_{0}_RecoveryAnswer", this.Id.ToString())] = this.RecoveryAnswer;
                localSettings.Values[String.Format("User_{0}_IsSecured", this.Id.ToString())] = this.IsSecured.ToString();
                localSettings.Values[String.Format("User_{0}_IsAdmin", this.Id.ToString())] = this.IsAdmin.ToString();
                localSettings.Values[String.Format("User_{0}_AllowToManageHandbooks", this.Id.ToString())] = this.AllowToManageHandbooks.ToString();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ChangePassword(string pass)
        {
            try
            {
                this.Password = DataDecoder.HashPassword(pass, this.Salt);
                
                return this.SaveUserDataToSettings();                    
            }
            catch
            {
                return false;
            }

        }
        
        public async Task<bool> SaveToFile()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder usersFolder = await localFolder.CreateFolderAsync(UserDataItem.UsersFolderName, CreationCollisionOption.OpenIfExists);

                if (usersFolder != null)
                {
                    UserDataItem userToFile = new UserDataItem();
                    userToFile.CompleteData(this);

                    string data = JsonConvert.SerializeObject(userToFile);

                    var buffer = DataDecoder.EncryptData(data);

                    StorageFile userFile = await usersFolder.CreateFileAsync(this.Id.ToString() + ".dat", CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteBufferAsync(userFile, buffer);
                    
                    return true;                                                            
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async Task<bool> DeleteFile()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder usersFolder = await localFolder.CreateFolderAsync(UserDataItem.UsersFolderName, CreationCollisionOption.OpenIfExists);

                if (usersFolder != null)
                {                    
                    StorageFile userFile = await usersFolder.CreateFileAsync(this.Id.ToString() + ".dat", CreationCollisionOption.ReplaceExisting);
                    if (userFile != null)
                        await userFile.DeleteAsync();
                    
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static async Task<UserDataItem> LoadFromFile(Guid id, UserDataItem user = null)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder usersFolder = await localFolder.CreateFolderAsync(UserDataItem.UsersFolderName, CreationCollisionOption.OpenIfExists);

                if (usersFolder != null)
                {
                    StorageFile userFile = (StorageFile)await usersFolder.TryGetItemAsync(id.ToString() + ".dat");

                    if (userFile == null)
                        return null;

                    var buffer = await FileIO.ReadBufferAsync(userFile);

                    string data = DataDecoder.DecryptData(buffer);
                    
                    UserDataItem userFromFile = JsonConvert.DeserializeObject<UserDataItem>(data);

                    if (user == null)
                        return userFromFile;

                    user.CompleteData(userFromFile);

                    return user;
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public bool CompleteData(UserDataItem user)
        {
            if (user == null)
                return false;

            this.Name = user.Name;
            this.Email = user.Email;
            this.IsTeacher = user.IsTeacher;
            this.Notes = user.Notes;
            this.Progress = user.Progress;
            this.WomiStates = user.WomiStates;
            this.BooksFilter = user.BooksFilter;
            
            return true;
        }

        #endregion        
    }

    public class UsersComparer : IComparer<UserDataItem>
    {        
        public int Compare(UserDataItem userA, UserDataItem userB)
        {
            return String.Compare(userA.Name, userB.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class Users
    {
        #region pola klasy

        public static Users Instance = new Users();

        private ObservableCollection<UserDataItem> _collection = new ObservableCollection<UserDataItem>();        

        #endregion
        
        #region propertisy

        private UserDataItem InstanceLoggedUser
        {
            get; set;
        }

        public ObservableCollection<UserDataItem> Collection
        {
            get { return this._collection; }
        }

        public static UserDataItem LoggedUser
        {
            get
            {
                if (Instance != null)
                    return Instance.InstanceLoggedUser;
                else
                    return null;
            }

            set
            {
                if (Instance != null)
                    Instance.InstanceLoggedUser = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null && value != null)
                {
                    localSettings.Values["Last_Logged_User"] = value.Id.ToString();
                }
            }
        }

        public static string LoggedUserId
        {
            get
            {
                string userId = String.Empty;
                UserDataItem user = Users.LoggedUser;
                if (user != null)
                    userId = "_" + user.Id.ToString();

                return userId;
            }
        }

        public static bool AnyUserExists
        {
            get { return Users.UsersAmount() > 0; }
        }

        public static bool IsTeacher
        {
            get
            {
                UserDataItem user = Users.LoggedUser;

                if (user != null)
                {
                    return user.IsTeacher;
                }
                else
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("User_IsTeacher"))
                    {
                        bool valBool = false;
                        String valString = localSettings.Values["User_IsTeacher"] as String;
                        if (!String.IsNullOrEmpty(valString) && Boolean.TryParse(valString, out valBool))
                            return valBool;
                    }

                    return false;
                }
            }

            set
            {

                UserDataItem user = Users.LoggedUser;

                if (user != null)
                {
                    user.IsTeacher = value;
                }
                else
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["User_IsTeacher"] = value.ToString();
                }
            }
        }

        public static int BooksFilter
        {
            get
            {
                UserDataItem user = Users.LoggedUser;

                if (user != null)
                {
                    return user.BooksFilter;
                }
                else
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null && localSettings.Values.ContainsKey("User_BooksFilter"))
                    {
                        int valInt = -1;
                        String valString = localSettings.Values["User_BooksFilter"] as String;
                        if (!String.IsNullOrEmpty(valString) && Int32.TryParse(valString, out valInt))
                            return valInt;
                    }

                    return -1;
                }
            }

            set
            {

                UserDataItem user = Users.LoggedUser;

                if (user != null)
                {
                    user.BooksFilter = value;
                }
                else
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings != null)
                        localSettings.Values["User_BooksFilter"] = value.ToString();
                }
            }
        }
        
        #endregion

        #region metody prywatne

        private bool GetUsersDataFromSettings()
        {
            if (this._collection.Count != 0)
                return true;

            _collection = new ObservableCollection<UserDataItem>();

            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings != null)
                {
                    String valString = String.Empty;
                    if (localSettings.Values.ContainsKey("Users_Guids"))
                    {
                        valString = localSettings.Values["Users_Guids"] as String;

                        if (!String.IsNullOrEmpty(valString))
                        {
                            var guids = valString.Split(',').ToList();

                            foreach (String guid in guids)
                            {
                                if (String.IsNullOrEmpty(guid))
                                    continue;

                                UserDataItem user = new UserDataItem(guid);

                                var key = String.Format("User_{0}_Name", guid);
                                if (localSettings.Values.ContainsKey(key))
                                    user.Name = localSettings.Values[key] as String;

                                key = String.Format("User_{0}_Salt", guid);
                                if (localSettings.Values.ContainsKey(key))
                                    user.Salt = localSettings.Values[key] as String;

                                key = String.Format("User_{0}_Password", guid);
                                if (localSettings.Values.ContainsKey(key))
                                    user.Password = localSettings.Values[key] as String;

                                key = String.Format("User_{0}_RecoveryQuestion", guid);
                                if (localSettings.Values.ContainsKey(key))
                                    user.RecoveryQuestion = localSettings.Values[key] as String;

                                key = String.Format("User_{0}_RecoveryAnswer", guid);
                                if (localSettings.Values.ContainsKey(key))
                                    user.RecoveryAnswer = localSettings.Values[key] as String;

                                key = String.Format("User_{0}_IsSecured", guid);
                                if (localSettings.Values.ContainsKey(key))
                                {
                                    bool val;
                                    if (Boolean.TryParse(localSettings.Values[key] as String, out val))
                                        user.IsSecured = val;
                                }

                                key = String.Format("User_{0}_IsAdmin", guid);
                                if (localSettings.Values.ContainsKey(key))
                                {
                                    bool val;
                                    if (Boolean.TryParse(localSettings.Values[key] as String, out val))
                                        user.IsAdmin = val;
                                }

                                key = String.Format("User_{0}_AllowToManageHandbooks", guid);
                                if (localSettings.Values.ContainsKey(key))
                                {
                                    bool val;
                                    if (Boolean.TryParse(localSettings.Values[key] as String, out val))
                                        user.AllowToManageHandbooks = val;
                                }

                                _collection.Add(user);
                            }
                        }
                    }
                    else
                    {
                       return Users.CreateDefaultUser();
                    }
                }
            }
            catch
            {
                _collection = null;
                return false;
            }

            return true;
        }

        private bool SaveUsersDataToSettings(bool onlyGuids = false)
        {
            if (this._collection.Count == 0)
                return false;

            try
            {
                bool result = true;
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings == null)
                    return false;

                StringBuilder guids = new StringBuilder();
                foreach (UserDataItem user in _collection)
                {
                    if (user == null)
                        continue;

                    if (guids.Length > 0)
                        guids.Append(",");

                    guids.Append(user.Id.ToString());

                    if (!onlyGuids && !user.SaveUserDataToSettings(localSettings))
                        result = false;
                }

                localSettings.Values["Users_Guids"] = guids.ToString();

                return result;
            }
            catch
            {
                return false;
            }
        }

        private UserDataItem FindUserById(Guid id)
        {
            if (this._collection.Count == 0)
                return null;

            try
            {
                return _collection.Where(u => u.Id.Equals(id)).SingleOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> RemoveAllUsers()
        {
            if (this._collection.Count == 0)
                return true;

            bool result = true;

            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

                foreach(UserDataItem user in Instance.Collection)
                {
                    if(user == null)
                        continue;

                    if (!await user.DeleteFile() || !Users.RemoveUserDataFromSettings(user.Id))
                        result = false;
                }

                if (result)
                {
                    localSettings.Values.Remove("Users_Guids");
                    this._collection.Clear();
                    this.InstanceLoggedUser = null;
                }                
            }
            catch
            {
                return false;
            }

            return result;
        }

        #endregion

        #region metody publiczne
        
        public static ObservableCollection<UserDataItem> GetUsers()
        {
            if (Instance.GetUsersDataFromSettings())
                return Instance.Collection;
            else
                return null;
        }        

        public static UserDataItem FindUser(Guid id)
        {
            return Instance.FindUserById(id);         
        }

        public async static Task<bool> DeleteUser(Guid id)
        {
            return await Instance.DeleteUserById(id);         
        }

        public async static Task<bool> DeleteAllUsers()
        {            
            return await Instance.RemoveAllUsers();            
        }
        
        public static UserDataItem GetLastLoggedUser()
        {            
            UserDataItem user = null;
            Guid id = Guid.Empty;
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings != null && localSettings.Values.ContainsKey("Last_Logged_User"))
            {
                if (Guid.TryParse(localSettings.Values["Last_Logged_User"].ToString(), out id))
                {
                    user = Users.FindUser(id);
                }
            }

            return user;
        }

        private static bool CreateDefaultUser()
        {
            try
            {
                UserDataItem user = new UserDataItem();                
                user.Id = Guid.NewGuid();
                user.IsSecured = false;
                user.IsAdmin = true;
                user.AllowToManageHandbooks = true;
                user.IsTeacher = Users.IsTeacher;
                user.BooksFilter = Users.BooksFilter;

                var localSettings = ApplicationData.Current.LocalSettings;
                if (!user.SaveUserDataToSettings(localSettings))
                    return false;

                Instance.Collection.Add(user);

                StringBuilder guids = new StringBuilder();
                foreach (UserDataItem item in Users.Instance.Collection)
                {
                    if (item == null)
                        continue;

                    if (guids.Length > 0)
                        guids.Append(",");

                    guids.Append(item.Id.ToString());
                }

                if (localSettings != null)
                    localSettings.Values["Users_Guids"] = guids.ToString();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static UserDataItem AddUser(string name, string pass, string question, string answer, string email, bool admin)
        {
            try
            {
                UserDataItem user = new UserDataItem();
                user.Name = name;
                user.Id = Guid.NewGuid();
                user.Salt = DataDecoder.CreateSalt();
                user.Password = DataDecoder.HashPassword(pass, user.Salt);
                user.RecoveryQuestion = question;
                user.RecoveryAnswer = DataDecoder.HashPassword(answer.ToLower(), user.Salt);
                user.Email = email;
                user.IsAdmin = admin;
                user.AllowToManageHandbooks = admin;
                if (user.IsAdmin)
                {
                    user.IsTeacher = Users.IsTeacher;
                    user.BooksFilter = Users.BooksFilter;
                }

                var localSettings = ApplicationData.Current.LocalSettings;
                if (!user.SaveUserDataToSettings(localSettings))
                    return null;

                Instance.Collection.Add(user);

                StringBuilder guids = new StringBuilder();
                foreach (UserDataItem item in Users.Instance.Collection)
                {
                    if (item == null)
                        continue;

                    if (guids.Length > 0)
                        guids.Append(",");

                    guids.Append(item.Id.ToString());
                }

                if (localSettings != null)
                    localSettings.Values["Users_Guids"] = guids.ToString();

                return user;
            }
            catch
            {
                return null;
            }
            
        }

        public static bool SaveUsers()
        {
            return Instance.SaveUsersDataToSettings();                
        }

        public static bool RemoveUserDataFromSettings(Guid id, ApplicationDataContainer localSettings = null)
        {
            try
            {
                if (localSettings == null)
                    localSettings = ApplicationData.Current.LocalSettings;

                if (localSettings == null)
                    return false;

                localSettings.Values.Remove(String.Format("User_{0}_Name", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_Salt", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_Password", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_RecoveryQuestion", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_RecoveryAnswer", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_IsSecured", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_IsAdmin", id.ToString()));
                localSettings.Values.Remove(String.Format("User_{0}_AllowToManageHandbooks", id.ToString()));                

                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static int UsersAmount()
        {
            return Instance._collection.Where(usr => usr.IsDefault == false).Count();
        }

        public async Task<bool> DeleteUserById(Guid id)
        {
            if (this._collection.Count == 0)
                return false;

            try
            {
                UserDataItem user = this.FindUserById(id);
                if (user != null)
                {
                    if (!user.IsAdmin && _collection.Remove(user))
                    {
                        if (await user.DeleteFile())
                        {
                            Users.RemoveUserDataFromSettings(id);
                            this.SaveUsersDataToSettings(true);

                            return true;
                        }
                    }                    
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static IComparer<UserDataItem> SortAscending()
        {
            return (IComparer<UserDataItem>)new UsersComparer();
        }

        #endregion                
    }

}
