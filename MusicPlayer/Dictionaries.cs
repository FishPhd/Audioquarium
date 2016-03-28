using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    class Dictionaries
    {
        public static Dictionary<string, string> GetColor()
        {
            var colors = new Dictionary<string, string>();
            colors.Add("Blue", "blue");
            colors.Add("Red", "red");
            colors.Add("Green", "green");
            colors.Add("Purple", "purple");
            colors.Add("Orange", "orange");
            colors.Add("Lime", "lime");
            colors.Add("Emerald", "emerald");
            colors.Add("Teal", "teal");
            colors.Add("Cyan", "cyan");
            colors.Add("Cobalt", "cobalt");
            colors.Add("Indigo", "indigo");
            colors.Add("Violet", "violet");
            colors.Add("Pink", "pink");
            colors.Add("Magenta", "magenta");
            colors.Add("Crimson", "crimson");
            colors.Add("Amber", "amber");
            colors.Add("Yellow", "yellow");
            colors.Add("Brown", "brown");
            colors.Add("Olive", "olive");
            colors.Add("Steel", "steel");
            colors.Add("Mauve", "mauve");
            colors.Add("Taupe", "taupe");
            colors.Add("Sienna", "sienna");
            return colors;
        }
    }
}
