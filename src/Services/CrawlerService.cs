using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public class CrawlerService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ICryptoService _cryptoService;
    private readonly IUtilityService _utilityService;
    private readonly CacheService _cacheService;
    private readonly CacheCrawlerConfig _crawlerConfig;
    private readonly StorageSingletonService _storageSingletonService;
    private readonly IHttpClientFactory _httpClientFactory;

    public CrawlerService(IHttpClientFactory httpClientFactory, ILogger<CrawlerService> logger, ICryptoService cryptoService, IUtilityService utilityService, CacheService cacheService, IOptions<CacheCrawlerConfig> crawlerConfig, StorageSingletonService storageSingletonService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cryptoService = cryptoService;
        _utilityService = utilityService;
        _cacheService = cacheService;
        _crawlerConfig = crawlerConfig.Value;
        _storageSingletonService = storageSingletonService;
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        _logger.LogInformation("Preparing crawler...");

        await Task.Yield(); // release blocking Start method

        BrowserFetcher? browserFetcher = null;
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

        using var client = _httpClientFactory.CreateClient();

        await using (var browser = await GetBrowserInstance())
        {
            await using var page = await browser.NewPageAsync();
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    var res = await client.GetAsync(_crawlerConfig.BaseUrl, stopToken);
                    if (res.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogWarning("Warning: target url is not available, response code: " + res.StatusCode);
                        await Task.Delay(TimeSpan.FromSeconds(10), stopToken);
                        continue;
                    }

                    // Preprocess routes, do it here because of config hot reload support
                    var crawlerTargets = new List<SpaPrerenderer.Models.PlaceholderTarget>();
                    if (_crawlerConfig.CacheRoutes == null) continue;

                    foreach (var route in _crawlerConfig.CacheRoutes)
                    {
                        var basePattern = route.Pattern ?? "";
                        _utilityService.PreparePlaceholderVariants(basePattern, ref crawlerTargets, route, new string[] { });
                    }

                    var crawledPages = 0;


                    foreach (var target in crawlerTargets)
                    {
                        var targetUrl = _crawlerConfig.BaseUrl + target.Url;
                        var targetUrlHash = _cryptoService.ComputeStringHash(target.Url ?? "");
                        try
                        {
                            await page.GoToAsync(targetUrl);
                            await page.WaitForTimeoutAsync(_crawlerConfig.PageScanTimeout);
                            //await Task.Delay(_crawlerConfig.PageScanWait, stopToken);
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

                            crawledPages++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Can't fetch page {targetUrl}");
                        }
                    }

                    _storageSingletonService.CrawledPages = crawledPages;
                    _storageSingletonService.CrawleCycles++;

                    await Task.Delay(_crawlerConfig.RescanInterval, stopToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Crawler thrown error");
                }
            }
        }
    }
    private async Task<Browser> GetBrowserInstance()
    {
        if (_crawlerConfig.Puppeteer?.BrowserSource == "local")
            return await Puppeteer.LaunchAsync(
                            new LaunchOptions
                            {
                                Headless = _crawlerConfig.Puppeteer.Headless,
                                Product = Product.Chrome,
                                DefaultViewport = new ViewPortOptions
                                {
                                    Width = 32,
                                    Height = 32,
                                },
                                EnqueueAsyncMessages = true,
                                EnqueueTransportMessages = true,
                                IgnoreHTTPSErrors = true,
                                Devtools = false,
                                Args = new[] {
                    "--no-sandbox",
                    "--disable-infobars",
                    "--disable-setuid-sandbox",
                    "--ignore-ICertificatePolicy-errors",
                }
                            }).ConfigureAwait(false);
        else
        {
            var opts = new ConnectOptions();
            if (_crawlerConfig.Puppeteer?.BrowserSource != null && (_crawlerConfig.Puppeteer.BrowserSource.StartsWith("http") || _crawlerConfig.Puppeteer.BrowserSource.StartsWith("https")))
                opts.BrowserURL = _crawlerConfig.Puppeteer.BrowserSource;
            else
                opts.BrowserWSEndpoint = _crawlerConfig.Puppeteer?.BrowserSource;

            return await Puppeteer.ConnectAsync(opts).ConfigureAwait(false);
        }
    }
}