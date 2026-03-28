using FluentValidation;
using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates commands/queries
/// using FluentValidation before they reach their handlers.
/// Returns a failure Result instead of throwing exceptions for validation errors.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type (must be a Result).</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Create the appropriate failure Result based on TResponse type
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errorMessage);
        }

        // For Result<T>, use reflection to call the generic Failure method
        var resultType = typeof(TResponse);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = resultType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(string)])!
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
        }

        return await next();
    }
}
