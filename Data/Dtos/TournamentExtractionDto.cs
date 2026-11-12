namespace othApi.Data.Dtos;

public class TournamentExtractionDto
{
    public string? TournamentName { get; set; }
    public string? RankRange { get; set; }
    public string? Format { get; set; }
    public string? TeamSize { get; set; }
    public string? MainSheetLink { get; set; }
    public string? BracketLink { get; set; }
}

public class ForumPostUrl
{
    public string Url { get; set; } = null!;
}