using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
      services.AddApplicationInsightsTelemetryWorkerService();
      services.ConfigureFunctionsApplicationInsights();
      services.AddSingleton(new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
    })
    .Build();

host.Run();
