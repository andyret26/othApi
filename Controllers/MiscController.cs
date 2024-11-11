using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using othApi.Data;
using othApi.Data.Dtos;
using othApi.Data.Exceptions;
using othApi.Services.Discord;
using othApi.Services.OsuApi;
using othApi.Services.Players;
using othApi.Utils;

namespace othApi.Controllers;

[Route("api/v1/misc")]
[Consumes("application/Json")]
[Produces("application/Json")]
[EnableRateLimiting("fixed")]
public class MiscController(IOsuApiService osuApiService, IPlayerService playerService, IDiscordService discordService) : ControllerBase
{
    private readonly IOsuApiService _osuApiService = osuApiService;
    private readonly IPlayerService _playerService = playerService;
    private readonly IDiscordService _discordService = discordService;

    [HttpPost("compare-matches-v2")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<Map>>> CompareMatches([FromBody] MiscCompareRequestDto matchInfo)
    {
        try
        {
            var games1 = await _osuApiService.GetMatchGamesAsync(matchInfo.MatchId1);
            var games2 = await _osuApiService.GetMatchGamesAsync(matchInfo.MatchId2);

            games1 = games1.Skip(matchInfo.IgnoreStart1).SkipLast(matchInfo.IgnoreEnd1).Where(e => e.Beatmap != null).ToList();
            games2 = games2.Skip(matchInfo.IgnoreStart2).SkipLast(matchInfo.IgnoreEnd2).Where(e => e.Beatmap != null).ToList();

            var maps = GamesToMapCompare.Compare(games1, games2, matchInfo);
            return Ok(maps);
        }
        catch (MatchNotFoundException e)
        {
            return NotFound(new { title = "NotFound", status = "404", detail = e.Message, });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }


    }


    [HttpPost("compare-matches-v1")]

    public async Task<ActionResult<List<MapV1>>> CompareMatchesV1([FromBody] MiscCompareRequestDto matchInfo)
    {
        // if (matchInfo == null) return BadRequest(new ErrorResponse("BadRequest", 400, "Match info == null"));
        var games1 = await _osuApiService.GetMatchGamesV1Async(matchInfo.MatchId1);
        var games2 = await _osuApiService.GetMatchGamesV1Async(matchInfo.MatchId2);
        var maps = GamesToMapCompare.CompareV1(games1, games2, matchInfo);
        var beatmapIds = maps.Select(m => m.Beatmap_id).ToList();

        var beatmaps = await _osuApiService.GetBeatmapsAsync(beatmapIds);
        foreach (var map in maps)
        {
            var bm = beatmaps.FirstOrDefault(bm => bm.Id == map.Beatmap_id)!;
            map.Title = bm.Beatmapset.Title;
            map.ImgUrl = bm.Beatmapset.Covers.Cover;
            map.SlimcoverUrl = bm.Beatmapset.Covers.Slimcover;

        }
        return Ok(maps);
    }

    [HttpGet("ping")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Ping(){
        var player = await _playerService.GetUsernameWithIdAsync(3191010);
        return Ok();
    }

    [HttpGet("ping-discord")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> PingDiscord(){
        await _discordService.SendMessage($"pinged from OTH server");
        return Ok();
    }
}