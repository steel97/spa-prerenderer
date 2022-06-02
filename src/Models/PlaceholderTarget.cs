namespace SpaPrerenderer.Models;

public class PlaceholderTarget
{
    public string? Url { get; set; }
    public SpaRoute? RouteLink { get; set; }
    public KeyValuePair<string, string>[]? UsedPlaceholders { get; set; }
}