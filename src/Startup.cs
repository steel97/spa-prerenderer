using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDetection();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddControllers();

            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IUtilityService, UtilityService>();
            services.AddSingleton<CacheService>();
            services.AddHostedService<CrawlerService>();
            services.AddHostedService<SitemapGeneratorService>();

            services.Configure<CacheCrawlerConfig>(Configuration.GetSection("CacheCrawler"));
            services.Configure<CommonConfig>(Configuration.GetSection("Common"));
            services.Configure<SitemapConfig>(Configuration.GetSection("Sitemap"));
            services.Configure<SPAConfig>(Configuration.GetSection("SPA"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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

        }
    }
}
