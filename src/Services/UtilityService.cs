using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using SpaPrerenderer.Models;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services
{
    public class UtilityService : IUtilityService
    {
        private readonly CommonConfig _commonConfig;

        public UtilityService(IOptions<CommonConfig> commonConfig)
        {
            _commonConfig = commonConfig.Value;
        }

        public void PreparePlaceholderVariants(string basePattern, ref List<PlaceholderTarget> results, Route routeLink, string[] placeholderWhitelist, KeyValuePair<string, string>[] keys = null, int order = -1)
        {
            var targetOrder = _commonConfig.Placeholders.Where(a => a.Order > order && (placeholderWhitelist.Contains(a.Key) || placeholderWhitelist.Count() == 0)).OrderBy(b => b.Order).FirstOrDefault();
            if (targetOrder == null)
            {
                if (results.Count(a => a.Url == basePattern) == 0)
                    results.Add(new PlaceholderTarget
                    {
                        Url = basePattern,
                        RouteLink = routeLink,
                        UsedPlaceholders = keys
                    });
                return;
            }
            foreach (var placeholder in _commonConfig.Placeholders)
            {
                if (!placeholderWhitelist.Contains(placeholder.Key) && placeholderWhitelist.Count() > 0) continue;
                if (placeholder.Order != targetOrder.Order) continue;

                foreach (var target in placeholder.Targets)
                {
                    var possibleTarget = basePattern.Replace($"{{{placeholder.Key}}}", target);

                    var ck = new List<KeyValuePair<string, string>>();
                    if (keys != null)
                        ck.AddRange(keys);

                    if (possibleTarget != basePattern && ck.Where(a => a.Key == placeholder.Key).Count() == 0)
                        ck.Add(new KeyValuePair<string, string>(placeholder.Key, target));

                    PreparePlaceholderVariants(possibleTarget, ref results, routeLink, placeholderWhitelist, ck.ToArray(), placeholder.Order);
                }
            }
        }
    }
}