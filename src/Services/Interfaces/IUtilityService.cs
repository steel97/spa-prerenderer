using SpaPrerenderer.Models;

namespace SpaPrerenderer.Services.Interfaces;

public interface IUtilityService
{
    void PreparePlaceholderVariants(string basePattern, ref List<PlaceholderTarget> results, SpaRoute routeLink, string[] placeholderWhitelist, KeyValuePair<string, string>[]? keys = null, int order = -1);
}