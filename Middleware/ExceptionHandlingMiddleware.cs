using Newtonsoft.Json;
using othApi.Services.Discord;

namespace othApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public ExceptionHandlingMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {

            using(var scope = _serviceProvider.CreateScope())
            {


                string fileInfo = string.Empty;
                if (ex.StackTrace != null)
                {
                    // Split the stack trace lines and find the line with file information
                    string[] stackLines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    foreach (string line in stackLines)
                    {
                        if (line.Contains(" in "))
                        {
                            fileInfo = line; // This line contains the file path and line number
                            break;
                        }
                    }
                }


                var disc = scope.ServiceProvider.GetService<IDiscordService>();


                await disc!.SendMessage($"{ex.Message}:\n{fileInfo}");

            }
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("An error occurred while processing your request.");
        }
    }
}
