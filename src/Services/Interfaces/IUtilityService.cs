using System.Collections.Generic;
using SpaPrerenderer.Models;

namespace SpaPrerenderer.Services.Interfaces
{
    public interface IUtilityService
    {
        void PreparePlaceholderVariants(string basePattern, ref List<PlaceholderTarget> results, Route routeLink, string[] placeholderWhitelist, KeyValuePair<string, string>[] keys = null, int order = -1);
    }
}