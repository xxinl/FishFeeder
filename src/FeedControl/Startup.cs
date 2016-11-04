using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FeedControl
{
  public class Startup
  {
    public Startup(IHostingEnvironment env, IOptions<FeedConfig> config)
    {
      var builder = new ConfigurationBuilder()
        .SetBasePath(env.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();
      Configuration = builder.Build();

      using (var db = new DataContext())
      {
        db.Database.Migrate();

        if (!db.Settings.Any(s => s.Key == "FEED_HOUR"))
        {
          db.Settings.Add(new Setting()
          {
            Key = "FEED_HOUR",
            Value = config.Value.FeedHour.ToString()
          });
        }

        if (!db.Settings.Any(s => s.Key == "FEED_NOW"))
        {
          db.Settings.Add(new Setting()
          {
            Key = "FEED_NOW",
            Value = "0"
          });
        }

        if (!db.Settings.Any(s => s.Key == "LAST_PING"))
        {
          db.Settings.Add(new Setting()
          {
            Key = "LAST_PING",
            Value = ""
          });
        }

        db.SaveChanges();
      }
    }

    public IConfigurationRoot Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      // Add framework services.
      services.AddMvc();

      services.AddOptions();
      services.Configure<FeedConfig>(Configuration);
      services.AddMemoryCache();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
      loggerFactory.AddConsole(Configuration.GetSection("Logging"));
      loggerFactory.AddDebug();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
        {
          HotModuleReplacement = true
        });
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
      }

      var provider = new FileExtensionContentTypeProvider();
      provider.Mappings[".py"] = "application/x-msdownload";
      app.UseStaticFiles(new StaticFileOptions()
      {
        ContentTypeProvider = provider
      });

      app.UseMvc(routes =>
      {
        routes.MapRoute(
          name: "default",
          template: "{controller=Home}/{action=Index}/{id?}");

        routes.MapSpaFallbackRoute(
          name: "spa-fallback",
          defaults: new {controller = "Home", action = "Index"});
      });
    }
  }

  public class FeedConfig
  {
    public int FeedHour { get; set; }
  }
}
