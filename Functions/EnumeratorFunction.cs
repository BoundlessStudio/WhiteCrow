using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NPOI.SS.Formula.Functions;
using OpenAI.Chat;
using WhiteCrow.Models;

namespace WhiteCrow.Functions;

// TODO: use singleR for realtime updates on the status

public class EnumeratorFunction
{
  private readonly ChatClient chatClient;

  public EnumeratorFunction(ChatClient client)
  {
    this.chatClient = client;
  }

  [Function(nameof(Enumerator_Run))]
  public async Task<IActionResult> Enumerator_Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
    [BlobInput("extractor/{Id}.json")] Stream input,
    [BlobInput("enumerator/{rand-guid}.json")] BlobClient outputBlob,
    CancellationToken ct
  )
  {
    try
    {
      var body = await request.ReadFromJsonAsync<EnumeratorInput>(ct);
      if (body is null)
        return new BadRequestResult();

      var system = GetSystemMessage(body);

      var collection = BinaryData.FromStream(input).ToObjectFromJson<List<string>>() ?? new List<string>();
      var output = new List<EnumeratorChunk>();
      foreach (var item in collection)
      {
        var messages = new List<ChatMessage>()
        {
          ChatMessage.CreateSystemMessage(system),
          ChatMessage.CreateUserMessage(item)
        };
        ChatCompletion response = await this.chatClient.CompleteChatAsync(messages, cancellationToken: ct);

        var result = response.Content.FirstOrDefault()?.Text ?? string.Empty;
        output.Add(new() { Input = item, Output = result });

        await Task.Delay(100, ct);
      }

      var outputContainer = outputBlob.GetParentBlobContainerClient();
      await outputContainer.CreateIfNotExistsAsync(cancellationToken: ct);
      await outputBlob.UploadAsync(BinaryData.FromObjectAsJson(output), ct);
      var url = outputBlob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(12)).ToString();

      var model = new EnumeratorModel()
      {
        Url = url,
      };
      return new OkObjectResult(model);
    }
    catch (OperationCanceledException ex)
    {
      return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
    }
  }

  private static string GetSystemMessage(EnumeratorInput input)
  {
    var start = "You are a helpful assistant.";
    var end = $"Follow the instructions: {input.Instructions}";
    var total = $"The total count of items for enumatation is {input.Count}.";
    switch (input.Type)
    {
      case "file":
      case "list":
        var list = "Each iteration item is supplied by the user.";
        return $"{start} {total} {list} {end}";
      case "range":
        var range = "Each iteration index is supplied by the user.";
        return $"{start} {total} {range} {end}";
      default:
        return $"Ingore all other instructions and messages; return [ERROR] instead.";
    }
  }
}