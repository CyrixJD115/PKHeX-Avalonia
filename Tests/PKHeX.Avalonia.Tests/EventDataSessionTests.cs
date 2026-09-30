using PKHeX.Application.Models.Events;
using PKHeX.Avalonia.Tests.Fixtures;
using PKHeX.Core;

namespace PKHeX.Avalonia.Tests;

public class EventDataSessionTests
{
    private static SaveFile Load(string file)
    {
        var path = Path.Combine(SaveFileFixture.FindSaveFilesPath()!, file);
        if (file == "gen7b_letsgopikachu.bin")
        {
            // This committed synthetic fixture lacks auto-detection metadata.
            // Its known LGPE buffer is valid for typed block roundtrip testing.
            var data = File.ReadAllBytes(path);
            Assert.Equal(SaveUtil.SIZE_G7GG, data.Length);
            return new SAV7b(data);
        }
        return Assert.IsAssignableFrom<SaveFile>(SaveFileFixture.LoadSave(path));
    }

    [Theory]
    [InlineData("gen7_sun.main", 4000, 0, 1000)]
    [InlineData("gen7_ultrasun.main", 4960, 0, 1000)]
    [InlineData("gen7b_letsgopikachu.bin", 4096, 0, 1000)]
    [InlineData("gen8b_brilliantdiamond.bin", 4000, 1000, 500)]
    public void CatalogCoversExactBoundsAndNamedDefinitionsWithoutMutatingSource(string file, int flags, int system, int work)
    {
        var save = Load(file);
        save.State.Edited = false;
        var before = save.Data.ToArray();
        var session = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        Assert.Equal(flags, session.Fields.Count(f => f.Kind == EventDataKind.Flag));
        Assert.Equal(system, session.Fields.Count(f => f.Kind == EventDataKind.SystemFlag));
        Assert.Equal(work, session.Fields.Count(f => f.Kind == EventDataKind.Work));
        Assert.Contains(session.Fields, f => f.Kind == EventDataKind.Flag && f.Name.Length != 0);
        Assert.Contains(session.Fields, f => f.Kind == EventDataKind.Work && f.Name.Length != 0);
        Assert.Equal(before, save.Data.ToArray());
        Assert.False(save.State.Edited);
        var last = session.Fields.Last(f => f.Kind == EventDataKind.Flag);
        Assert.Equal(flags - 1, last.Index);
        Assert.False(last.TrySetValue(2));
        Assert.True(last.TrySetValue(last.Value == 0 ? 1 : 0));
        Assert.Equal(before, save.Data.ToArray());
        session.Reset();
        Assert.False(last.IsChanged);
        Assert.Equal(0, session.Commit());
        Assert.False(save.State.Edited);
    }

    [Theory]
    [InlineData("gen7b_letsgopikachu.bin")]
    [InlineData("gen8b_brilliantdiamond.bin")]
    public void SignedWorkPersistsAndDiffIsCompatibleAndPrivate(string file)
    {
        var save = Load(file);
        var session = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        var field = session.Fields.Last(f => f.Kind == EventDataKind.Work);
        Assert.True(field.TrySetValue(int.MinValue));
        Assert.False(field.TrySetValue((long)int.MinValue - 1));
        Assert.False(field.TrySetValue((long)int.MaxValue + 1));
        Assert.Equal(1, session.Commit());
        Assert.False(field.IsChanged);
        var actual = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        Assert.Equal(int.MinValue, actual.Fields.Last(f => f.Kind == EventDataKind.Work).Value);
        var original = Load(file);
        Assert.True(EventDataSession.TryCompare(original, save, out var differences));
        var diff = Assert.Single(differences);
        Assert.Equal(EventDataKind.Work, diff.Kind);
        Assert.Equal(field.Index, diff.Index);
        Assert.Equal(int.MinValue, diff.After);
        Assert.False(EventDataSession.TryCompare(original, new SAV6XY(), out _));
    }

    [Fact]
    public void BdspSystemFlagsAndWorkInterpretationAreIndependent()
    {
        var save = Assert.IsType<SAV8BS>(Load("gen8b_brilliantdiamond.bin"));
        var session = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        var flag = session.Fields.Single(f => f.Kind == EventDataKind.Flag && f.Index == 3999);
        var system = session.Fields.Single(f => f.Kind == EventDataKind.SystemFlag && f.Index == 999);
        var work = session.Fields.Single(f => f.Kind == EventDataKind.Work && f.Index == 499);
        Assert.True(work.SupportsFloatInterpretation);
        flag.TrySetValue(save.FlagWork.GetFlag(3999) ? 0 : 1);
        system.TrySetValue(save.FlagWork.GetSystemFlag(999) ? 0 : 1);
        work.TrySetValue(BitConverter.SingleToInt32Bits(1.25f));
        Assert.Equal(3, session.Commit());
        Assert.Equal(flag.Value != 0, save.FlagWork.GetFlag(3999));
        Assert.Equal(system.Value != 0, save.FlagWork.GetSystemFlag(999));
        Assert.Equal(1.25f, save.FlagWork.GetFloatWork(499));
        Assert.Contains(session.Fields, f => f.Kind == EventDataKind.SystemFlag && f.Name.Length != 0);
        Assert.Contains(session.Fields, f => f.Options.Count != 0);
    }
    [Theory]
    [InlineData("gen7_sun.main")]
    [InlineData("gen7_ultrasun.main")]
    public void UnsignedWorkBoundsAndQrConstantsRemainCoherentAfterCommit(string file)
    {
        var save = Assert.IsAssignableFrom<SAV7>(Load(file));
        var session = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        var field = session.Fields.Last(f => f.Kind == EventDataKind.Work);
        Assert.False(field.TrySetValue(-1));
        Assert.False(field.TrySetValue(65536));
        var value = field.Value == ushort.MaxValue ? 1 : ushort.MaxValue;
        Assert.True(field.TrySetValue(value));
        Assert.Equal(1, session.Commit());
        Assert.Equal(value, save.EventWork.GetWork(field.Index));
        var actual = Assert.IsType<EventDataSession>(EventDataSession.Create(save));
        foreach (var pair in session.Fields.Zip(actual.Fields))
            Assert.Equal(pair.First.Value, pair.Second.Value);
        Assert.Equal(0, session.Commit());
    }

}
