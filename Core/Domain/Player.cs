namespace Core.Domain;

public class Player
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Position { get; set; }
    public required string Status { get; set; }
    public required string InjuryDesignation { get; set; }
}
