using Octokit;
using SkiaSharp;

#region Variable Declarations

string? token = Environment.GetEnvironmentVariable("GH_TOKEN");
string path = "../../resources";

GitHubClient client;
User user;

Random rnd = new Random();

#endregion

#region Init Client

try
{
    client = new GitHubClient(new ProductHeaderValue("LangStats"))
    {
        Credentials = new Credentials(token)
    };

    user = await client.User.Current();
    Console.WriteLine($"✅ Authenticated as: {user.Login}");
}
catch (AuthorizationException)
{
    Console.WriteLine("❌ Invalid token: Failed to authenticate.");
    return;
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Error verifying credentials: {ex.Message}");
    return;
}

#endregion

#region Consult Info

var allRepos = await client.Repository.GetAllForCurrent();
Dictionary<string, long> languageTotals = new();

foreach (var repo in allRepos)
{
    if (repo.Fork) continue;
    Console.WriteLine("Watching \"" + (repo.Private ? new string('*', rnd.Next(8, 18)) : repo.Name) + "\" repo");
    var languages = await client.Repository.GetAllLanguages(repo.Id);
    foreach (var lang in languages)
    {
        if (languageTotals.ContainsKey(lang.Name))
            languageTotals[lang.Name] += lang.NumberOfBytes;
        else
            languageTotals[lang.Name] = lang.NumberOfBytes;
    }
}

#endregion

#region Sort Info

var sorted = languageTotals.OrderByDescending(x => x.Value).ToArray();
string[] labels = sorted.Select(x => x.Key).ToArray();
float[] values = sorted.Select(x => (float)x.Value).ToArray();

#endregion

#region Check Dir

if (!Directory.Exists(path)) Directory.CreateDirectory(path);

#endregion

#region Create Graph

int width = 1000, margin = 25, marginLeft = labels.Max(x => x.Length) * 10, barHeight = 30, barSpacing = 20, textSize = 16, titleSize = 20, footerSize = 11;
int height = (margin * 2) + (barHeight * values.Length) + (barSpacing * values.Length) + titleSize;

using SKBitmap bitmap = new SKBitmap(width, height);
using SKCanvas canvas = new SKCanvas(bitmap);
canvas.Clear(SKColors.Transparent);

int maxVal = (int)values.Max();

//Candidate colors: BlueViolet, SkyBlue, SlateBlue, SteelBlue
using SKPaint barPaint = new SKPaint { Color = SKColors.Transparent, IsAntialias = true };
using SKPaint borderBarPaint = new SKPaint { Color = SKColors.SlateBlue, IsStroke = true, StrokeWidth = 2 }; 
using SKPaint borderPaint = new SKPaint { Color = SKColors.BlueViolet, IsStroke = true, StrokeWidth = 2 };
using SKPaint textPaint = new SKPaint {Color = SKColors.DarkGray, IsAntialias = true };
using SKFont textFont = new SKFont { Size = textSize };
using SKPaint titlePaint = new SKPaint {Color = SKColors.White, IsAntialias = true };
using SKFont titleFont = new SKFont { Size = titleSize, Embolden = true };
using SKFont footerFont = new SKFont { Size = footerSize, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic) };

canvas.DrawText("Top Languages", width / 2, margin + titleSize, SKTextAlign.Center, titleFont, titlePaint);

for (int i = 0; i < values.Length; i++)
{
    float y = margin + i * (barHeight + barSpacing) + barSpacing + titleSize;

    // Bar width proportional to value
    float barWidth = (width - marginLeft - 3 * margin) * values[i] / (float)maxVal;
    float x = marginLeft;

    // Draw bar
    canvas.DrawRect(x, y, barWidth, barHeight, barPaint);
    canvas.DrawRect(x, y, barWidth, barHeight, borderBarPaint);

    // Draw label (to left of bar)
    canvas.DrawText(labels[i], x - 10, y + barHeight * 0.7f, SKTextAlign.Right, textFont, textPaint);

    // Draw value (to right of bar)
    canvas.DrawText(Math.Round(values[i] * 100 / values.Sum(), 2).ToString() + "%", x + barWidth + 5, y + barHeight * 0.7f, SKTextAlign.Left, textFont, textPaint);
}
canvas.DrawText($"Updated on {DateTime.Now.ToString("dd/MM/yyyy")}", width - 10, height - 10, SKTextAlign.Right, footerFont, textPaint);
canvas.DrawRoundRect(1, 1, width - 2, height - 2, 20, 20, borderPaint);

using var image = SKImage.FromBitmap(bitmap);
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(path + "/languages.png");
data.SaveTo(stream);

Console.WriteLine("Bar chart exported to languages.png");

#endregion