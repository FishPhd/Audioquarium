using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using File = TagLib.File;

namespace Audioquarium
{
  public partial class MainWindow
  {
    public static readonly MediaPlayer Mplayer = new MediaPlayer();
    private readonly KeyboardHookListener m_keyboardListener;
    private readonly Random _rnd = new Random();
    private bool _audioPlaying;
    private bool _audiouMuted;
    private bool _dragStarted;
    private bool _shuffleSongs;
    private bool _repeatSong;
    private Itemsource.Songs _selectedSong;
    private int _playerSize = 2; // Guppy(0) Minnow(1) Shark(2) Whale(3)
    private int _currentView; // Song(0) Album(1) Artist(2) 

    public MainWindow()
    {
      InitializeComponent();
      Cfg.Initial(false);
      Load();

      m_keyboardListener = new KeyboardHookListener(new GlobalHooker()) { Enabled = true };
      m_keyboardListener.KeyDown += HookManager_KeyDown;

      var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(0.5)};
      timer.Tick += timer_Tick;
      timer.Start();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      if (Mplayer.Source != null && Mplayer.NaturalDuration.HasTimeSpan)
      {
        SongDataGrid.SelectedIndex = SongDataGrid.SelectedIndex;
        //_selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

        ScrubBar.Minimum = 0;
        ScrubBar.Maximum = Mplayer.NaturalDuration.TimeSpan.TotalSeconds;

        if (!_dragStarted)
        {
          ScrubBar.Value = Mplayer.Position.TotalSeconds;
          if (_selectedSong != null && Mplayer.HasAudio)
          {
            ScrubTime.Content = Mplayer.Position.ToString(@"mm\:ss") + " - " + _selectedSong.Length;
          }
        }
      }

      if (Mplayer.Source != null && Mplayer.NaturalDuration.HasTimeSpan
          && Mplayer.Position.TotalSeconds == Mplayer.NaturalDuration.TimeSpan.TotalSeconds)
      {
        if (_shuffleSongs)
          SongDataGrid.SelectedIndex = _rnd.Next(0, SongDataGrid.Items.Count);
        else if (_repeatSong)
          SongDataGrid.SelectedIndex = SongDataGrid.SelectedIndex;
        else if (_selectedSong == SongDataGrid.SelectedItem as Itemsource.Songs)
          Next(); // If the current song is the same as the one playing then go to next

        _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

        if (_selectedSong != null)
        {
          Mplayer.Open(new Uri(_selectedSong.FileName));
          Mplayer.Play();
          GetAlbumart();

          _audioPlaying = true;
          if (_selectedSong.Name == null)
            NowPlayingSong.Content = _selectedSong.AltName + " - " + _selectedSong.Artist;
          else
            NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;

          NowPlayingAlbum.Text = _selectedSong.Album;
          NowPlayingTrack.Text = _selectedSong.Track;
        }
      }
    }

    private void HookManager_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
      if (e.KeyData.ToString() == Key.MediaPlayPause.ToString() || e.KeyData.ToString() == Key.Play.ToString())
        Play();
      else if (e.KeyData.ToString() == Key.MediaNextTrack.ToString() || e.KeyData.ToString() == Key.Next.ToString())
        Next();
      else if (e.KeyData.ToString() == Key.MediaPreviousTrack.ToString())
        Previous();
      else if (e.KeyData.ToString() == Key.VolumeMute.ToString())
        Mute();
    }

    public void Load()
    {
      if (Cfg.ConfigFile["Music.Directory1"] != "")
      {
        Itemsource.LoadSongs(Cfg.ConfigFile["Music.Directory1"]);
        SongDataGrid.ItemsSource = Itemsource.SongLibrary;
      }
      else
        SongDataGrid.ItemsSource = null;
      ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Cfg.ConfigFile["Player.Color"]),
        ThemeManager.GetAppTheme("BaseDark"));
      Colors.SelectedValue = Cfg.ConfigFile["Player.Color"];
      Directory1Text.Text = Cfg.ConfigFile["Music.Directory1"];
    }

    private void SongGrid_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _selectedSong = SongDataGrid.CurrentItem as Itemsource.Songs;

      if (SongDataGrid.ItemsSource != null && _selectedSong != null) //&& selectedSong.FileName != _currentSong
      {
        Mplayer.Open(new Uri(_selectedSong.FileName));
        //Console.WriteLine(selectedSong.FileName);
        Mplayer.Play();
        _audioPlaying = true;
        GetAlbumart();

        if (_selectedSong.Name == null)
          NowPlayingSong.Content = _selectedSong.AltName + " - " + _selectedSong.Artist;
        else
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;

        NowPlayingAlbum.Text = _selectedSong.Album;
        NowPlayingTrack.Text = _selectedSong.Track;
        PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};
      }
    }

    private void GetAlbumart()
    {
      var tagFile = File.Create(_selectedSong.FileName);
      // = SongDataGrid.SelectedItem as Itemsource.Songs;
      if (_selectedSong != null)
      {
        try
        {
          var pic = tagFile.Tag.Pictures[0];
          var ms = new MemoryStream(pic.Data.Data);
          ms.Seek(0, SeekOrigin.Begin);

          var bitmap = new BitmapImage();
          bitmap.BeginInit();
          bitmap.StreamSource = ms;
          bitmap.EndInit();

          var img = new Image {Source = bitmap};
          AlbumArt.Source = img.Source;
          AlbumArt.ToolTip = tagFile.Tag.Album;
          Placeholder.Visibility = Visibility.Hidden;
        }
        catch
        {
          AlbumArt.Source = null;
          AlbumArtToolTip.ToolTip = tagFile.Tag.Album;
          Placeholder.Visibility = Visibility.Visible;
        }
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
      if (!IsLoaded || Directory1Text.Text == "")
        return;
      Load();
    }

    private void PlayerSettings_OnClick(object sender, RoutedEventArgs e)
    {
      FlyoutHandler(SettingsGrid, "Settings");
    }

    private void btnClear1_OnClick(object sender, RoutedEventArgs e)
    {
      Directory1Text.Text = "";
      Cfg.SetVariable("Music.Directory1", "", ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("music_prefs.cfg", Cfg.ConfigFile);
      Load();
    }

    private void Color_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!IsLoaded)
        return;
      ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Colors.SelectedValue.ToString()),
        ThemeManager.GetAppTheme("BaseDark"));

      if (_currentView == 0)
      {
        SongSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        SongSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");
      }
      if (_currentView == 1)
      {
        AlbumSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        AlbumSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");
      }
      else if (_currentView == 2)
      {
        ArtistSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        ArtistSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");
      }

      Cfg.ConfigFile["Player.Color"] = Colors.SelectedValue.ToString();
      Cfg.SaveConfigFile("music_prefs.cfg", Cfg.ConfigFile);
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
      var objname = ((Button) sender).Name;
      var dialog = new CommonOpenFileDialog {IsFolderPicker = true};
      CommonFileDialogResult result = dialog.ShowDialog();

      if (result == CommonFileDialogResult.Ok)
      {
        Cfg.SetVariable("Music." + objname, Convert.ToString(dialog.FileName), ref Cfg.ConfigFile);
        Cfg.SaveConfigFile("music_prefs.cfg", Cfg.ConfigFile);
        Directory1Text.Text = Cfg.ConfigFile["Music.Directory1"];
        Load();
      }
    }

    private void SongSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (!Equals(SongSorting.Background, Brushes.LightGray))
      {
        _currentView = 0; // Set our view to songs grid
        SongDataGrid.ItemsSource = Itemsource.SongLibrary;
        //Sort("Name");
        //songSorting.Background = Brushes.LightGray;
        SongSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        SongSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");

        AlbumSorting.ClearValue(BackgroundProperty);
        AlbumSortingIcon.Fill = Brushes.LightGray;
        AlbumSortingLabel.Foreground = Brushes.LightGray;
        ArtistSorting.ClearValue(BackgroundProperty);
        ArtistSortingIcon.Fill = Brushes.LightGray;
        ArtistSortingLabel.Foreground = Brushes.LightGray;

        ArtistsSelector.Visibility = Visibility.Hidden;
        AlbumsSelector.Visibility = Visibility.Hidden;
        SongsSelector.Visibility = Visibility.Visible;

        SongDataGrid.Visibility = Visibility.Visible;
        ScrollViewer.Visibility = Visibility.Hidden;
      }
    }

    private void AlbumSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (!Equals(AlbumSorting.Background, Brushes.LightGray))
      {
        GrabAlbums();

        Sort("Album", SongDataGrid);
        _currentView = 1; // Set our view to album grid

        //albumSorting.Background = Brushes.LightGray;
        AlbumSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        AlbumSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");

        SongSorting.ClearValue(BackgroundProperty);
        SongSortingIcon.Fill = Brushes.LightGray;
        SongSortingLabel.Foreground = Brushes.LightGray;
        ArtistSorting.ClearValue(BackgroundProperty);
        ArtistSortingIcon.Fill = Brushes.LightGray;
        ArtistSortingLabel.Foreground = Brushes.LightGray;

        ArtistsSelector.Visibility = Visibility.Hidden;
        AlbumsSelector.Visibility = Visibility.Visible;
        SongsSelector.Visibility = Visibility.Hidden;

        SongDataGrid.Visibility = Visibility.Hidden;
        ScrollViewer.Visibility = Visibility.Visible;
      }
    }

    private void ArtistSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (!Equals(ArtistSorting.Background, Brushes.LightGray))
      {
        _currentView = 2; // Set our view to artist grid
        Sort("Artist", SongDataGrid);
        GrabArtists();

        //albumSorting.Background = Brushes.LightGray;
        ArtistSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
        ArtistSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");

        SongSorting.ClearValue(BackgroundProperty);
        SongSortingIcon.Fill = Brushes.LightGray;
        SongSortingLabel.Foreground = Brushes.LightGray;
        AlbumSorting.ClearValue(BackgroundProperty);
        AlbumSortingIcon.Fill = Brushes.LightGray;
        AlbumSortingLabel.Foreground = Brushes.LightGray;

        ArtistsSelector.Visibility = Visibility.Visible;
        AlbumsSelector.Visibility = Visibility.Hidden;
        SongsSelector.Visibility = Visibility.Hidden;

        SongDataGrid.Visibility = Visibility.Hidden;
        ScrollViewer.Visibility = Visibility.Visible;
      }
    }

    private void Sort(string col, DataGrid grid)
    {
      if (grid.ItemsSource != null)
      {
        ICollectionView dataView = CollectionViewSource.GetDefaultView(SongDataGrid.ItemsSource);
        dataView.SortDescriptions.Clear();
        dataView.SortDescriptions.Add(new SortDescription(col, ListSortDirection.Ascending));
        dataView.Refresh();
      }
    }

    private void Next()
    {
      if (_shuffleSongs)
        SongDataGrid.SelectedIndex = _rnd.Next(0, SongDataGrid.Items.Count);
      else
        SongDataGrid.SelectedIndex = SongDataGrid.SelectedIndex + 1;

      _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

      if (_selectedSong != null)
      {
        Mplayer.Open(new Uri(_selectedSong.FileName));
        Mplayer.Play();
        GetAlbumart();
        PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

        _audioPlaying = true;
        if (_selectedSong.Name == null)
          NowPlayingSong.Content = _selectedSong.AltName + " - " + _selectedSong.Artist;
        else
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
        NowPlayingAlbum.Text = _selectedSong.Album;
        NowPlayingTrack.Text = _selectedSong.Track;
      }
    }

    private void GrabAlbums()
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();

      WrapPanel.Children.Clear();
      var song = Itemsource.SongLibrary;
      var noduplicates = song.GroupBy(x => x.Album).Select(x => x.First()).ToList();

      foreach (var item in noduplicates)
      {
        var tagFile = File.Create(item.FileName);

        Tile newTile = new Tile
        {
          VerticalContentAlignment = VerticalAlignment.Bottom,
          Width = 175,
          ToolTip = item.Album + " - " + item.Artist,
          Height = 175,
          Margin = new Thickness(5),
          FontSize = 14,
          Title = item.Album,
          Foreground = Brushes.Transparent
        };

        //newTile.Foreground = (Brush)FindResource("AccentColorBrush");
        newTile.Click += Album_OnClick;

        try
        {
          var pic = tagFile.Tag.Pictures[0];
          var ms = new MemoryStream(pic.Data.Data);
          ms.Seek(0, SeekOrigin.Begin);

          var bitmap = new BitmapImage();
          bitmap.BeginInit();
          bitmap.StreamSource = ms;
          bitmap.EndInit();

          newTile.Background = new ImageBrush {ImageSource = bitmap};
        }
        catch
        {
          newTile.Foreground = Brushes.White;
          newTile.Background = (Brush) FindResource("AccentColorBrush2");
        }

        WrapPanel.Children.Add(newTile);
      }
      watch.Stop();
      Console.WriteLine(@"Album art loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
    }

    private void GrabArtists()
    {
      bool isGray = false;
      WrapPanel.Children.Clear();
      var songs = Itemsource.SongLibrary.GroupBy(x => x.Artist).Select(x => x.First()).ToList();

      foreach (var song in songs)
      {
        Tile newTile = new Tile
        {
          Width = 600,
          Height = 50,
          Margin = new Thickness(5),
          FontSize = 14,
          Title = song.Artist,
          Foreground = Brushes.Transparent
        };

        //newTile.Foreground = (Brush)FindResource("AccentColorBrush");
        newTile.Click += Artist_OnClick;

        newTile.Foreground = Brushes.White;
        if (!isGray)
        {
          newTile.Background = (Brush) FindResource("AccentColorBrush2");
          isGray = true;
        }
        else
        {
          newTile.Background = (Brush) FindResource("AccentColorBrush3");
          isGray = false;
        }

        WrapPanel.Children.Add(newTile);
        //Console.WriteLine(title);
      }
    }

    private void Album_OnClick(object sender, RoutedEventArgs e)
    {
      //_selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;
      List<Itemsource.Songs> album = Itemsource.SongLibrary.ToList();

      var tile = sender as Tile;
      if (tile != null)
      {
        string content = tile.Title;
        if (_selectedSong != null)
        {
          album = album.Where(x => x.Album == content).ToList();
          SongDataGrid.ItemsSource = album;
          var tagFile = File.Create(_selectedSong.FileName);
          SongDataGrid.SelectedIndex = 0;
          try
          {
            var pic = tagFile.Tag.Pictures[0];
            var ms = new MemoryStream(pic.Data.Data);
            ms.Seek(0, SeekOrigin.Begin);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            var img = new Image {Source = bitmap};
            AlbumArt.Source = img.Source;
            Placeholder.Visibility = Visibility.Hidden;
          }
          catch
          {
            Placeholder.Visibility = Visibility.Visible;
          }
        }
        if (_selectedSong != null && !Mplayer.HasAudio)
        {
          NowPlayingAlbum.Text = content;
          NowPlayingTrack.Text = _selectedSong.Track;
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
        }
      }
      ScrollViewer.Visibility = Visibility.Hidden;
      SongDataGrid.Visibility = Visibility.Visible;
    }

    private void AlbumArt_MouseDown(object sender, MouseButtonEventArgs e)
    {
      List<Itemsource.Songs> album = Itemsource.SongLibrary.ToList();

      if (_selectedSong != null)
      {
        string content = _selectedSong.Album;
        album = album.Where(x => x.Album == content).ToList();
      }
      SongDataGrid.ItemsSource = album;
      _currentView = 1; // Set our view to songs grid

      //albumSorting.Background = Brushes.LightGray;
      AlbumSortingIcon.Fill = (Brush) FindResource("AccentColorBrush");
      AlbumSortingLabel.Foreground = (Brush) FindResource("AccentColorBrush");

      SongSorting.ClearValue(BackgroundProperty);
      SongSortingIcon.Fill = Brushes.LightGray;
      SongSortingLabel.Foreground = Brushes.LightGray;
      ArtistSorting.ClearValue(BackgroundProperty);
      ArtistSortingIcon.Fill = Brushes.LightGray;
      ArtistSortingLabel.Foreground = Brushes.LightGray;

      ArtistsSelector.Visibility = Visibility.Hidden;
      AlbumsSelector.Visibility = Visibility.Visible;
      SongsSelector.Visibility = Visibility.Hidden;

      SongDataGrid.Visibility = Visibility.Visible;
      ScrollViewer.Visibility = Visibility.Hidden;
    }

    private void Artist_OnClick(object sender, RoutedEventArgs e)
    {
      List<Itemsource.Songs> artist = Itemsource.SongLibrary.ToList();

      var tile = sender as Tile;
      if (tile != null)
      {
        string content = tile.Title;
        artist = artist.Where(x => x.Artist == content).ToList();
        SongDataGrid.ItemsSource = artist;

        if (!Mplayer.HasAudio)
        {
          SongDataGrid.SelectedIndex = 0;
          _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;
          if (_selectedSong != null)
          {
            var tagFile = File.Create(_selectedSong.FileName);

            try
            {
              var pic = tagFile.Tag.Pictures[0];
              var ms = new MemoryStream(pic.Data.Data);
              ms.Seek(0, SeekOrigin.Begin);

              var bitmap = new BitmapImage();
              bitmap.BeginInit();
              bitmap.StreamSource = ms;
              bitmap.EndInit();

              var img = new Image {Source = bitmap};
              AlbumArt.Source = img.Source;
              Placeholder.Visibility = Visibility.Hidden;
            }
            catch
            {
              Placeholder.Visibility = Visibility.Visible;
            }
          }
          NowPlayingAlbum.Text = content;
          if (_selectedSong != null)
          {
            NowPlayingTrack.Text = _selectedSong.Track;
            NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
          }
        }
      }

      ScrollViewer.Visibility = Visibility.Hidden;
      SongDataGrid.Visibility = Visibility.Visible;
    }

    private void Play()
    {
      if (Mplayer.Source == null && _shuffleSongs == false)
      {
        SongDataGrid.SelectedIndex = 0;
        _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

        if (_selectedSong != null)
        {
          Mplayer.Open(new Uri(_selectedSong.FileName));
          Mplayer.Play();
          PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

          _audioPlaying = true;
          GetAlbumart();
          if (_selectedSong.Name == null)
            NowPlayingSong.Content = _selectedSong.AltName + " - " + _selectedSong.Artist;
          else
            NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
          NowPlayingAlbum.Text = _selectedSong.Album;
          NowPlayingTrack.Text = _selectedSong.Track;
        }
      }
      else if (Mplayer.Source == null && _shuffleSongs)
      {
        SongDataGrid.SelectedIndex = _rnd.Next(0, SongDataGrid.Items.Count);
        _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

        if (_selectedSong != null)
        {
          Mplayer.Open(new Uri(_selectedSong.FileName));
          Mplayer.Play();
          PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

          _audioPlaying = true;
          GetAlbumart();
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
          NowPlayingAlbum.Text = _selectedSong.Album;
          NowPlayingTrack.Text = _selectedSong.Track;
        }
      }
      else if (_audioPlaying)
      {
        Mplayer.Pause();
        PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Play")};
        _audioPlaying = false;
      }
      else
      {
        Mplayer.Play();
        PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};
        _audioPlaying = true;
      }
    }

    private void Previous()
    {
      if (SongDataGrid.SelectedIndex != 0)
      {
        if (Mplayer.Position.TotalSeconds > 3)
          SongDataGrid.SelectedIndex = SongDataGrid.SelectedIndex;
        else
          SongDataGrid.SelectedIndex = SongDataGrid.SelectedIndex - 1;

        _selectedSong = SongDataGrid.SelectedItem as Itemsource.Songs;

        if (_selectedSong != null)
        {
          Mplayer.Open(new Uri(_selectedSong.FileName));
          Mplayer.Play();
          GetAlbumart();
          PlayPause.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("Pause")};

          _audioPlaying = true;
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
          NowPlayingAlbum.Text = _selectedSong.Album;
          NowPlayingTrack.Text = _selectedSong.Track;
        }
      }
    }

    private void Mute()
    {
      if (_audiouMuted)
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_2")};
        MuteButton.Width = 20;
        Mplayer.IsMuted = false;
        _audiouMuted = false;
      }
      else
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
        MuteButton.Width = 10;
        Mplayer.IsMuted = true;
        _audiouMuted = true;
      }
    }

    private void Shuffle_OnClick(object sender, MouseButtonEventArgs e)
    {
      if (_shuffleSongs)
      {
        _shuffleSongs = false;
        Shuffle.Fill = Brushes.White;
      }
      else
      {
        _shuffleSongs = true;
        Shuffle.Fill = (Brush) FindResource("AccentColorBrush");
      }
    }

    private void Repeat_OnClick(object sender, MouseButtonEventArgs e)
    {
      if (_repeatSong)
      {
        _repeatSong = false;
        Repeat.Fill = Brushes.White;
      }
      else
      {
        _repeatSong = true;
        Repeat.Fill = (Brush) FindResource("AccentColorBrush");
      }
    }

    private void scrubBar_OnValueChanged(object sender, MouseButtonEventArgs e)
    {
      if (Mplayer.HasAudio)
      {
        _dragStarted = true;
        Mplayer.Position = TimeSpan.FromSeconds(ScrubBar.Value);
        Mplayer.IsMuted = false;
      }
      _dragStarted = false;
    }

    private void Slider_DragStarted(object sender, DragStartedEventArgs e)
    {
      Mplayer.IsMuted = true;
      _dragStarted = true;
    }

    private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      Mplayer.Volume = VolumeSlider.Value;
      if (VolumeSlider.Value > 0.6)
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_2")};
        MuteButton.Width = 20;
      }
      else if (VolumeSlider.Value > 0.2)
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_1")};
        MuteButton.Width = 15;
      }
      else
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
        MuteButton.Width = 10;
      }
    }

    #endregion

    private void ShrinkButton_Click(object sender, RoutedEventArgs e)
    {
      if (_playerSize > 0)
        _playerSize--;

      if (_playerSize == 2) // Whale->Shark
      {
        Application.Current.MainWindow.Height = 549;
        Application.Current.MainWindow.Width = 900;
        Application.Current.MainWindow.WindowState = WindowState.Normal;
        ExpandButton.Visibility = Visibility.Visible;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_shark"),
          Stretch = Stretch.Uniform
        };
      }
      else if (_playerSize == 1) // Shark->Minnow
      {
        Application.Current.MainWindow.Height = 80;
        Application.Current.MainWindow.Width = 400;
        Application.Current.MainWindow.Topmost = true;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_minnow"),
          Stretch = Stretch.Uniform
        };
      }
      else if (_playerSize == 0) // Minnow->Guppy
      {
        Application.Current.MainWindow.Height = 80;
        Application.Current.MainWindow.Width = 190;
        ShrinkButton.Visibility = Visibility.Hidden;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_guppy"),
          Stretch = Stretch.Uniform
        };
      }
    }

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
      if (_playerSize < 3)
        _playerSize++;

      if (_playerSize == 3) // Shark->Whale
      {
        Application.Current.MainWindow.WindowState = WindowState.Maximized;
        ExpandButton.Visibility = Visibility.Hidden;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_whale"),
          Stretch = Stretch.Uniform
        };
      }
      else if (_playerSize == 2) // Minnow->Shark
      {
        Application.Current.MainWindow.Height = 549;
        Application.Current.MainWindow.Width = 900;
        Application.Current.MainWindow.Topmost = false;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_shark"),
          Stretch = Stretch.Uniform
        };
      }
      else if (_playerSize == 1) // Guppy->Minnow
      {
        Application.Current.MainWindow.Height = 80;
        Application.Current.MainWindow.Width = 400;
        ShrinkButton.Visibility = Visibility.Visible;
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_minnow"),
          Stretch = Stretch.Uniform
        };
      }
    }
  }
}