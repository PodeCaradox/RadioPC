using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioSX.ViewModel
{
    class MainViewModel : ViewModelBase
    {
        private ObservableCollection<DownloadRadioStream> radioStreams;

        public ObservableCollection<DownloadRadioStream> RadioStreams
        {
            get {
                return radioStreams;
            }
            set {
                radioStreams = value;
                RaisePropertyChanged("RadioStreams");
            }
        }

        private int volume;

        public int Volume
        {
            get { return volume; }
            set {
                volume = value;
                if(radioPlayer!=null)
                radioPlayer.SetVolume(volume);
                RaisePropertyChanged("Volume");
            }
        }
        private Song selectedSong;
        public Song SelectedSong
        {
            get { return selectedSong; }
            set
            {
                selectedSong = value;
                RaisePropertyChanged("SelectedSong");
            }
        }

        private String newRadioURL;
        public String NewRadioURL
        {
            get { return newRadioURL; }
            set
            {
                newRadioURL = value;
                RaisePropertyChanged("NewRadioURL");
            }
        }

        private String newRadioName;
        public String NewRadioName
        {
            get { return newRadioName; }
            set
            {
                newRadioName = value;
                RaisePropertyChanged("NewRadioName");
            }
        }

        private DownloadRadioStream downloadRadioStream;

        public DownloadRadioStream ActualRadioStream
        {
            get { return downloadRadioStream; }
            set
            {
                downloadRadioStream = value;
         
                RaisePropertyChanged("ActualRadioStream");
            }
        }

        private RadioPlayer radioPlayer;

                private String actualSong;

        public String ActualSong
        {
            get { return actualSong; }
            set { actualSong = value;
                RaisePropertyChanged("ActualSong");
            }
        }
        public void LoadRadioStreams()
        {
            String list="";
            if (File.Exists("Radios\\Radios.txt"))
            {

                list = File.ReadAllText("Radios\\Radios.txt");


            }
       
            radioStreams = new ObservableCollection<DownloadRadioStream>();
            if (String.IsNullOrEmpty(list))
            {
                radioStreams.Add(new DownloadRadioStream("https://de-hz-fal-stream02.rautemusik.fm/charthits", "Chart Hits"));
                radioStreams.Add(new DownloadRadioStream("http://de-hz-fal-stream01.rautemusik.fm/top40", "Top 40"));
                radioStreams.Add(new DownloadRadioStream("http://main-high.rautemusik.fm/stream.mp3?ref=rmpage", "Main"));
                radioStreams.Add(new DownloadRadioStream("http://breakz-high.rautemusik.fm/stream.mp3?ref=rmpage", "BreakZ.FM"));
                radioStreams.Add(new DownloadRadioStream("http://weihnachten-high.rautemusik.fm/stream.mp3?ref=rmpage", "Weinachten"));
                radioStreams.Add(new DownloadRadioStream("http://lw3.mp3.tb-group.fm/tb.mp3", "Technobase.fm"));

            }
            else
            {
                var items = list.Split(';');
                for (int i = 0; i < items.Length-1; i+=2)
                {
                    radioStreams.Add(new DownloadRadioStream(items[i], items[i+1]));
                }
            }
               
            
         
          
        
        
        }

        public String url { get; private set; }

        internal void StartRadioPlayer(string url)
        {
            this.url = url;
            if (radioPlayer == null)
            {

                radioPlayer = new RadioPlayer(url);
            }
            else
            {
                radioPlayer.ChangeUrl(url);
            }
        }

        internal void StartFilePlayer(string file)
        {
            url = "";
            radioPlayer.StartRadio(file);
            
        }

        internal void AddNewRadio()
        {
            radioStreams.Add(new DownloadRadioStream(newRadioURL, newRadioName));
            newRadioURL = "";
            newRadioName = "";
            String data="";
            foreach (var radiostream in radioStreams)
            {
                data += radiostream.StreamingUrl + ";" + radiostream.RadioName+";\n";
            }
            File.WriteAllLines("Radios\\Radios.txt", data.Split('\n'));
        }
    }
}
