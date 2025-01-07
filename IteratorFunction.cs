using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Linq;
using System.Text.Json.Serialization;

namespace WhiteCrow
{
  public class OrchestratorInput
  {
    
    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new List<string>();

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;



  }

  public class OrchestratorChuck
  {
    [JsonPropertyName("i")]
    public int Index { get; set; }

    [JsonPropertyName("v")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Instructions { get; set; } = string.Empty;

  }

  public class OrchestratorStatus
  {
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("progress")]
    public float Progress { get; set; } = 0;
  }

  public class IteratorFunction
  {
    private readonly ChatClient client;

    public IteratorFunction(ChatClient client)
    {
      this.client = client;
    }

    [Function(nameof(Iterator_Orchestrator))]
    public async Task<List<string>> Iterator_Orchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
      var logger = context.CreateReplaySafeLogger(nameof(Iterator_Orchestrator));
      var input = context.GetInput<OrchestratorInput>() ?? new OrchestratorInput();

      var outputs = new List<string>();

      for (int i = 0; i < input.Values.Count; i++)
      {
        var chuck = new OrchestratorChuck()
        {
          Index = i,
          Instructions = input.Instructions,
          Value = input.Values[i]
        };
        var result = await context.CallActivityAsync<string>(nameof(Iterator_Work), chuck);
        outputs.Add(result);

        var status = new OrchestratorStatus()
        {
          Index = i,
          Count = input.Values.Count,
          Progress = input.Values.Count == 0 ? 0f : ((float)i / (float)input.Values.Count) * 100f
        };
        context.SetCustomStatus(status);

        var deadline = context.CurrentUtcDateTime.Add(TimeSpan.FromMicroseconds(100));
        await context.CreateTimer(deadline, CancellationToken.None);
      }

      return outputs;
    }

    [Function(nameof(Iterator_Work))]
    public string Iterator_Work([ActivityTrigger] OrchestratorChuck chunk, FunctionContext context)
    {
      var logger = context.GetLogger(nameof(Iterator_Work));

      var messages = new List<ChatMessage>()
      {
        ChatMessage.CreateSystemMessage(chunk.Instructions),
        ChatMessage.CreateUserMessage(chunk.Value)
      };
      ChatCompletion response = this.client.CompleteChat(messages, cancellationToken: context.CancellationToken);
      var result = response.Content.FirstOrDefault()?.Text ?? string.Empty;

      return result;
    }

    [Function(nameof(Iterator_Start))]
    public async Task<HttpResponseData> Iterator_Start([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [DurableClient] DurableTaskClient client, FunctionContext context)
    {
      var logger = context.GetLogger(nameof(Iterator_Start));
      var input = await req.ReadFromJsonAsync<OrchestratorInput>();

      var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Iterator_Orchestrator), input);
      logger.LogInformation("Started orchestration with ID = {instanceId}", instanceId);

      return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
  }
}
