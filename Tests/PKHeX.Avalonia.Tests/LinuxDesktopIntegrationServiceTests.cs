using PKHeX.Application.Abstractions;
using PKHeX.Infrastructure.Desktop;

namespace PKHeX.Avalonia.Tests;

/// <summary>
/// The user-local desktop integration must install the AppImage under the canonical stable name,
/// install icon + desktop entry into the XDG user directories, and clean all three up again.
/// File layout is exercised for real against temp directories; the environment (APPIMAGE/APPDIR,
/// Linux check, refresh helpers) is injected so the tests run identically on every CI platform.
/// </summary>
public class LinuxDesktopIntegrationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pkhex-desktop-integration-{Guid.NewGuid():N}");
    private readonly List<(string File, IReadOnlyList<string> Args)> _commands = [];

    private string DataHome => Path.Combine(_root, "share");
    private string OptDir => Path.Combine(_root, "opt", "PKHeX-Avalonia");
    private string SourceAppImage => Path.Combine(_root, "Downloads", "PKHeX-Avalonia-1.87.3-x86_64.AppImage");
    private string AppDir => Path.Combine(_root, "AppDir");
    private string IconSource => Path.Combine(AppDir, "usr", "share", "icons", "hicolor", "64x64", "apps", "io.pkhex.avalonia.png");

    private string InstalledApp => Path.Combine(OptDir, "PKHeX-Avalonia.AppImage");
    private string DesktopEntry => Path.Combine(DataHome, "applications", "io.pkhex.avalonia.desktop");
    private string InstalledIcon => Path.Combine(DataHome, "icons", "hicolor", "64x64", "apps", "io.pkhex.avalonia.png");

    public LinuxDesktopIntegrationServiceTests()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SourceAppImage)!);
        File.WriteAllBytes(SourceAppImage, [0x7f, 0x45, 0x4c, 0x46, 1, 2, 3, 4, 5, 6, 7, 8]);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best-effort temp cleanup */ }
    }

    private LinuxDesktopIntegrationService CreateService(
        string? source = null, string? appDir = null, bool isLinux = true) =>
        new(source ?? SourceAppImage, appDir ?? AppDir, DataHome, OptDir, isLinux,
            (file, args) => { _commands.Add((file, args)); return 0; });

    [Fact]
    public async Task Register_InstallsStableCopyIconAndDesktopEntry()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IconSource)!);
        await File.WriteAllBytesAsync(IconSource, [1, 2, 3, 4]);

        var service = CreateService();
        Assert.True(service.IsSupported);

        var result = await service.RegisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.Success, result.Outcome);
        Assert.Equal(InstalledApp, result.InstalledPath);
        Assert.True(File.Exists(InstalledApp));
        Assert.True(File.Exists(InstalledIcon));
        Assert.True(File.Exists(DesktopEntry));
        Assert.True(service.IsRegistered);
        Assert.Equal(InstalledApp, service.InstalledAppImagePath);

        // The installed copy is byte-identical to the running AppImage.
        Assert.Equal(await File.ReadAllBytesAsync(SourceAppImage), await File.ReadAllBytesAsync(InstalledApp));

        var entry = await File.ReadAllTextAsync(DesktopEntry);
        Assert.Contains("Type=Application", entry);
        Assert.Contains("Name=PKHeX-Avalonia", entry);
        Assert.Contains($"Exec=\"{InstalledApp}\"", entry);
        Assert.Contains($"TryExec={InstalledApp}", entry);
        Assert.Contains("Icon=io.pkhex.avalonia", entry);
        Assert.Contains("Terminal=false", entry);

        // Desktop databases are refreshed best-effort.
        Assert.Contains(_commands, c => c.File == "update-desktop-database");
    }

    [Fact]
    public async Task Register_WithoutBundledIcon_SucceedsWithoutIconKey()
    {
        var service = CreateService();

        var result = await service.RegisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.Success, result.Outcome);
        Assert.False(File.Exists(InstalledIcon));
        var entry = await File.ReadAllTextAsync(DesktopEntry);
        Assert.DoesNotContain("Icon=", entry);
    }

    [Fact]
    public async Task Register_OverwritesStaleInstalledCopy()
    {
        Directory.CreateDirectory(OptDir);
        await File.WriteAllTextAsync(InstalledApp, "an older installed version");

        var service = CreateService();
        var result = await service.RegisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.Success, result.Outcome);
        Assert.Equal(await File.ReadAllBytesAsync(SourceAppImage), await File.ReadAllBytesAsync(InstalledApp));
    }

    [Fact]
    public async Task Register_LeavesNoStagingFileBehind()
    {
        var service = CreateService();
        await service.RegisterAsync();

        Assert.False(File.Exists(InstalledApp + ".tmp"));
        Assert.False(File.Exists(DesktopEntry + ".tmp"));
    }

    [Fact]
    public async Task Register_NotLinux_ReturnsNotSupported()
    {
        var service = CreateService(isLinux: false);

        var result = await service.RegisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.NotSupported, result.Outcome);
        Assert.False(service.IsSupported);
        Assert.False(File.Exists(InstalledApp));
    }

    [Fact]
    public async Task Register_SourceAppImageMissing_ReturnsNotSupported()
    {
        var service = CreateService(source: Path.Combine(_root, "does-not-exist.AppImage"));

        Assert.False(service.IsSupported);
        var result = await service.RegisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.NotSupported, result.Outcome);
    }

    [Fact]
    public async Task Unregister_RemovesEntryCopyAndIcon()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IconSource)!);
        await File.WriteAllBytesAsync(IconSource, [1, 2, 3, 4]);
        var service = CreateService();
        await service.RegisterAsync();

        var result = await service.UnregisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.Success, result.Outcome);
        Assert.False(File.Exists(DesktopEntry));
        Assert.False(File.Exists(InstalledApp));
        Assert.False(File.Exists(InstalledIcon));
        Assert.False(Directory.Exists(OptDir));
        Assert.False(service.IsRegistered);
        Assert.Null(service.InstalledAppImagePath);

        // The user's downloaded source AppImage is never touched.
        Assert.True(File.Exists(SourceAppImage));
    }

    [Fact]
    public async Task Unregister_WhenNothingInstalled_Succeeds()
    {
        var service = CreateService();

        var result = await service.UnregisterAsync();

        Assert.Equal(DesktopIntegrationOutcome.Success, result.Outcome);
    }
}
