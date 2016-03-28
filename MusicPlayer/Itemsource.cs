using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = TagLib.File;

namespace Audioquarium
{
  internal class Itemsource
  {
    public static List<Songs> Info = new List<Songs>();

    public static void LoadSongs(string path)
    {
      Info.Clear();
      var d = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
        .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav") || s.EndsWith(".aac") || s.EndsWith(".flac"));

      foreach (var file in d)
      {
        var tagFile = File.Create(file);

        if (tagFile.Tag.Title == null)
        {
          Info.Add(new Songs
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
          Info.Add(new Songs
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
          Info.Add(new Songs
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
          Info.Add(new Songs
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