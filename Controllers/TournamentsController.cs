using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using othApi.Data.Dtos;
using othApi.Data.Entities;
using othApi.Data.Exceptions;
using othApi.Services.OsuApi;
using othApi.Services.Players;
using othApi.Services.Tournaments;

namespace othApi.Controllers
{
    [Route("api/v1/tournament")]
    [ApiController]
    [Tags("OTH Tournament")]
    [Produces("application/Json")]
    [Consumes("application/Json")]
    [EnableRateLimiting("fixed")]
    public class TournamentsController(
        IMapper mapper,
        IOsuApiService osuApiService,
        IPlayerService playerService,
        ITournamentService tournamentService,
        ILogger<TournamentsController> logger
    ) : ControllerBase
    {
        private readonly ILogger<TournamentsController> _logger = logger;
        private readonly ITournamentService _tournamentService = tournamentService;
        private readonly IMapper _mapper = mapper;
        private readonly IOsuApiService _osuApiService = osuApiService;
        private readonly IPlayerService _playerService = playerService;

        [HttpGet]
        public async Task<ActionResult<List<TournamentDto>>> GetTournaments()
        {
            var tournaments = await _tournamentService.Get();
            var tournamentDtos = _mapper.Map<List<TournamentDto>>(tournaments);
            return tournamentDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TournamentDto>> GetTournament(int id)
        {
            var tournament = await _tournamentService.GetById(id);
            var tournamentDto = _mapper.Map<TournamentDto>(tournament);
            return tournamentDto;
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TournamentDto>> PostTournament([FromBody] TournamentPostDto tournament)
        {
            try
            {
                if (await _tournamentService.TournamentWithTeamNameExists(tournament.TeamName, tournament.Name))
                {
                    return Conflict(new { title = "Conflict", status = "409", detail = "This Tournament already have a team with this Team Name", });
                }

                var tournamentToPost = _mapper.Map<Tournament>(tournament);
                var addedTournament = await _tournamentService.PostAsync(tournamentToPost);


                if (tournament.TeamMateIds != null)
                {
                    // Check if players exists in db if not add them
                    var playersDoNotExists = await _osuApiService.GetPlayers(tournament.TeamMateIds);
                    if (playersDoNotExists != null)
                    {
                        foreach (var player in playersDoNotExists)
                        {
                            await _playerService.PostAsync(player);
                        }
                    }

                    //  Get Players from db
                    var teamMatesToAdd = await _playerService.GetMultipleById(tournament.TeamMateIds);
                    if (teamMatesToAdd == null)
                    {
                        return NotFound("One or more players do not exist in the database");
                    }
                    var resTournament = await _tournamentService.AddTeamMates(teamMatesToAdd, addedTournament.Id);

                    var tDto = _mapper.Map<TournamentDto>(resTournament);

                    return CreatedAtAction("GetTournament", new { id = tDto.Id }, tDto);
                }
                else
                {
                    var tDto = _mapper.Map<TournamentDto>(addedTournament);

                    return CreatedAtAction("GetTournament", new { id = tDto.Id }, tDto);
                }
            }
            catch (NotFoundException)
            {
                return NotFound(new { title = "NotFound", status = "404", detail = "Forum post not found.", });
            }

        }

        [HttpPut("{id}")]
        [Authorize]
        [EnableRateLimiting("fixed")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> PutTournament(int id, [FromBody] TournamentPutDto tournamentDto)
        {
            var authSub = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            _logger.LogInformation("User with id {id} is trying to update tournament with id {tournamentId}", authSub.Split("|")[2], id);
            if (authSub.Split("|")[2] != tournamentDto.AddedById.ToString())
            {
                return Unauthorized(new { message = "Faild to authorize Update" });
            }


            try
            {
                var updatedTourney1 = await _tournamentService.UpdateAsync(_mapper.Map<Tournament>(tournamentDto));
                var updatedTourney2 = await _tournamentService.UpdateTeamMatesAsync(id, tournamentDto.TeamMateIds);
                if (updatedTourney1 == null || updatedTourney2 == null)
                {
                    return NotFound(new
                    {
                        detail = "One or more players do not exist in the database",
                        type = "NotFound",
                        status = "404",
                    });
                }

            }
            catch (ConflitctException)
            {
                return Conflict(new { title = "Conflict", status = "409", detail = "This Tournament already have a team with this Team Name", });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Faild to authorize Update" });
            }
            return NoContent();
        }

        /**
         * Get Tournaments by player id
         */
        [HttpGet("player/{id}")]
        public async Task<ActionResult<List<TournamentDto>>> GetTournamentsByPlayerId(int id)
        {
            var tournaments = await _tournamentService.GetByPlayerId(id);
            var tournamentDtos = _mapper.Map<List<TournamentDto>>(tournaments);
            return tournamentDtos;

        }
    }
}
