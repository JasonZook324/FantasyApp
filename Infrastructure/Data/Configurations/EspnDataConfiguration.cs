using Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class EspnDataConfiguration : IEntityTypeConfiguration<EspnData>
{
    public void Configure(EntityTypeBuilder<EspnData> builder)
    {
        builder.ToTable("EspnData");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.EspnS2)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("espn_s2");

        builder.Property(e => e.SWID)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LeagueId)
            .IsRequired();

        builder.Property(e => e.SeasonId)
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.SeasonId, e.LeagueId })
            .IsUnique();
    }
}
