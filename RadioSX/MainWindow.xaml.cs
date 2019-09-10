
//using Gma.System.MouseKeyHook;
using NAudio.Wave;
//using Process.NET;
//using Process.NET.Memory;
using RadioSX.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace RadioSX
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
      
        MainViewModel _vm;
        //WpfOverlayDemoExample _overlay;
        bool _work;
        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            this.DataContext = _vm;
            _vm.Volume = 20;
            _vm.LoadRadioStreams();
       

            ServicePointManager.DefaultConnectionLimit = 6;
            //Thread thread = new Thread(timeout => Update());
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.IsBackground = true;
            //thread.Start();

        }




        //public void Update()
        //{
        //    try
        //    {
        //        var process = System.Diagnostics.Process.GetProcessesByName("Notepad").FirstOrDefault();
        //        if (process == null) return;

        //        var _processSharp = new ProcessSharp(process, MemoryType.Remote);
        //        _overlay = new WpfOverlayDemoExample();

        //        var wpfOverlay = (WpfOverlayDemoExample)_overlay;

        //        // This is done to focus on the fact the Init method
        //        // is overriden in the wpf overlay demo in order to set the
        //        // wpf overlay window instance
        //        wpfOverlay.Initialize(_processSharp.WindowFactory.MainWindow);
        //        wpfOverlay.Enable();

        //        _work = true;

        //        wpfOverlay.OverlayWindow.Draw += OnDraw;

        //        var info = wpfOverlay.Settings.Current;
        //        // Do work
        //        while (_work)
        //        {
        //            _overlay.Update();
        //        }
        //    }
        //    finally 
        //    {


        //    }

        //}


        //[Obsolete]
        //private static void OnDraw(object sender, DrawingContext context)
        //{
        //    // Draw a formatted text string into the DrawingContext.
        //    context.DrawText(
        //        new FormattedText("Click Me!", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
        //            new Typeface("Verdana"), 36, Brushes.BlueViolet), new Point(200, 116));

        //    context.DrawLine(new Pen(Brushes.Blue, 10), new Point(100, 100), new Point(10, 10));

        //}


        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                var button = sender as Button;
                var url = _vm.RadioStreams[int.Parse(button.Tag.ToString())].StreamingUrl;
                this.Title = _vm.RadioStreams[int.Parse(button.Tag.ToString())].RadioName;
                _vm.ActualRadioStream = _vm.RadioStreams[int.Parse(button.Tag.ToString())];
                
                if (url != _vm.url)
                {
                    _vm.StartRadioPlayer(url);
                }
            }
        }
  
        private void dg_GotFocus(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = e.OriginalSource as DataGridCell;
            if (cell != null && cell.Column is DataGridCheckBoxColumn)
            {
                ListBoxSongs.BeginEdit();
                CheckBox chkBox = cell.Content as CheckBox;
                if (chkBox != null)
                {
                
                    chkBox.IsChecked = !chkBox.IsChecked;
                }
            }
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
   
            if (!(e.OriginalSource is Rectangle))
            {
                if (_vm.SelectedSong != null)
                {
                    _vm.StartFilePlayer((ListBoxSongs.SelectedItem as Song).SavedFile);

                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            
            var songs = _vm.ActualRadioStream.Songs.Where(x => x.ExportSong).ToList();
            String daten = "";
            foreach (var song in songs)
            {
                daten += song.YoutubeLink + "\n";
            }
            File.WriteAllLines("Radios\\YoutubeLinks.txt", daten.Split('\n'));
        }

        private void AddRadio_Click(object sender, RoutedEventArgs e)
        {
            AddRadioStream addRadioStream = new AddRadioStream();
            addRadioStream.DataContext = this.DataContext;
            addRadioStream.ShowDialog();
            if (!String.IsNullOrEmpty(_vm.NewRadioName) && !String.IsNullOrEmpty(_vm.NewRadioURL))
            {
                _vm.AddNewRadio();
            }

        }
    }
}
