using System.Text.Json;
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
            ExpirationScanFrequency = TimeSpan.FromHours(24)
        });

        _cryptoService = cryptoService;
        _utilityService = utilityService;
        _crawlerConfig = crawlerConfig;
        _spaConfig = spaConfig;

        if (_spaConfig.CurrentValue.NotFound == null || _spaConfig.CurrentValue.NotFound.KnownRoutes == null) return;

        // actually bad thing, should me moved to service later
        _ = Task.Factory.StartNew(() =>
        {
            var routesCachePath = "./cache/known_routes.json";
            try
            {
                if (File.Exists(routesCachePath))
                    KnownRoutes = JsonSerializer.Deserialize<List<PlaceholderTarget>>(File.ReadAllText(routesCachePath))!;
            }
            catch
            {

            }

            try
            {
                var localRoutes = new List<PlaceholderTarget>();

                foreach (var route in _spaConfig.CurrentValue.NotFound.KnownRoutes)
                {
                    ArgumentNullException.ThrowIfNull(route);
                    ArgumentNullException.ThrowIfNull(route.Pattern);

                    _utilityService.PreparePlaceholderVariants(route.Pattern, ref localRoutes, route, Array.Empty<string>());
                }

                // cache known routes
                var json = JsonSerializer.Serialize(localRoutes);
                File.WriteAllText(routesCachePath, json);

                KnownRoutes = localRoutes;
            }
            catch
            {

            }
        }, TaskCreationOptions.LongRunning);
    }

    public string? GetPageContents(string path)
    {
        var hash = _cryptoService.ComputeStringHash(path);
        if (_crawlerConfig.CurrentValue.CacheToMemory)
        {
            if (CrawlerCache.TryGetValue(hash, out string? res))
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