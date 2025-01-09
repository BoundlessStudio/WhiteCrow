using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using ThrottlingTroll;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication((builderContext, workerAppBuilder) => {
      workerAppBuilder.UseThrottlingTroll(options => {
        options.Config = new ThrottlingTrollConfig
        {
          Rules =
          [
            new ThrottlingTrollRule
            {
              UriPattern = "/api/Iterator_Start",
              LimitMethod = new FixedWindowRateLimitMethod
              {
                PermitLimit = 10,
                IntervalInSeconds = 3600,
              }
            }
          ]
        };
        options.IdentityIdExtractor = request =>
        {
          var httpRequest = ((IIncomingHttpRequestProxy)request).Request;
          return httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString();
        };
      });
    })
    .ConfigureServices(services =>
    {
      //services.AddApplicationInsightsTelemetryWorkerService();
      //services.ConfigureFunctionsApplicationInsights();
      services.AddSingleton(new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
    })
    .Build();

host.Run();
