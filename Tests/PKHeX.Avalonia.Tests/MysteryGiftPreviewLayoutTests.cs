using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class MysteryGiftPreviewLayoutTests
{
    [AvaloniaTheory]
    [InlineData(GameVersion.W2)]
    [InlineData(GameVersion.X)]
    [InlineData(GameVersion.SN)]
    [InlineData(GameVersion.GP)]
    public void AlbumPreviewRendersWithoutMutatingSave(GameVersion version)
    {
        using var app = new HeadlessAppFixture();
        var save = BlankSaveFile.Get(version);
        var storage = ((IMysteryGiftStorageProvider)save).MysteryGiftStorage;
        var gift = storage.GetMysteryGift(0);
        gift.IsEntity = true;
        gift.Species = 25;
        gift.CardID = 123;
        storage.SetMysteryGift(0, gift);
        save.State.Edited = false;
        var before = save.Data.ToArray();
        app.LoadSaveInstance(save);
        var vm = Assert.IsType<MysteryGiftEditorViewModel>(app.ViewModel.MysteryGiftEditor);
        var view = new MysteryGiftEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 700, Height = 600 };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(vm.SelectedGift!.PreviewRows);
            Assert.NotNull(vm.SelectedGift.Sprite);
            Assert.True(view.FindControl<ScrollViewer>("CardPreviewScroll")!.Bounds.Height > 100);
            Assert.Equal(before, save.Data.ToArray());
            Assert.False(save.State.Edited);
        }
        finally { window.Close(); }
    }
}
