using Microsoft.AspNetCore.Rewrite;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services;
using SpaPrerenderer.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDetection();
builder.Services.AddControllers();

builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddSingleton<IUtilityService, UtilityService>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<StorageSingletonService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<CrawlerService>();
builder.Services.AddHostedService<SitemapGeneratorService>();

builder.Services.Configure<CacheCrawlerConfig>(builder.Configuration.GetSection("CacheCrawler"));
builder.Services.Configure<CommonConfig>(builder.Configuration.GetSection("Common"));
builder.Services.Configure<SitemapConfig>(builder.Configuration.GetSection("Sitemap"));
builder.Services.Configure<SPAConfig>(builder.Configuration.GetSection("SPA"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

var sitemapProxy = app.Configuration.GetSection("Sitemap:SitemapProxy").Get<SitemapProxy?>();

if (sitemapProxy != null && sitemapProxy.From != null && sitemapProxy.To != null)
{
    Console.WriteLine(sitemapProxy.From);
    app.UseRewriter(new RewriteOptions()
        .AddRewrite(sitemapProxy.From, sitemapProxy.To, skipRemainingRules: false));
}

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true
});


app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "SPA",
        pattern: "{*url}",
        defaults: new
        {
            controller = "SPA",
            action = "Index"
        });
});

app.Run();