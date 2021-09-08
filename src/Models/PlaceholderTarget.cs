using System.Collections.Generic;

namespace SpaPrerenderer.Models
{
    public class PlaceholderTarget
    {
        public string Url { get; set; }
        public Route RouteLink { get; set; }
        public KeyValuePair<string, string>[] UsedPlaceholders { get; set; }
    }
}