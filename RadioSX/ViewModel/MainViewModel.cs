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

    
        public void LoadRadioStreams()
        {
            String list="";
            if (File.Exists("Radios\\Radios.txt"))
            {

                list = File.ReadAllText("Radios\\Radios.txt");


            }
       
            RadioStreams = new ObservableCollection<DownloadRadioStream>();
            if (String.IsNullOrEmpty(list))
            {
                RadioStreams.Add(new DownloadRadioStream("https://de-hz-fal-stream02.rautemusik.fm/charthits", "Chart Hits"));
                RadioStreams.Add(new DownloadRadioStream("http://de-hz-fal-stream01.rautemusik.fm/top40", "Top 40"));
                RadioStreams.Add(new DownloadRadioStream("http://main-high.rautemusik.fm/stream.mp3", "Main"));
                RadioStreams.Add(new DownloadRadioStream("http://breakz-high.rautemusik.fm/stream.mp3", "BreakZ.FM"));
                RadioStreams.Add(new DownloadRadioStream("http://weihnachten-high.rautemusik.fm/stream.mp3", "Weinachten"));
                RadioStreams.Add(new DownloadRadioStream("http://lw3.mp3.tb-group.fm/tb.mp3", "Technobase.fm"));

            }
            else
            {

                RadioStreams = JsonConvert.DeserializeObject<ObservableCollection<DownloadRadioStream>>(list);
               
            }

            DownloadRadioStream.VariableChangedEvent += VariableChangedEvent;





        }

        private void VariableChangedEvent(object sender, EventArgs e)
        {
            SafeRadios();
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
            SafeRadios();
            
        }

        private void SafeRadios()
        {
            var convertedJson = JsonConvert.SerializeObject(radioStreams, Formatting.Indented);
            File.WriteAllText("Radios\\Radios.txt", convertedJson);
        }

        internal void DeleteRadio(object tag)
        {
            int id = int.Parse(tag.ToString());
            var radio = radioStreams.Where(x => x.ID == id).FirstOrDefault();
            if (radio == null) return;
            radio.Show = false;
        }
    }
}
