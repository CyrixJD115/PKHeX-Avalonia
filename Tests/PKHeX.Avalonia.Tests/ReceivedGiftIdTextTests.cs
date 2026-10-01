using Moq;
using PKHeX.Application.UseCases;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class ReceivedGiftIdTextTests
{
    [Fact]
    public void ParseDeduplicatesSortsAndIncludesBoundaryIds()
    {
        Assert.True(ReceivedGiftIdText.TryParse("2047; 0000\n42,42\t7", 2048, out var ids));
        Assert.Equal([0, 7, 42, 2047], ids);
        Assert.True(ReceivedGiftIdText.TryParse(ReceivedGiftIdText.Export(ids), 2048, out var roundtrip));
        Assert.Equal(ids, roundtrip);
    }

    [Theory]
    [InlineData("42,-1")]
    [InlineData("42,2048")]
    [InlineData("42,no")]
    [InlineData("42,999999999999999999")]
    public void InvalidInputHasNoPartialResult(string text)
    {
        Assert.False(ReceivedGiftIdText.TryParse(text, 2048, out var ids));
        Assert.Empty(ids);
    }

    [Fact]
    public void NoOpApplyPreservesFlagZeroAndEditedState()
    {
        var sav = new SAV6XY();
        var flags = (IMysteryGiftFlags)((IMysteryGiftStorageProvider)sav).MysteryGiftStorage;
        flags.SetMysteryGiftReceivedFlag(0, true);
        sav.State.Edited = false;
        var before = sav.Data.ToArray();
        var vm = new MysteryGiftEditorViewModel(sav, new Mock<IDialogService>().Object);
        vm.SaveCommand.Execute(null);
        Assert.True(flags.GetMysteryGiftReceivedFlag(0));
        Assert.Equal(before, sav.Data);
        Assert.False(sav.State.Edited);
    }

    [Fact]
    public void ApplyChangesStagedFlagsAndPreservesIndependentChanges()
    {
        var sav = new SAV6XY();
        var flags = (IMysteryGiftFlags)((IMysteryGiftStorageProvider)sav).MysteryGiftStorage;
        flags.SetMysteryGiftReceivedFlag(42, true);
        var vm = new MysteryGiftEditorViewModel(sav, new Mock<IDialogService>().Object);
        vm.ReceivedFlags.Remove("0042");
        vm.ReceivedFlags.Add("0007");
        flags.SetMysteryGiftReceivedFlag(99, true);
        vm.SaveCommand.Execute(null);
        Assert.False(flags.GetMysteryGiftReceivedFlag(42));
        Assert.True(flags.GetMysteryGiftReceivedFlag(7));
        Assert.True(flags.GetMysteryGiftReceivedFlag(99));
    }

    [Fact]
    public void ResetRestoresReceivedIds()
    {
        var sav = new SAV6XY();
        var flags = (IMysteryGiftFlags)((IMysteryGiftStorageProvider)sav).MysteryGiftStorage;
        flags.SetMysteryGiftReceivedFlag(42, true);
        var vm = new MysteryGiftEditorViewModel(sav, new Mock<IDialogService>().Object);
        vm.ReceivedFlags.Clear();
        vm.ResetCommand.Execute(null);
        Assert.Contains("0042", vm.ReceivedFlags);
    }
}
