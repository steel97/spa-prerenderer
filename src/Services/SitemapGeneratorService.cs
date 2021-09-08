using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using SpaPrerenderer.Models;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services
{
    public class SitemapGeneratorService : BackgroundService
    {
        private readonly XNamespace _nameSpace = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private readonly XNamespace _nameSpaceAlternate = "http://www.w3.org/1999/xhtml";

        private readonly ILogger _logger;
        private readonly IUtilityService _utilityService;
        private readonly CacheService _cacheService;
        private readonly SitemapConfig _sitemapConfig;

        public SitemapGeneratorService(ILogger<SitemapGeneratorService> logger, IUtilityService utilityService, CacheService cacheService, IOptions<SitemapConfig> sitemapConfig)
        {
            _logger = logger;
            _utilityService = utilityService;
            _cacheService = cacheService;
            _sitemapConfig = sitemapConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stopToken)
        {
            if (!_sitemapConfig.UseSitemapGenerator)
            {
                _logger.LogInformation("Sitemap generator disabled.");
                return;
            }

            _logger.LogInformation("Using sitemap generator.");

            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    var sitemapTargets = new List<PlaceholderTarget>();
                    foreach (var route in _sitemapConfig.Routes)
                    {
                        var basePattern = route.Pattern;
                        _utilityService.PreparePlaceholderVariants(basePattern, ref sitemapTargets, route, new string[] { });
                    }



                    var vset = new List<object>();
                    foreach (var target in sitemapTargets)
                    {
                        var targetUrl = _sitemapConfig.BaseUrl + target.Url;

                        // adding elements
                        var baseAddress = new XElement(_nameSpace + "url");

                        // adding basic entries
                        var routeDecoded = (SitemapRoute)target.RouteLink;

                        var targetUrlEl = new XElement(_nameSpace + "loc", targetUrl);
                        baseAddress.Add(targetUrlEl);

                        if (!string.IsNullOrEmpty(routeDecoded.ChangeFrequency))
                        {
                            var targetChangeFrequencyEl = new XElement(_nameSpace + "changefreq", routeDecoded.ChangeFrequency);
                            baseAddress.Add(targetChangeFrequencyEl);
                        }

                        var targetPriorityEl = new XElement(_nameSpace + "priority", routeDecoded.Priority);
                        baseAddress.Add(targetPriorityEl);

                        // lookup for alternates
                        if (routeDecoded.WithAlternate != null)
                        {
                            foreach (var alternate in routeDecoded.WithAlternate)
                            {
                                var alternateLookup = _sitemapConfig.Alternates.Where(a => a.Id == alternate).FirstOrDefault();
                                if (alternateLookup == null)
                                {
                                    _logger.LogWarning($"Can't find alternate definition with id: {alternate}");
                                    continue;
                                }
                                if (!alternateLookup.WithVariants)
                                {
                                    var element = new XElement(_nameSpaceAlternate + alternateLookup.TagName);
                                    foreach (var elProp in alternateLookup.Props)
                                    {
                                        var val = elProp.Value;
                                        // replace placeholders {__url} only here
                                        val = val.Replace("{__url}", targetUrl);
                                        element.Add(new XAttribute(elProp.Name, val));
                                    }

                                    baseAddress.Add(element);
                                    continue;
                                }

                                //foreach (var variant in alternateLookup.Variants)
                                //{
                                var tPattern = target.RouteLink.Pattern;
                                var variantedUrls = new List<PlaceholderTarget>();
                                var variants = new List<string>();
                                foreach (var vr in alternateLookup.Variants)
                                {
                                    var ph = vr.Split(':');
                                    if (ph[1] == "current")
                                        variants.Add(ph[0]);
                                }

                                _utilityService.PreparePlaceholderVariants(target.RouteLink.Pattern, ref variantedUrls, target.RouteLink, variants.ToArray());
                                //var variantedUrls = sitemapTargets.Where(a => a.RouteLink == target.RouteLink).ToList();
                                foreach (var currentUrl in variantedUrls)
                                {

                                    var element = new XElement(_nameSpaceAlternate + alternateLookup.TagName);
                                    foreach (var elProp in alternateLookup.Props)
                                    {
                                        var val = elProp.Value;
                                        val = val.Replace("{__url_variant}", _sitemapConfig.BaseUrl + currentUrl.Url);
                                        foreach (var currentPlaceholder in target.UsedPlaceholders)
                                            val = val.Replace($"{{__{currentPlaceholder.Key}}}", currentPlaceholder.Value);

                                        foreach (var vr in alternateLookup.Variants)
                                        {
                                            var ph = vr.Split(':');
                                            var currentPlaceholder = currentUrl.UsedPlaceholders.Where(a => a.Key == ph[0]).FirstOrDefault();
                                            var currentPlaceholderTop = target.UsedPlaceholders.Where(a => a.Key == ph[0]).FirstOrDefault();
                                            if (ph[1] == "current")
                                                val = val.Replace($"{{{currentPlaceholder.Key}}}", currentPlaceholder.Value);
                                            else
                                                val = val.Replace($"{{{currentPlaceholderTop.Key}}}", currentPlaceholderTop.Value);
                                        }


                                        element.Add(new XAttribute(elProp.Name, val));
                                    }

                                    baseAddress.Add(element);

                                }
                                //}
                            }
                        }

                        vset.Add(baseAddress);
                    }

                    // set actual sitemap
                    var sitemap = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(_nameSpace + "urlset", vset));

                    XmlWriterSettings xws = new XmlWriterSettings();
                    xws.OmitXmlDeclaration = false;
                    xws.Indent = true;
                    xws.Async = true;
                    xws.Encoding = Encoding.UTF8;

                    using var sw = new StringWriter();
                    using var xw = XmlWriter.Create(sw, xws);

                    await sitemap.WriteToAsync(xw, stopToken);
                    await xw.FlushAsync();

                    _cacheService.FilesCache.Set<string>("sitemap.xml", sw.ToString());


                    await Task.Delay(_sitemapConfig.RescanConfigInterval, stopToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sitemap generator thrown error");
                }
            }
        }
    }
}