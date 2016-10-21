using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Audioquarium.Properties;
using Gma.System.MouseKeyHook;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using File = TagLib.File;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Audioquarium
{
  public partial class MainWindow
  {
    public static readonly MediaPlayer Mplayer = new MediaPlayer();
    private readonly Random _rnd = new Random();
    private readonly DispatcherTimer _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(0.5)};
    private int _albumCount;
    private int _artistCount;
    private bool _audioMuted;
    private bool _audioPlaying;
    private int _currentView; // Song(0) Album(1) Artist(2) 
    private bool _dragStarted;
    private bool _isWindowActive = true;
    private string _keydata;
    private int _playerSize = 2; // Guppy(0) Minnow(1) Shark(2) Whale(3)
    private bool _repeatSong;
    private Itemsource.Songs _selectedSong;
    private bool _shuffleSongs;
    private int _songCount;
    private readonly Stopwatch _stopWatch = new Stopwatch();
    private IKeyboardMouseEvents m_GlobalHook;

    public MainWindow()
    {
      InitializeComponent();
      Load();

      m_GlobalHook = Hook.GlobalEvents();
      m_GlobalHook.KeyDown += GlobalHook_KeyDown;

      _timer.Tick += timer_Tick;
      _timer.Start();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      if (_stopWatch.ElapsedMilliseconds > 100)
        _stopWatch.Reset();

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
        else
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

    private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
    {
      if (_stopWatch.ElapsedMilliseconds < 50 && _stopWatch.IsRunning)
      {
        _stopWatch.Reset();
        return;
      }
      _keydata = e.KeyData.ToString();
      _isWindowActive = Application.Current.MainWindow.IsActive;
      if (_keydata == Key.MediaPlayPause.ToString() || _keydata == Key.Play.ToString() || (_isWindowActive && _keydata == Key.Space.ToString()))
        Play();
      else if (_keydata == Key.MediaNextTrack.ToString() || _keydata == Key.Next.ToString() || (_isWindowActive && _keydata == Key.Right.ToString()))
        Next();
      else if (_keydata == Key.MediaPreviousTrack.ToString() || (_isWindowActive && _keydata == Key.Left.ToString()))
        Previous();
      else if (_keydata == Key.VolumeMute.ToString())
        Mute();
      _stopWatch.Restart();
    }

    public void Load()
    {
      var song = Itemsource.SongLibrary;
      if (Settings.Default.MusicDirectory != "") // Check if music directory is empty
      {
        _songCount = Itemsource.LoadSongs(Settings.Default.MusicDirectory);
        SongGrid.ItemsSource = Itemsource.SongLibrary;
        SongGrid.Items.Refresh();
        NoLoadLabel.Visibility = Visibility.Hidden;
      }
      else // Empty setting load nothing (saves a load error)
      {
        NoLoadLabel.Visibility = Visibility.Visible;
        SongGrid.ItemsSource = null;
      }
      // Get the color they had
      ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Settings.Default.PlayerColor),
        ThemeManager.GetAppTheme("BaseDark"));
      Colors.SelectedValue = Settings.Default.PlayerColor; // Color setting set to color (dictionaries.s)
      Directory1Text.Text = Settings.Default.MusicDirectory; // Music directory set to music directory (or empty string)

      var albums = song.GroupBy(x => x.Album).Select(x => x.First()).ToList();
      var artists = Itemsource.SongLibrary.GroupBy(x => x.Artist).Select(x => x.First()).ToList();

      foreach (var item in albums)
        _albumCount++;
      foreach (var item in artists)
        _artistCount++;
      Console.WriteLine(_albumCount + " albums from " + _artistCount + " artists");
    }

    private void SongGrid_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _selectedSong = SongGrid.CurrentItem as Itemsource.Songs;

      if (SongGrid.ItemsSource != null && _selectedSong != null) //&& selectedSong.FileName != _currentSong
      {
        Mplayer.Open(new Uri(_selectedSong.FileName));
        Mplayer.Play();
        Console.WriteLine(_selectedSong.FileName);
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

    private void FlyoutHandler(Grid sender, string header, bool closeFlyout = false)
    {
      Flyout.IsOpen = !closeFlyout;
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
        SongGrid.HeadersVisibility = DataGridHeadersVisibility.None;
        LeftMainColumn.Width = new GridLength(1, GridUnitType.Star);
        RightMainColumn.Width = new GridLength(2, GridUnitType.Star);
        SongGrid.Columns[0].Width = 175;
        SongGrid.Columns[1].Width = 150;
        SongGrid.Columns[2].Width = 175;
        SongGrid.Columns[3].Width = 30;
        SongGrid.Columns[4].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

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
        GhostTime.Visibility = Visibility.Hidden;
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
        ScrubText.Visibility = Visibility.Hidden;
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
        LeftMainColumn.Width = new GridLength(1.5, GridUnitType.Star);
        RightMainColumn.Width = new GridLength(2, GridUnitType.Star);
        SongGrid.HeadersVisibility = DataGridHeadersVisibility.All;

        for (var i = 0; i < SongGrid.Columns.Count - 1; i++)
        {
          SongGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

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
        ScrubText.Visibility = Visibility.Visible;
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
        ScrubText.Visibility = Visibility.Visible;
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
      if (Flyout.IsOpen)
        FlyoutHandler(SettingsGrid, "Settings", true);
      else
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
      Settings.Default.MusicDirectory = "";
      Settings.Default.Save();
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

      Settings.Default.PlayerColor = Colors.SelectedValue.ToString();
      Settings.Default.Save();
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
        Settings.Default.MusicDirectory = Convert.ToString(dialog.FileName);
        Settings.Default.Save();
        Directory1Text.Text = Settings.Default.MusicDirectory;
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

        GrabAlbums();
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
        Console.WriteLine(_albumCount + @" albums and art loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
      }
    }

    private void ArtistSorting_OnClick(object sender, RoutedEventArgs e)
    {
      if (_currentView != 2)
      {
        var watch = new Stopwatch();
        watch.Start();

        GrabArtists();
        _currentView = 2; // Set our view to artist grid
        Sort("Artist", SongGrid);

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
        Console.WriteLine(_artistCount + @" artists loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
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

    private void GrabAlbums()
    {
      WrapPanel.Children.Clear();
      var albums = Itemsource.SongLibrary.GroupBy(x => x.Album).Select(x => x.First()).ToList();

      foreach (var item in albums)
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
        WrapPanel.Children.Add(newTile);
      }
    }

    private void GrabArtists()
    {
      var isGray = false;
      WrapPanel.Children.Clear();
      var artists = Itemsource.SongLibrary.GroupBy(x => x.Artist).Select(x => x.First()).ToList();

      foreach (var song in artists)
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
        WrapPanel.Children.Add(newTile);
      }
    }

    private void Album_OnClick(object sender, RoutedEventArgs e)
    {
      var tile = sender as Tile;
      if (tile != null)
      {
        var content = tile.Title;
        var album = Itemsource.SongLibrary.Where(x => x.Album == content).ToList();

        SongGrid.ItemsSource = album;
        if (Mplayer.HasAudio)
        {
          GetAlbumart();
        }
        else
        {
          SongGrid.SelectedIndex = 0;
          _selectedSong = SongGrid.SelectedItem as Itemsource.Songs;
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
          Console.WriteLine(_selectedSong.FileName);
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
        Console.WriteLine(_selectedSong.FileName);
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
        VolumeSlider.IsEnabled = true;
        _audioMuted = false;
      }
      else
      {
        MuteButton.OpacityMask = new VisualBrush {Visual = (Visual) FindResource("appbar_sound_0")};
        MuteButton.Width = 10;
        Mplayer.IsMuted = true;
        VolumeSlider.IsEnabled = false;
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
      if (Mplayer.HasAudio && _dragStarted)
      {
        Mplayer.Position = TimeSpan.FromSeconds(ScrubBar.Value);
        Mplayer.IsMuted = false;
      }
      _dragStarted = false;
    }

    private void ScrubBar_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
      var track = ScrubBar.Template.FindName("PART_Track", ScrubBar) as Track;
      var timeSpan = TimeSpan.FromSeconds(track.ValueFromPoint(e.GetPosition(ScrubBar)));
      Mplayer.Position = timeSpan;
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

    private void ScrubBar_OnMouseMove(object sender, MouseEventArgs e)
    {
      if (_playerSize > 1)
      {
        var track = ScrubBar.Template.FindName("PART_Track", ScrubBar) as Track;

        if (track != null)
        {
          GhostTime.Visibility = Visibility.Visible;
          GhostTime.Content = TimeSpan.FromSeconds(track.ValueFromPoint(e.GetPosition(ScrubBar))).ToString(@"mm\:ss");
        }
      }
    }

    private void ScrubBar_OnMouseLeave(object sender, MouseEventArgs e)
    {
      GhostTime.Visibility = Visibility.Hidden;
    }

    private void SongGrid_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      // Just here to avoid user scrolling datagrid with keyboard
      e.Handled = true;
    }

    private void MainWindow_OnClosed(object sender, EventArgs e)
    {
      Settings.Default.Save();
    }

    #endregion
  }
}