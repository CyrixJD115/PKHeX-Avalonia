using System.Text.RegularExpressions;
using PKHeX.Infrastructure.Updating;

namespace PKHeX.Avalonia.Tests;

/// <summary>
/// The AppImage self-update swap script must land the new file on the canonical stable name
/// (PKHeX-Avalonia.AppImage) in the same directory, delete the old versioned file only after the
/// swap succeeds, and restore it untouched when the swap fails. These tests pin the generated
/// shell script so a regression in that choreography fails the build.
/// </summary>
public class LinuxUpdateStrategyTests
{
    private const string Log = "/tmp/pkhex-update-helper.log";
    private const string NewAppImage = "/tmp/PKHeX-Avalonia-1.87.4-x86_64.AppImage";
    private const string OldVersioned = "/home/user/Downloads/PKHeX-Avalonia-1.87.3-x86_64.AppImage";
    private const string Stable = "/home/user/Downloads/PKHeX-Avalonia.AppImage";

    private static string ScriptFor(string current) =>
        string.Join('\n', LinuxUpdateStrategy.BuildAppImageSwapScript(NewAppImage, current, Log));

    [Fact]
    public void VersionedAppImage_IsRenamedToStableName()
    {
        var script = ScriptFor(OldVersioned);

        Assert.Contains($"mv \"{OldVersioned}\" \"{OldVersioned}.bak\"", script);
        Assert.Contains($"if mv \"{NewAppImage}\" \"{Stable}\"", script);
        Assert.Contains($"chmod +x \"{Stable}\"", script);
        Assert.Contains($"\"{Stable}\" &", script);

        // The old versioned file must not survive a successful swap (it lives on only as the .bak
        // recovery copy, which the success branch removes).
        Assert.Contains($"rm -f \"{OldVersioned}.bak\"", script);
        Assert.DoesNotContain($"rm -f \"{OldVersioned}\"", script);
    }

    [Fact]
    public void VersionedAppImage_FailedSwap_RestoresOriginalPath()
    {
        var script = ScriptFor(OldVersioned);

        Assert.Contains($"mv \"{OldVersioned}.bak\" \"{OldVersioned}\"", script);
        Assert.Contains($"\"{OldVersioned}\" &", script);
    }

    [Fact]
    public void CanonicalAppImage_IsSwappedInPlaceWithoutRename()
    {
        var script = ScriptFor(Stable);

        Assert.Contains($"if mv \"{NewAppImage}\" \"{Stable}\"", script);
        Assert.Contains($"chmod +x \"{Stable}\"", script);
        Assert.Contains($"\"{Stable}\" &", script);
        Assert.Contains($"mv \"{Stable}.bak\" \"{Stable}\"", script);

        // In-place swap only: every mv destination is the canonical path or its .bak recovery —
        // there is never a move to a third name. (Quoted destinations only; the trailing
        // 2>>"$LOG" redirection is not a quoted path.)
        var destinations = Regex.Matches(script, """mv "[^"]+" ("[^"]+")""")
            .Select(m => m.Groups[1].Value.Trim('"'))
            .ToList();
        Assert.True(destinations.Count >= 2, $"expected mv lines, got: {script}");
        Assert.All(destinations, d => Assert.True(d == Stable || d == Stable + ".bak", $"unexpected destination: {d}"));
    }

    [Fact]
    public void Script_WaitsForOwnerProcess_BeforeTouchingFiles()
    {
        var lines = LinuxUpdateStrategy.BuildAppImageSwapScript(NewAppImage, Stable, Log);

        Assert.Equal("#!/bin/sh", lines[0]);
        Assert.Equal("PID=\"$1\"", lines[1]);
        Assert.Contains("while kill -0 \"$PID\" 2>/dev/null; do sleep 0.5; done", lines);
        // No file mutation may precede the wait-for-exit loop.
        var waitIndex = Array.IndexOf(lines, "while kill -0 \"$PID\" 2>/dev/null; do sleep 0.5; done");
        for (var i = 0; i < waitIndex; i++)
            Assert.DoesNotContain("mv ", lines[i]);
    }
}
