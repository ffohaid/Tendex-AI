using MediatR;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.ActiveDirectory.Commands;

/// <summary>
/// Command to enable or disable Active Directory integration for a tenant.
/// </summary>
public sealed record ToggleActiveDirectoryCommand(
    Guid TenantId,
    bool IsEnabled) : IRequest<Result>;

/// <summary>
/// Handles toggling AD integration on/off for a tenant.
/// </summary>
public sealed class ToggleActiveDirectoryCommandHandler
    : IRequestHandler<ToggleActiveDirectoryCommand, Result>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public ToggleActiveDirectoryCommandHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(
        ToggleActiveDirectoryCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.GetDbSet<ActiveDirectoryConfiguration>()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        if (config is null)
            return Result.Failure("لم يتم العثور على إعدادات الدليل النشط لهذه الجهة.");

        if (request.IsEnabled)
            config.Enable();
        else
            config.Disable();

        await ((DbContext)_dbContext).SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
