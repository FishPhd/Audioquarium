using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dewritwo.Resources;
using File = TagLib.File;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MusicPlayer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MediaPlayer mplayer = new MediaPlayer();
        private readonly Random rnd = new Random();
        private bool audioPlaying;
        private bool audiouMuted;
        private bool dragStarted;
        private bool shuffleSongs;

        public MainWindow()

        {
            InitializeComponent();
            Cfg.Initial(false);
            Load();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (mplayer.Source != null && mplayer.NaturalDuration.HasTimeSpan)
            {
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                scrubBar.Minimum = 0;
                scrubBar.Maximum = mplayer.NaturalDuration.TimeSpan.TotalSeconds;

                if (!dragStarted)
                {
                    scrubBar.Value = mplayer.Position.TotalSeconds;
                    scrubTime.Content = mplayer.Position.ToString(@"mm\:ss") + " - " + selectedSong.Length;
                }
            }

            if (mplayer.Source != null && mplayer.NaturalDuration.HasTimeSpan &&
                Convert.ToInt32(mplayer.Position.TotalSeconds) ==
                Convert.ToInt32(mplayer.NaturalDuration.TimeSpan.TotalSeconds))
            {
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex + 1;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();

                audioPlaying = true;
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Content = selectedSong.Album;
                nowPlayingTrack.Content = selectedSong.Track;
            }
        }

        public void Load()
        {
            if (Cfg.configFile["Music.Directory1"] != "")
            {
                songDataGrid.ItemsSource = Itemsource.LoadSongs(Cfg.configFile["Music.Directory1"]);
            }
            else
            {
                songDataGrid.ItemsSource = null;
            }

            directory1.Text = Cfg.configFile["Music.Directory1"];
            /*
            directory1.Text = Cfg.configFile["Music.Directory2"];
            directory1.Text = Cfg.configFile["Music.Directory3"];
            directory1.Text = Cfg.configFile["Music.Directory4"];
            */
        }

        private void SongGrid_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mplayer.Close();
            var selectedSong = songDataGrid.CurrentItem as Itemsource.Songs;

            mplayer.Open(new Uri(selectedSong.FileName));
            mplayer.Play();
            audioPlaying = true;
            GetAlbumart();

            nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
            nowPlayingAlbum.Content = selectedSong.Album;
            nowPlayingTrack.Content = selectedSong.Track;
            playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};
        }

        private void GetAlbumart()
        {
            var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;
            var tagFile = File.Create(selectedSong.FileName);

            // Load you image data in MemoryStream
            var pic = tagFile.Tag.Pictures[0];
            var ms = new MemoryStream(pic.Data.Data);
            ms.Seek(0, SeekOrigin.Begin);

            // ImageSource for System.Windows.Controls.Image
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            // Create a System.Windows.Controls.Image control
            var img = new Image();
            img.Source = bitmap;

            if (img.Source == null)
                placeholder.Visibility = Visibility.Visible;
            else
            {
                albumArt.Source = img.Source;
                placeholder.Visibility = Visibility.Hidden;
            }
        }

        private void FlyoutHandler(Grid sender, string header)
        {
            Flyout.IsOpen = true;
            sender.Visibility = Visibility.Visible;
            Flyout.Header = header;
        }

        private void PlayerSettings_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutHandler(settingsGrid, "Settings");
        }

        private void btnClear1_OnClick(object sender, RoutedEventArgs e)
        {
            directory1.Text = "";
            Cfg.SetVariable("Music.Directory1", "", ref Cfg.configFile);
            Cfg.SaveConfigFile("music_prefs.cfg", Cfg.configFile);
            Load();
        }

        private void Directory1_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Load();
        }

        private void btnChange_OnClick(object sender, RoutedEventArgs e)
        {
            var objname = ((Button) sender).Name;
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                Cfg.SetVariable("Music." + objname, Convert.ToString(dialog.FileName), ref Cfg.configFile);
                Cfg.SaveConfigFile("music_prefs.cfg", Cfg.configFile);
                directory1.Text = Cfg.configFile["Music.Directory1"];
                /*
                directory2.Text = Cfg.configFile["Music.Directory2"];
                directory3.Text = Cfg.configFile["Music.Directory3"];
                directory4.Text = Cfg.configFile["Music.Directory4"];
                */
            }
        }

        private void PlayPause_OnClick(object sender, RoutedEventArgs e)
        {
            if (mplayer.Source == null && shuffleSongs == false)
            {
                songDataGrid.SelectedIndex = 0;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

                audioPlaying = true;
                GetAlbumart();
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Content = selectedSong.Album;
                nowPlayingTrack.Content = selectedSong.Track;
            }
            else if (mplayer.Source == null && shuffleSongs)
            {
                songDataGrid.SelectedIndex = rnd.Next(0, songDataGrid.Items.Count);
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

                audioPlaying = true;
                GetAlbumart();
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Content = selectedSong.Album;
                nowPlayingTrack.Content = selectedSong.Track;
            }
            else if (audioPlaying)
            {
                mplayer.Pause();
                playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Play")};
                audioPlaying = false;
            }
            else
            {
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};
                audioPlaying = true;
            }
        }

        private void NextSong_OnClick(object sender, MouseButtonEventArgs e)
        {
            if (shuffleSongs)
                songDataGrid.SelectedIndex = rnd.Next(0, songDataGrid.Items.Count);
            else
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex + 1;

            var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

            mplayer.Open(new Uri(selectedSong.FileName));
            mplayer.Play();
            GetAlbumart();
            playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

            audioPlaying = true;
            nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
            nowPlayingAlbum.Content = selectedSong.Album;
            nowPlayingTrack.Content = selectedSong.Track;
        }

        private void PreviousSong_OnClick(object sender, MouseButtonEventArgs e)
        {
            if (songDataGrid.SelectedIndex != 0)
            {
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex - 1;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                GetAlbumart();
                playPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

                audioPlaying = true;
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Content = selectedSong.Album;
                nowPlayingTrack.Content = selectedSong.Track;
            }
        }

        private void scrubBar_OnValueChanged(object sender, MouseButtonEventArgs e)
        {
            if (mplayer.HasAudio)
            {
                dragStarted = true;
                mplayer.Position = TimeSpan.FromSeconds(scrubBar.Value);

                mplayer.IsMuted = false;
            }
            dragStarted = false;
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            mplayer.IsMuted = true;
            dragStarted = true;
        }

        private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mplayer.Volume = volumeSlider.Value;
            if (volumeSlider.Value > 0.6)
            {
                mute.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_2")};
                mute.Width = 20;
            }
            else if (volumeSlider.Value > 0.2)
            {
                mute.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_1")};
                mute.Width = 15;
            }
            else
            {
                mute.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
                mute.Width = 10;
            }
        }

        private void Mute_OnClick(object sender, MouseButtonEventArgs e)
        {
            if (audiouMuted)
            {
                mute.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_2")};
                mute.Width = 20;
                mplayer.IsMuted = false;
                audiouMuted = false;
            }
            else
            {
                mute.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
                mute.Width = 10;
                mplayer.IsMuted = true;
                audiouMuted = true;
            }
        }

        private void Shuffle_OnClick(object sender, MouseButtonEventArgs e)
        {
            if (shuffleSongs)
            {
                shuffleSongs = false;
                shuffle.Fill = Brushes.White;
            }
            else
            {
                shuffleSongs = true;
                shuffle.Fill = (Brush) FindResource("AccentColorBrush");
            }
        }

        
    }
}