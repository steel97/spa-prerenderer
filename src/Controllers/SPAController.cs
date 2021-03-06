using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Wangkanai.Detection.Services;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services;

namespace SpaPrerenderer.Controllers;

[Controller]
public class SPAController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IDetectionService _detectionService;
    private readonly CacheService _cacheService;
    private readonly CommonConfig _commonConfig;
    private readonly SPAConfig _spaConfig;
    private readonly SitemapConfig _sitemapConfig;

    public SPAController(ILogger<SPAController> logger,
        IDetectionService detectionService,
        CacheService cacheService,
        IOptionsSnapshot<CommonConfig> commonConfig,
        IOptionsSnapshot<SPAConfig> spaConfig,
        IOptionsSnapshot<SitemapConfig> sitemapConfig
    )
    {
        _logger = logger;
        _detectionService = detectionService;
        _cacheService = cacheService;
        _commonConfig = commonConfig.Value;
        _spaConfig = spaConfig.Value;
        _sitemapConfig = sitemapConfig.Value;
    }

    [HttpGet("{*url}", Order = int.MaxValue)]
    public ActionResult Index(string url)
    {
        var skipCrawlerCheck = false; // for internal usage

        var includeQueryString = _spaConfig.NotFound?.IncludeQueryString ?? false;

        // sitemap generator response
        if (_sitemapConfig.UseSitemapGenerator)
        {
            var sitemapReqMatch = Regex.IsMatch("/" + url + (includeQueryString ? Request.QueryString : ""), _sitemapConfig.SitemapUrlPattern ?? "/sitemap.xml");
            if (sitemapReqMatch)
            {
                return new ContentResult
                {
                    ContentType = "application/xml",
                    StatusCode = (int)HttpStatusCode.OK,
                    Content = _cacheService.FilesCache.Get<string>("sitemap.xml")
                };
            }
        }

        // set 404 response code if match config
        var matchNotFound = false;
        if (_spaConfig.NotFound?.Use404Code ?? false)
        {
            var foundRoute = false;
            foreach (var route in _cacheService.KnownRoutes)
            {
                if (Regex.IsMatch("/" + url + (includeQueryString ? Request.QueryString : ""), route.Url ?? ""))
                {
                    foundRoute = true;
                    break;
                }
            }
            if (!foundRoute) matchNotFound = true;
        }

        if (matchNotFound) url = "404";

        // redirects
        var _301target = "";
        if (_spaConfig.Redirect != null && _spaConfig.Redirect.RedirectRoutes != null)
        {
            foreach (var route in _spaConfig.Redirect.RedirectRoutes)
            {
                if (Regex.IsMatch("/" + url + (includeQueryString ? Request.QueryString : ""), route.Match ?? ""))
                {
                    _301target = route.Target;
                    break;
                }
            }
        }

        // internal specific
        var inp = "/" + url;

        var reg1 = new Regex(@"([a-z]+)\/coin\/sero");
        if (reg1.IsMatch(inp))
        {
            var matches = reg1.Matches(inp);
            if (matches.Count > 0)
            {
                var groups = matches[0].Groups;
                if (groups.Count > 1)
                {
                    _301target = $"/{groups[1].ToString()}/coin/veil";
                    skipCrawlerCheck = true;
                }
            }
        }

        var reg2 = new Regex(@"([a-z]+)\/coin\/btg");
        if (reg2.IsMatch(inp))
        {
            var matches = reg2.Matches(inp);
            if (matches.Count > 0)
            {
                var groups = matches[0].Groups;
                if (groups.Count > 1)
                {
                    _301target = $"/{groups[1].ToString()}/coin/aion";
                    skipCrawlerCheck = true;
                }
            }
        }

        // end

        var indexPage = "";
        if (_commonConfig.CacheHTML)
        {
            if (!_cacheService.SPACache.TryGetValue("index", out indexPage))
            {
                indexPage = System.IO.File.ReadAllText("./wwwroot/index.html");
                _cacheService.SPACache.Set<string>("index", indexPage, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(_commonConfig.CacheHTMLInterval)
                });
            }
        }
        else
        {

        }

        if (_detectionService.Crawler.IsCrawler || skipCrawlerCheck)
        {
            if (!string.IsNullOrEmpty(_301target))
                return RedirectPermanent(_301target);

            var returnContent = _cacheService.GetPageContents("/" + url);
            if (returnContent == null)
            {
                _logger.LogWarning($"Missing cache for /{url}");
                returnContent = indexPage;
            }
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = matchNotFound ? (int)HttpStatusCode.NotFound : (int)HttpStatusCode.OK,
                Content = returnContent
            };
        }


        return new ContentResult
        {
            ContentType = "text/html",
            StatusCode = matchNotFound ? (int)HttpStatusCode.NotFound : (int)HttpStatusCode.OK,
            Content = indexPage
        };
    }
}