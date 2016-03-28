using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
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

      Info?.Clear();
      var filetypes = new[] { "mp3", "wav", "aac", "flac", "wma"};

      /*
      var d = Directory.EnumerateFileSystemEntries(path, "*.*", SearchOption.AllDirectories)
        .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav") || s.EndsWith(".aac") 
        || s.EndsWith(".flac") || s.EndsWith(".wma")).ToList();
        */

      foreach (var file in FilterFiles(path, filetypes))
      {
        var tagFile = File.Create(file);

        try
        {
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
        catch
        {
          Info?.Add(new Songs
          {
            Name = Path.GetFileNameWithoutExtension(file),
            AltName = Path.GetFileNameWithoutExtension(file)
          });
        }
      }

      watch.Stop();
      Console.WriteLine(@"Songs loaded in " + watch.ElapsedMilliseconds + @" milliseconds");
    }

    public static IEnumerable<string> FilterFiles(string path, params string[] exts)
    {
      return
          exts.Select(x => "*." + x) // turn into globs
          .SelectMany(x =>
              Directory.EnumerateFiles(path, x, SearchOption.AllDirectories)
              );
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