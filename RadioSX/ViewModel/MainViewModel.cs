using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            radioStreams = new ObservableCollection<DownloadRadioStream>();
            radioStreams.Add(new DownloadRadioStream("https://de-hz-fal-stream02.rautemusik.fm/charthits", "Chart Hits"));
            radioStreams.Add(new DownloadRadioStream("http://de-hz-fal-stream01.rautemusik.fm/top40", "Top 40"));
            radioStreams.Add(new DownloadRadioStream("http://main-high.rautemusik.fm/stream.mp3?ref=rmpage", "Main"));
            radioStreams.Add(new DownloadRadioStream("http://breakz-high.rautemusik.fm/stream.mp3?ref=rmpage", "BreakZ.FM"));
            radioStreams.Add(new DownloadRadioStream("http://weihnachten-high.rautemusik.fm/stream.mp3?ref=rmpage", "Weinachten"));
            radioStreams.Add(new DownloadRadioStream("http://lw3.mp3.tb-group.fm/tb.mp3", "Technobase.fm"));
        
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
    }
}
