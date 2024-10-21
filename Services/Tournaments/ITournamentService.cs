using othApi.Data.Entities;
namespace othApi.Services.Tournaments;
public interface ITournamentService
{
    Task<List<Tournament>> Get();
    Task<Tournament?> GetById(int id);
    Task<Tournament> PostAsync(Tournament tournament);
    Task<Tournament?> UpdateAsync(Tournament tournament);
    Task<Tournament?> Delete(int id);
    Task<Tournament?> AddTeamMates(List<Player> TeamMates, int tournamentId);
    Task<List<Tournament>> GetByPlayerId(int playerId);
    Task<Tournament?> UpdateTeamMatesAsync(int tournamentId, int[] TeamIds);

    Task<bool> TournamentWithTeamNameExists(string? teamName, string tournamentName);
}