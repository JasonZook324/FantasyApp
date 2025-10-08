namespace Core.Domain;

public class EspnData
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string EspnS2 { get; set; }
    public required string SWID { get; set; }
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }

    public User? User { get; set; }
}
