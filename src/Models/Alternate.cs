namespace SpaPrerenderer.Models;

public class Alternate
{
    public string? Id { get; set; }
    public string? TagName { get; set; }
    public bool WithVariants { get; set; }
    public Prop[]? Props { get; set; }
    public string[]? Variants { get; set; }
}