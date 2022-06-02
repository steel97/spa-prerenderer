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
    private readonly IOptionsMonitor<CacheCrawlerConfig> _crawlerConfig;
    private readonly IOptionsMonitor<SPAConfig> _spaConfig;

    public List<PlaceholderTarget> KnownRoutes = new();

    public CacheService(ICryptoService cryptoService, IUtilityService utilityService, IOptionsMonitor<CacheCrawlerConfig> crawlerConfig, IOptionsMonitor<SPAConfig> spaConfig)
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
        _crawlerConfig = crawlerConfig;
        _spaConfig = spaConfig;

        if (_spaConfig.CurrentValue.NotFound == null || _spaConfig.CurrentValue.NotFound.KnownRoutes == null) return;

        // actually bad thing, should me moved to service later
        Task.Factory.StartNew(() =>
        {
            var localRoutes = new List<PlaceholderTarget>();

            foreach (var route in _spaConfig.CurrentValue.NotFound.KnownRoutes)
            {
                ArgumentNullException.ThrowIfNull(route);
                ArgumentNullException.ThrowIfNull(route.Pattern);

                _utilityService.PreparePlaceholderVariants(route.Pattern, ref localRoutes, route, new string[] { });
            }

            KnownRoutes = localRoutes;
        }, TaskCreationOptions.LongRunning);
    }

    public string? GetPageContents(string path)
    {
        var hash = _cryptoService.ComputeStringHash(path);
        if (_crawlerConfig.CurrentValue.CacheToMemory)
        {
            string? res = null;
            if (CrawlerCache.TryGetValue<string>(hash, out res))
                return res;
        }

        if (_crawlerConfig.CurrentValue.CacheToFS)
        {
            if (File.Exists($"./cache/{hash}.html"))
                return File.ReadAllText($"./cache/{hash}.html");
        }

        return null;
    }
}