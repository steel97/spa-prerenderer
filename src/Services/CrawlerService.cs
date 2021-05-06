using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services
{
    public class CrawlerService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IUtilityService _utilityService;
        private readonly CacheService _cacheService;
        private readonly CacheCrawler _crawlerConfig;

        public CrawlerService(ILogger<CrawlerService> logger, ICryptoService cryptoService, IUtilityService utilityService, CacheService cacheService, IOptions<CacheCrawler> crawlerConfig)
        {
            _logger = logger;
            _cryptoService = cryptoService;
            _utilityService = utilityService;
            _cacheService = cacheService;
            _crawlerConfig = crawlerConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stopToken)
        {
            _logger.LogInformation("Preparing crawler...");

            return;
            BrowserFetcher browserFetcher = null;
            try
            {
                browserFetcher = new();
                var browserInfo = await browserFetcher.DownloadAsync();
                _logger.LogInformation($"Using browser {browserInfo.Platform} {browserInfo.Revision}");
            }
            catch
            {
                _logger.LogError("Can't download browser!");
            }

            var watchingTargets = new List<string>();

            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    // Preprocess routes, do it here because of config hot reload support
                    var crawlerTargets = new List<string>();
                    foreach (var route in _crawlerConfig.CacheRoutes)
                    {
                        var basePattern = route.Pattern;
                        _utilityService.PreparePlaceholderVariants(basePattern, ref crawlerTargets);
                    }

                    foreach (var target in watchingTargets)
                    {
                        if (crawlerTargets.Contains(target)) continue;

                        // Remove outdated cache
                        if (File.Exists($"./cache/{target}.html")) File.Delete($"./cache/{target}.html");
                        _cacheService.CrawlerCache.Remove(target);
                    }

                    watchingTargets.Clear();
                    watchingTargets.AddRange(crawlerTargets);

                    await using (var browser = await GetBrowserInstance())
                    {
                        await using var page = await browser.NewPageAsync();
                        foreach (var target in crawlerTargets)
                        {
                            var targetUrl = _crawlerConfig.BaseUrl + target;
                            var targetUrlHash = _cryptoService.ComputeStringHash(target);
                            try
                            {
                                await page.GoToAsync(targetUrl, _crawlerConfig.PageScanTimeout);
                                await Task.Delay(_crawlerConfig.PageScanWait, stopToken);
                                var htmlData = await page.GetContentAsync();

                                if (_crawlerConfig.CacheToMemory)
                                {
                                    _cacheService.CrawlerCache.Set<string>(targetUrlHash, htmlData, new MemoryCacheEntryOptions
                                    {
                                        Priority = CacheItemPriority.NeverRemove
                                    });
                                }
                                else
                                {
                                    await File.WriteAllTextAsync($"./cache/{targetUrlHash}.html", htmlData, Encoding.UTF8, stopToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Can't fetch page {targetUrl}");
                            }
                        }
                    }

                    await Task.Delay(_crawlerConfig.RescanInterval, stopToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Crawler thrown error");
                }
            }
        }
        private Task<Browser> GetBrowserInstance()
        {
            if (_crawlerConfig.Puppeteer.BrowserSource == "local")
                return Puppeteer.LaunchAsync(
                                new LaunchOptions
                                {
                                    Headless = _crawlerConfig.Puppeteer.Headless
                                });
            else
            {
                var opts = new ConnectOptions();
                if (_crawlerConfig.Puppeteer.BrowserSource.StartsWith("http") || _crawlerConfig.Puppeteer.BrowserSource.StartsWith("https"))
                    opts.BrowserURL = _crawlerConfig.Puppeteer.BrowserSource;
                else
                    opts.BrowserWSEndpoint = _crawlerConfig.Puppeteer.BrowserSource;

                return Puppeteer.ConnectAsync(opts);
            }
        }
    }
}