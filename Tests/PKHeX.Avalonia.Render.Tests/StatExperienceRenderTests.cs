using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Moq;
using PKHeX.Application.Abstractions;
using PKHeX.Application.Services;
using PKHeX.Avalonia.Services;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Render.Tests;

public class StatExperienceRenderTests
{
    [AvaloniaTheory]
    [InlineData(GameVersion.RD, false)]
    [InlineData(GameVersion.C, true)]
    public async Task FiveDigitStatExperienceFitsCompactStatsTable(GameVersion version, bool dark)
    {
        var application = global::Avalonia.Application.Current!;
        var previousTheme = application.RequestedThemeVariant;
        var previousSources = GameInfo.FilteredSources;
        var save = BlankSaveFile.Get(version);
        var pokemon = Assert.IsAssignableFrom<GBPKM>(save.BlankPKM);
        pokemon.Species = 25;
        pokemon.CurrentLevel = 50;
        pokemon.MaxEVs();
        GameInfo.FilteredSources = new FilteredGameDataSource(save, GameInfo.Sources, false);
        var renderer = new AvaloniaSpriteRenderer(new AppSettings());
        renderer.Initialize(save);
        var vm = new PokemonEditorViewModel(pokemon, save, renderer, Mock.Of<IDialogService>(), Mock.Of<IWindowService>());
        vm.SelectEditorSectionCommand.Execute("1");
        var view = new PokemonEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 360, Height = 700 };
        try
        {
            application.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            window.Show();
            await PKHeX.Testing.HeadlessRenderSettling.Settle(window);
            var names = new[] { vm.HpEvAutomationName, vm.AtkEvAutomationName, vm.DefEvAutomationName,
                vm.SpaEvAutomationName, vm.SpdEvAutomationName, vm.SpeEvAutomationName };
            foreach (var name in names)
            {
                var input = view.GetVisualDescendants().OfType<NumericUpDown>()
                    .Single(control => AutomationProperties.GetName(control) == name);
                Assert.Equal(65535m, input.Value);
                var presenter = input.GetVisualDescendants().OfType<TextPresenter>().Single();
                Assert.Equal("65535", presenter.Text);
                Assert.True(presenter.TextLayout.Width <= presenter.Bounds.Width,
                    $"Five-digit text needs {presenter.TextLayout.Width}px; input has {presenter.Bounds.Width}px.");
                var position = input.TranslatePoint(default, window)!.Value;
                Assert.InRange(position.X + input.Bounds.Width, 0, window.ClientSize.Width);
            }
            Assert.Equal(327675, vm.EVTotal);
            using var frame = new RenderTargetBitmap(new PixelSize(360, 700));
            frame.Render(window);
            if (Environment.GetEnvironmentVariable("PKHEX_HEADLESS_CAPTURE_DIR") is { } directory)
            {
                Directory.CreateDirectory(directory);
                frame.Save(Path.Combine(directory, $"stat-experience-{version}-{(dark ? "dark" : "light")}.png"));
            }
        }
        finally
        {
            window.Close();
            application.RequestedThemeVariant = previousTheme;
            GameInfo.FilteredSources = previousSources;
        }
    }
}
