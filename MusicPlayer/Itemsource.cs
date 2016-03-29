using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using File = TagLib.File;

namespace Audioquarium
{
  internal class Itemsource
  {
    public static List<Songs> Info = new List<Songs>();

    public static void LoadSongs(string path)
    {
      Stopwatch watch = new Stopwatch();
      watch.Start();

      Info.Clear();

      var filetypes = new[] {"*.mp3", "*.wav", "*.aac", "*.flac", "*.wma"};

      List<string> files = new List<string>();

      foreach (string t in filetypes)
        files.AddRange(GetFiles(path, t));

      foreach (var file in files)
      {
          var tagFile = File.Create(file);
          if (tagFile.Tag.Title == null)
          {
            Info?.Add(new Songs
            {
              Name = Path.GetFileNameWithoutExtension(file),
              Artist = tagFile.Tag.FirstAlbumArtist,
              Album = tagFile.Tag.Album,
              Length = tagFile.Properties.Duration.ToString(@"mm\:ss"),
              Track = tagFile.Tag.Track.ToString(),
              FileName = tagFile.Name,
              AltName = Path.GetFileNameWithoutExtension(file)
            });
          }
          else if (tagFile.Tag.FirstAlbumArtist == null)
          {
            Info?.Add(new Songs
            {
              Name = Path.GetFileNameWithoutExtension(file),
              Artist = tagFile.Tag.Performers[0],
              Album = tagFile.Tag.Album,
              Length = tagFile.Properties.Duration.ToString(@"mm\:ss"),
              Track = tagFile.Tag.Track.ToString(),
              FileName = tagFile.Name,
              AltName = Path.GetFileNameWithoutExtension(file)
            });
          }
          else if (tagFile.Tag.FirstAlbumArtist == null && tagFile.Tag.Title == null)
          {
            Info?.Add(new Songs
            {
              Name = Path.GetFileNameWithoutExtension(file),
              Artist = tagFile.Tag.Performers[0],
              Album = tagFile.Tag.Album,
              Length = tagFile.Properties.Duration.ToString(@"mm\:ss"),
              Track = tagFile.Tag.Track.ToString(),
              FileName = tagFile.Name,
              AltName = Path.GetFileNameWithoutExtension(file)
            });
          }
          else
          {
            Info?.Add(new Songs
            {
              Name = tagFile.Tag.Title,
              Artist = tagFile.Tag.FirstAlbumArtist,
              Album = tagFile.Tag.Album,
              Length = tagFile.Properties.Duration.ToString(@"mm\:ss"),
              Track = tagFile.Tag.Track.ToString(),
              FileName = tagFile.Name,
              AltName = Path.GetFileNameWithoutExtension(file)
            });
          }
      }

      watch.Stop();
      Console.WriteLine (@"Songs loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
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
      catch (UnauthorizedAccessException) { }

      return files;
    }

    public class Songs
    {
      public string Name { get; set; }
      public string Artist { get; set; }
      public string Album { get; set; }
      public string Track { get; set; }
      public string Length { get; set; }
      public string FileName { get; set; }
      public string AltName { get; set; }
    }
  }
}
 