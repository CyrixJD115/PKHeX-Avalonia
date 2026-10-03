using CommunityToolkit.Mvvm.ComponentModel;
using PKHeX.Core;
using PKHeX.Presentation.Localization;
using System.Collections.ObjectModel;

namespace PKHeX.Presentation.ViewModels;

public partial class PokedexGen9EditorViewModel
{
    public ObservableCollection<SvDexFormViewModel> FormStates { get; } = [];
    public ObservableCollection<SvDexDisplayViewModel> RegionalDisplays { get; } = [];
    private void LoadDlc(ushort species)
    {
        _baseline = null; FormStates.Clear(); RegionalDisplays.Clear();
        var names = FormConverter.GetFormList(species, GameInfo.Strings.Types, GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
        int count = Math.Max(1, (int)_sav.Personal[species].FormCount);
        for (byte form = 0; form < Math.Min(count, 32); form++)
        {
            if (!_session.IsFormSupported(species, form)) continue;
            byte index = form; var state = _session.ReadForm(species, form);
            string name = form < names.Length && names[form].Length != 0 ? names[form] : LocalizedStrings.Instance["Dex9a_BaseForm"];
            FormStates.Add(new(index, name, state, row =>
            {
                if (_closed) return;
                _session.WriteForm(species, index, row.Obtained, row.Seen, row.Heard, row.Viewed);
                IsCaught = _session.ReadCaught(species);
            }));
        }
        var ids = _session.Identifiers(species);
        foreach (int region in new[] { 1, 2, 3 })
        {
            ushort id = region == 1 ? ids.Paldea : region == 2 ? ids.Kitakami : ids.Blueberry;
            if (id == 0) continue;
            var state = _session.ReadDisplay(species, region);
            RegionalDisplays.Add(new(region, state, value =>
            {
                if (!_closed) _session.WriteDisplay(species, value.Region, value.Form, value.Gender, value.Shiny);
            }));
        }
        IsCaught = _session.ReadCaught(species);
    }
}

public partial class SvDexFormViewModel : ViewModelBase
{
    public byte Form { get; }
    public string Name { get; }
    private readonly Action<SvDexFormViewModel> _changed;
    [ObservableProperty] private bool _obtained;
    [ObservableProperty] private bool _seen;
    [ObservableProperty] private bool _heard;
    [ObservableProperty] private bool _viewed;
    public SvDexFormViewModel(byte form, string name, (bool Obtained, bool Seen, bool Heard, bool Viewed) state, Action<SvDexFormViewModel> changed)
    {
        Form = form; Name = name; _changed = changed;
        _obtained = state.Obtained; _seen = state.Seen; _heard = state.Heard; _viewed = state.Viewed;
    }
    partial void OnObtainedChanged(bool value) => _changed(this);
    partial void OnSeenChanged(bool value) => _changed(this);
    partial void OnHeardChanged(bool value) => _changed(this);
    partial void OnViewedChanged(bool value) => _changed(this);
}

public partial class SvDexDisplayViewModel : ViewModelBase
{
    public int Region { get; }
    private readonly Action<SvDexDisplayViewModel> _changed;
    [ObservableProperty] private uint _form;
    [ObservableProperty] private int _gender;
    [ObservableProperty] private bool _shiny;
    public SvDexDisplayViewModel(int region, (uint Form, int Gender, bool Shiny) state, Action<SvDexDisplayViewModel> changed)
    { Region = region; _changed = changed; _form = state.Form; _gender = state.Gender; _shiny = state.Shiny; }
    partial void OnFormChanged(uint value) => _changed(this);
    partial void OnGenderChanged(int value) => _changed(this);
    partial void OnShinyChanged(bool value) => _changed(this);
}
