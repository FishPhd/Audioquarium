using System;
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
using System.Windows.Navigation;
using System.Windows.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using File = TagLib.File;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace Audioquarium
{
  public partial class MainWindow
  {
    public static readonly MediaPlayer Mplayer = new MediaPlayer();
    private readonly KeyboardHookListener _mKeyboardListener;
    private readonly Random _rnd = new Random();
    private bool _audioPlaying;
    private bool _audioMuted;
    private int _currentView; // Song(0) Album(1) Artist(2) 
    private bool _dragStarted;
    private int _playerSize = 2; // Guppy(0) Minnow(1) Shark(2) Whale(3)
    private bool _repeatSong;
    private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
    private Itemsource.Songs _selectedSong;
    private bool _shuffleSongs;
    private bool _isWindowActive = true;
    private string _keydata;

    public MainWindow()
    {
      InitializeComponent();
      Cfg.Initial(false);
      Load();

      _mKeyboardListener = new KeyboardHookListener(new GlobalHooker()) {Enabled = true};
      _mKeyboardListener.KeyDown += HookManager_KeyDown;

      _timer.Tick += timer_Tick;
      _timer.Start();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      if (Mplayer.Source != null && Mplayer.NaturalDuration.HasTimeSpan)
      {
        SongGrid.SelectedIndex = SongGrid.SelectedIndex;

        ScrubBar.Minimum = 0;
        ScrubBar.Maximum = Mplayer.NaturalDuration.TimeSpan.TotalSeconds;

        if (!_dragStarted)
        {
          ScrubBar.Value = Mplayer.Position.TotalSeconds;
          if (_selectedSong != null && Mplayer.HasAudio)
          {
            ScrubTime.Content = TimeSpan.FromSeconds(ScrubBar.Value).ToString(@"mm\:ss") + " - " + _selectedSong.Length;
          }
        }
        else if (_dragStarted)
        {
          ScrubTime.Content = TimeSpan.FromSeconds(ScrubBar.Value).ToString(@"mm\:ss") + " - " + _selectedSong.Length;
        }
      }

      // When the song ends
      if (Mplayer.Source != null && Mplayer.NaturalDuration.HasTimeSpan
          && Mplayer.Position.TotalSeconds == Mplayer.NaturalDuration.TimeSpan.TotalSeconds)
      {
        if (_shuffleSongs)
          SongGrid.SelectedIndex = _rnd.Next(0, SongGrid.Items.Count);
        else if (_repeatSong)
          SongGrid.SelectedIndex = SongGrid.SelectedIndex;
        else if (_selectedSong == SongGrid.SelectedItem as Itemsource.Songs)
          Next(); // If the current song is the same as the one playing then go to next

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

    private void HookManager_KeyDown(object sender, KeyEventArgs e)
    {
      _keydata = e.KeyData.ToString();
      _isWindowActive = Application.Current.MainWindow.IsActive;
      if (_keydata  == Key.MediaPlayPause.ToString() || _keydata == Key.Play.ToString() || (_isWindowActive && _keydata == Key.Space.ToString()))
        Play();
      else if (_keydata == Key.MediaNextTrack.ToString() || _keydata == Key.Next.ToString() || (_isWindowActive && _keydata == Key.Right.ToString()))
        Next();
      else if (_keydata == Key.MediaPreviousTrack.ToString() || (_isWindowActive && _keydata == Key.Left.ToString()))
        Previous();
      else if (_keydata == Key.VolumeMute.ToString())
        Mute();
    }

    public void Load()
    {
      if (Cfg.ConfigFile["Music.Directory1"] != "")
      {
        Itemsource.LoadSongs(Cfg.ConfigFile["Music.Directory1"]);
        SongGrid.ItemsSource = Itemsource.SongLibrary;
        NoLoadLabel.Visibility = Visibility.Hidden;
      }
      else
      {
        NoLoadLabel.Visibility = Visibility.Visible;
        SongGrid.ItemsSource = null;
      }
      ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Cfg.ConfigFile["Player.Color"]),
        ThemeManager.GetAppTheme("BaseDark"));
      Colors.SelectedValue = Cfg.ConfigFile["Player.Color"];
      Directory1Text.Text = Cfg.ConfigFile["Music.Directory1"];
    }

    private void SongGrid_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _selectedSong = SongGrid.CurrentItem as Itemsource.Songs;

      if (SongGrid.ItemsSource != null && _selectedSong != null) //&& selectedSong.FileName != _currentSong
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
      if (_selectedSong == null)
        _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;

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
        LeftMainColumn.Width = new GridLength(1, GridUnitType.Star);
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
        ScrubTime.Visibility = Visibility.Hidden;
        ScrubPanel.Margin = new Thickness(160, 0, 20, 0);
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
        ScrubPanel.Visibility = Visibility.Hidden;
        PreviousButton.Margin = new Thickness(42, 18, 0, 0);
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
        LeftMainColumn.Width = new GridLength(2, GridUnitType.Star);
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
        ScrubPanel.Margin = new Thickness(175, 0, 20, 0);
        ScrubTime.Visibility = Visibility.Visible;
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
        ScrubPanel.Visibility = Visibility.Visible;
        PreviousButton.Margin = new Thickness(30, 18, 0, 0);
        PlayerSizeRect.Fill = new VisualBrush
        {
          Visual = (Visual) FindResource("appbar_minnow"),
          Stretch = Stretch.Uniform
        };
      }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      Process.Start(e.Uri.ToString());
    }

    #region Settings

    /*
    private void Directory1_OnTextChanged(object sender, TextChangedEventArgs e)
    {
      if (!IsLoaded || Directory1Text.Text == "")
        return;
      //Load();
    }
    */

    private void PlayerSettings_OnClick(object sender, RoutedEventArgs e)
    {
      FlyoutHandler(SettingsGrid, "Settings");
    }

    private void btnClear1_OnClick(object sender, RoutedEventArgs e)
    {
      Directory1Text.Text = "";
      Mplayer.Close();
      AlbumArt.Source = null;
      AlbumArtToolTip.ToolTip = "Album";
      Placeholder.Visibility = Visibility.Visible;
      Itemsource.SongLibrary?.Clear();
      ScrubTime.Content = "00:00 / 00:00";
      ScrubBar.Value = 0;
      NowPlayingAlbum.Text = "Album";
      NowPlayingTrack.Text = "Track";
      NowPlayingSong.Content = "Song - Artist";
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
      var result = dialog.ShowDialog();

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
      if (_currentView != 0)
      {
        var watch = new Stopwatch();
        watch.Start();

        _currentView = 0; // Set our view to songs grid
        SongGrid.ItemsSource = Itemsource.SongLibrary;

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

        SongGrid.Visibility = Visibility.Visible;
        ScrollViewer.Visibility = Visibility.Hidden;
        watch.Stop();
        Console.WriteLine(@"Songs loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
      }
    }

    private void AlbumSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (_currentView != 1 || _currentView == 1 && SongGrid.Visibility == Visibility.Visible)
      {
        var watch = new Stopwatch();
        watch.Start();
        var albumCount = GrabAlbums();

        Sort("Album", SongGrid);
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

        SongGrid.Visibility = Visibility.Hidden;
        ScrollViewer.Visibility = Visibility.Visible;
        watch.Stop();
        Console.WriteLine(albumCount + @" albums and art loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
      }
    }

    private void ArtistSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (_currentView != 2)
      {
        var watch = new Stopwatch();
        watch.Start();
        _currentView = 2; // Set our view to artist grid
        Sort("Artist", SongGrid);
        var artistcount = GrabArtists();

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

        SongGrid.Visibility = Visibility.Hidden;
        ScrollViewer.Visibility = Visibility.Visible;
        watch.Stop();
        Console.WriteLine(artistcount + @" artists loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
      }
    }

    private void Sort(string col, DataGrid grid)
    {
      if (grid.ItemsSource != null)
      {
        var dataView = CollectionViewSource.GetDefaultView(SongGrid.ItemsSource);
        dataView.SortDescriptions.Clear();
        dataView.SortDescriptions.Add(new SortDescription(col, ListSortDirection.Ascending));
        dataView.Refresh();
      }
    }

    private int GrabAlbums()
    {
      WrapPanel.Children.Clear();
      var song = Itemsource.SongLibrary;
      var noduplicates = song.GroupBy(x => x.Album).Select(x => x.First()).ToList();
      var albumCount = 0;

      foreach (var item in noduplicates)
      {
        var tagFile = File.Create(item.FileName);

        var newTile = new Tile
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
        albumCount++;
        WrapPanel.Children.Add(newTile);
      }
      return albumCount;
    }

    private int GrabArtists()
    {
      var isGray = false;
      WrapPanel.Children.Clear();
      var songs = Itemsource.SongLibrary.GroupBy(x => x.Artist).Select(x => x.First()).ToList();
      var artistCount = 0;
      foreach (var song in songs)
      {
        var newTile = new Tile
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
        artistCount++;
        WrapPanel.Children.Add(newTile);
      }
      return artistCount;
    }

    private void Album_OnClick(object sender, RoutedEventArgs e)
    {
      var tile = sender as Tile;
      if (tile != null)
      {
        var content = tile.Title;
        var album = Itemsource.SongLibrary.Where(x => x.Album == content).ToList();

        SongGrid.ItemsSource = album;
        if (_selectedSong != null)
        {
          GetAlbumart();
        }
        else
        {
          SongGrid.SelectedIndex = 0;
          GetAlbumart();
        }
        if (_selectedSong != null && !Mplayer.HasAudio)
        {
          NowPlayingAlbum.Text = content;
          NowPlayingTrack.Text = _selectedSong.Track;
          NowPlayingSong.Content = _selectedSong.Name + " - " + _selectedSong.Artist;
        }
      }
      ScrollViewer.Visibility = Visibility.Hidden;
      SongGrid.Visibility = Visibility.Visible;
    }

    private void AlbumArt_MouseDown(object sender, MouseButtonEventArgs e)
    {
      var album = Itemsource.SongLibrary.ToList();

      if (_selectedSong != null)
      {
        var content = _selectedSong.Album;
        album = album.Where(x => x.Album == content).ToList();
      }
      SongGrid.ItemsSource = album;
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

      SongGrid.Visibility = Visibility.Visible;
      ScrollViewer.Visibility = Visibility.Hidden;
    }

    private void Artist_OnClick(object sender, RoutedEventArgs e)
    {
      var tile = sender as Tile;
      if (tile != null)
      {
        var content = tile.Title;
        var artist = Itemsource.SongLibrary.Where(x => x.Artist == content).ToList();
        SongGrid.ItemsSource = artist;
        //GetAlbumart();
        if (!Mplayer.HasAudio)
        {
          SongGrid.SelectedIndex = 0;
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
      SongGrid.Visibility = Visibility.Visible;
    }

    private void Play()
    {
      if (Mplayer.Source == null && _shuffleSongs == false)
      {
        SongGrid.SelectedIndex = 0;
        _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;

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
        SongGrid.SelectedIndex = _rnd.Next(0, SongGrid.Items.Count);
        _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;

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
      if (SongGrid.SelectedIndex != 0)
      {
        if (Mplayer.Position.TotalSeconds > 3)
          SongGrid.SelectedIndex = SongGrid.SelectedIndex;
        else
          SongGrid.SelectedIndex = SongGrid.SelectedIndex - 1;

        _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;

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

    private void Next()
    {
      if (_shuffleSongs)
        SongGrid.SelectedIndex = _rnd.Next(0, SongGrid.Items.Count);
      else
        SongGrid.SelectedIndex = SongGrid.SelectedIndex + 1;

      _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;

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

    private void Mute()
    {
      if (_audioMuted)
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_2")};
        MuteButton.Width = 20;
        Mplayer.IsMuted = false;
        _audioMuted = false;
      }
      else
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
        MuteButton.Width = 10;
        Mplayer.IsMuted = true;
        _audioMuted = true;
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
        _timer.Stop();
        _dragStarted = true;
        Mplayer.Position = TimeSpan.FromSeconds(ScrubBar.Value);
        Mplayer.IsMuted = false;
      }
      _timer.Start();
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
  }
}