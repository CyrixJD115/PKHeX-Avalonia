using PKHeX.Application.Services;
using PKHeX.Core;
using Xunit.Abstractions;
using PKHeX.Presentation.ViewModels;
using Moq;
using PKHeX.Application.Abstractions;

namespace PKHeX.Avalonia.Tests;

public class SvPokedexSessionTests(ITestOutputHelper output)
{
    private static SAV9SV LoadBase() => Assert.IsType<SAV9SV>(Fixtures.SaveFileFixture.LoadSave(Path.Combine(Fixtures.SaveFileFixture.FindSaveFilesPath()!, "gen9_scarlet.main")));
    [Fact]
    public async Task SeenOnlyConfirmationPreservesCaughtAndStagesUntilApply()
    {
        var save = LoadBase(); save.Blocks.Zukan.DexPaldea.Get(906).SetCaught(false);
        var dialogs = new Mock<IDialogService>(); dialogs.Setup(dialog => dialog.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        using var vm = new PokedexGen9EditorViewModel(save, dialogs.Object); vm.SelectedSpecies = vm.SpeciesList.Single(choice => choice.Value == 906);
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        await vm.SeenAllCommand.ExecuteAsync(null); Assert.False(vm.IsCaught); Assert.True(vm.CanUndo);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        vm.SaveCurrentCommand.Execute(null); Assert.Empty(vm.Error); Assert.False(save.Blocks.Zukan.GetCaught(906)); Assert.True(save.Blocks.Zukan.GetSeen(906));
    }
    [Fact]
    public async Task ConfirmedCaughtCanBeUndoneAndLateAnswersCannotRestoreAfterReset()
    {
        var save = LoadBase(); var answer = new TaskCompletionSource<bool>();
        var dialogs = new Mock<IDialogService>(); dialogs.Setup(dialog => dialog.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(answer.Task);
        using var vm = new PokedexGen9EditorViewModel(save, dialogs.Object);
        var pending = vm.CaughtAllCommand.ExecuteAsync(null); vm.ResetCommand.Execute(null); answer.SetResult(true); await pending; Assert.False(vm.CanUndo);
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        dialogs.Setup(dialog => dialog.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        await vm.CaughtAllCommand.ExecuteAsync(null); vm.UndoCommand.Execute(null); vm.SaveCurrentCommand.Execute(null); Assert.Empty(vm.Error);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
    }
    [Theory] [InlineData("gen9_scarlet.main")] [InlineData("gen9_violet.main")] [InlineData("gen9_violet_indigo_public.main")]
    public void ViewModelDraftsDoNotWriteBeforeApplyAndResetDiscards(string filename)
    {
        var save = Assert.IsType<SAV9SV>(Fixtures.SaveFileFixture.LoadSave(Path.Combine(Fixtures.SaveFileFixture.FindSaveFilesPath()!, filename)));
        output.WriteLine($"{filename}: revision={save.SaveRevision}, mode={save.Blocks.Zukan.GetRevision()}, maxSpecies={save.MaxSpeciesID}");
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        using var vm = new PokedexGen9EditorViewModel(save);
        Assert.Contains(vm.SpeciesList, choice => choice.Value == save.MaxSpeciesID);
        if (vm.UsesDlcFormat) vm.FormStates[0].Seen = !vm.FormStates[0].Seen;
        else vm.IsSeenMale = !vm.IsSeenMale;
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        vm.ResetCommand.Execute(null); vm.SaveCurrentCommand.Execute(null); Assert.Empty(vm.Error);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
    }
    [Theory] [InlineData("gen9_scarlet.main")] [InlineData("gen9_violet.main")] [InlineData("gen9_violet_indigo_public.main")]
    public void CorpusUsesTheActiveFormatAndPreservesNoOpBytes(string filename)
    {
        var save = Assert.IsType<SAV9SV>(Fixtures.SaveFileFixture.LoadSave(Path.Combine(Fixtures.SaveFileFixture.FindSaveFilesPath()!, filename)));
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        var session = new SvPokedexDataSession(save);
        output.WriteLine($"{filename}: revision={save.SaveRevision}, mode={save.Blocks.Zukan.GetRevision()}, maxSpecies={save.MaxSpeciesID}");
        Assert.Equal(save.Blocks.Zukan.GetRevision() != 0, session.UsesDlcFormat);
        Assert.Equal(save.MaxSpeciesID, session.MaxSpecies);
        foreach (ushort species in session.Species())
        {
            var form = session.ReadForm(species, 0); session.WriteForm(species, 0, form.Obtained, form.Seen, form.Heard, form.Viewed);
        }
        Assert.True(session.TryCommit());
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        Assert.IsType<SAV9SV>(SaveUtil.GetSaveFile(save.Write()));
    }
}
