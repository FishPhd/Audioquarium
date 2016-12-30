using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using File = TagLib.File;

namespace Audioquarium
{
  internal class Itemsource
  {
    public static readonly List<Songs> SongLibrary = new List<Songs>();
    private static readonly string[] Filetypes = {"*.mp3", "*.wav", "*.aac", "*.flac", "*.wma"};
    private static string _songName;
    private static string _songArtist;

    public static int LoadSongs(string path)
    {
      var watch = new Stopwatch();
      watch.Start();

      SongLibrary?.Clear();

      var songCount = 0;
      var files = new List<string>();

      foreach (var t in Filetypes)
        files.AddRange(GetFiles(path, t));

      foreach (var file in files)
      {
        try
        {
          var tagFile = File.Create(file);

          _songName = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(file);
          _songArtist = tagFile.Tag.FirstAlbumArtist ?? tagFile.Tag.Performers[0];

          SongLibrary?.Add(new Songs
          {
            Name = _songName,
            Artist = _songArtist,
            Album = tagFile.Tag.Album,
            Length = tagFile.Properties.Duration.TotalSeconds,
            Duration = TimeSpan.FromSeconds(tagFile.Properties.Duration.TotalSeconds).ToString(@"mm\:ss"),
            Track = tagFile.Tag.Track.ToString(),
            FileName = tagFile.Name,
            AltName = Path.GetFileNameWithoutExtension(file)
          });
          songCount++;
        }
        catch
        {
          Console.WriteLine(@"Metadata could not be created for " + file);
        }
      }

      watch.Stop();
      Console.WriteLine(songCount + @" songs loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
      return songCount;
    }

    private static List<string> GetFiles(string path, string pattern)
    {
      var files = new List<string>();

      try
      {
        files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
        foreach (var directory in Directory.GetDirectories(path))
          files.AddRange(GetFiles(directory, pattern));
      }
      catch
      {
        Console.WriteLine(@"Music directory is unable to be read. File access issue?");
      }

      return files;
    }

    public static void SetRowVisibilityByFilterText(string filterText, DataGrid datagrid, bool charDeleted)
    {
      GetVisibleRows(datagrid, charDeleted)
          .ToList()
          .ForEach(
          x =>
          {
            if (x == null)
              return;

            x.Visibility =
              DataMatchesFilterText(x, filterText.ToLower(), datagrid) ? Visibility.Visible : Visibility.Collapsed;
          });
    }

    private static bool DataMatchesFilterText(DataGridRow toBeFiltered, string filterText, DataGrid datagrid)
    {
      DataGridCell cell = GetCell(datagrid, toBeFiltered, 0);
      TextBlock content = (TextBlock)cell.Content;
      string cellValue = content.Text.ToLower();
      if (cell != null)
      {
        int index = cellValue.Zip(filterText, (c1, c2) => c1 == c2).TakeWhile(b => b).Count() + 1;
        Console.WriteLine(index + "  " + cellValue);
        if (index > filterText.Length)
          return true;
      }
      return false;
    }

    public static DataGridCell GetCell(DataGrid dataGrid, DataGridRow row, int column)
    {
      if (row != null)
      {
        DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
        DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
        return cell;
      }
      return null;
    }

    public static T GetVisualChild<T>(Visual parent) where T : Visual
    {
      T child = default(T);
      int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < numVisuals; i++)
      {
        Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
        child = v as T ?? GetVisualChild<T>(v);
        if (child != null)
        {
          break;
        }
      }
      return child;
    }

    public static IEnumerable<DataGridRow> GetVisibleRows(DataGrid grid, bool charDeleted)
    {
      int count = 0;
      var itemsSource = grid.ItemsSource as IEnumerable;
      if (null == itemsSource) yield return null;
      foreach (var item in itemsSource)
      {
        count++;
        var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
        if(row != null && row.Visibility == Visibility.Visible || charDeleted)
          yield return row;
      }
      /*
    int count = 0;
      foreach (DataGridRow dr in grid.ItemsSource)
      {
        
        count++;
        if(dr.Visibility == Visibility.Visible)
          yield return (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(count);
      }
      */
    }

    public class Songs
    {
      public string Name { get; set; }
      public string Artist { get; set; }
      public string Album { get; set; }
      public string Track { get; set; }
      public double Length { get; set; }
      public string Duration { get; set; }
      public string FileName { get; set; }
      public string AltName { get; set; }
    }
  }
}