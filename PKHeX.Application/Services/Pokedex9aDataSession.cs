using PKHeX.Core;

namespace PKHeX.Application.Services;

/// <summary>Stages ZA dex data and atomically commits only changed documented fields.</summary>
public sealed class Pokedex9aDataSession
{
    private readonly SAV9ZA _source;
    private byte[] _original;
    public SAV9ZA Staged { get; private set; }
    public Zukan9a Dex => Staged.Zukan;

    public Pokedex9aDataSession(SAV9ZA source)
    {
        _source = source;
        Staged = (SAV9ZA)source.Clone();
        _original = Dex.Data.ToArray();
    }

    public void Reset()
    {
        Staged = (SAV9ZA)_source.Clone();
        _original = Dex.Data.ToArray();
    }

    public bool TryCommit()
    {
        var desired = Dex.Data;
        var current = _source.Zukan.Data;
        if (current.Length != _original.Length || desired.Length != _original.Length) return false;
        var writes = new List<(int Offset, byte Value)>();
        for (int offset = 0; offset < desired.Length; offset++)
        {
            int field = offset % PokeDexEntry9a.SIZE;
            if (field is >= 0x12 and < 0x5A or > 0x5C) continue;
            byte original = _original[offset], target = desired[offset], live = current[offset];
            if (field is 0x0A or 0x11 or 0x5C)
            {
                // Core represents these as nonzero booleans. A semantic revert must
                // retain a noncanonical original/live byte rather than normalize it.
                if ((original != 0) == (target != 0) || (live != 0) == (target != 0)) continue;
                writes.Add((offset, target));
            }
            else if (field is 0x5A or 0x5B)
            {
                if (original == target || live == target) continue;
                if (live != original) return false;
                writes.Add((offset, target));
            }
            else
            {
                byte mask = field switch { 0x09 => 0x03, 0x0B or 0x10 => 0x07, _ => 0xFF };
                byte changed = (byte)((original ^ target) & mask);
                if (changed == 0) continue;
                byte merged = (byte)((live & ~changed) | (target & changed));
                if (merged != live) writes.Add((offset, merged));
            }
        }
        foreach (var (offset, value) in writes) current[offset] = value;
        if (writes.Count != 0) _source.State.Edited = true;
        Reset();
        return true;
    }
}
