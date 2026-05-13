using Precept.Language;

namespace Precept.Pipeline;
internal static partial class TypeChecker
{
    /// <summary>
    /// Structural validation sub-pass: computed-field cycle detection.
    /// Reads <see cref="CheckContext.ComputedDeps"/> (populated during computed expression
    /// resolution) and <see cref="CheckContext.Fields"/>.
    /// </summary>
    private static void ValidateStructural(CheckContext ctx)
    {
        // ── Computed field cycle detection (DFS) ──────────────────────────
        // Build adjacency list from ComputedDeps: fieldName → set of dependent field names.
        // O(n) construction, O(n) DFS traversal.
        if (ctx.ComputedDeps.Count > 0)
        {
            var adjacency = new Dictionary<string, List<string>>(ctx.ComputedDeps.Count);
            foreach (var dep in ctx.ComputedDeps)
            {
                if (!adjacency.TryGetValue(dep.FieldName, out var deps))
                {
                    deps = [];
                    adjacency[dep.FieldName] = deps;
                }
                deps.AddRange(dep.DependsOn);
            }

            // DFS with three-color marking: white (unvisited), gray (in stack), black (done)
            var white = new HashSet<string>(adjacency.Keys);
            var gray = new HashSet<string>();
            var black = new HashSet<string>();

            foreach (var startNode in adjacency.Keys)
            {
                if (!white.Contains(startNode)) continue;
                DetectCycles(startNode, adjacency, white, gray, black, [], ctx);
            }
        }

    }

    /// <summary>
    /// DFS cycle detection helper. Walks the adjacency graph using three-color marking.
    /// On back-edge detection (gray → gray), emits <see cref="DiagnosticCode.CircularComputedField"/>.
    /// </summary>
    private static void DetectCycles(
        string node,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> white,
        HashSet<string> gray,
        HashSet<string> black,
        List<string> path,
        CheckContext ctx)
    {
        white.Remove(node);
        gray.Add(node);
        path.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (gray.Contains(neighbor))
                {
                    // Back edge → cycle. Build cycle description from path.
                    int cycleStart = path.IndexOf(neighbor);
                    var cycle = string.Join(" → ", path.Skip(cycleStart)) + " → " + neighbor;
                    var field = ctx.FieldLookup[neighbor];
                    ctx.Diagnostics.Add(
                        Diagnostics.Create(DiagnosticCode.CircularComputedField, field.Syntax.Span,
                            neighbor, cycle));
                }
                else if (white.Contains(neighbor))
                {
                    DetectCycles(neighbor, adjacency, white, gray, black, path, ctx);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        gray.Remove(node);
        black.Add(node);
    }
}
