using System.Text;
using DiskSpaceAnalyzer.Infrastructure.Cleanup;

namespace DiskSpaceAnalyzer.Tests.Infrastructure;

public class EmbeddedCleanupCatalogTests
{
    private static readonly string Base = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dsa-catalog"));
    private static readonly string FakeLocal = Path.Combine(Base, "Local");
    private static readonly string FakeWindows = Path.Combine(Base, "Windows");

    /// <summary>Resuelve las variables que usa windows.json hacia carpetas inventadas.</summary>
    private static string FakeExpand(string raw)
        => raw.Replace("%LOCALAPPDATA%", FakeLocal).Replace("%SystemRoot%", FakeWindows);

    private static EmbeddedCleanupCatalog FromJson(string json, string platform = "windows")
        => new(platform, FakeExpand, _ => new MemoryStream(Encoding.UTF8.GetBytes(json)));

    private static string OneRule(string path, string id = "r1", string name = "Regla", string description = "Descripción.")
        => $$"""
           { "platform": "windows", "rules": [ { "id": "{{id}}", "name": "{{name}}", "path": "{{path.Replace("\\", "\\\\")}}", "description": "{{description}}" } ] }
           """;

    // ---- El archivo windows.json incrustado ----

    [Fact]
    public void WindowsFile_LoadsAndEveryRuleIsWellFormed()
    {
        var rules = new EmbeddedCleanupCatalog("windows", FakeExpand).GetRules();

        Assert.NotEmpty(rules);
        Assert.All(rules, rule =>
        {
            Assert.True(Path.IsPathRooted(rule.Path), $"{rule.Id}: la ruta no es absoluta");
            Assert.False(string.IsNullOrWhiteSpace(rule.Name));
            Assert.False(string.IsNullOrWhiteSpace(rule.Description));
            Assert.NotEqual(Path.GetPathRoot(rule.Path), rule.Path);
        });
        Assert.Equal(rules.Count, rules.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void WindowsFile_IncludesTheTempAndDxCacheLocations()
    {
        var rules = new EmbeddedCleanupCatalog("windows", FakeExpand).GetRules();

        Assert.Equal(Path.Combine(FakeLocal, "Temp"), rules.Single(r => r.Id == "windows-user-temp").Path);
        Assert.Equal(Path.Combine(FakeLocal, "NVIDIA", "DXCache"), rules.Single(r => r.Id == "nvidia-dxcache").Path);
    }

    [Fact]
    public void WindowsFile_RulesWhoseVariableCannotBeResolved_AreSkipped()
        => Assert.Empty(new EmbeddedCleanupCatalog("windows", raw => raw).GetRules());

    [Fact]
    public void ThisMachine_LoadsItsCatalogWithoutErrors()
    {
        var rules = new EmbeddedCleanupCatalog().GetRules();

        if (OperatingSystem.IsWindows())
        {
            Assert.NotEmpty(rules);
        }
    }

    [Fact]
    public void APlatformWithoutAFile_HasNoRules()
        => Assert.Empty(new EmbeddedCleanupCatalog("amigaos", FakeExpand).GetRules());

    // ---- Validación de archivos mal formados ----

    [Fact]
    public void ARule_IsReadWithItsFieldsAndANormalizedPath()
    {
        var rule = Assert.Single(FromJson(OneRule(Path.Combine(Base, "x") + Path.DirectorySeparatorChar, "r9", "Mi regla", "Algo.")).GetRules());

        Assert.Equal("r9", rule.Id);
        Assert.Equal("Mi regla", rule.Name);
        Assert.Equal("Algo.", rule.Description);
        Assert.Equal(Path.Combine(Base, "x"), rule.Path); // sin separador final
    }

    [Theory]
    [InlineData("", "Nombre", "Desc.")]
    [InlineData("id", "", "Desc.")]
    [InlineData("id", "Nombre", "")]
    public void ARuleWithEmptyFields_IsRejected(string id, string name, string description)
        => Assert.Throws<InvalidDataException>(() => FromJson(OneRule(Path.Combine(Base, "x"), id, name, description)).GetRules());

    [Fact]
    public void ADuplicatedId_IsRejected()
    {
        var json = $$"""
            { "platform": "windows", "rules": [
              { "id": "dup", "name": "A", "path": "{{Path.Combine(Base, "a").Replace("\\", "\\\\")}}", "description": "d" },
              { "id": "dup", "name": "B", "path": "{{Path.Combine(Base, "b").Replace("\\", "\\\\")}}", "description": "d" } ] }
            """;

        Assert.Throws<InvalidDataException>(() => FromJson(json).GetRules());
    }

    [Fact]
    public void ARelativePath_IsRejected()
        => Assert.Throws<InvalidDataException>(() => FromJson(OneRule("carpeta/relativa")).GetRules());

    [Fact]
    public void ADriveRoot_IsRejectedBecauseItWouldMarkTheWholeDisk()
        => Assert.Throws<InvalidDataException>(() => FromJson(OneRule(Path.GetPathRoot(Base)!)).GetRules());

    [Fact]
    public void APlatformThatDoesNotMatchTheFileName_IsRejected()
    {
        var json = OneRule(Path.Combine(Base, "x")).Replace("\"windows\"", "\"linux\"");

        Assert.Throws<InvalidDataException>(() => FromJson(json, platform: "windows").GetRules());
    }
}
