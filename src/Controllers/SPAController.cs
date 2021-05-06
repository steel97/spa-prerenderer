using System;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Wangkanai.Detection.Services;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services;

namespace SpaPrerenderer.Controllers
{
    [Controller]
    public class SPAController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDetectionService _detectionService;
        private readonly CacheService _cacheService;
        private readonly Common _commonConfig;
        private readonly SPA _spaConfig;

        public SPAController(ILogger<SPAController> logger, IDetectionService detectionService, CacheService cacheService, IOptions<Common> commonConfig, IOptions<SPA> spaConfig)
        {
            _logger = logger;
            _detectionService = detectionService;
            _cacheService = cacheService;
            _commonConfig = commonConfig.Value;
            _spaConfig = spaConfig.Value;

        }

        [HttpGet("{*url}", Order = int.MaxValue)]
        public ActionResult Index(string url)
        {
            // set 404 response code if match config
            var matchNotFound = false;
            if (_spaConfig.NotFound.Use404Code)
            {
                var foundRoute = false;
                foreach (var route in _cacheService.KnownRoutes)
                {
                    if (Regex.IsMatch("/" + url + (_spaConfig.NotFound.IncludeQueryString ? Request.QueryString : ""), route))
                    {
                        foundRoute = true;
                        break;
                    }
                }
                if (!foundRoute) matchNotFound = true;
            }

            if (matchNotFound) url = "404";

            var indexPage = "";
            if (_commonConfig.CacheHTML)
            {
                if (_cacheService.SPACache.TryGetValue("index", out indexPage))
                {

                }
                else
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

            if (_detectionService.Crawler.IsCrawler)
            {
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
}