using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
  private readonly IAuthenticationSchemeProvider _schemeProvider;
  private readonly IAuthenticationHandlerProvider _handlerProvider;

  public JwtAuthenticationMiddleware(
      IAuthenticationSchemeProvider schemeProvider,
      IAuthenticationHandlerProvider handlerProvider)
  {
    _schemeProvider = schemeProvider;
    _handlerProvider = handlerProvider;
  }

  public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
  {
    // We only do this if there's an actual HTTP context (i.e. an HTTP-triggered function).
    var httpContext = context.GetHttpContext();
    if (httpContext is null)
    {
      await next(context);
      return;
    }

    // Check which scheme is set as the "default"
    var defaultScheme = await _schemeProvider.GetDefaultAuthenticateSchemeAsync();
    if (defaultScheme == null)
    {
      await next(context);
      return;
    }

    // Find the authentication handler for this scheme
    var handler = await _handlerProvider.GetHandlerAsync(httpContext, defaultScheme.Name);
    if (handler == null)
    {
      await next(context);
      return;
    }

    // Perform the authentication (JWT Bearer, in our case)
    var result = await handler.AuthenticateAsync();
    if (result.Succeeded && result.Principal != null)
    {
      // Attach the principal to HttpContext.User
      httpContext.User = result.Principal;
    }

    // Continue to next middleware / function
    await next(context);
  }
}