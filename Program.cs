using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenAI.Chat;
using WhiteCrow;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication((builderContext, workerAppBuilder) => {
      workerAppBuilder.UseMiddleware<JwtAuthenticationMiddleware>();
      workerAppBuilder.UseMiddleware<PlanHeaderMiddleware>();
      workerAppBuilder.UseDefaultThrottling();
    })
    .ConfigureServices(services =>
    {
      services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
          options.Authority = $"https://venatiostudios.us.auth0.com";
          options.Audience = "https://iterator.one";
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidIssuer = $"https://venatiostudios.us.auth0.com",
            ValidateAudience = true,
            ValidAudience = "https://iterator.one",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
          };
        });
      services.AddAuthorization();
      services.AddSingleton(new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
      services.AddHttpClient();
    })
    .Build();

host.Run();
