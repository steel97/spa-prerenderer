using SpaPrerenderer.Models;

namespace SpaPrerenderer.Configs;

public class CacheCrawlerConfig
{
    public Puppeteer? Puppeteer { get; set; }
    public int RescanInterval { get; set; }
    public int PageScanTimeout { get; set; }
    public int PageScanWait { get; set; }
    public bool CacheToMemory { get; set; }
    public bool CacheToFS { get; set; }
    public string? BaseUrl { get; set; }
    public ChunkSplit? ChunkSplit { get; set; }
    public SpaRoute[]? CacheRoutes { get; set; }
}

public class ChunkSplit
{
    public bool UseChunkSplit { get; set; }
    public int ItemsPerPage { get; set; }
}