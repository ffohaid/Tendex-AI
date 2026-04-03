using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Committees;

/// <summary>
/// EF Core configuration for the Committee entity.
/// Maps to the committees.Committees table in the tenant database.
/// </summary>
public sealed class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> builder)
    {
        builder.ToTable("Committees", "committees");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.IsPermanent)
            .IsRequired();

        builder.Property(c => c.ScopeType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(CommitteeScopeType.Comprehensive);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.StartDate)
            .IsRequired();

        builder.Property(c => c.EndDate)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.ActiveFromPhase)
            .HasConversion<int?>();

        builder.Property(c => c.ActiveToPhase)
            .HasConversion<int?>();

        builder.Property(c => c.StatusChangeReason)
            .HasMaxLength(2000);

        builder.Property(c => c.StatusChangedBy)
            .HasMaxLength(450);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(450);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Committees_TenantId");

        builder.HasIndex(c => new { c.TenantId, c.Type })
            .HasDatabaseName("IX_Committees_TenantId_Type");

        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasDatabaseName("IX_Committees_TenantId_Status");

        builder.HasIndex(c => new { c.TenantId, c.IsPermanent })
            .HasDatabaseName("IX_Committees_TenantId_IsPermanent");

        builder.HasIndex(c => new { c.TenantId, c.ScopeType })
            .HasDatabaseName("IX_Committees_TenantId_ScopeType");

        // ----- Relationships -----
        builder.HasMany(c => c.Members)
            .WithOne(m => m.Committee)
            .HasForeignKey(m => m.CommitteeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.Competitions)
            .WithOne(cc => cc.Committee)
            .HasForeignKey(cc => cc.CommitteeId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- Backing fields -----
        builder.Navigation(c => c.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.Competitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore domain events collection
        builder.Ignore(c => c.DomainEvents);
    }
}
