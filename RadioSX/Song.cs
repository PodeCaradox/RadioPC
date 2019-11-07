using Newtonsoft.Json;
using RadioSX.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioSX
{
    public class Song : ViewModelBase
    {
        public int StartTime { get; set; }

        public int EndTime { get; set; }

        public String SavedFile { get; set; }

        private String songname;
        public String Songname
        {
            get { return songname; }
            set {
                songname = value;
             
                RaisePropertyChanged("Songname");
            }
        }

        private String youtubeLink;
        public String YoutubeLink
        {
            get { return youtubeLink; }
            set
            {
                youtubeLink = value;
                RaisePropertyChanged("YoutubeLink");
            }
        }

        private bool exportSong;

        [JsonIgnore]
        public bool ExportSong
        {
            get { return exportSong; }
            set
            {
                exportSong = value;
                RaisePropertyChanged("ExportSong");
            }
        }

        private int numberPlayed;
        public int NumberPlayed
        {
            get { return numberPlayed; }
            set
            {
                numberPlayed = value;
                RaisePropertyChanged("NumberPlayed");
            }
        }


        public Song()
        {

        }
    }
}
