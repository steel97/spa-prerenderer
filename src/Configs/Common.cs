using SpaPrerenderer.Models;

namespace SpaPrerenderer.Configs
{
    public class Common
    {
        public bool CacheHTML { get; set; }
        public int CacheHTMLInterval { get; set; }
        public Placeholder[] Placeholders { get; set; }
    }
}