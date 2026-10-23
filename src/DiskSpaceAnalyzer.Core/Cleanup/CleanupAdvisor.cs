using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Core.Cleanup;

/// <summary>
/// Compara rutas contra las reglas de limpieza. Solo lee: no toca el disco ni borra nada.
/// La comparación es por ruta completa, nunca por nombre suelto: una carpeta llamada "Temp"
/// dentro de un proyecto no se marca.
/// </summary>
public sealed class CleanupAdvisor : ICleanupClassifier, ICleanupAnalyzer
{
    private readonly IReadOnlyList<CleanupRule> _rules;
    private readonly StringComparison _comparison;

    public CleanupAdvisor(IEnumerable<CleanupRule> rules, StringComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = rules.ToArray();
        _comparison = comparison;
    }

    /// <summary>Windows y macOS no distinguen mayúsculas en las rutas; Linux sí.</summary>
    public static StringComparison DefaultComparison
        => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public CleanupMark? Classify(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        CleanupMark? inside = null;

        foreach (var rule in _rules)
        {
            if (string.Equals(fullPath, rule.Path, _comparison))
            {
                return new CleanupMark(rule, IsRoot: true);
            }

            if (inside is null && IsInside(fullPath, rule.Path))
            {
                inside = new CleanupMark(rule, IsRoot: false);
            }
        }

        return inside;
    }

    public CleanupReport Analyze(DirectoryNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var findings = new List<CleanupFinding>();

        foreach (var rule in _rules)
        {
            var node = FindNode(root, rule.Path);
            if (node is not null)
            {
                findings.Add(new CleanupFinding(rule, node));
            }
        }

        return new CleanupReport(KeepOutermost(findings));
    }

    /// <summary>Si dos hallazgos se solapan (uno dentro de otro, o el mismo), solo cuenta el más externo.</summary>
    private List<CleanupFinding> KeepOutermost(List<CleanupFinding> findings)
    {
        var kept = new List<CleanupFinding>();

        foreach (var finding in findings.OrderBy(f => f.Node.FullPath.Length))
        {
            var path = finding.Node.FullPath;
            var covered = kept.Any(k => string.Equals(path, k.Node.FullPath, _comparison)
                                        || IsInside(path, k.Node.FullPath));
            if (!covered)
            {
                kept.Add(finding);
            }
        }

        return kept;
    }

    /// <summary>Localiza en el árbol el nodo de la ruta de la regla, o <c>null</c> si no fue analizado.</summary>
    private FileSystemNode? FindNode(DirectoryNode root, string rulePath)
    {
        if (string.Equals(root.FullPath, rulePath, _comparison))
        {
            return root;
        }

        // Se analizó una carpeta que está dentro de la ubicación segura: todo el árbol es limpiable.
        if (IsInside(root.FullPath, rulePath))
        {
            return root;
        }

        if (!IsInside(rulePath, root.FullPath))
        {
            return null;
        }

        // La regla cuelga de la raíz analizada: se baja segmento a segmento, sin recorrer todo el árbol.
        var relative = rulePath[root.FullPath.Length..];
        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        FileSystemNode current = root;
        foreach (var segment in segments)
        {
            var next = current.Children.FirstOrDefault(child =>
                child.Kind == NodeKind.Directory && string.Equals(child.Name, segment, _comparison));
            if (next is null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    /// <summary>Verdadero si <paramref name="path"/> está estrictamente dentro de <paramref name="parent"/>.</summary>
    private bool IsInside(string path, string parent)
    {
        var prefix = parent.EndsWith(Path.DirectorySeparatorChar) || parent.EndsWith(Path.AltDirectorySeparatorChar)
            ? parent
            : parent + Path.DirectorySeparatorChar;

        return path.Length > prefix.Length && path.StartsWith(prefix, _comparison);
    }
}
