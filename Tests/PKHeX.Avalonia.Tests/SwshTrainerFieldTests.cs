using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class SwshTrainerFieldTests
{
    [Fact]
    public void IdentityCardMapAndDatesRoundtripOnlyAfterApply()
    {
        var save = TrainerScBlockSessionTests.LoadPublicSave(); var before = save.AllBlocks.ToDictionary(b => b.Key, b => b.Data.ToArray());
        using var vm = new Misc8EditorViewModel(save);
        vm.TrainerName = "Tester"; vm.LeagueCardName = "Card"; vm.UniformNumber = "123"; vm.LeagueTrainerId = 456789; vm.RotoRallyScore = 234;
        vm.Bp = 12; vm.Watts = 345; vm.MapId = 0x123456789abcdef0ul; vm.X = 10; vm.Y = -20; vm.Z = 30;
        vm.SX = 1.5; vm.SY = 2; vm.SZ = 0.5; vm.Rotation = 135;
        vm.StartedDate = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        vm.LastSavedDate = vm.StartedDate; vm.LastSavedTime = new TimeSpan(13, 14, 0);
        Assert.True(vm.CanSave); foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.SaveCommand.Execute(null); Assert.False(vm.HasError);
        var reopened = Assert.IsType<SAV8SWSH>(SaveUtil.GetSaveFile(save.Write())); using var check = new Misc8EditorViewModel(reopened);
        Assert.Equal("Tester", check.TrainerName); Assert.Equal("Card", check.LeagueCardName); Assert.Equal("123", check.UniformNumber);
        Assert.Equal(456789, check.LeagueTrainerId); Assert.Equal(234, check.RotoRallyScore); Assert.Equal(12, check.Bp);
        Assert.Equal(0x123456789abcdef0ul, check.MapId); Assert.Equal(-20, check.Y); Assert.Equal(1.5, check.SX); Assert.Equal(135, check.Rotation, 4);
        Assert.Equal(vm.StartedDate, check.StartedDate); Assert.Equal(vm.LastSavedTime, check.LastSavedTime);
        var after = save.AllBlocks.ToDictionary(b => b.Key, b => b.Data.ToArray()); vm.SaveCommand.Execute(null);
        foreach (var b in save.AllBlocks) Assert.Equal(after[b.Key], b.Data.ToArray());
    }
    [Fact]
    public void NoOpAndResetPreserveSourceAndUnrelatedLiveChanges()
    {
        var save = TrainerScBlockSessionTests.LoadPublicSave(); var before = save.AllBlocks.ToDictionary(b => b.Key, b => b.Data.ToArray());
        using var vm = new Misc8EditorViewModel(save); vm.SaveCommand.Execute(null); Assert.False(vm.HasError);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.LeagueCardName = "Discard"; vm.X = 88; vm.ResetCommand.Execute(null); vm.SaveCommand.Execute(null);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.X += 1; save.Money = 123; vm.SaveCommand.Execute(null); Assert.False(vm.HasError); Assert.Equal(123u, save.Money);
    }
}
