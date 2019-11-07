using Newtonsoft.Json;
using RadioSX.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        
        [JsonConstructor]
        public DownloadRadioStream(string StreamingUrl, string RadioName, String SongNamesSearchForString, String SongNamesUrl,bool Record, int ID = 0, bool ReadStreamTags = true,bool Active = true, bool Show = true)
        {
            this.RadioName = RadioName;
            this.StreamingUrl = StreamingUrl;
            this.Record = Record;
            this.ReadStreamTags = ReadStreamTags;
            
            this.Active = Active;
            this.SongNamesSearchForString = SongNamesSearchForString;
            this.SongNamesUrl = SongNamesUrl;
            this.Show = Show;
            if (File.Exists("Radios\\" + RadioName + ".json"))
            {
                var list = File.ReadAllText("Radios\\" + RadioName + ".json");
                Songs = JsonConvert.DeserializeObject<ObservableCollection<Song>>(list);
                if (Songs == null) Songs = new ObservableCollection<Song>();
            }
            if (id< ID)
            {
                id = ID;

            }
        
            this.ID = id;
            id++;
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

        private bool show;

        public bool Show
        {
            get { return show; }
            set
            {
                show = value;

            }
        }

        private String songNamesUrl;

        public String SongNamesUrl
        {
            get { return songNamesUrl; }
            set
            {
                songNamesUrl = value;

            }
        }

        private String songNamesSearchForString;

        public String SongNamesSearchForString
        {
            get { return songNamesSearchForString; }
            set
            {
                songNamesSearchForString = value;
                VariableChangedEvent(this, new EventArgs());

            }
        }

        private Task task;
        private bool active;

        public bool Active
        {
            get { return active; }
            set
            {

                active = value;
                if (value)
                {
                    if (task == null || (task.Status != TaskStatus.Running && task.Status != TaskStatus.WaitingToRun))
                    {
                       
                        task = Task.Factory.StartNew(StartRecording, TaskCreationOptions.LongRunning);
                    }
                    
                }
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
                if (record == false && readStreamTags == false)
                {
                    Active = false;
                }else if (record == true && readStreamTags == false)
                {
                    Active = true;
                    
                }
            }
        }

        private bool readStreamTags;

        public bool ReadStreamTags
        {
            get { return readStreamTags; }
            set
            {
           
                readStreamTags = value;
                RaisePropertyChanged("ReadStreamTags");
                VariableChangedEvent(this, new EventArgs());
                if (record == false && readStreamTags == false)
                {
                    Active = false;
                }
                else if (record == false && readStreamTags == true)
                {
                    Active = true;

                }
            }
        }


        private String actualSong = "";
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


        private String youtubeLink;
        [JsonIgnore]
        public String YoutubeLink
        {
            get { return youtubeLink; }
            set
            {
                youtubeLink = value;
                RaisePropertyChanged("YoutubeLink");
            }
        }


        VideoSearch videoSearch = new VideoSearch();
        Stopwatch stopwatch;
        public void StartRecording()
        {
            Console.WriteLine(StreamingUrl);

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

            stopwatch = new Stopwatch();
            if (!String.IsNullOrEmpty(SongNamesUrl)) stopwatch.Start();
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
                while (active)
                {
                    stopwatch.Start();
                    if (!readStreamTags && !record&& byteOut!=null)
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

                        if (!Active) break;

                        // if there is a header, the 'headerLength' would be set to a value != 0. Then we save the header to a string
                        if (!String.IsNullOrEmpty(SongNamesUrl))
                        {
                            if (!String.IsNullOrEmpty(SongNamesSearchForString))
                                if (stopwatch.ElapsedMilliseconds >= 20000 || String.IsNullOrEmpty(actualSong))//1min = 60000
                                {
                                    var result = new WebClient().DownloadString(SongNamesUrl).Replace("\n",String.Empty).Replace("\r", String.Empty).Replace("\t", ""); ;
                                    result = WebUtility.HtmlDecode(result);
                                    String[] spearator = { "|####|" };
                                    String[] searchStrings = SongNamesSearchForString.Replace("\n", String.Empty).Replace("\r", String.Empty).Split(spearator,StringSplitOptions.RemoveEmptyEntries);

                                    int index = int.MaxValue;
                                    String searchString = "";
                                    foreach (var newsearchString in searchStrings)
                                    {
                                        if (result.Contains(newsearchString))
                                        {
                                            var newIndex = result.IndexOf(newsearchString);
                                            if (newIndex < index)
                                            {
                                                searchString = newsearchString;
                                                index = newIndex;
                                            }

                                        }
                                    }

                                    if (!String.IsNullOrEmpty(searchString))
                                    {
                                        index += searchString.Length;
                                        var start = result.IndexOf(">", index) + 1;
                                        var end = result.IndexOf("</", index);
                                        var fileName = result.Substring(start, end - start);
                                        if (!actualSong.Equals(fileName))
                                        {
                                            actualSong = fileName;
                                            ActualSong = fileName;
                                            SaveSong(fileName);
                                        }
                                    }
                                   
                                    stopwatch.Restart();
                                }
                           

                        }


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
                             

                                    if (String.IsNullOrEmpty(fileName)) {

                                    

                                        fileName = RadioName;
                                    }
                                    else
                                    {
                                        ActualSong = fileName;
                                        SaveSong(songBefore);
                                        songBefore = fileName;
                                    }
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

        private void SaveSong(string songBefore)
        {
            YoutubeLink = videoSearch.SearchQuery(songBefore, 1).FirstOrDefault().Url;
            if (!String.IsNullOrEmpty(songBefore))
            {
                // extract songtitle from metadata header. Trim was needed, because some stations don't trim the songtitle

                Song songsearched = Songs.Where(x => x.Songname == songBefore).FirstOrDefault();
                if (songsearched == null)
                {
                    // Keyword


                    var song = new Song();

                    song.Songname = songBefore;
                    song.SavedFile = "Radios\\" + songBefore + ".mp3";
                    song.StartTime = 0;
                    song.EndTime = 0;
                    song.NumberPlayed = 1;
                    song.ExportSong = false;
                    song.YoutubeLink = YoutubeLink;
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
