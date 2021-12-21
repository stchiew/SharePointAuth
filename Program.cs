using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PnP.Core.Services;
using System.Threading.Tasks;

namespace SharePointAuth
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      Console.WriteLine("Hello World!");

      var host = Host.CreateDefaultBuilder()
      .ConfigureLogging((hostingContext, logging) =>
      {
        logging.AddEventSourceLogger();
        logging.AddConsole();
      })
      .ConfigureServices((hostingContext, services) =>
      {
        var customSettings = new CustomSettings();
        hostingContext.Configuration.Bind("CustomSettings", customSettings);
        services.AddPnPCore(options =>
        {
          options.PnPContext.GraphFirst = true;
          options.Sites.Add("DemoSite",
            new PnP.Core.Services.Builder.Configuration.PnPCoreSiteOptions
            {
              SiteUrl = customSettings.DemoSiteUrl
            });
        });
        services.AddPnPCoreAuthentication(options =>
        {
          options.Credentials.Configurations.Add("interactive",
            new PnP.Core.Auth.Services.Builder.Configuration.PnPCoreAuthenticationCredentialConfigurationOptions
            {
              ClientId = customSettings.ClientId,
              TenantId = customSettings.TenantId,
              Interactive = new PnP.Core.Auth.Services.Builder.Configuration.PnPCoreAuthenticationInteractiveOptions
              {
                RedirectUri = customSettings.RedirectUri
              }
            });

          options.Credentials.Configurations.Add("credentials",
            new PnP.Core.Auth.Services.Builder.Configuration.PnPCoreAuthenticationCredentialConfigurationOptions
            {
              ClientId = customSettings.ClientId,
              TenantId = customSettings.TenantId,
              Interactive = new PnP.Core.Auth.Services.Builder.Configuration.PnPCoreAuthenticationInteractiveOptions
              {
                RedirectUri = customSettings.RedirectUri
              }
            });

          options.Credentials.DefaultConfiguration = "interactive";
          // Map the site defined in AddPnPCore with the 
          // Authentication Provider configured in this action
          options.Sites.Add("DemoSite",
            new PnP.Core.Auth.Services.Builder.Configuration.PnPCoreAuthenticationSiteOptions
            {
              AuthenticationProviderName = "interactive"
            });
        });
      })
      //Let the builder know we're running in a console
      .UseConsoleLifetime()
      // Add services to the container
      .Build();

      await host.StartAsync();

      using (var scope = host.Services.CreateScope())
      {
        var pnpContextFactory = scope.ServiceProvider.GetRequiredService<IPnPContextFactory>();
        using (var context = await pnpContextFactory.CreateAsync("DemoSite"))
        {
          var web = await context.Web.GetAsync(p => p.Title, p => p.Lists);
          Console.WriteLine($"Title: {web.Title}");
          Console.WriteLine($"# Lists: {web.Lists.Length}");
        }
      }

      host.Dispose();
    }

  }
}
