using SpaPrerenderer.Models;

namespace SpaPrerenderer.Configs
{
    public class CacheCrawler
    {
        public Puppeteer Puppeteer { get; set; }
        public int RescanInterval { get; set; }
        public int PageScanTimeout { get; set; }
        public int PageScanWait { get; set; }
        public bool CacheToMemory { get; set; }
        public bool CacheToFS { get; set; }
        public string BaseUrl { get; set; }
        public Route[] CacheRoutes { get; set; }
    }
}