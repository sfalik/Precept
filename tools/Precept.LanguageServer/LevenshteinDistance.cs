namespace Precept.LanguageServer;

internal static class LevenshteinDistance
{
    internal static int Compute(string s, string t)
    {
        s = s.ToLowerInvariant();
        t = t.ToLowerInvariant();

        var rows = s.Length + 1;
        var cols = t.Length + 1;
        var distances = new int[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j < cols; j++)
        {
            distances[0, j] = j;
        }

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[rows - 1, cols - 1];
    }
}
