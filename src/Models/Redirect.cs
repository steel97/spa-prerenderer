namespace SpaPrerenderer.Models
{
    public class Redirect
    {
        public bool IncludeQueryString { get; set; }
        public RedirectRoute[] RedirectRoutes { get; set; }
    }
}