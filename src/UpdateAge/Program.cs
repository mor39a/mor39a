string path = "../../README.md";
string[] fullFile;
int age = DateTime.Now.Year - 2007;

if (File.Exists(path))
{
    fullFile = File.ReadAllLines(path);
    fullFile = fullFile.Select(line => line.Trim().StartsWith("public int age =") ? $"{line.Substring(0, line.IndexOf('=') + 1)} {age};" : line).ToArray();
    File.WriteAllLines(path, fullFile);
    Console.WriteLine($"Age updated to {age}");
}
else
{
    Console.WriteLine("Warning! File Not Found");
}