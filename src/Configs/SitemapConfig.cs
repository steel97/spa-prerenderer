using SpaPrerenderer.Models;

namespace SpaPrerenderer.Configs
{
    public class SitemapConfig
    {
        public bool UseSitemapGenerator { get; set; }
        public Route[] Routes { get; set; }
    }
}