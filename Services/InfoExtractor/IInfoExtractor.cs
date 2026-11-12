using othApi.Data.Dtos;

namespace othApi.Services.InfoExtractor;

public interface IInfoExtractor
{
    Task<string> GetTournamentText(string url);
    Task<TournamentExtractionDto> ExtractTournamentInfo(string tournamentText);
}