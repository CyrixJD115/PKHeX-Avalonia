using Moq;
using System.Buffers.Binary;
using PKHeX.Avalonia.Tests.Fixtures;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class ZaTrainerWorkflowTests
{
    internal static SAV9ZA CreateSave()
    {
        var save = Assert.IsType<SAV9ZA>(SaveUtil.GetSaveFile(File.ReadAllBytes(
            Path.Combine(SaveFileFixture.FindSaveFilesPath()!, "gen9a_legendsza.main"))));
        save.SetValue(SaveBlockAccessor9ZA.KSaveRevision, 1ul);
        // This pre-unlock corpus save lacks both optional DLC blocks. Build an
        // explicitly synthetic variant through public Core parsing, retaining
        // every original block; this is test data, not a production repair.
        var survey = ParseBlock(SaveBlockAccessor9ZA.KHyperspaceSurveyPoints, SCTypeCode.UInt32, new byte[4]);
        var street = ParseBlock(SaveBlockAccessor9ZA.KStreetName, SCTypeCode.Object, new byte[0x26]);
        var keys = new[] { survey.Key, street.Key };
        var blocks = save.AllBlocks.Where(block => !keys.Contains(block.Key)).Append(survey).Append(street).OrderBy(block => block.Key).ToArray();
        return new SAV9ZA(SwishCrypto.Encrypt(blocks));
    }

    internal static SCBlock ParseBlock(uint key, SCTypeCode type, byte[] data)
    {
        var xor = new SCXorShift32(key); bool objectType = type == SCTypeCode.Object;
        int start = objectType ? 5 : 1;
        var encoded = new byte[start + data.Length]; encoded[0] = (byte)((byte)type ^ xor.Next());
        if (objectType) BinaryPrimitives.WriteInt32LittleEndian(encoded.AsSpan(1), data.Length ^ xor.Next32());
        for (int i = 0; i < data.Length; i++) encoded[start + i] = (byte)(data[i] ^ xor.Next());
        int offset = 0; return SCBlock.ReadFromOffset(encoded, key, ref offset);
    }

    [Fact]
    public void MapDateAndDlcFieldsRoundtripAndOnlyExpectedBlocksChange()
    {
        var save = CreateSave(); var expected = (SAV9ZA)save.Clone();
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        var vm = new TrainerEditorViewModel(save);
        Assert.True(vm.HasHyperspacePoints); Assert.True(vm.HasStreetName);
        vm.ZaMap = "test_map"; vm.X = 11.5; vm.Y = -3.25; vm.Z = 50; vm.ZaRotation = 135;
        vm.ZaLastSavedDate = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        vm.ZaLastSavedTime = new TimeSpan(13, 25, 0);
        vm.HyperspacePoints = 12345; vm.StreetName = "Test Street";
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        expected.Coordinates.Map = "test_map"; expected.Coordinates.X = 11.5f; expected.Coordinates.Y = -3.25f;
        expected.Coordinates.Z = 50f;
        double angle = 135 * Math.PI / 360;
        expected.Coordinates.SetPlayerRotation(0, 0, (float)Math.Sin(angle), (float)Math.Cos(angle));
        expected.LastSaved.Timestamp = new DateTime(2026, 10, 2, 13, 25, 0);
        expected.SetValue(SaveBlockAccessor9ZA.KHyperspaceSurveyPoints, 12345u);
        expected.SetString(expected.Blocks.GetBlock(SaveBlockAccessor9ZA.KStreetName).Data, "Test Street", 18, StringConverterOption.ClearZero);
        vm.SaveCommand.Execute(null);
        foreach (var block in save.AllBlocks) Assert.Equal(expected.Blocks.GetBlock(block.Key).Data.ToArray(), block.Data.ToArray());
        var reopened = new TrainerEditorViewModel(new SAV9ZA(save.Write()));
        Assert.Equal(12345u, reopened.HyperspacePoints); Assert.Equal("Test Street", reopened.StreetName);
        Assert.Equal("test_map", reopened.ZaMap); Assert.Equal(135, reopened.ZaRotation, 4);
        Assert.Equal(new TimeSpan(13, 25, 0), reopened.ZaLastSavedTime);
    }

    [Fact]
    public void NoOpApplyAndResetDiscardPreserveEveryByteAndEditedFlag()
    {
        var save = CreateSave(); save.State.Edited = false;
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        var vm = new TrainerEditorViewModel(save);
        vm.SaveCommand.Execute(null);
        Assert.False(save.State.Edited);
        vm.ZaMap = "discard"; vm.HyperspacePoints = 123;
        vm.ResetCommand.Execute(null); vm.SaveCommand.Execute(null);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        Assert.False(save.State.Edited);
    }

    [Theory] [InlineData("Screws")] [InlineData("TMs")]
    public async Task ConfirmedCollectionCanBeUndoneWithoutLosingPendingFieldEdits(string kind)
    {
        var save = CreateSave(); var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(dialog => dialog.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var vm = new TrainerEditorViewModel(save, dialogs.Object) { StreetName = "Pending" };
        await vm.CollectZaCommand.ExecuteAsync(kind);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
        vm.UndoZaCollectionCommand.Execute(null);
        Assert.Equal("Pending", vm.StreetName);
        vm.ResetCommand.Execute(null); vm.SaveCommand.Execute(null);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
    }

    [Fact]
    public void PreUnlockSaveDoesNotExposeOrManufactureMissingDlcBlocks()
    {
        var save = Assert.IsType<SAV9ZA>(SaveUtil.GetSaveFile(File.ReadAllBytes(
            Path.Combine(SaveFileFixture.FindSaveFilesPath()!, "gen9a_legendsza.main"))));
        var keys = save.AllBlocks.Select(block => block.Key).ToArray();
        var vm = new TrainerEditorViewModel(save);
        Assert.False(vm.HasHyperspacePoints); Assert.False(vm.HasStreetName);
        vm.SaveCommand.Execute(null);
        Assert.Equal(keys, save.AllBlocks.Select(block => block.Key).ToArray());
    }

    [Theory] [InlineData("Screws")] [InlineData("TMs")]
    public async Task DeclinedCollectionLeavesSourceAndUndoUnchanged(string kind)
    {
        var save = CreateSave(); var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(dialog => dialog.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        var vm = new TrainerEditorViewModel(save, dialogs.Object);
        await vm.CollectZaCommand.ExecuteAsync(kind);
        Assert.False(vm.CanUndoZaCollection);
        foreach (var block in save.AllBlocks) Assert.Equal(before[block.Key], block.Data.ToArray());
    }
}
