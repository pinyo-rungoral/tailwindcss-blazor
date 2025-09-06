namespace TailwindCSS.Blazor;

public class TailwindWatcherOptions
{
    public string InputPath { get; set; } = "app.css";
    public string OutputFileName { get; set; } = "app.css";
    public bool EnableInDevelopment { get; set; } = true;
    public bool EnableInProduction { get; set; } = false;
    public string[] AdditionalArguments { get; set; } = Array.Empty<string>();
    public string? ConfigPath { get; set; }
    public bool EnableMinify { get; set; }

    public static string SectionName { get; set; } = "TailwindCSS";
}