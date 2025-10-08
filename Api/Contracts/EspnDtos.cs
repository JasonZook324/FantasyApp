namespace Api.Contracts;

public record UpsertEspnDataRequest(int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);
public record LeagueNameResponse(string? LeagueName, int LeagueId, int SeasonId);
