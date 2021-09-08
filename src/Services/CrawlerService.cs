using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly CacheCrawlerConfig _crawlerConfig;

        public CrawlerService(ILogger<CrawlerService> logger, ICryptoService cryptoService, IUtilityService utilityService, CacheService cacheService, IOptions<CacheCrawlerConfig> crawlerConfig)
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

            BrowserFetcher browserFetcher = null;
            try
            {
                browserFetcher = new(Product.Chrome);
                var browserInfo = await browserFetcher.DownloadAsync();
                _logger.LogInformation($"Using browser {browserInfo.Platform} {browserInfo.Revision}");
            }
            catch
            {
                _logger.LogError("Can't download browser!");
            }

            await using (var browser = await GetBrowserInstance())
            {
                await using var page = await browser.NewPageAsync();
                while (!stopToken.IsCancellationRequested)
                {
                    try
                    {
                        // Preprocess routes, do it here because of config hot reload support
                        var crawlerTargets = new List<SpaPrerenderer.Models.PlaceholderTarget>();
                        foreach (var route in _crawlerConfig.CacheRoutes)
                        {
                            var basePattern = route.Pattern;
                            _utilityService.PreparePlaceholderVariants(basePattern, ref crawlerTargets, route, new string[] { });
                        }

                        foreach (var target in crawlerTargets)
                        {
                            var targetUrl = _crawlerConfig.BaseUrl + target.Url;
                            var targetUrlHash = _cryptoService.ComputeStringHash(target.Url);
                            try
                            {
                                await page.GoToAsync(targetUrl, _crawlerConfig.PageScanTimeout);
                                await Task.Delay(_crawlerConfig.PageScanWait, stopToken);
                                var targetData = await page.GetContentAsync();

                                // preprocess resulting html
                                var htmlData = targetData;

                                // strip unneeded data by comment
                                htmlData = Regex.Replace(htmlData, @"<!--seo-strip-->(.*?)<!--seo-strip-end-->", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                // strip unneeded data by tag
                                htmlData = Regex.Replace(htmlData, @"<div class=""seo-strip"">(.*?)<\/div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                htmlData = Regex.Replace(htmlData, @"<div class=""seo-strip-2""><\/div>(.*?)<div class=""seo-strip-2""><\/div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                                if (_crawlerConfig.CacheToMemory)
                                {
                                    _cacheService.CrawlerCache.Set<string>(targetUrlHash, htmlData, new MemoryCacheEntryOptions
                                    {
                                        Priority = CacheItemPriority.NeverRemove
                                    });
                                }
                                if (_crawlerConfig.CacheToFS)
                                    await File.WriteAllTextAsync($"./cache/{targetUrlHash}.html", htmlData, Encoding.UTF8, stopToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Can't fetch page {targetUrl}");
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
        }
        private Task<Browser> GetBrowserInstance()
        {
            if (_crawlerConfig.Puppeteer.BrowserSource == "local")
                return Puppeteer.LaunchAsync(
                                new LaunchOptions
                                {
                                    Headless = _crawlerConfig.Puppeteer.Headless,
                                    Product = Product.Chrome
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