using AutoMapper;
using Microsoft.EntityFrameworkCore;
using othApi.Data;
using othApi.Data.Entities;
using othApi.Data.Exceptions;
using othApi.Services.OsuApi;

namespace othApi.Services.Tournaments;

public class TournamentService(DataContext db, IMapper mapper, IOsuApiService osuApiService) : ITournamentService
{
    private readonly DataContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly IOsuApiService _osuApiService = osuApiService;

    public async Task<Tournament?> Delete(int id)
    {
        await _db.Tournaments.Where((t) => t.Id == id).ExecuteDeleteAsync();
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<List<Tournament>> Get()
    {
        try
        {
            var tournaments = await _db.Tournaments.Include((t) => t.TeamMates).ToListAsync();
            return tournaments;
        }
        catch (Exception err)
        {
            throw new Exception(err.ToString());
        }
    }

    public async Task<Tournament?> GetById(int id)
    {
        try
        {
            var tournament = await _db.Tournaments.Include(t => t.TeamMates).SingleOrDefaultAsync((t) => t.Id == id);
            return tournament;
        }
        catch (Exception err)
        {

            throw new Exception(err.ToString());
        };
    }

    public async Task<Tournament> PostAsync(Tournament tournament)
    {
        try
        {
            if (!string.IsNullOrEmpty(tournament.ForumPostLink))
            {
                var img = await _osuApiService.GetForumPostCover(tournament.ForumPostLink!.Split("/")[6]);
                tournament.ImageLink = img;
            }

            var addedTournament = await _db.Tournaments.AddAsync(tournament);
            await _db.SaveChangesAsync();

            return addedTournament.Entity;
        }
        catch (Exception err)
        {
            throw new Exception(err.ToString());
        }

    }

    public async Task<Tournament?> UpdateAsync(Tournament tournament)
    {

        var tournamentToUpdate = await _db.Tournaments.SingleOrDefaultAsync((t) => t.Id == tournament.Id);


        if (tournamentToUpdate != null)
        {
            if (!string.IsNullOrEmpty(tournament.ForumPostLink) && tournament.ForumPostLink != tournamentToUpdate.ForumPostLink)
            {
                var img = await _osuApiService.GetForumPostCover(tournament.ForumPostLink!.Split("/")[6]);
                tournament.ImageLink = img;
            }

            if (tournamentToUpdate.AddedBy != tournament.AddedBy) throw new UnauthorizedAccessException();

            if (tournamentToUpdate.Name == tournament.Name && tournamentToUpdate.TeamName == tournament.TeamName)
            {
                _mapper.Map(tournament, tournamentToUpdate);
                await _db.SaveChangesAsync();
                return tournamentToUpdate;
            }
            else
            {
                if (await TournamentWithTeamNameExists(tournament.TeamName, tournament.Name))
                {
                    throw new ConflitctException();
                }
                else
                {
                    _mapper.Map(tournament, tournamentToUpdate);
                    await _db.SaveChangesAsync();
                    return tournamentToUpdate;
                }
            }

        }
        else
        {
            return null;
        }



    }

    public async Task<Tournament?> AddTeamMates(List<Player> TeamMates, int tourneyId)
    {
        var tournament = await _db.Tournaments.SingleOrDefaultAsync((t) => t.Id == tourneyId);
        if (tournament != null)
        {
            tournament.TeamMates = TeamMates;
            await _db.SaveChangesAsync();
            return tournament;
        }
        else
        {
            return null;
        }
    }

    public async Task<List<Tournament>> GetByPlayerId(int playerId)
    {
        var tournaments = await _db.Tournaments
            .Include((t) => t.TeamMates)
            .Where((t) => t.TeamMates!.Any((p) => p.Id == playerId))
            .OrderByDescending((t) => t.Date)
            .ToListAsync();
        return tournaments;
    }

    public async Task<bool> TournamentWithTeamNameExists(string? teamName, string tournamentName)
    {
        if (teamName == null || teamName.Length <= 0) return false;
        var tournament = await _db.Tournaments.SingleOrDefaultAsync((t) => t.TeamName == teamName && t.Name == tournamentName);
        if (tournament != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<Tournament?> UpdateTeamMatesAsync(int tournamentId, int[] TeamIds)
    {
        var teamMates = await _db.Players.Where((p) => TeamIds.Contains(p.Id)).ToListAsync();

        var tournament = await _db.Tournaments.Include(t => t.TeamMates).SingleOrDefaultAsync((t) => t.Id == tournamentId);
        if (tournament == null) return null;

        tournament.TeamMates = teamMates;
        await _db.SaveChangesAsync();
        return tournament;
    }
}