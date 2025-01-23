using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

public class PlanHeaderMiddleware : IFunctionsWorkerMiddleware
{
  public PlanHeaderMiddleware()
  {

  }

  public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
  {
    var httpContext = context.GetHttpContext();
    if (httpContext is null)
    {
      await next(context);
      return;
    }

    if (httpContext.User?.Identity?.IsAuthenticated == true)
    {
      var planClaim = httpContext.User.FindFirst("https://namespace.iterator.one/plan");
      httpContext.Request.Headers["X-Plan"] = planClaim?.Value ?? "free";
    }
    else
    {
      httpContext.Request.Headers["X-Plan"] = "free";
    }

    await next(context);
  }
}
