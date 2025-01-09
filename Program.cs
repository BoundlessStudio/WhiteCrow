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
            // Anonymous-10/Day
            new ThrottlingTrollRule
            {
              UriPattern = "/api/Iterator_Start",
              LimitMethod = new SlidingWindowRateLimitMethod
              {
                PermitLimit = 10,
                IntervalInSeconds = 86400,
                NumOfBuckets = 3
              },
            },
            // Authorized-100/Day
            new ThrottlingTrollRule
            {
              UriPattern = "/api/Iterator_Start",
              HeaderName = "Authorization",
              LimitMethod = new SlidingWindowRateLimitMethod
              {
                PermitLimit = 100,
                IntervalInSeconds = 86400,
                NumOfBuckets = 3,
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
