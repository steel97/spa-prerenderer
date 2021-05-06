using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpaPrerenderer.Configs;

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

            services.Configure<CacheCrawler>(Configuration.GetSection("CacheCrawler"));
            services.Configure<Common>(Configuration.GetSection("Common"));
            services.Configure<SPA>(Configuration.GetSection("SPA"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true
            });

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
