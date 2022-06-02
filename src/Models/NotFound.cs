namespace SpaPrerenderer.Models;
public class NotFound
{
    public bool Use404Code { get; set; }
    public bool IncludeQueryString { get; set; }
    public SpaRoute[]? KnownRoutes { get; set; }
}