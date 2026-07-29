using FluentValidation;

namespace InterviewSimulator.Api.Features.Common;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not T request)
        {
            return Results.BadRequest(new { error = "Invalid request payload." });
        }

        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
                .ToList();

            return Results.ValidationProblem(errors.ToDictionary(e => e.field, e => new[] { e.error }));
        }

        return await next(context);
    }
}
