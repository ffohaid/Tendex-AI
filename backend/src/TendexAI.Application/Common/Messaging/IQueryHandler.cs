using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Messaging;

/// <summary>
/// Handler for queries that return a typed result.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
