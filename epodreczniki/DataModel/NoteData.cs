using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace epodreczniki.DataModel
{
    public enum NoteType { Note_0 = 0, Note_1 = 1, Note_2 = 2, Note_3 = 3, Bookmark_0 = 4, Bookmark_1 = 5, Bookmark_2 = 6 };

    [DataContract]
    public class NoteDataItem
    {
        #region propertisy

        [DataMember(Name = "noteId")]
        public uint NoteId { get; set; }

        [DataMember(Name = "userId")]
        public uint UserId { get; set; }

        [DataMember(Name = "localNoteId")]
        public string LocalNoteId { get; set; }

        [DataMember(Name = "localUserId")]
        public string LocalUserId  { get; set; }

        [DataMember(Name = "handbookId")]
        public string HandbookId { 
            get
            {
                return this.ContentId + ":" + this.Version;
            }
            set
            {
                if(!String.IsNullOrEmpty(value))
                {
                    string[] ar = value.Split(':');
                    if(ar != null && ar.Length > 1)
                    {
                        this.ContentId = ar[0];
                        this.Version = ar[1];
                    }
                }                                    
            }
        }

        [DataMember(Name = "moduleId")]
        public string ModuleId { get; set; }

        [DataMember(Name = "pageId")]
        public string PageId { get; set; }
        
        [DataMember(Name = "mdContentId")]
        public string ContentId { get; set; }

        [DataMember(Name = "mdVersion")]
        public string Version { get; set; }

        [DataMember(Name = "location")]
        public Location[] Location { get; set; }

        [DataMember(Name = "subject")]
        public string Subject { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }

        [DataMember(Name = "type")]
        public NoteType Type { get; set; }

        [DataMember(Name = "accepted")]
        public bool Accepted { get; set; }

        [DataMember(Name = "referenceTo")]
        public string ReferenceTo { get; set; }

        [DataMember(Name = "referenceBy")]
        public string[] ReferenceBy { get; set; }
    
        [DataMember(Name = "modifyTime")]
        public DateTime? ModifyTime { get; set; }

        public int Index { get; set; }   

        public static SolidColorBrush ConvertToColorBrush(string value)
        {
            byte alpha;
            byte pos = 0;

            string hex = value.Replace("#", "");

            if (hex.Length == 8)
            {
                alpha = System.Convert.ToByte(hex.Substring(pos, 2), 16);
                pos = 2;
            }
            else
            {
                alpha = System.Convert.ToByte("ff", 16);
            }

            byte red = System.Convert.ToByte(hex.Substring(pos, 2), 16);

            pos += 2;
            byte green = System.Convert.ToByte(hex.Substring(pos, 2), 16);

            pos += 2;
            byte blue = System.Convert.ToByte(hex.Substring(pos, 2), 16);

            return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, red, green, blue));
        }

        public static string GetTypeColor(NoteType type)
        {
            string color = String.Empty;
            switch (type)
            {
                case NoteType.Note_0:
                    color = "#a9d6a4";
                    break;
                case NoteType.Note_1:
                    color = "#9fddf6";
                    break;
                case NoteType.Note_2:
                    color = "#fac9cf";
                    break;
                case NoteType.Note_3:
                    color = "#fff49a";
                    break;
                case NoteType.Bookmark_0:
                    color = "#43bb00";
                    break;
                case NoteType.Bookmark_1:
                    color = "#5da5d9";
                    break;
                case NoteType.Bookmark_2:
                    color = "#ec0c00";
                    break;
                default:
                    break;
            }

            return color;
        }

        public string Color {
            get
            {
                return NoteDataItem.GetTypeColor(this.Type);                
            }
        }

        public bool IsBookmark
        {
            get
            {
                return (this.Type.Equals(NoteType.Bookmark_0) || this.Type.Equals(NoteType.Bookmark_1) || this.Type.Equals(NoteType.Bookmark_2));
            }
        }

        #endregion


        public NoteDataItem()
        {            
        }

        public NoteDataItem(NewNoteData note)
        {
            this.LocalNoteId = Guid.NewGuid().ToString();
            //this.LocalUserId = userId;

            if (note != null)
            {
                if (note.Text.Length > 30)
                    this.Subject = note.Text.Substring(0, 30);
                else
                    this.Subject = note.Text;
                
                this.Value = note.Text;
                this.Location = note.Location;
            }
        }

        public NoteDataItem(string userId, string contentId, string version, string moduleId, string pageId, int index, string subject, string value, NoteType type,  NewNoteData note)
        {
            this.LocalNoteId = Guid.NewGuid().ToString();
            this.LocalUserId = userId;

            this.ContentId = contentId;
            this.Version = version;
            this.ModuleId = moduleId;
            this.PageId = pageId;
            this.Index = index;

            this.Subject = subject;
            this.Value = value;
            this.Type = type;

            if(note != null)
                this.Location = note.Location;
        }
        
    }


    public class NewNoteData
    {
        public string Text { get; set; }
        public Location[] Location { get; set; }
        public string[] ToMarge { get; set; }
    }

    [DataContract]
    public class Location
    {
        [DataMember(Name = "sid")]
        public string sid { get; set; }
        [DataMember(Name = "ranges")]
        public Range[] ranges { get; set; }
    }

    [DataContract]
    public class Range
    {
        [DataMember(Name = "characterRange")]
        public Characterrange characterRange { get; set; }
        [DataMember(Name = "backward")]
        public bool backward { get; set; }
    }

    [DataContract]
    public class Characterrange
    {
        [DataMember(Name = "start")]
        public int start { get; set; }
        [DataMember(Name = "end")]
        public int end { get; set; }
    }

}
