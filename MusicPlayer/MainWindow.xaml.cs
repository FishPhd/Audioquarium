﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using File = TagLib.File;
using Microsoft.WindowsAPICodePack.Dialogs;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Native;

namespace MusicPlayer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly MediaPlayer mplayer = new MediaPlayer();
        private readonly Random rnd = new Random();
        private bool audioPlaying;
        private bool audiouMuted;
        private bool dragStarted;
        private bool shuffleSongs;
        private readonly KeyboardHookListener m_KeyboardHookManager;
        private bool repeatSong;

        public MainWindow()
        {
            InitializeComponent();
            Cfg.Initial(false);
            Load();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += timer_Tick;
            timer.Start();

            m_KeyboardHookManager = new KeyboardHookListener(new GlobalHooker());
            m_KeyboardHookManager.Enabled = true;
            m_KeyboardHookManager.KeyDown += HookManager_KeyDown;
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
                    if (selectedSong != null)
                    {
                        scrubTime.Content = mplayer.Position.ToString(@"mm\:ss") + " - " + selectedSong.Length;
                    }
                }
            }

            if (mplayer.Source != null && mplayer.NaturalDuration.HasTimeSpan && Convert.ToInt32(mplayer.Position.TotalSeconds) 
                == Convert.ToInt32(mplayer.NaturalDuration.TimeSpan.TotalSeconds))
            {
                if (shuffleSongs)
                    songDataGrid.SelectedIndex = rnd.Next(0, songDataGrid.Items.Count);
                else if (repeatSong)
                    songDataGrid.SelectedIndex = songDataGrid.SelectedIndex;
                else
                {
                    Next();
                }
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                GetAlbumart();

                audioPlaying = true;
                if (selectedSong.Name == null)
                    nowPlayingSong.Content = selectedSong.AltName + " - " + selectedSong.Artist;
                else
                    nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;

                nowPlayingAlbum.Text = selectedSong.Album;
                nowPlayingTrack.Text = selectedSong.Track;
            }
        }

        private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyData.ToString() == Key.MediaPlayPause.ToString())
                Play();
            else if (e.KeyData.ToString() == Key.MediaNextTrack.ToString())
                Next();
            else if (e.KeyData.ToString() == Key.MediaPreviousTrack.ToString())
                Previous();
            else if (e.KeyData.ToString() == Key.VolumeMute.ToString())
                Mute();
        }

        public void Load()
        {
            if (Cfg.configFile["Music.Directory1"] != "")
            {
                Itemsource.LoadSongs(Cfg.configFile["Music.Directory1"]);
                songDataGrid.ItemsSource = Itemsource.info;
            }
            else
            {
                songDataGrid.ItemsSource = null;
            }

            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Cfg.configFile["Player.Color"]), ThemeManager.GetAppTheme("BaseDark"));
            Colors.SelectedValue = Cfg.configFile["Player.Color"];
            directory1.Text = Cfg.configFile["Music.Directory1"];
        }

        private void SongGrid_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedSong = songDataGrid.CurrentItem as Itemsource.Songs;

            if (songDataGrid.ItemsSource != null && selectedSong != null)
            {
                mplayer.Close();

                mplayer.Open(new Uri(selectedSong.FileName));
                Console.WriteLine(selectedSong.FileName);
                mplayer.Play();
                audioPlaying = true;
                GetAlbumart();

                if (selectedSong.Name == null)
                    nowPlayingSong.Content = selectedSong.AltName + " - " + selectedSong.Artist;
                else
                    nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;

                nowPlayingAlbum.Text = selectedSong.Album;
                nowPlayingTrack.Text = selectedSong.Track;
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };
            }
        }

        private void GetAlbumart()
        {
            try
            {
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;
                var tagFile = File.Create(selectedSong.FileName);

                var pic = tagFile.Tag.Pictures[0];
                var ms = new MemoryStream(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                var img = new Image();
                img.Source = bitmap;
                albumArt.Source = img.Source;
                placeholder.Visibility = Visibility.Hidden;
            }
            catch
            {
                albumArt.Source = null;
                placeholder.Visibility = Visibility.Visible;
            }
        }

        private void FlyoutHandler(Grid sender, string header)
        {
            Flyout.IsOpen = true;
            sender.Visibility = Visibility.Visible;
            Flyout.Header = header;
        }

        #region Settings


        private void Directory1_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Load();
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

        private void Color_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Colors.SelectedValue.ToString()), ThemeManager.GetAppTheme("BaseDark"));
            Cfg.configFile["Player.Color"] = Colors.SelectedValue.ToString();
            Cfg.SaveConfigFile("music_prefs.cfg", Cfg.configFile);
        }

        #endregion

        #region Controls

        private void PlayPause_OnClick(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void NextSong_OnClick(object sender, MouseButtonEventArgs e)
        {
            Next();
        }

        private void PreviousSong_OnClick(object sender, MouseButtonEventArgs e)
        {
            Previous();
        }

        private void Mute_OnClick(object sender, MouseButtonEventArgs e)
        {
            Mute();
        }

        private void btnChange_OnClick(object sender, RoutedEventArgs e)
        {
            var objname = ((System.Windows.Controls.Button)sender).Name;
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                Cfg.SetVariable("Music." + objname, Convert.ToString(dialog.FileName), ref Cfg.configFile);
                Cfg.SaveConfigFile("music_prefs.cfg", Cfg.configFile);
                directory1.Text = Cfg.configFile["Music.Directory1"];
            }
        }

        private void SongSorting_OnClick(object sender, RoutedEventArgs e)
        {
            if (songSorting.Background != Brushes.LightGray)
            {
                songDataGrid.ItemsSource = Itemsource.info;
                //Sort("Name");
                //songSorting.Background = Brushes.LightGray;
                songSortingIcon.Fill = (Brush)FindResource("AccentColorBrush");
                songSortingLabel.Foreground = (Brush)FindResource("AccentColorBrush");

                albumSorting.ClearValue(BackgroundProperty);
                albumSortingIcon.Fill = Brushes.LightGray;
                albumSortingLabel.Foreground = Brushes.LightGray;
                artistSorting.ClearValue(BackgroundProperty);
                artistSortingIcon.Fill = Brushes.LightGray;
                artistSortingLabel.Foreground = Brushes.LightGray;

                artistsSelector.Visibility = Visibility.Hidden;
                albumsSelector.Visibility = Visibility.Hidden;
                songsSelector.Visibility = Visibility.Visible;

                songDataGrid.Visibility = Visibility.Visible;
                scrollViewer.Visibility = Visibility.Hidden;
            }
        }

        private void AlbumSorting_OnClick(object sender, RoutedEventArgs e)
        {
            if (albumSorting.Background != Brushes.LightGray)
            {
                Sort("Album");
                GrabAlbums();

                //albumSorting.Background = Brushes.LightGray;
                albumSortingIcon.Fill = (Brush)FindResource("AccentColorBrush");
                albumSortingLabel.Foreground = (Brush)FindResource("AccentColorBrush");

                songSorting.ClearValue(BackgroundProperty);
                songSortingIcon.Fill = Brushes.LightGray;
                songSortingLabel.Foreground = Brushes.LightGray;
                artistSorting.ClearValue(BackgroundProperty);
                artistSortingIcon.Fill = Brushes.LightGray;
                artistSortingLabel.Foreground = Brushes.LightGray;

                artistsSelector.Visibility = Visibility.Hidden;
                albumsSelector.Visibility = Visibility.Visible;
                songsSelector.Visibility = Visibility.Hidden;

                songDataGrid.Visibility = Visibility.Hidden;
                scrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void ArtistSorting_OnClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Not implemented");
            if (artistSorting.Background != Brushes.LightGray)
            {
                Sort("Artist");
                GrabArtists();

                //albumSorting.Background = Brushes.LightGray;
                artistSortingIcon.Fill = (Brush)FindResource("AccentColorBrush");
                artistSortingLabel.Foreground = (Brush)FindResource("AccentColorBrush");

                songSorting.ClearValue(BackgroundProperty);
                songSortingIcon.Fill = Brushes.LightGray;
                songSortingLabel.Foreground = Brushes.LightGray;
                albumSorting.ClearValue(BackgroundProperty);
                albumSortingIcon.Fill = Brushes.LightGray;
                albumSortingLabel.Foreground = Brushes.LightGray;

                artistsSelector.Visibility = Visibility.Visible;
                albumsSelector.Visibility = Visibility.Hidden;
                songsSelector.Visibility = Visibility.Hidden;

                songDataGrid.Visibility = Visibility.Hidden;
                scrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void Sort(string col)
        {
            if (songDataGrid.ItemsSource != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(songDataGrid.ItemsSource);
                dataView.SortDescriptions.Clear();
                dataView.SortDescriptions.Add(new SortDescription(col, ListSortDirection.Ascending));
                dataView.Refresh();
            }
        }

        private void Next()
        {
            if (shuffleSongs)
                songDataGrid.SelectedIndex = rnd.Next(0, songDataGrid.Items.Count);
            else
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex + 1;

            var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

            mplayer.Open(new Uri(selectedSong.FileName));
            mplayer.Play();
            GetAlbumart();
            playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };

            audioPlaying = true;
            if (selectedSong.Name == null)
                nowPlayingSong.Content = selectedSong.AltName + " - " + selectedSong.Artist;
            else
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
            nowPlayingAlbum.Text = selectedSong.Album;
            nowPlayingTrack.Text = selectedSong.Track;
        }

        private void GrabAlbums()
        {
            wrapPanel.Children.Clear();
            var song = Itemsource.info;
            var noduplicates = song.GroupBy(x => x.Album).Select(x => x.First()).ToList();

            song.GroupBy(x => x.Album).Select(x => x.First()).Distinct();

            foreach (var item in noduplicates)
            {
                var tagFile = File.Create(item.FileName);

                Tile newTile = new Tile();
                
                newTile.VerticalContentAlignment = VerticalAlignment.Bottom;
                newTile.Width = 175;
                newTile.Height = 175;
                newTile.Margin = new Thickness(5);
                newTile.FontSize = 14;
                newTile.Title = item.Album;
                newTile.Foreground = Brushes.Transparent;
                //newTile.Foreground = (Brush)FindResource("AccentColorBrush");
                newTile.Click += new RoutedEventHandler(Album_OnClick);

                try
                {
                    var pic = tagFile.Tag.Pictures[0];
                    var ms = new MemoryStream(pic.Data.Data);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();

                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = bitmap;
                    newTile.Background = ib;
                }
                catch
                {
                    newTile.Foreground = Brushes.White;
                    newTile.Background = (Brush)FindResource("AccentColorBrush2");
                }

                wrapPanel.Children.Add(newTile);
                Console.WriteLine(item.Album);
            }
        }

        private void GrabArtists()
        {
            bool isGray = false;
            wrapPanel.Children.Clear();
            var song = Itemsource.info;
            var noduplicates = song.GroupBy(x => x.Artist).Select(x => x.First()).ToList();

            song.GroupBy(x => x.Artist).Select(x => x.First()).Distinct();

            

            foreach (var item in noduplicates)
            {
                Tile newTile = new Tile();

                newTile.Width = 600;
                newTile.Height = 50;
                newTile.Margin = new Thickness(5);
                newTile.FontSize = 14;
                newTile.Title = item.Artist;
                newTile.Foreground = Brushes.Transparent;
                //newTile.Foreground = (Brush)FindResource("AccentColorBrush");
                newTile.Click += new RoutedEventHandler(Artist_OnClick);

                newTile.Foreground = Brushes.White;
                if (!isGray)
                {
                    newTile.Background = (Brush)FindResource("AccentColorBrush2");
                    isGray = true;
                }
                else
                {
                    newTile.Background = (Brush)FindResource("AccentColorBrush3");
                    isGray = false;
                }

                wrapPanel.Children.Add(newTile);
                Console.WriteLine(item.Artist);
            }
        }

        private void Album_OnClick(object sender, RoutedEventArgs e)
        {
            List<Itemsource.Songs> album = Itemsource.info.ToList();

            string content = (sender as Tile).Title;
            album = album.Where(x => x.Album == content).ToList();
            songDataGrid.ItemsSource = album;

            if (!mplayer.HasAudio)
            {
                songDataGrid.SelectedIndex = 0;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;
                var tagFile = File.Create(selectedSong.FileName);

                try
                {
                    var pic = tagFile.Tag.Pictures[0];
                    var ms = new MemoryStream(pic.Data.Data);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();

                    var img = new Image();
                    img.Source = bitmap;
                    albumArt.Source = img.Source;
                    placeholder.Visibility = Visibility.Hidden;
                }
                catch
                {
                    placeholder.Visibility = Visibility.Visible;
                }
                nowPlayingAlbum.Text = content;
                nowPlayingTrack.Text = selectedSong.Track;
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
            }
            scrollViewer.Visibility = Visibility.Hidden;
            songDataGrid.Visibility = Visibility.Visible;
        }

        private void Artist_OnClick(object sender, RoutedEventArgs e)
        {
            List<Itemsource.Songs> artist = Itemsource.info.ToList();

            string content = (sender as Tile).Title;
            artist = artist.Where(x => x.Artist == content).ToList();
            songDataGrid.ItemsSource = artist;

            if (!mplayer.HasAudio)
            { 
                songDataGrid.SelectedIndex = 0;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;
                var tagFile = File.Create(selectedSong.FileName);

                try
                {
                    var pic = tagFile.Tag.Pictures[0];
                    var ms = new MemoryStream(pic.Data.Data);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();

                    var img = new Image();
                    img.Source = bitmap;
                    albumArt.Source = img.Source;
                    placeholder.Visibility = Visibility.Hidden;
                }
                catch
                {
                    placeholder.Visibility = Visibility.Visible;
                }
                nowPlayingAlbum.Text = content;
                nowPlayingTrack.Text = selectedSong.Track;
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
            }
            
            scrollViewer.Visibility = Visibility.Hidden;
            songDataGrid.Visibility = Visibility.Visible;
        }

        private void Play()
        {
            if (mplayer.Source == null && shuffleSongs == false)
            {
                songDataGrid.SelectedIndex = 0;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };

                audioPlaying = true;
                GetAlbumart();
                if (selectedSong.Name == null)
                    nowPlayingSong.Content = selectedSong.AltName + " - " + selectedSong.Artist;
                else
                    nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Text = selectedSong.Album;
                nowPlayingTrack.Text = selectedSong.Track;
            }
            else if (mplayer.Source == null && shuffleSongs)
            {
                songDataGrid.SelectedIndex = rnd.Next(0, songDataGrid.Items.Count);
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };

                audioPlaying = true;
                GetAlbumart();
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Text = selectedSong.Album;
                nowPlayingTrack.Text = selectedSong.Track;
            }
            else if (audioPlaying)
            {
                mplayer.Pause();
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Play") };
                audioPlaying = false;
            }
            else
            {
                mplayer.Play();
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };
                audioPlaying = true;
            }
        }

        private void Previous()
        {
            if (songDataGrid.SelectedIndex != 0)
            {
                songDataGrid.SelectedIndex = songDataGrid.SelectedIndex - 1;
                var selectedSong = songDataGrid.SelectedItem as Itemsource.Songs;

                mplayer.Open(new Uri(selectedSong.FileName));
                mplayer.Play();
                GetAlbumart();
                playPause.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("Pause") };

                audioPlaying = true;
                nowPlayingSong.Content = selectedSong.Name + " - " + selectedSong.Artist;
                nowPlayingAlbum.Text = selectedSong.Album;
                nowPlayingTrack.Text = selectedSong.Track;
            }
        }

        private void Mute()
        {
            if (audiouMuted)
            {
                mute.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("appbar_sound_2") };
                mute.Width = 20;
                mplayer.IsMuted = false;
                audiouMuted = false;
            }
            else
            {
                mute.OpacityMask = new VisualBrush { Visual = (Visual)FindResource("appbar_sound_0") };
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
                shuffle.Fill = (Brush)FindResource("AccentColorBrush");
            }
        }

        private void Repeat_OnClick(object sender, MouseButtonEventArgs e)
        {
            if (repeatSong)
            {
                repeatSong = false;
                repeat.Fill = Brushes.White;
            }
            else
            {
                repeatSong = true;
                repeat.Fill = (Brush)FindResource("AccentColorBrush");
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

        #endregion

        
    }
}