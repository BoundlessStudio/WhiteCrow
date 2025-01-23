using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Primitives;
using ThrottlingTroll;

namespace WhiteCrow;

public static class ThrottlingExtension
{
  public static IFunctionsWorkerApplicationBuilder UseDefaultThrottling(this IFunctionsWorkerApplicationBuilder builder)
  {
    var method = new SlidingWindowRateLimitMethod
    {
      PermitLimit = 100,
      IntervalInSeconds = 3600,
      NumOfBuckets = 3
    };

    builder.UseThrottlingTroll(options =>
    {
      options.Config = new ThrottlingTrollConfig
      {
        Rules =
        [
          new ThrottlingTrollRule
          {
            Name = "EnumeratorStart",
            UriPattern = "/api/Enumerator_Start",
            LimitMethod = method,
          },
          new ThrottlingTrollRule
          {
            Name = "ExtractorStart",
            UriPattern = "/api/Extractor_Start",
            LimitMethod = method,
          }
        ]
      };
      options.CostExtractor = request =>
      {
        var httpRequest = ((IIncomingHttpRequestProxy)request).Request;
        if (httpRequest is null)
          return 10;

        httpRequest.Headers.TryGetValue("X-Plan", out StringValues plan);
        switch (plan)
        {
          case "gold":    return 1;
          case "sliver":  return 2;
          case "bronze":  return 5;
          case "free":
          default:        return 10;
        }
      };
      options.IdentityIdExtractor = request =>
      {
        var httpRequest = ((IIncomingHttpRequestProxy)request).Request;
        return httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString();
      };
    });
    return builder;
  }
}
