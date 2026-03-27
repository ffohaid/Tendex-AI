using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Messaging;

/// <summary>
/// Marker interface for commands that do not return a value.
/// Follows the CQRS pattern via MediatR.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Marker interface for commands that return a typed result.
/// </summary>
/// <typeparam name="TResponse">The response type wrapped in a Result.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
