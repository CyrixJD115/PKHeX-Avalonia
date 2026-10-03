using PKHeX.Application.Services;
using PKHeX.Core;
using Xunit.Abstractions;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class SvPokedexSessionTests(ITestOutputHelper output)
{
    [Theory] [InlineData("gen9_scarlet.main")] [InlineData("gen9_violet.main")]
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
    [Theory] [InlineData("gen9_scarlet.main")] [InlineData("gen9_violet.main")]
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
