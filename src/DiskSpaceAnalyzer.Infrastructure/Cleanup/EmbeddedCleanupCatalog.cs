using System.Reflection;
using System.Text.Json;
using DiskSpaceAnalyzer.Core.Cleanup;

namespace DiskSpaceAnalyzer.Infrastructure.Cleanup;

/// <summary>
/// Carga las reglas de limpieza del sistema operativo actual desde un JSON incrustado en el ensamblado
/// (<c>CleanupRules/windows.json</c>, etc.). Añadir otro sistema es añadir su archivo; no hay que tocar código.
/// </summary>
public sealed class EmbeddedCleanupCatalog : ICleanupCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _platform;
    private readonly Func<string, string> _expandVariables;
    private readonly Func<string, Stream?> _openResource;

    public EmbeddedCleanupCatalog()
        : this(CurrentPlatform, Environment.ExpandEnvironmentVariables)
    {
    }

    /// <param name="platform">Nombre del archivo de reglas sin extensión: "windows", "linux" o "macos".</param>
    /// <param name="expandVariables">Resuelve variables de entorno (<c>%LOCALAPPDATA%</c>). Inyectable para pruebas.</param>
    public EmbeddedCleanupCatalog(string platform, Func<string, string> expandVariables)
        : this(platform, expandVariables, OpenEmbeddedResource)
    {
    }

    /// <param name="openResource">Abre un recurso por nombre; devuelve <c>null</c> si no existe. Permite probar archivos mal formados.</param>
    internal EmbeddedCleanupCatalog(
        string platform,
        Func<string, string> expandVariables,
        Func<string, Stream?> openResource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        ArgumentNullException.ThrowIfNull(expandVariables);
        ArgumentNullException.ThrowIfNull(openResource);

        _platform = platform;
        _expandVariables = expandVariables;
        _openResource = openResource;
    }

    public static string CurrentPlatform
        => OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsMacOS() ? "macos"
            : OperatingSystem.IsLinux() ? "linux"
            : "unknown";

    /// <exception cref="InvalidDataException">El archivo de reglas está mal formado.</exception>
    public IReadOnlyList<CleanupRule> GetRules()
    {
        using var stream = _openResource($"CleanupRules.{_platform}.json");
        if (stream is null)
        {
            return []; // Sistema sin catálogo todavía: no se marca nada.
        }

        var file = JsonSerializer.Deserialize<RulesFile>(stream, JsonOptions)
                   ?? throw new InvalidDataException($"El archivo de reglas '{_platform}' está vacío.");

        if (!string.Equals(file.Platform, _platform, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"El archivo '{_platform}.json' declara la plataforma '{file.Platform}': deben coincidir.");
        }

        var rules = new List<CleanupRule>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in file.Rules ?? [])
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Name)
                || string.IsNullOrWhiteSpace(entry.Description) || string.IsNullOrWhiteSpace(entry.Path))
            {
                throw new InvalidDataException($"Una regla de '{_platform}' tiene campos vacíos (id: '{entry.Id}').");
            }

            if (!ids.Add(entry.Id))
            {
                throw new InvalidDataException($"El id de regla '{entry.Id}' está repetido en '{_platform}'.");
            }

            var path = Resolve(entry.Path);
            if (path is null)
            {
                continue; // La variable de entorno no existe en esta máquina: la regla no aplica.
            }

            rules.Add(new CleanupRule(entry.Id, entry.Name, entry.Description, path));
        }

        return rules;
    }

    private static Stream? OpenEmbeddedResource(string name)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

    /// <returns>La ruta absoluta y normalizada, o <c>null</c> si alguna variable no se pudo resolver.</returns>
    private string? Resolve(string rawPath)
    {
        var expanded = _expandVariables(rawPath);
        if (expanded.Contains('%'))
        {
            return null;
        }

        if (!Path.IsPathRooted(expanded))
        {
            throw new InvalidDataException($"La ruta '{rawPath}' no es absoluta.");
        }

        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(expanded));

        // Una regla sobre una raíz de unidad ("C:\") marcaría el disco entero: es siempre un error.
        if (string.Equals(Path.GetPathRoot(full), full, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"La regla '{rawPath}' apunta a la raíz de una unidad.");
        }

        return full;
    }

    private sealed record RulesFile(string? Platform, List<RuleEntry>? Rules);

    private sealed record RuleEntry(string? Id, string? Name, string? Description, string? Path);
}
