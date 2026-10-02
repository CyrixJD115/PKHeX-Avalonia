using PKHeX.Core;

namespace PKHeX.Application.Services;

/// <summary>Stages trainer fields and cross-block collection actions without touching the live save.</summary>
public sealed class ZaTrainerDataSession
{
    private readonly SAV9ZA _source;
    private Dictionary<uint, byte[]> _original;
    private readonly Stack<SAV9ZA> _undo = new();
    public SAV9ZA Staged { get; private set; }
    public bool CanUndo => _undo.Count != 0;

    public ZaTrainerDataSession(SAV9ZA source)
    {
        _source = source;
        Staged = (SAV9ZA)source.Clone();
        _original = Snapshot(Staged);
    }

    private static Dictionary<uint, byte[]> Snapshot(SAV9ZA save) =>
        save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());

    public void Reset()
    {
        Staged = (SAV9ZA)_source.Clone();
        _original = Snapshot(Staged);
        _undo.Clear();
    }

    public void ApplyCollection(bool technicalMachines)
    {
        var before = (SAV9ZA)Staged.Clone();
        try
        {
            if (technicalMachines) TechnicalMachine9a.SetAllTechnicalMachines(Staged, true);
            else ColorfulScrew9a.CollectScrews(Staged);
        }
        catch { Staged = before; throw; }
        if (Staged.AllBlocks.Any(block => !block.Data.SequenceEqual(before.Blocks.GetBlock(block.Key).Data)))
            _undo.Push(before);
    }

    public void Undo()
    {
        if (_undo.TryPop(out var previous)) Staged = previous;
    }

    public bool TryCommit()
    {
        var writes = new List<(SCBlock Live, byte[] Data)>();
        foreach (var block in Staged.AllBlocks)
        {
            var original = _original[block.Key];
            if (block.Data.SequenceEqual(original)) continue;
            var live = _source.Blocks.GetBlock(block.Key);
            if (live.Type != block.Type || live.Data.Length != block.Data.Length) return false;
            if (live.Data.SequenceEqual(block.Data)) continue;
            // Treat each changed block as a transaction unit: reject a concurrent
            // update rather than accidentally combine bytes of a scalar/string.
            if (!live.Data.SequenceEqual(original)) return false;
            writes.Add((live, block.Data.ToArray()));
        }
        foreach (var (live, data) in writes) data.CopyTo(live.Data);
        if (writes.Count != 0) _source.State.Edited = true;
        Reset();
        return true;
    }
}
