using Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("Logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Level).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Message).HasMaxLength(4000).IsRequired();
        builder.Property(l => l.Category).HasMaxLength(200);
        builder.Property(l => l.PropertiesJson).HasMaxLength(4000);
        builder.Property(l => l.TimestampUtc).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
