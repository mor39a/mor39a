using System.Text.Json;

namespace MonkeyTypeAPI
{
    public class PersonalBest
    {
        public double acc { get; set; }
        public double consistency { get; set; }
        public string? difficulty { get; set; }
        public bool lazyMode { get; set; }
        public string? language { get; set; }
        public bool punctuation { get; set; }
        public double raw { get; set; }
        public double wpm { get; set; }
        public bool numbers { get; set; }
        public long timestamp { get; set; }
    }

    public class PersonalBestResponse
    {
        public string? message { get; set; }
        public Dictionary<string, List<PersonalBest>>? data { get; set; }
    }

    class Program
    {
        static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            string? apeKey = Environment.GetEnvironmentVariable("MONKEYTYPE_API_KEY");

            if (string.IsNullOrEmpty(apeKey))
            {
                Console.WriteLine("❌ Error: The environment variable 'MONKEYTYPE_API_KEY' is not defined.");
                return;
            }

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"ApeKey {apeKey}");

            try
            {
                string endpoint = "https://api.monkeytype.com/users/personalBests?mode=time";

                Console.WriteLine("🔄 Requesting personal bests...");

                HttpResponseMessage response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                // Console.WriteLine("📦 Received JSON:");
                // Console.WriteLine(responseBody);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                PersonalBestResponse? data = JsonSerializer.Deserialize<PersonalBestResponse>(responseBody, options);

                if (data?.data != null && data?.data.Count > 0)
                {
                    /*foreach (var time in data?.data!)
                    {
                        var pb = time.Value[0];

                        Console.WriteLine($"\n✅ Personal Best ({time.Key}s):");
                        Console.WriteLine($"WPM: {pb.wpm}");
                        Console.WriteLine($"Accuracy: {pb.acc}%");
                        Console.WriteLine($"Raw: {pb.raw}");
                        Console.WriteLine($"Consistency: {pb.consistency}");
                        Console.WriteLine($"Language: {pb.language}");
                        Console.WriteLine($"Difficulty: {pb.difficulty}");
                        Console.WriteLine($"Timestamp (Unix ms): {pb.timestamp}");
                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(pb.timestamp).ToLocalTime();
                        Console.WriteLine($"DateTimeOffset: {dateTimeOffset}");
                    }*/

                    KeyValuePair<string, PersonalBest>? best = data?.data
                        .SelectMany(kv => kv.Value.Select(obj => new KeyValuePair<string, PersonalBest>(kv.Key, obj)))
                        .OrderByDescending(kvp => kvp.Value.wpm)
                        .FirstOrDefault();

                    if (best.HasValue)
                    {
                        var pb = best.Value.Value;

                        Console.WriteLine($"\n✅ Top Personal Best ({best.Value.Key}s):");
                        Console.WriteLine($"WPM: {pb.wpm}");
                        Console.WriteLine($"Accuracy: {pb.acc}%");
                        Console.WriteLine($"Raw: {pb.raw}");
                        Console.WriteLine($"Consistency: {pb.consistency}");
                        Console.WriteLine($"Language: {pb.language}");
                        Console.WriteLine($"Difficulty: {pb.difficulty}");
                        Console.WriteLine($"Timestamp (Unix ms): {pb.timestamp}");
                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(pb.timestamp).ToUniversalTime();
                        Console.WriteLine($"DateTimeOffset: {dateTimeOffset}");
                        Console.WriteLine($"Date: {dateTimeOffset:dd MMM yyy}");

                        //-----------------------

                        WriteStats($"{best.Value.Key}s {ToTitleCase(pb.language)}", pb.wpm.ToString(), pb.acc.ToString() + "%", pb.raw.ToString(), dateTimeOffset.ToString("d MMM yyyy"));

                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No personal bests found.");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("❌ Error connecting to the API:");
                Console.WriteLine($"Message: {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine("❌ Error processing the JSON:");
                Console.WriteLine($"Message: {e.Message}");
            }
        }

        static void WriteStats(string mode, string wpm, string acc, string raw, string date)
        {
            string path = "../../README.md";
            const string markStart = "<!--Monkeytype-Start-->";
            const string markEnd = "<!--Monkeytype-End-->";
            string[] before;
            string[] after;
            string[] write;
            int tab;
            string[] fullFile;

            if (File.Exists(path))
            {
                fullFile = File.ReadAllLines(path);
                if (fullFile.Any(x => x.Contains(markStart)) && fullFile.Any(x => x.Contains(markEnd)))
                {
                    before = fullFile
                        .TakeWhile(line => line.Trim() != markStart)
                        .ToArray();
                    after = fullFile
                        .SkipWhile(line => line.Trim() != markEnd)
                        .ToArray();
                    tab = fullFile
                        .Where(line => line.Trim() == markStart)
                        .First()
                        .ToString()
                        .IndexOf(markStart);
                    write = [
                        new string(' ', tab) + markStart,
                        new string(' ', tab) + $"| Mode{AddChar(' ', 4, mode.Length)} | WPM{AddChar(' ', 3, wpm.Length)} | Accuracy{AddChar(' ', 8, acc.Length)} | Raw WPM{AddChar(' ', 7, raw.Length)} | Date{AddChar(' ', 4, date.Length)} |",
                        new string(' ', tab) + $"|-{AddChar('-', 4, mode.Length, true)}-|-{AddChar('-', 3, wpm.Length, true)}-|-{AddChar('-', 8, acc.Length, true)}-|-{AddChar('-', 7, raw.Length, true)}-|-{AddChar('-', 4, date.Length, true)}-|",
                        new string(' ', tab) + $"| {mode}{AddChar('-', mode.Length, 4)} | {wpm}{AddChar(' ', wpm.Length, 3)} | {acc}{AddChar(' ', acc.Length, 8)} | {raw}{AddChar(' ', raw.Length, 7)} | {date}{AddChar(' ', date.Length, 4)} |",
                    ];
                    fullFile = before.Concat(write).Concat(after).ToArray();
                    File.WriteAllLines(path, fullFile);
                    Console.WriteLine($"\n✅ Stats updated to {wpm} wpm");
                }
                else
                {
                    Console.WriteLine("⚠️ Warning! Marks Not Found");
                }

                string AddChar(char c, int bas, int con, bool add = false)
                    => new string(c, Cal(bas, con) + (add ? bas : 0));

                int Cal(int bas, int con)
                    => bas > con ? 0 : con - bas;
            }
            else
            {
                Console.WriteLine("❌ Error! File Not Found");
            }
        }

        static string? ToTitleCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            else return char.ToUpper(input[0]) + input[1..].ToLower();
        }
    }
}