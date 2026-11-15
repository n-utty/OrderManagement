using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain.Common.Exceptions;

namespace OrderManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            var problem = ToProblem(exception);

            if (problem.Status >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = problem.Status!.Value;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception,
            });
        }
    }

    private static ProblemDetails ToProblem(Exception exception) => exception switch
    {
        ValidationException validation => new HttpValidationProblemDetails(GroupByProperty(validation))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more fields are invalid.",
        },

        // Malformed JSON, an unknown enum value or an unparsable route/query value.
        BadHttpRequestException badRequest => new ProblemDetails
        {
            Status = badRequest.StatusCode,
            Title = "The request could not be read.",
        },

        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found.",
            Detail = notFound.Message,
        },

        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order conflicts with an existing order.",
            Detail = conflict.Message,
            Extensions = { ["existingOrderId"] = conflict.ExistingOrderId },
        },

        BusinessRuleException rule => WithExtensions(
            new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "The request breaks a business rule.",
                Detail = rule.Message,
            },
            rule.Details),

        // Nothing from the exception is exposed; the details are in the log.
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        },
    };

    private static Dictionary<string, string[]> GroupByProperty(ValidationException exception) =>
        exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

    private static ProblemDetails WithExtensions(ProblemDetails problem, IReadOnlyDictionary<string, object?> extensions)
    {
        foreach (var (key, value) in extensions)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }
}
