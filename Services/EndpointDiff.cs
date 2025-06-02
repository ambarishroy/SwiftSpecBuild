using SwiftSpecBuild.Models;

namespace SwiftSpecBuild.Services
{
    public static class EndpointDiff
    {
        public static (List<ParsedEndpoint> Added, List<ParsedEndpoint> Removed, List<ParsedEndpoint> Modified) GetEndpointChanges(
            List<ParsedEndpoint> oldList,
            List<ParsedEndpoint> newList)
        {
            var added = newList
                .Where(n => !oldList.Any(o => o.OperationId == n.OperationId))
                .ToList();

            var removed = oldList
                .Where(o => !newList.Any(n => n.OperationId == o.OperationId))
                .ToList();

            var modified = newList
                .Where(n =>
                {
                    var match = oldList.FirstOrDefault(o => o.OperationId == n.OperationId);
                    return match != null && (
                        !AreDictionariesEqual(match.Parameters, n.Parameters) ||
                        !AreDictionariesEqual(match.RequestBody, n.RequestBody) ||
                        !AreDictionariesEqual(match.ResponseBody, n.ResponseBody)
                    );
                })
                .ToList();

            return (added, removed, modified);
        }

        private static bool AreDictionariesEqual(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            if (dict1.Count != dict2.Count) return false;

            foreach (var key in dict1.Keys)
            {
                if (!dict2.ContainsKey(key) || dict2[key] != dict1[key])
                    return false;
            }

            return true;
        }
    }

}
