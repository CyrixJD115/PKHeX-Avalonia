using PKHeX.Core;
using PKHeX.Presentation.Localization;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class PlaPokedexFoundationTests
{
    private static SAV8LA LoadSave() => Assert.IsType<SAV8LA>(Fixtures.SaveFileFixture.LoadSave(
        Path.Combine(Fixtures.SaveFileFixture.FindSaveFilesPath()!, "gen8a_legendsarceus.main")));
    [Fact]
    public void SearchClearsAndRowletUsesBaseFormAndCoreDelta()
    {
        var save = LoadSave(); using var vm = new PokedexLAEditorViewModel(save); int all = vm.SpeciesList.Count;
        var rowlet = vm.SpeciesList.Single(entry => entry.Name == GameInfo.Strings.Species[722]);
        Assert.Equal(LocalizedStrings.Instance["Dex9a_BaseForm"], rowlet.Forms[0].Name);
        Assert.Equal(190, rowlet.ReportedResearchLevel); Assert.Equal(190, rowlet.UnreportedResearchLevel);
        vm.SearchText = GameInfo.Strings.Species[25]; Assert.Single(vm.SpeciesList); Assert.Same(vm.SpeciesList[0], vm.SelectedSpecies);
        vm.SearchText = "no such species"; Assert.Empty(vm.SpeciesList); Assert.Null(vm.SelectedSpecies);
        vm.SearchText = string.Empty; Assert.Equal(all, vm.SpeciesList.Count);
    }
    [Fact]
    public void NoOpAndDiscardPreserveEverySourceBlock()
    {
        var save = LoadSave(); var before = save.AllBlocks.ToDictionary(b => b.Key, b => b.Data.ToArray());
        using var vm = new PokedexLAEditorViewModel(save); vm.SaveCommand.Execute(null); Assert.Empty(vm.Error);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.SpeciesList[0].Tasks[0].CurrentValue++; vm.ResetCommand.Execute(null); vm.SaveCommand.Execute(null);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.ReportAllCommand.Execute(null);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
        vm.Dispose(); vm.SaveCommand.Execute(null);
        foreach (var b in save.AllBlocks) Assert.Equal(before[b.Key], b.Data.ToArray());
    }
}
