using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services
{
    public class CacheService
    {
        public MemoryCache SPACache { get; set; }
        public MemoryCache CrawlerCache { get; set; }

        private readonly ICryptoService _cryptoService;
        private readonly IUtilityService _utilityService;
        private readonly CacheCrawler _crawlerConfig;
        private readonly SPA _spaConfig;

        public List<string> KnownRoutes = new List<string>();

        public CacheService(ICryptoService cryptoService, IUtilityService utilityService, IOptions<CacheCrawler> crawlerConfig, IOptions<SPA> spaConfig)
        {
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

            foreach (var route in _spaConfig.NotFound.KnownRoutes)
            {
                _utilityService.PreparePlaceholderVariants(route.Pattern, ref KnownRoutes);
            }
        }

        public string GetPageContents(string path)
        {
            var hash = _cryptoService.ComputeStringHash(path);
            if (_crawlerConfig.CacheToMemory)
            {
                string res = null;
                if (CrawlerCache.TryGetValue<string>(hash, out res))
                    return res;
            }
            else
            {
                if (File.Exists($"./cache/{hash}.html"))
                    return File.ReadAllText($"./cache/{hash}.html");
            }
            return null;
        }
    }
}