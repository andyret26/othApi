using othApi.Data;
using othApi.Data.Entities;

namespace othApi.Services.Players;

public interface IPlayerService
{
    Task<List<Player>> Get();
    Task<List<Player>> GetMinimal();
    Task<Player?> GetByIdAsync(int id);
    Task<string> GetUsernameWithIdAsync(int id);
    public Task<List<Player>> GetMultipleById(List<int> ids);
    Task<Player> PostAsync(Player player);
    Task AddMultipleAsync(List<Player> players);
    Task<Player?> Update(Player player);
    Task<Player?> Delete(int id);
    Task<bool> Exists(int id);
    Task<PlayerStats> GetStats(int id);
    Task UpdateDiscordUsername(int id, string newDiscordUsername);

}