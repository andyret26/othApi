using othApi.Data.Dtos;
using System.Text.Json;
using HtmlAgilityPack;
using OllamaSharp;
using OllamaSharp.Models;

namespace othApi.Services.InfoExtractor;

class InfoExtractor : IInfoExtractor
{
    public async Task<TournamentExtractionDto> ExtractTournamentInfo(string tournamentText)
    {
        try
        {  // 2. Connect to local Ollama instance
            var uri = new Uri("http://localhost:11434");
            var ollamaClient = new OllamaApiClient(uri)
            {
                SelectedModel = "llama3" // Ensure this matches your local model name
            };

            // 3. Draft the prompt enforcing your schema
            string prompt = $"""
            You are a data extraction bot. Analyze the text below and map it into a JSON object.
            You MUST follow this exact JSON schema structure:
            
                ""TournamentName"": ""Name of the tournament or null"",
                ""RankRange"": ""Allowed ranks/skill levels or null"",
                ""Format"": ""Tournament structure/format or null"",
                ""TeamSize"": ""Number of players per team or null"",
                ""MainSheetLink"": ""The full URL to the main spreadsheet/tracking document or null"",
                ""BracketLink"": ""The full URL to the bracket/Challonge/Battlefy page or null""
            

            Rules:
            - If a field is not found in the text, assign it a literal value of null.
            - Respond ONLY with the valid JSON object. Do not include markdown blocks like ```json.
            
            Usually you find this info within the first few sentances of the text, Usually header.
            Examples of titles:
            - [STD] 6 Digit European Cup 2026 | 100k-999k | Player/Staff Reg Open!!!
            - [STD] Makita Powertools Cup [99k - 200k] [team regs CLOSED]
            - [STD] Omoigakenai Nightclub Party // 70k - 250k, 2v2 TS3 // Concluded!
            - 🌍 [STD + Lazer] [Staff/Player REGS CLOSED] | Osu 6-Digit WorldWide Return | #100.000~#999.999
            - [STD] Corsix 4 [4v4 ts8 100k-999k] Registrations closed!
            - [STD] Hanamirai Endurance Cup | 80k-400k | 2v2 TS4 [Regs Closed]

            so title is usuallyb right after [STD] 
            anything that is xVx eg 2v2, 3v3, 4v4 is format, forma could also be "solo"
            TS is stands for Teams size, so TS4 = 4v4, TS8 = 8v8, TS2 = 2v2, TS3 = 3v3
            Rank range is usually 80k-400k, 100k-999k, 70k-250k, 100.000~#999.999, 99k - 200k, 100k-999k, 100k +, open rank

            Text to analyze:
            {tournamentText}
            """;

            var chatRequest = new GenerateRequest 
            { 
                Prompt = prompt,
                Format = "json" // Hardware-forces JSON grammar rules in Ollama
            };

            string fullResponse = "";
            await foreach (var stream in ollamaClient.GenerateAsync(chatRequest))
            {
                fullResponse += stream.Response;
            }

            // 4. Parse it safely back into C# to return standard application/json
            var result = JsonSerializer.Deserialize<TournamentExtractionDto>(fullResponse, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            return result ?? throw new Exception("Failed to deserialize the response into TournamentExtractionDto.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting tournament info: {ex.Message}");
        }
    }

    public async Task<string> GetTournamentText(string url)
    {
        Console.WriteLine($"Extracting tournament text from URL: {url}");
        if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            throw new Exception("A valid absolute URL must be provided.");
        }

        try
        {
            // 1. Fetch and strip out non-content HTML elements
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            string html = await httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//comment()|//nav|//footer");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove) node.Remove();
            }

            string rawText = doc.DocumentNode.InnerText;
            string cleanText = System.Net.WebUtility.HtmlDecode(rawText);
            return System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
        }
        catch (Exception ex)
        {
            // Log your exception here as needed
            throw new Exception($"Somthing went wrong with fething HTML: \n {ex}");
        }
    }
}