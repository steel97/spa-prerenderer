using System.Collections.Generic;

namespace SpaPrerenderer.Services.Interfaces
{
    public interface IUtilityService
    {
        void PreparePlaceholderVariants(string basePattern, ref List<string> results, int order = -1);
    }
}