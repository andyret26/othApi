using othApi.Data;
using othApi.Data.Entities;

namespace othApi.Services.OsuApi;

public interface IOsuApiService
{
    Task<string> GetToken();
    Task<Player[]?> GetPlayers(List<int> ids);

    //Get players, nothing else like the other one that does some crazy stuff with adding and checking if they exist, just a simple request to get players by id, if they dont exist return null
    Task<Player[]?> GetPlayers2(List<int> ids);
    Task<Player> GetPlayerByUsername(string username);
    Task<string> GetForumPostCover(string forumPostId);
    Task<List<Game>> GetMatchGamesAsync(long MatchId);
    Task<List<GameV1>> GetMatchGamesV1Async(long matchId);
    Task<List<Beatmap>> GetBeatmapsAsync(List<int> mapIds);

}