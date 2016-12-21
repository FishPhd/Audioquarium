using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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