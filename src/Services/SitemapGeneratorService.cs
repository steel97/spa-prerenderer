using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using SpaPrerenderer.Models;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public class SitemapGeneratorService : BackgroundService
{
    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }

    private readonly XNamespace _nameSpace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private readonly XNamespace _nameSpaceAlternate = "http://www.w3.org/1999/xhtml";

    private readonly ILogger _logger;
    private readonly IUtilityService _utilityService;
    private readonly CacheService _cacheService;
    private readonly IOptionsMonitor<SitemapConfig> _sitemapConfig;
    private readonly StorageSingletonService _storageSingletonService;

    public SitemapGeneratorService(ILogger<SitemapGeneratorService> logger,
        IUtilityService utilityService,
        CacheService cacheService,
        IOptionsMonitor<SitemapConfig> sitemapConfig,
        StorageSingletonService storageSingletonService
    )
    {
        _logger = logger;
        _utilityService = utilityService;
        _cacheService = cacheService;
        _sitemapConfig = sitemapConfig;
        _storageSingletonService = storageSingletonService;
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        var changed = true;
        _sitemapConfig.OnChange((config) =>
        {
            changed = true;
        });

        if (!_sitemapConfig.CurrentValue.UseSitemapGenerator)
        {
            _logger.LogInformation("Sitemap generator disabled.");
            return;
        }

        _logger.LogInformation("Using sitemap generator.");

        await Task.Yield(); // release blocking Start method

        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                if (!changed)
                {
                    await Task.Delay(_sitemapConfig.CurrentValue.RescanConfigInterval, stopToken);
                    continue;
                }

                changed = false;

                var sitemapTargets = new List<PlaceholderTarget>();
                if (_sitemapConfig.CurrentValue.Routes == null)
                {
                    await Task.Delay(10000, stopToken);
                    continue;
                }

                foreach (var route in _sitemapConfig.CurrentValue.Routes)
                {
                    var basePattern = route.Pattern ?? "";
                    _utilityService.PreparePlaceholderVariants(basePattern, ref sitemapTargets, route, Array.Empty<string>());
                }



                var vset = new List<object>();
                foreach (var target in sitemapTargets)
                {
                    var targetUrl = _sitemapConfig.CurrentValue.BaseUrl + target.Url;
                    if (targetUrl.Contains('{')) continue;

                    // adding elements
                    var baseAddress = new XElement(_nameSpace + "url");

                    // adding basic entries
                    if (target.RouteLink == null) continue;
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
                            var alternateLookup = _sitemapConfig.CurrentValue.Alternates?.Where(a => a.Id == alternate).FirstOrDefault();
                            if (alternateLookup == null)
                            {
                                _logger.LogWarning("Can't find alternate definition with id: {id}", alternate);
                                continue;
                            }
                            if (!alternateLookup.WithVariants)
                            {
                                var element = new XElement(_nameSpace + (alternateLookup.TagName ?? ""));
                                if (alternateLookup.Props != null)
                                {
                                    foreach (var elProp in alternateLookup.Props)
                                    {
                                        var val = elProp.Value ?? "";
                                        // replace placeholders {__url} only here
                                        val = val.Replace("{__url}", targetUrl);
                                        if (!val.Contains('{'))
                                            element.Add(new XAttribute(elProp.Name ?? "", val));
                                    }
                                }

                                baseAddress.Add(element);
                                continue;
                            }

                            //foreach (var variant in alternateLookup.Variants)
                            //{
                            var tPattern = target?.RouteLink.Pattern;
                            var variantedUrls = new List<PlaceholderTarget>();
                            var variants = new List<string>();
                            if (alternateLookup.Variants != null)
                            {
                                foreach (var vr in alternateLookup.Variants)
                                {
                                    var ph = vr.Split(':');
                                    if (ph[1] == "current")
                                    {
                                        if (!ph[0].Contains('{'))
                                            variants.Add(ph[0]);
                                    }
                                }
                            }

                            _utilityService.PreparePlaceholderVariants(target?.RouteLink.Pattern ?? "", ref variantedUrls, target!.RouteLink, variants.ToArray());
                            //var variantedUrls = sitemapTargets.Where(a => a.RouteLink == target.RouteLink).ToList();
                            foreach (var currentUrl in variantedUrls)
                            {
                                var element = new XElement(_nameSpace + (alternateLookup.TagName ?? ""));

                                if (alternateLookup.Props != null)
                                {
                                    foreach (var elProp in alternateLookup.Props)
                                    {
                                        var val = elProp.Value ?? "";
                                        val = val.Replace("{__url_variant}", _sitemapConfig.CurrentValue.BaseUrl + currentUrl?.Url);

                                        if (target?.UsedPlaceholders != null)
                                            foreach (var currentPlaceholder in target.UsedPlaceholders)
                                                val = val.Replace($"{{__{currentPlaceholder.Key}}}", currentPlaceholder.Value);

                                        if (alternateLookup.Variants != null)
                                        {
                                            foreach (var vr in alternateLookup.Variants)
                                            {
                                                var ph = vr.Split(':');
                                                var currentPlaceholder = currentUrl?.UsedPlaceholders?.Where(a => a.Key == ph[0]).FirstOrDefault();
                                                var currentPlaceholderTop = target?.UsedPlaceholders?.Where(a => a.Key == ph[0]).FirstOrDefault();

                                                if (ph[1] == "current")
                                                    val = val.Replace($"{{{currentPlaceholder?.Key}}}", currentPlaceholder?.Value);
                                                else
                                                    val = val.Replace($"{{{currentPlaceholderTop?.Key}}}", currentPlaceholderTop?.Value);
                                            }
                                        }

                                        if (!val.Contains('{'))
                                            element.Add(new XAttribute(elProp.Name ?? "", val));
                                    }
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

                var xws = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    Async = true,
                    Encoding = Encoding.UTF8
                };

                using TextWriter sw = new Utf8StringWriter();
                await sitemap.SaveAsync(sw, SaveOptions.None, stopToken);

                _cacheService.FilesCache.Set("sitemap.xml", sw.ToString() ?? "");


                _storageSingletonService.SitemapCycles++;
                await Task.Delay(_sitemapConfig.CurrentValue.RescanConfigInterval, stopToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sitemap generator thrown error");
            }

            try
            {
                GC.Collect();
            }
            catch
            {

            }
        }
    }
}