using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

string[] traceSources = [];
string[] meterNames = [];
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;

    options
        .SetResourceBuilder(
        ResourceBuilder
            .CreateDefault()
            .AddService("web")
        )
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri("http://collector:4317/");
        });
});

builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource.AddService("web"))
  .WithTracing(tracing => tracing
    .AddSource(traceSources)
    .AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri("http://collector:4317/");
    })
    .AddAspNetCoreInstrumentation()
  )
  .WithMetrics(metrics => metrics
    .AddMeter(meterNames)
    .AddOtlpExporter(o =>
      {
          o.Endpoint = new Uri("http://collector:4317/");
      }
    )
    .AddAspNetCoreInstrumentation()
  );

builder.Services.AddAkka("your actor system name here", (builder, serviceProvider) =>
{
    builder
      .ConfigureLoggers(logConfig =>
      {
          logConfig.AddLoggerFactory();
      })
      .WithRemoting(hostname: "0.0.0.0", port: 8110, publicHostname: "how can people find you goes here") // hosts for others to find you
      .WithClustering(new ClusterOptions
      {
          Roles = [],
          SeedNodes = ["seed nodes go here"]
      })
    .AddPetabridgeCmd(
      cmd =>
      {
          cmd.RegisterCommandPalette(ClusterCommands.Instance);
          cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
          // cmd.RegisterCommandPalette(FailureInjectionCommands.Instance);
          cmd.RegisterCommandPalette(new RemoteCommands());
      }
    );
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
