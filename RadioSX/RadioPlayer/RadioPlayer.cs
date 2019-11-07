using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RadioSX
{
    class RadioPlayer
    {
        
        CancellationTokenSource tokenSource;
        public RadioPlayer(String url)
        {
            this.url = url;
            tokenSource = new CancellationTokenSource();
           

            playbackState = StreamingPlaybackState.Playing;
            bufferedWaveProvider = null;
            waveOut = null;
            AudioThread = new Task(() => {
                StreamMp3(tokenSource.Token);
            }, tokenSource.Token);

            AudioThread.Start();

        }

        public void SetVolume(int volume)
        {
            this.volume = (float)volume / 100;
            if(waveOut!=null) waveOut.Volume = this.volume;
        }
        float volume=0.2f;
        public void ChangeUrl(String url)
        {
            this.url = url;
            file = "";
            StartRadioPlayerNew();
        }
        String file="";
        public void StartRadio(String file)
        {
            if (File.Exists(file))
            {
                this.url = "";
                this.file = file;

                StartRadioPlayerNew();
            }
            else
            {
                MessageBox.Show("File not found: " + file, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void StartRadioPlayerNew()
        {
            if (waveOut != null) waveOut.Stop();

            tokenSource.Cancel();
            tokenSource.Dispose();
            Console.WriteLine("AudioThread.Wait(); start");

            AudioThread.Wait();



            Console.WriteLine("AudioThread.Wait(); end");

            AudioThread.Dispose();

            bufferedWaveProvider = null;
            waveOut = null;

            tokenSource = new CancellationTokenSource();
            AudioThread = new Task(() =>
            {
                StreamMp3(tokenSource.Token);
            }, tokenSource.Token);

            AudioThread.Start();
        }

        private Task AudioThread;
        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState = StreamingPlaybackState.Stopped;
        private VolumeWaveProvider16 volumeProvider;
        private String url;
  

        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                       bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                       < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }


        private void StreamMp3(CancellationToken cancelToken)
        {

            //16384*4
            var buffer = new byte[16384]; // needs to be big enough to hold a decompressed frame
            if (String.IsNullOrEmpty(file))
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(url);

                webRequest.Headers.Clear();
                 HttpWebResponse resp;
                try
                {
                    resp = (HttpWebResponse)webRequest.GetResponse();

                }
                catch (WebException e)
                {
                    if (e.Status != WebExceptionStatus.RequestCanceled)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return;
                }

                IMp3FrameDecompressor decompressor = null;
                try
                {
                  
                    using (var responseStream = resp.GetResponseStream())
                    {
                        PlayStream(responseStream, cancelToken, buffer, decompressor);

                        Console.WriteLine("Exiting");
                        // was doing this in a finally block, but for some reason
                        // we are hanging on response stream .Dispose so never get there
                        if (decompressor != null) decompressor.Dispose();
                    }
                }

                finally
                {
                    if (decompressor != null)
                    {
                        decompressor.Dispose();
                    }
                }
            }
            else
            {
                
                    using (var responseStream = File.OpenRead(file))
                    {
                        PlayStream(responseStream, cancelToken, buffer);

                        Console.WriteLine("Exiting");
                        // was doing this in a finally block, but for some reason
                        // we are hanging on response stream .Dispose so never get there

                    }
                
               
                    
            }
           



           
        }

        private void PlayStream(Stream responseStream, CancellationToken cancelToken, byte[] buffer, IMp3FrameDecompressor decompressor = null)
        {
            var readFullyStream = new ReadFullyStream(responseStream);
            while (!cancelToken.IsCancellationRequested)
            {


                if (IsBufferNearlyFull)
                {
                    Console.WriteLine("Buffer getting full, taking a break");
                    Thread.Sleep(500);
                    
                }
                else
                {
                    Mp3Frame frame;
                    try
                    {
                        frame = Mp3Frame.LoadFromStream(readFullyStream);
                    }
                    catch (EndOfStreamException)
                    {
                        var fullyDownloaded = true;
                        // reached the end of the MP3 file / stream
                        break;
                    }
                    catch (WebException)
                    {
                        // probably we have aborted download from the GUI thread
                        break;
                    }
                    if (frame == null) break;
                    if (decompressor == null)
                    {
                        // don't think these details matter too much - just help ACM select the right codec
                        // however, the buffered provider doesn't know what sample rate it is working at
                        // until we have a frame
                        decompressor = CreateFrameDecompressor(frame);
                        bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                        bufferedWaveProvider.BufferDuration =
                            TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                                                      //this.bufferedWaveProvider.BufferedDuration = 250;
                    }
                    try
                    {
                        int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                        if (bufferedWaveProvider != null) bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                    }
                    catch (Exception)
                    {

                        
                    }
                   
                    //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                   
                }

                if (waveOut == null && bufferedWaveProvider != null)
                {
                    waveOut = new WaveOut();

                    volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                    volumeProvider.Volume = 1.0f;
                    waveOut.Init(volumeProvider);
                    waveOut.Volume = volume;
                    waveOut.Play();
                }

            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }
    }
}
