using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Messaging;

/// <summary>
/// Marker interface for queries that return a typed result.
/// Follows the CQRS pattern via MediatR.
/// </summary>
/// <typeparam name="TResponse">The response type wrapped in a Result.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
