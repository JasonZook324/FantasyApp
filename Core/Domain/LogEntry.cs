namespace Core.Domain;

public class LogEntry
{
    public int Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public required string Level { get; set; }
    public required string Message { get; set; }
    public string? Category { get; set; }
    public int? UserId { get; set; }
    public string? Exception { get; set; }
    public string? PropertiesJson { get; set; }

    public User? User { get; set; }
}
