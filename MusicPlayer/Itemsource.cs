using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = TagLib.File;

namespace MusicPlayer
{
    internal class Itemsource
    {
        public static List<Songs> LoadSongs(string path)
        {
            var d = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav") || s.EndsWith(".aac") || s.EndsWith(".flac"));

            var info = new List<Songs>();

            foreach (var file in d)
            {
                var tagFile = File.Create(file);
                info.Add(new Songs
                {
                    Name = tagFile.Tag.Title,
                    Artist = tagFile.Tag.FirstAlbumArtist,
                    Album = tagFile.Tag.Album,
                    Length = tagFile.Properties.Duration.ToString(@"mm\:ss"),
                    Track = tagFile.Tag.Track.ToString(),
                    FileName = tagFile.Name
                });
            }
            return info;
        }

        public class Songs
        {
            public string Name { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Track { get; set; }
            public string Length { get; set; }
            public string FileName { get; set; }
            public string Art { get; set; }
        }
    }
}