using Microsoft.Extensions.Options;
using SpaPrerenderer.Models;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services;

public class UtilityService : IUtilityService
{
    private readonly IOptionsMonitor<CommonConfig> _commonConfig;

    public UtilityService(IOptionsMonitor<CommonConfig> commonConfig)
    {
        _commonConfig = commonConfig;
    }

    public void PreparePlaceholderVariants(string basePattern, ref List<PlaceholderTarget> results, SpaRoute routeLink, string[] placeholderWhitelist, KeyValuePair<string, string>[]? keys = null, int order = -1)
    {
        var targetOrder = _commonConfig.CurrentValue?.Placeholders?.Where(a => a.Order > order && (placeholderWhitelist.Contains(a.Key) || placeholderWhitelist.Length == 0)).OrderBy(b => b.Order).FirstOrDefault();
        if (targetOrder == null)
        {
            if (!results.Any(a => a.Url == basePattern))
                results.Add(new PlaceholderTarget
                {
                    Url = basePattern,
                    RouteLink = routeLink,
                    UsedPlaceholders = keys
                });
            return;
        }

        if (_commonConfig.CurrentValue?.Placeholders == null) return;

        foreach (var placeholder in _commonConfig.CurrentValue?.Placeholders!)
        {
            if (!placeholderWhitelist.Contains(placeholder.Key) && placeholderWhitelist.Length > 0) continue;
            if (placeholder.Order != targetOrder.Order) continue;
            if (placeholder.Targets == null) continue;

            foreach (var target in placeholder.Targets)
            {
                var possibleTarget = basePattern.Replace($"{{{placeholder.Key}}}", target);

                var ck = new List<KeyValuePair<string, string>>();
                if (keys != null)
                    ck.AddRange(keys);

                if (possibleTarget != basePattern && !ck.Where(a => a.Key == placeholder.Key).Any())
                    ck.Add(new KeyValuePair<string, string>(placeholder.Key ?? "", target));

                PreparePlaceholderVariants(possibleTarget, ref results, routeLink, placeholderWhitelist, ck.ToArray(), placeholder.Order);
            }
        }
    }
}