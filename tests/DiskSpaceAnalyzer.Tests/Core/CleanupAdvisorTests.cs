using DiskSpaceAnalyzer.Core.Cleanup;
using DiskSpaceAnalyzer.Core.Models;

namespace DiskSpaceAnalyzer.Tests.Core;

public class CleanupAdvisorTests
{
    private static readonly string Base = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dsa-cleanup"));
    private static readonly string Local = Path.Combine(Base, "Users", "kire", "AppData", "Local");
    private static readonly string Temp = Path.Combine(Local, "Temp");
    private static readonly string DxCache = Path.Combine(Local, "NVIDIA", "DXCache");

    private static CleanupRule Rule(string id, string path) => new(id, id, $"Descripción de {id}.", path);

    private static CleanupAdvisor Advisor(StringComparison comparison, params CleanupRule[] rules) => new(rules, comparison);

    private static DirectoryNode Dir(string path, params FileSystemNode[] children)
        => new(Path.GetFileName(path) is { Length: > 0 } name ? name : path, path, children);

    private static FileNode File(string path, long size) => new(Path.GetFileName(path), path, size);

    // ---- Classify ----

    [Fact]
    public void Classify_TheExactPath_IsTheRoot()
    {
        var mark = Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Temp);

        Assert.NotNull(mark);
        Assert.True(mark.IsRoot);
        Assert.Equal("temp", mark.Rule.Id);
    }

    [Fact]
    public void Classify_ADescendant_IsInsideButNotTheRoot()
    {
        var mark = Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Path.Combine(Temp, "a", "b"));

        Assert.NotNull(mark);
        Assert.False(mark.IsRoot);
    }

    [Fact]
    public void Classify_ASiblingSharingTheNamePrefix_IsNotInside()
        => Assert.Null(Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Temp + "2"));

    [Fact]
    public void Classify_ASameNamedFolderElsewhere_IsNotMarked()
        => Assert.Null(Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Path.Combine(Base, "Projects", "Temp")));

    [Fact]
    public void Classify_TheParentOfTheSafeLocation_IsNotMarked()
        => Assert.Null(Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Local));

    [Fact]
    public void Classify_IgnoringCase_MatchesDifferentCasing()
        => Assert.NotNull(Advisor(StringComparison.OrdinalIgnoreCase, Rule("temp", Temp)).Classify(Temp.ToUpperInvariant()));

    [Fact]
    public void Classify_CaseSensitive_DoesNotMatchDifferentCasing()
        => Assert.Null(Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(Temp.ToUpperInvariant()));

    [Fact]
    public void Classify_WhenNestedRulesExist_PrefersTheExactMatch()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("local", Local), Rule("temp", Temp));

        var mark = advisor.Classify(Temp);

        Assert.Equal("temp", mark?.Rule.Id);
        Assert.True(mark?.IsRoot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_BlankPaths_AreNotMarked(string path)
        => Assert.Null(Advisor(StringComparison.Ordinal, Rule("temp", Temp)).Classify(path));

    // ---- Analyze ----

    [Fact]
    public void Analyze_FindsEachSafeLocationBelowTheScanRootAndSumsThem()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp), Rule("dx", DxCache));
        var user = Path.Combine(Base, "Users", "kire");
        var tree = Dir(user,
            Dir(Path.Combine(user, "AppData"),
                Dir(Local,
                    Dir(Temp, File(Path.Combine(Temp, "a.tmp"), 100)),
                    Dir(Path.Combine(Local, "NVIDIA"),
                        Dir(DxCache, File(Path.Combine(DxCache, "s.bin"), 50))),
                    Dir(Path.Combine(Local, "Other"), File(Path.Combine(Local, "Other", "x"), 999)))));

        var report = advisor.Analyze(tree);

        Assert.Equal(["dx", "temp"], report.Findings.Select(f => f.Rule.Id).OrderBy(id => id));
        Assert.Equal(150, report.ReclaimableBytes);
    }

    [Fact]
    public void Analyze_WhenSafeLocationsOverlap_OnlyTheOutermostCounts()
    {
        var nvidia = Path.Combine(Local, "NVIDIA");
        var advisor = Advisor(StringComparison.Ordinal, Rule("nvidia", nvidia), Rule("dx", DxCache));
        var tree = Dir(Local,
            Dir(nvidia, Dir(DxCache, File(Path.Combine(DxCache, "s.bin"), 70)), File(Path.Combine(nvidia, "f"), 30)));

        var report = advisor.Analyze(tree);

        var finding = Assert.Single(report.Findings);
        Assert.Equal("nvidia", finding.Rule.Id);
        Assert.Equal(100, report.ReclaimableBytes);
    }

    [Fact]
    public void Analyze_WhenTheLocationWasNotScanned_FindsNothing()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp));
        var tree = Dir(Path.Combine(Base, "Documents"), File(Path.Combine(Base, "Documents", "a"), 10));

        var report = advisor.Analyze(tree);

        Assert.Empty(report.Findings);
        Assert.Equal(0, report.ReclaimableBytes);
    }

    [Fact]
    public void Analyze_WhenTheScanRootIsTheSafeLocation_ReportsTheWholeTree()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp));
        var tree = Dir(Temp, File(Path.Combine(Temp, "a"), 40));

        var finding = Assert.Single(advisor.Analyze(tree).Findings);

        Assert.Same(tree, finding.Node);
    }

    [Fact]
    public void Analyze_WhenTheScanRootIsInsideTheSafeLocation_ReportsTheWholeTree()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp));
        var inner = Path.Combine(Temp, "cache");
        var tree = Dir(inner, File(Path.Combine(inner, "a"), 40));

        var finding = Assert.Single(advisor.Analyze(tree).Findings);

        Assert.Same(tree, finding.Node);
    }

    [Fact]
    public void Analyze_ADirectoryNamedLikeTheRuleButMissingTheRestOfThePath_IsNotAFinding()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp));
        var user = Path.Combine(Base, "Users", "kire");
        var tree = Dir(user, Dir(Path.Combine(user, "Temp"), File(Path.Combine(user, "Temp", "a"), 10)));

        Assert.Empty(advisor.Analyze(tree).Findings);
    }

    [Fact]
    public void Analyze_MatchesDirectoryNamesIgnoringCaseWhenAsked()
    {
        var advisor = Advisor(StringComparison.OrdinalIgnoreCase, Rule("temp", Temp));
        var lowerTemp = Path.Combine(Local, "temp");
        var tree = Dir(Local, Dir(lowerTemp, File(Path.Combine(lowerTemp, "a"), 5)));

        Assert.Single(advisor.Analyze(tree).Findings);
    }

    [Fact]
    public void Analyze_ScanRootAtADriveRoot_WalksDownWithoutTrailingSeparatorProblems()
    {
        var driveRoot = Path.GetPathRoot(Base)!; // por ejemplo "C:\"
        var a = Path.Combine(driveRoot, "dsa-a");
        var b = Path.Combine(a, "dsa-b");
        var advisor = Advisor(StringComparison.Ordinal, Rule("b", b));
        var tree = Dir(driveRoot, Dir(a, Dir(b, File(Path.Combine(b, "f"), 25))));

        var finding = Assert.Single(advisor.Analyze(tree).Findings);

        Assert.Equal(b, finding.Node.FullPath);
    }

    [Fact]
    public void Analyze_IgnoresFilesThatShareTheNameOfARuleSegment()
    {
        var advisor = Advisor(StringComparison.Ordinal, Rule("temp", Temp));
        var tree = Dir(Local, File(Temp, 10)); // "Temp" es un archivo, no una carpeta

        Assert.Empty(advisor.Analyze(tree).Findings);
    }
}
