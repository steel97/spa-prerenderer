using SpaPrerenderer.Models;

namespace SpaPrerenderer.Configs
{
    public class SitemapConfig
    {
        public bool UseSitemapGenerator { get; set; }
        public string SitemapUrlPattern { get; set; }
        public string BaseUrl { get; set; }
        public int RescanConfigInterval { get; set; }
        public Alternate[] Alternates { get; set; }
        public SitemapRoute[] Routes { get; set; }
    }
}