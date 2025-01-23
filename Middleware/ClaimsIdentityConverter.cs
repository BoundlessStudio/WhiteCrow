using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using System.Security.Claims;

namespace WhiteCrow;

public class ClaimsIdentityConverter : IInputConverter
{
  public bool CanConvert(ConverterContext context)
  {
    // Only convert if the target type is ClaimsIdentity
    return context.TargetType == typeof(System.Security.Claims.ClaimsIdentity);
  }

  public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
  {
    // Attempt to retrieve an HTTP context
    var httpContext = context.FunctionContext.GetHttpContext();
    if (httpContext is null)
    {
      // Not an HTTP-triggered function or no HttpContext available.
      // Indicate we don't handle it.
      return ValueTask.FromResult(ConversionResult.Unhandled());
    }

    // Extract or create the ClaimsIdentity
    var user = httpContext.User.Identity ?? new ClaimsIdentity();

    // Return a successful result
    return ValueTask.FromResult(ConversionResult.Success(user));
  }
}
