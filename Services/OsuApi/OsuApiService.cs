
using System.Net;
using System.Runtime.Intrinsics.X86;
using AutoMapper;
using Newtonsoft.Json;
using othApi.Data;
using othApi.Data.Entities;
using othApi.Data.Exceptions;
using othApi.Services.Players;

namespace othApi.Services.OsuApi;

class OsuApiService(IMapper mapper, IPlayerService playerService) : IOsuApiService
{
    private readonly IMapper _mapper = mapper;
    private readonly IPlayerService _playerService = playerService;

    public async Task<string> GetToken()
    {
        string apiUrl = "https://osu.ppy.sh/oauth/token";
        string clientId = Environment.GetEnvironmentVariable("OSU_CLIENT_ID")!;
        string clientSecret = Environment.GetEnvironmentVariable("OSU_CLIENT_SECRET")!;
        string grantType = "client_credentials";
        string scope = "public";


        // Prepare the request data
        var requestData = new List<KeyValuePair<string, string>>
        {
            new ("client_id", clientId),
            new ("client_secret", clientSecret),
            new ("grant_type", grantType),
            new ("scope", scope)
        };

        // Create a request content with the form URL-encoded data
        var content = new FormUrlEncodedContent(requestData);

        // Send the POST request
        using (HttpClient http = new())
        {
            var response = await http.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                return responseObj!.Access_token;
            }
            else
            {
                throw new Exception("Request failed with status code: " + response.StatusCode);
            }
        }
    }


    public async Task<Player[]?> GetPlayers(List<int> ids)
    {
        string bearerToken = await GetToken();

        // Check if players already exist in DB
        var playerIdsToAdd = new List<int>();
        foreach (int id in ids)
        {
            if (!await _playerService.Exists(id))
            {
                playerIdsToAdd.Add(id);
            }
        }

        if (playerIdsToAdd.Count == 0)
        {
            return null;
        }
        // DO Request

        var baseUrl = new Uri("https://osu.ppy.sh/api/v2/users");

        var queryParams = new List<KeyValuePair<string, string>>();

        foreach (int id in playerIdsToAdd)
        {
            queryParams.Add(new KeyValuePair<string, string>("ids[]", id.ToString()));
        }

        var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var fullUrl = new Uri($"{baseUrl}?{queryString}");

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

            HttpResponseMessage response = await client.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<ManyPlayersResponseData>(responseBody)!;
                var players = _mapper.Map<Player[]>(respObj.Users);
                return players;
            }
            else
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }
        }
    }

    public async Task<Player> GetPlayerByUsername(string username)
    {
        string bearerToken = await GetToken();

        var url = new Uri($"https://osu.ppy.sh/api/v2/users/{username}/osu?key=username");
        using (HttpClient http = new HttpClient())
        {
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            HttpResponseMessage response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<PlayerResponseData>(responseBody)!;
                if (await _playerService.Exists(respObj.Id))
                {
                    throw new AlreadyExistException();
                }
                else
                {
                    var players = await GetPlayers(new List<int> { respObj.Id });
                    if (players == null)
                    {
                        throw new NotFoundException();
                    }
                    return players[0];
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            else
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }
        }
    }

    public async Task<string> GetForumPostCover(string forumPostId)
    {
        string bearerToken = await GetToken();

        var url = new Uri($"https://osu.ppy.sh/api/v2/forums/topics/{forumPostId}");
        using (HttpClient http = new HttpClient())
        {
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            HttpResponseMessage response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var respObj = JsonConvert.DeserializeObject<TopicResponseObj>(responseBody)!;
                var subStrings = respObj.Posts[0].Body.Raw.Split("img]");
                var stringsWithImg = new List<string>();
                foreach (var s in subStrings)
                {
                    var sStriped = s.TrimEnd("/[".ToCharArray());
                    if (sStriped.Contains("https"))
                    {
                        if (sStriped.Contains("url=")) continue;
                        if (sStriped.Contains("drive.google.com")) {
                            var id = sStriped.Split("id=")[1];
                            stringsWithImg.Add($"https://drive.google.com/thumbnail?id={id}");
                        }

                        if (!(sStriped.Contains(".png") || sStriped.Contains(".jpg") || sStriped.Contains(".jpeg")))
                        {
                            sStriped += ".png";
                        }

                        if (!sStriped.Contains("osuflags"))
                        {
                            stringsWithImg.Add(sStriped);
                            break;
                        }
                    }

                }

                if (stringsWithImg.Count == 0)
                {
                    return null!;
                }

                return stringsWithImg[0];
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine("Forum post not found");
                throw new NotFoundException();
            }
            else
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }
        }
    }

    public async Task<List<Game>> GetMatchGamesAsync(long matchId)
    {
        using HttpClient http = new();

        string bearerToken = await GetToken();

        var url = new Uri($"https://osu.ppy.sh/api/v2/matches/{matchId}");
        http.DefaultRequestHeaders.Add("Accept", "application/json");
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

        HttpResponseMessage response = await http.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var matchInfo = JsonConvert.DeserializeObject<MatchResponseObj>(responseBody);
            List<Game> games = matchInfo!.Events.Where(e => e.Game != null).Select(e => e.Game).ToList()!;
            return games!;
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new MatchNotFoundException(int.Parse(matchId.ToString()));
        }
        else
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }
    }

    public async Task<List<GameV1>> GetMatchGamesV1Async(long matchId)
    {
        var key = Environment.GetEnvironmentVariable("OSU_API_V1_KEY")!;

        var url = new Uri($"https://osu.ppy.sh/api/get_match?mp={matchId}&k={key}");
        using (HttpClient http = new HttpClient())
        {
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpResponseMessage response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var games = JsonConvert.DeserializeObject<MatchResponseObjV1>(responseBody)!.Games.GroupBy(g => g.Beatmap_id).Select(group => group.Last()).ToList();

                return games;

            }
            else
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }
        }
    }

    public async Task<List<Beatmap>> GetBeatmapsAsync(List<int> mapIds)
    {
        using HttpClient http = new();

        string bearerToken = await GetToken();

        var baseUrl = new Uri("https://osu.ppy.sh/api/v2/beatmaps");

        var queryParams = new List<KeyValuePair<string, string>>();
        foreach (int id in mapIds)
        {
            queryParams.Add(new KeyValuePair<string, string>("ids[]", id.ToString()));
        }

        var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var fullUrl = new Uri($"{baseUrl}?{queryString}");


        http.DefaultRequestHeaders.Add("Accept", "application/json");
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

        HttpResponseMessage response = await http.GetAsync(fullUrl);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<BeatmapResponseObj>(responseBody)!.Beatmaps;
        }
        else
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }

    }


    public class TokenResponse
    {
        public string Access_token { get; set; } = null!;
        public string Token_type { get; set; } = null!;
        // Add more properties as needed to match the JSON structure
    }
}