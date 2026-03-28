using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Configurations.Identity;

public sealed class MfaRecoveryCodeConfiguration : IEntityTypeConfiguration<MfaRecoveryCode>
{
    public void Configure(EntityTypeBuilder<MfaRecoveryCode> builder)
    {
        builder.ToTable("MfaRecoveryCodes", "identity");

        builder.HasKey(rc => rc.Id);

        builder.Property(rc => rc.CodeHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(rc => new { rc.UserId, rc.IsUsed })
            .HasDatabaseName("IX_MfaRecoveryCodes_User_Used");
    }
}
