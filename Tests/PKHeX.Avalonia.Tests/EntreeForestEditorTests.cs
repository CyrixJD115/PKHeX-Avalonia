using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Threading;
using Moq;
using PKHeX.Application.Abstractions;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;
using PKHeX.Presentation.Localization;

namespace PKHeX.Avalonia.Tests;

public sealed class EntreeForestEditorTests
{
    private static SAV5B2W2 LoadWhite2()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../savefiles/gen5_white2.sav"));
        return Assert.IsType<SAV5B2W2>(FileUtil.GetSupportedFile(path));
    }

    [Fact]
    public void AreaAndSlotSelectionUpdateTheSpritePreview()
    {
        var renderer = new Mock<ISpriteRenderer>();
        var pixels = new byte[] { 1, 2, 3 };
        renderer.Setup(r => r.GetSprite(It.IsAny<ushort>(), It.IsAny<byte>(), It.IsAny<byte>(),
                It.IsAny<uint>(), It.IsAny<bool>(), EntityContext.Gen5))
            .Returns(pixels);
        var vm = new EntralinkEditorViewModel(LoadWhite2(), renderer.Object);
        var slot = Assert.IsType<EntreeSlotViewModel>(vm.SelectedEntreeSlot);
        var changed = new List<string?>();
        slot.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        slot.Species = 25;

        Assert.Equal(pixels, slot.Sprite);
        Assert.Contains(nameof(slot.Sprite), changed);
        Assert.Equal(GameInfo.Strings.Species[25], slot.SpeciesName);

        vm.SelectedArea = vm.Areas[1];
        Assert.Same(vm.Areas[1].Slots[0], vm.SelectedEntreeSlot);
    }

    [Fact]
    public async Task RandomizeRequiresConfirmationAndCanBeUndoneBeforeSave()
    {
        var save = LoadWhite2();
        var originalBytes = save.Data.ToArray();
        var dialogs = new RecordingDialogService();
        var vm = new EntralinkEditorViewModel(save, dialogService: dialogs, random: new Random(17));
        var originalSpecies = vm.SelectedEntreeSlot!.Species;
        var originalUnlocked = vm.UnlockedAreas;

        await vm.RandomizeForestCommand.ExecuteAsync(null);
        Assert.Single(dialogs.Confirmations);
        Assert.Equal(originalSpecies, vm.SelectedEntreeSlot!.Species);
        Assert.False(vm.UndoRandomizeForestCommand.CanExecute(null));

        dialogs.ConfirmResult = true;
        await vm.RandomizeForestCommand.ExecuteAsync(null);
        Assert.True(vm.UndoRandomizeForestCommand.CanExecute(null));
        Assert.NotEqual((ushort)0, vm.SelectedEntreeSlot!.Species);
        Assert.Equal(6, vm.UnlockedAreas);
        Assert.True(vm.Unlock9thArea);
        Assert.Equal(originalBytes, save.Data.ToArray());

        vm.UndoRandomizeForestCommand.Execute(null);
        Assert.Equal(originalSpecies, vm.SelectedEntreeSlot!.Species);
        Assert.Equal(originalUnlocked, vm.UnlockedAreas);
        Assert.False(vm.UndoRandomizeForestCommand.CanExecute(null));
        Assert.Equal(originalBytes, save.Data.ToArray());
        vm.SaveCommand.Execute(null);
        Assert.Equal(originalBytes, save.Data.ToArray());

        vm = new EntralinkEditorViewModel(save, dialogService: dialogs, random: new Random(17));
        await vm.RandomizeForestCommand.ExecuteAsync(null);
        vm.SaveCommand.Execute(null);
        Assert.NotEqual(originalBytes, save.Data.ToArray());
        var reloaded = new SAV5B2W2(save.Write().ToArray());
        var forest = reloaded.EntreeForest;
        forest.StartAccess();
        Assert.Equal(6, forest.Unlock38Areas);
        Assert.True(forest.Unlock9thArea);
        forest.EndAccess();
    }

    [AvaloniaTheory]
    [InlineData("en")]
    [InlineData("de")]
    public void ForestGridAndPreviewRemainReachableAtCompactSize(string language)
    {
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        LocalizedStrings.Instance.SetLanguage(language);
        var save = new SAV5B2W2();
        var renderer = new global::PKHeX.Avalonia.Services.AvaloniaSpriteRenderer(new global::PKHeX.Application.Services.AppSettings());
        renderer.Initialize(save);
        var vm = new EntralinkEditorViewModel(save, renderer);
        vm.SelectedEntreeSlot!.Species = 25;
        var view = new EntralinkEditor
        {
            DataContext = vm,
            Width = 640,
            Height = 550,
        };
        var window = new Window { Content = view, Width = 650, Height = 560 };
        try
        {
            view.FindControl<TabControl>("EntralinkTabs")!.SelectedIndex = 2;
            window.Show();
            for (var i = 0; i < 5; i++)
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
            var grid = Assert.IsType<DataGrid>(view.FindControl<DataGrid>("ForestGrid"));
            var preview = Assert.IsType<Image>(view.FindControl<Image>("ForestPreview"));
            Assert.Equal(6, grid.Columns.Count);
            Assert.Equal("Visible", ScrollViewer.GetHorizontalScrollBarVisibility(grid).ToString());
            Assert.True(preview.Bounds.Width >= 64,
                $"Preview width={preview.Bounds.Width}; visible={preview.IsVisible}; tab={view.FindControl<TabControl>("EntralinkTabs")!.SelectedIndex}");
            Assert.NotNull(((EntralinkEditorViewModel)view.DataContext!).SelectedEntreeSlot);
        }
        finally
        {
            window.Close();
            LocalizedStrings.Instance.SetLanguage(previous);
        }
    }
}
