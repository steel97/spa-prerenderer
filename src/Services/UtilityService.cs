using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using SpaPrerenderer.Configs;
using SpaPrerenderer.Services.Interfaces;

namespace SpaPrerenderer.Services
{
    public class UtilityService : IUtilityService
    {
        private readonly Common _commonConfig;

        public UtilityService(IOptions<Common> commonConfig)
        {
            _commonConfig = commonConfig.Value;
        }

        public void PreparePlaceholderVariants(string basePattern, ref List<string> results, int order = -1)
        {
            var targetOrder = _commonConfig.Placeholders.Where(a => a.Order > order).OrderBy(b => b.Order).FirstOrDefault();
            if (targetOrder == null)
            {
                if (!results.Contains(basePattern))
                    results.Add(basePattern);
                return;
            }
            foreach (var placeholder in _commonConfig.Placeholders)
            {
                if (placeholder.Order != targetOrder.Order) continue;
                foreach (var target in placeholder.Targets)
                {
                    var possibleTarget = basePattern.Replace($"{{{placeholder.Key}}}", target);
                    PreparePlaceholderVariants(possibleTarget, ref results, placeholder.Order);
                }
            }
        }
    }
}