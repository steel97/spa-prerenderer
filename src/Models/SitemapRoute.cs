namespace SpaPrerenderer.Models
{
    public class SitemapRoute : Route
    {
        public string ChangeFrequency { get; set; }
        public double Priority { get; set; }
        public string[] WithAlternate { get; set; }
    }
}