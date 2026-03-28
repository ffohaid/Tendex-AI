using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for the <see cref="UserInvitation"/> entity.
/// </summary>
public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("UserInvitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.FirstNameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.LastNameAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.FirstNameEn)
            .HasMaxLength(100);

        builder.Property(i => i.LastNameEn)
            .HasMaxLength(100);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.TenantId)
            .IsRequired();

        builder.Property(i => i.InvitedByUserId)
            .IsRequired();

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(i => i.Token)
            .IsUnique()
            .HasDatabaseName("IX_UserInvitations_Token");

        builder.HasIndex(i => new { i.NormalizedEmail, i.TenantId })
            .HasDatabaseName("IX_UserInvitations_NormalizedEmail_TenantId");

        builder.HasIndex(i => new { i.TenantId, i.Status })
            .HasDatabaseName("IX_UserInvitations_TenantId_Status");

        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("IX_UserInvitations_ExpiresAt");

        // Relationships (NoAction to prevent cascade deletes)
        builder.HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.Role)
            .WithMany()
            .HasForeignKey(i => i.RoleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
