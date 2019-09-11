using Newtonsoft.Json;
using RadioSX.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YoutubeSearch;

namespace RadioSX
{
    internal class DownloadRadioStream : ViewModelBase
    {
        private static int id = 0;

        public DownloadRadioStream(string StreamingUrl, string RadioName) : this(StreamingUrl, RadioName, id++) { }

        [JsonConstructor]
        public DownloadRadioStream(string StreamingUrl, string RadioName,int ID,bool Record = true,bool Show = true)
        {
            this.Show = Show;
            this.RadioName = RadioName;
            this.StreamingUrl = StreamingUrl;
            this.Record = Record;
            this.ID = ID;
            if (!Show) return;

            if (File.Exists("Radios\\" + RadioName + ".json"))
            {
                var list = File.ReadAllText("Radios\\" + RadioName + ".json");
                Songs = JsonConvert.DeserializeObject<ObservableCollection<Song>>(list);
                if (Songs == null) Songs = new ObservableCollection<Song>();
            }
            Task.Factory.StartNew(StartRecording, TaskCreationOptions.LongRunning);
            if (id< ID)
            {
                id = ID;
            }
        }

        public static event EventHandler VariableChangedEvent = new EventHandler((e, a) => { });


        private ObservableCollection<Song> songs = new ObservableCollection<Song>();

        [JsonIgnore]
        public ObservableCollection<Song> Songs
        {
            get { return songs; }
            set
            {
                songs = value;
                RaisePropertyChanged("Songs");
            }
        }



        public int ID { get; set; }

        public string RadioName { get; set; }


        private String streamingUrl;

        public String StreamingUrl
        {
            get { return streamingUrl; }
            set
            {
                streamingUrl = value;

            }
        }

        private bool record;

        public bool Record
        {
            get { return record; }
            set
            {

                record = value;
                RaisePropertyChanged("Record");
                VariableChangedEvent(this, new EventArgs());
            }
        }

        private bool show;

        public bool Show
        {
            get { return show; }
            set
            {

                show = value;
                RaisePropertyChanged("Show");
                VariableChangedEvent(this, new EventArgs());
            }
        }







        private String actualSong;
        [JsonIgnore]
        public String ActualSong
        {
            get { return "Actual Song: " + actualSong; }
            set
            {
                actualSong = value;
                RaisePropertyChanged("ActualSong");
            }
        }
   

        public void StartRecording()
        {
            if (String.IsNullOrEmpty(streamingUrl)) return;

            HttpWebRequest request = null; // web request
            int metaInt = 0; // blocksize of mp3 data
            int count = 0; // byte counter
            int metadataLength = 0; // length of metadata header
            string metadataHeader = ""; // metadata header that contains the actual songtitle
            string oldMetadataHeader = null; // previous metadata header, to compare with new header and find next song
            byte[] buffer = new byte[512]; // receive buffer
            Stream socketStream = null; // input stream on the web request
            Stream byteOut = null; // output stream on the destination file
            request = (HttpWebRequest)WebRequest.Create(streamingUrl);
            HttpWebResponse response = null; // web response

            request.Headers.Clear();
            request.Headers.Add("Icy-MetaData", "1");
            request.Headers.Add("GET", "/" + " HTTP/1.0");
            request.UserAgent = "WinampMPEG/5.09";

          
           
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            var dummy = response.GetResponseHeader("icy-metaint");
            if (!String.IsNullOrEmpty(dummy))
            {
                metaInt = Convert.ToInt32(dummy);
            }


            String songBefore = "";

            try
            {
                // open stream on response
                socketStream = response.GetResponseStream();

                // rip stream in an endless loop
                while (Show)
                {
                    if (!record&& byteOut!=null)
                    {
                        byteOut.Flush();
                        byteOut.Close();
                        byteOut = null;
                    }

                    // read byteblock
                    int bufLen = socketStream.Read(buffer, 0, buffer.Length);
                    if (bufLen < 0)
                        return;

                    for (int i = 0; i < bufLen; i++)
                    {
                        // if there is a header, the 'headerLength' would be set to a value != 0. Then we save the header to a string
                        if (metadataLength != 0)
                        {
                            metadataHeader += Convert.ToChar(buffer[i]);
                            metadataLength--;
                            if (metadataLength == 0) // all metadata informations were written to the 'metadataHeader' string
                            {
                                string fileName = "";

                                // if songtitle changes, create a new file
                                if (!metadataHeader.Equals(oldMetadataHeader))
                                {
                                    
                                    // flush and close old byteOut stream
                                    if (record && byteOut != null)
                                    {
                                        byteOut.Flush();
                                        byteOut.Close();
                                    }
                                    fileName = Regex.Match(metadataHeader, "(StreamTitle=')(.*)(';StreamUrl)").Groups[2].Value.Trim();
                                    ActualSong = fileName;
                                    if (!String.IsNullOrEmpty(songBefore))
                                    {
                                        // extract songtitle from metadata header. Trim was needed, because some stations don't trim the songtitle

                                        Song songsearched = Songs.Where(x => x.Songname == songBefore).FirstOrDefault();
                                        if (songsearched == null)
                                        {
                                            // Keyword
                                            var items = new VideoSearch();

                                            var song = new Song();

                                            song.Songname = songBefore;
                                            song.SavedFile = "Radios\\" + songBefore + ".mp3";
                                            song.StartTime = 0;
                                            song.EndTime = 0;
                                            song.NumberPlayed = 1;
                                            song.ExportSong = false;
                                            song.YoutubeLink = items.SearchQuery(songBefore, 1).FirstOrDefault().Url; ;
                                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                Songs.Add(song);
                                                var convertedJson = JsonConvert.SerializeObject(Songs, Formatting.Indented);
                                                File.WriteAllText("Radios\\" + RadioName + ".json", convertedJson);
                                            }));

                                        }
                                        else
                                        {
                                            songsearched.NumberPlayed++;
                                        }
                                    }


                                    songBefore = fileName;

                                    if (String.IsNullOrEmpty(fileName)) fileName = RadioName;
                                    // write new songtitle to console for information


                                    // create new file with the songtitle from header and set a stream on this file
                                    if (record) byteOut = createNewFile("Radios\\", fileName);

                                    // save new header to 'oldMetadataHeader' string, to compare if there's a new song starting
                                    oldMetadataHeader = metadataHeader;
                                }
                                metadataHeader = "";
                            }
                        }
                        else // write mp3 data to file or extract metadata headerlength
                        {
                            if (count++ < metaInt) // write bytes to filestream
                            {
                                if (record && byteOut != null) // as long as we don't have a songtitle, we don't open a new file and don't write any bytes
                                {

                                    byteOut.Write(buffer, i, 1);

                                    byteOut.Flush();



                                }
                            }
                            else // get headerlength from lengthbyte and multiply by 16 to get correct headerlength
                            {
                                metadataLength = Convert.ToInt32(buffer[i]) * 16;
                                count = 0;

                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (byteOut != null)
                    byteOut.Close();
                if (socketStream != null)
                    socketStream.Close();
            }


        }

        /// <summary>
        /// Create new file without overwritin existing files with the same filename.
        /// </summary>
        /// <param name="destPath">destination path of the new file</param>
        /// <param name="filename">filename of the file to be created</param>
        /// <returns>an output stream on the file</returns>
        private static Stream createNewFile(String destPath, String filename)
        {
            // replace characters, that are not allowed in filenames. (quick and dirrrrrty ;) )
            filename = filename.Replace(":", "");
            filename = filename.Replace("/", "");
            filename = filename.Replace("\\", "");
            filename = filename.Replace("<", "");
            filename = filename.Replace(">", "");
            filename = filename.Replace("|", "");
            filename = filename.Replace("?", "");
            filename = filename.Replace("*", "");
            filename = filename.Replace("\"", "");

            try
            {
                // create directory, if it doesn't exist
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);

                // create new file
                if (File.Exists(destPath + filename + ".mp3"))
                {
                    int i = 1;
                    while (File.Exists(destPath + filename + i + ".mp3"))
                    {
                        i++;
                    }
                    return File.Create(destPath + filename + i + ".mp3");
                }



                return File.Create(destPath + filename + ".mp3");

            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
