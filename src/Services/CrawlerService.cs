using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public partial class CrawlerService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ICryptoService _cryptoService;
    private readonly IUtilityService _utilityService;
    private readonly CacheService _cacheService;
    private readonly IOptionsMonitor<CacheCrawlerConfig> _crawlerConfig;
    private readonly StorageSingletonService _storageSingletonService;
    private readonly IHttpClientFactory _httpClientFactory;

    public CrawlerService(IHttpClientFactory httpClientFactory, ILogger<CrawlerService> logger, ICryptoService cryptoService, IUtilityService utilityService, CacheService cacheService, IOptionsMonitor<CacheCrawlerConfig> crawlerConfig, StorageSingletonService storageSingletonService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cryptoService = cryptoService;
        _utilityService = utilityService;
        _cacheService = cacheService;
        _crawlerConfig = crawlerConfig;
        _storageSingletonService = storageSingletonService;
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        _logger.LogInformation("Preparing crawler...");

        await Task.Yield(); // release blocking Start method

        BrowserFetcher? browserFetcher = null;
        try
        {
            browserFetcher = new(SupportedBrowser.Chrome);
            var browserInfo = await browserFetcher.DownloadAsync();
            _logger.LogInformation("Using browser {platform} {revision}", browserInfo.Platform, browserInfo.BuildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Can't download browser!");
        }

        using var client = _httpClientFactory.CreateClient();

        await using var browser = await GetBrowserInstance();
        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                var res = await client.GetAsync(_crawlerConfig.CurrentValue.BaseUrl, stopToken);
                if (res.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogWarning("Warning: target url is not available, response code: {statusCode}", res.StatusCode);
                    await Task.Delay(TimeSpan.FromSeconds(10), stopToken);
                    continue;
                }

                // Preprocess routes, do it here because of config hot reload support
                var crawlerTargets = new List<Models.PlaceholderTarget>();
                if (_crawlerConfig.CurrentValue.CacheRoutes == null) continue;

                foreach (var route in _crawlerConfig.CurrentValue.CacheRoutes)
                {
                    var basePattern = route.Pattern ?? "";
                    _utilityService.PreparePlaceholderVariants(basePattern, ref crawlerTargets, route, Array.Empty<string>());
                }

                var crawledPages = 0;

                try
                {
                    GC.Collect();
                }
                catch
                {

                }

                var crawlerTargetsSplitted = new List<List<Models.PlaceholderTarget>>();

                if (_crawlerConfig.CurrentValue.ChunkSplit?.UseChunkSplit ?? false)
                {
                    var itemsPerPage = _crawlerConfig.CurrentValue.ChunkSplit?.ItemsPerPage ?? 10;
                    var currentList = new List<Models.PlaceholderTarget>();
                    var counter = 0;
                    crawlerTargets.ForEach(item =>
                    {
                        if (counter == itemsPerPage)
                        {
                            crawlerTargetsSplitted.Add(currentList);
                            currentList = new List<Models.PlaceholderTarget>();
                            counter = 0;
                        }

                        currentList.Add(item);

                        counter++;
                    });

                    if (currentList.Count > 0)
                    {
                        crawlerTargetsSplitted.Add(currentList);
                    }
                }
                else
                    crawlerTargetsSplitted.Add(crawlerTargets);

                foreach (var chunk in crawlerTargetsSplitted)
                {
                    await using var page = await browser.NewPageAsync();
                    foreach (var target in chunk)
                    {
                        var targetUrl = _crawlerConfig.CurrentValue.BaseUrl + target.Url;
                        var targetUrlHash = _cryptoService.ComputeStringHash(target.Url ?? "");
                        try
                        {
                            await page.GoToAsync(targetUrl);
                            await page.WaitForTimeoutAsync(_crawlerConfig.CurrentValue.PageScanTimeout);
                            //await Task.Delay(_crawlerConfig.PageScanWait, stopToken);
                            var targetData = await page.GetContentAsync();

                            // preprocess resulting html
                            var htmlData = targetData;

                            // strip unneeded data by comment
                            htmlData = SeoStrip1Regex().Replace(htmlData, "");
                            // strip unneeded data by tag
                            htmlData = SeoStrip2Regex().Replace(htmlData, "");
                            htmlData = SeoStrip3Regex().Replace(htmlData, "");

                            if (_crawlerConfig.CurrentValue.CacheToMemory)
                            {
                                _cacheService.CrawlerCache.Set(targetUrlHash, htmlData, new MemoryCacheEntryOptions
                                {
                                    Priority = CacheItemPriority.NeverRemove
                                });
                            }
                            if (_crawlerConfig.CurrentValue.CacheToFS)
                                await File.WriteAllTextAsync($"./cache/{targetUrlHash}.html", htmlData, Encoding.UTF8, stopToken);

                            crawledPages++;
                            _storageSingletonService.CurrentlyCrawledPages = crawledPages;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Can't fetch page {url}", targetUrl);
                        }

                        if (stopToken.IsCancellationRequested)
                            break;
                    }


                }

                _storageSingletonService.CrawledPages = crawledPages;
                _storageSingletonService.CrawleCycles++;

                await Task.Delay(_crawlerConfig.CurrentValue.RescanInterval, stopToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Crawler thrown error");
            }
        }
    }
    private async Task<IBrowser> GetBrowserInstance()
    {
        if (_crawlerConfig.CurrentValue.Puppeteer?.BrowserSource == "local")
            return await Puppeteer.LaunchAsync(
                            new LaunchOptions
                            {
                                Headless = _crawlerConfig.CurrentValue.Puppeteer.Headless,
                                Browser = SupportedBrowser.Chrome,
                                DefaultViewport = new ViewPortOptions
                                {
                                    Width = 32,
                                    Height = 32,
                                },
                                EnqueueAsyncMessages = false, // was true
                                EnqueueTransportMessages = false,
                                IgnoreHTTPSErrors = true,
                                Devtools = false,
                                Args = new[] {
                    "--no-sandbox",
                    "--disable-infobars",
                    "--disable-setuid-sandbox",
                    "--ignore-ICertificatePolicy-errors",
                    "--disable-dev-shm-usage",
                    "--js-flags=\"--max-old-space-size=1024\"" // 1.5GB of js mem
                }
                            }).ConfigureAwait(false);
        else
        {
            var opts = new ConnectOptions();
            if (_crawlerConfig.CurrentValue.Puppeteer?.BrowserSource != null && (_crawlerConfig.CurrentValue.Puppeteer.BrowserSource.StartsWith("http") || _crawlerConfig.CurrentValue.Puppeteer.BrowserSource.StartsWith("https")))
                opts.BrowserURL = _crawlerConfig.CurrentValue.Puppeteer.BrowserSource;
            else
                opts.BrowserWSEndpoint = _crawlerConfig.CurrentValue.Puppeteer?.BrowserSource;

            return await Puppeteer.ConnectAsync(opts).ConfigureAwait(false);
        }
    }

    [GeneratedRegex("<!--seo-strip-->(.*?)<!--seo-strip-end-->", RegexOptions.IgnoreCase | RegexOptions.Singleline, "ru-RU")]
    private static partial Regex SeoStrip1Regex();
    [GeneratedRegex("<div class=\"seo-strip\">(.*?)<\\/div>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "ru-RU")]
    private static partial Regex SeoStrip2Regex();
    [GeneratedRegex("<div class=\"seo-strip-2\"><\\/div>(.*?)<div class=\"seo-strip-2\"><\\/div>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "ru-RU")]
    private static partial Regex SeoStrip3Regex();
}