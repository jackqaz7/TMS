using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Services
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client disconnect/request cancellation is not an application fault.
                throw;
            }
            catch (Exception ex)
            {
                await HandleUnexpectedExceptionAsync(context, ex);
            }
        }

        private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                traceId);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "Cannot write ProblemDetails because the response has already started. TraceId: {TraceId}",
                    traceId);
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Unexpected error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. Please contact support with the trace id.",
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = traceId;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
