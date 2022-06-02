using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using SpaPrerenderer.Models;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public class CacheService
{
    public MemoryCache FilesCache { get; set; }
    public MemoryCache SPACache { get; set; }
    public MemoryCache CrawlerCache { get; set; }

    private readonly ICryptoService _cryptoService;
    private readonly IUtilityService _utilityService;
    private readonly CacheCrawlerConfig _crawlerConfig;
    private readonly SPAConfig _spaConfig;

    public List<PlaceholderTarget> KnownRoutes = new();

    public CacheService(ICryptoService cryptoService, IUtilityService utilityService, IOptions<CacheCrawlerConfig> crawlerConfig, IOptions<SPAConfig> spaConfig)
    {
        FilesCache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(10)
        });

        SPACache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(10)
        });

        CrawlerCache = new MemoryCache(new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(10)
        });

        _cryptoService = cryptoService;
        _utilityService = utilityService;
        _crawlerConfig = crawlerConfig.Value;
        _spaConfig = spaConfig.Value;

        if (_spaConfig.NotFound == null || _spaConfig.NotFound.KnownRoutes == null) return;

        foreach (var route in _spaConfig.NotFound.KnownRoutes)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(route.Pattern);

            _utilityService.PreparePlaceholderVariants(route.Pattern, ref KnownRoutes, route, new string[] { });
        }
    }

    public string? GetPageContents(string path)
    {
        var hash = _cryptoService.ComputeStringHash(path);
        if (_crawlerConfig.CacheToMemory)
        {
            string? res = null;
            if (CrawlerCache.TryGetValue<string>(hash, out res))
                return res;
        }

        if (_crawlerConfig.CacheToFS)
        {
            if (File.Exists($"./cache/{hash}.html"))
                return File.ReadAllText($"./cache/{hash}.html");
        }

        return null;
    }
}