using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

public partial class PokedexGen9EditorViewModel : ViewModelBase, IDisposable
{
    private readonly SvPokedexDataSession _session;
    private SAV9SV _sav => _session.Staged;
    private Zukan9 _zukan => _sav.Blocks.Zukan;
    private ushort _loadedSpecies;
    private bool _closed;
    private LegacySnapshot? _baseline;
    [ObservableProperty] private string _error = string.Empty;
    public bool UsesDlcFormat => _session.UsesDlcFormat;
    public bool UsesLegacyFormat => !UsesDlcFormat;
    
    public ObservableCollection<ComboItem> SpeciesList { get; } = [];
    public ObservableCollection<ComboItem> FilteredSpeciesList { get; private set; } = [];

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterList();
        }
    }

    private ComboItem? _selectedSpecies;
    public ComboItem? SelectedSpecies
    {
        get => _selectedSpecies;
        set
        {
            if (_selectedSpecies is not null && _selectedSpecies != value) FlushLegacy();
            if (SetProperty(ref _selectedSpecies, value))
                LoadEntry();
        }
    }
    
    // Entry Properties
    [ObservableProperty] private bool _isCaught; // Derived or manually managed?
    [ObservableProperty] private bool _isSeenMale;
    [ObservableProperty] private bool _isSeenFemale;
    [ObservableProperty] private bool _isSeenGenderless;
    [ObservableProperty] private bool _isSeenShiny;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private int _displayGender; // 0=M, 1=F, 2=G
    [ObservableProperty] private bool _displayShiny;
    [ObservableProperty] private int _displayForm;
    [ObservableProperty] private bool _displayGenderDiff;
    
    // Languages
    [ObservableProperty] private bool _langJPN;
    [ObservableProperty] private bool _langENG;
    [ObservableProperty] private bool _langFRE;
    [ObservableProperty] private bool _langITA;
    [ObservableProperty] private bool _langGER;
    [ObservableProperty] private bool _langSPA;
    [ObservableProperty] private bool _langKOR;
    [ObservableProperty] private bool _langCHS;
    [ObservableProperty] private bool _langCHT;

    public ObservableCollection<string> Forms { get; } = [];
    
    public PokedexGen9EditorViewModel(SAV9SV sav)
    {
        _session = new(sav);
        
        LoadSpeciesList();
    }

    private void LoadSpeciesList()
    {
        SpeciesList.Clear();
        foreach (ushort species in _session.Species().OrderBy(species => _session.Identifiers(species).Paldea == 0 ? 10000 + species : _session.Identifiers(species).Paldea))
        {
            var ids = _session.Identifiers(species);
            SpeciesList.Add(new ComboItem($"{GameInfo.Strings.Species[species]}  P:{ids.Paldea} K:{ids.Kitakami} B:{ids.Blueberry}", species));
        }
            
        FilterList();
        if (FilteredSpeciesList.Count > 0)
            SelectedSpecies = FilteredSpeciesList[0];
    }

    private void FilterList()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredSpeciesList = new ObservableCollection<ComboItem>(SpeciesList);
        }
        else
        {
            FilteredSpeciesList = new ObservableCollection<ComboItem>(
                SpeciesList.Where(x => x.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }
        OnPropertyChanged(nameof(FilteredSpeciesList));
    }

    private void LoadEntry()
    {
        if (SelectedSpecies is null) return;
        
        ushort species = (ushort)SelectedSpecies.Value;
        _loadedSpecies = species;
        if (UsesDlcFormat) { LoadDlc(species); return; }
        
        var entry = _zukan.DexPaldea.Get(species);
        
        // Load Forms
        Forms.Clear();
        var formList = FormConverter.GetFormList(species, GameInfo.Strings.Types, GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
        foreach(var f in formList) Forms.Add(f);
        
        // Properties
        IsNew = entry.GetDisplayIsNew();
        IsCaught = entry.IsCaught;
        IsSeenMale = entry.GetIsGenderSeen(0);
        IsSeenFemale = entry.GetIsGenderSeen(1);
        IsSeenGenderless = entry.GetIsGenderSeen(2);
        IsSeenShiny = entry.GetSeenIsShiny();
        
        DisplayGender = (int)entry.GetDisplayGender();
        DisplayShiny = entry.GetDisplayIsShiny();
        DisplayGenderDiff = entry.GetDisplayGenderIsDifferent();
        DisplayForm = unchecked((int)entry.GetDisplayForm());

        // Languages
        LangJPN = entry.GetLanguageFlag((int)LanguageID.Japanese);
        LangENG = entry.GetLanguageFlag((int)LanguageID.English);
        LangFRE = entry.GetLanguageFlag((int)LanguageID.French);
        LangITA = entry.GetLanguageFlag((int)LanguageID.Italian);
        LangGER = entry.GetLanguageFlag((int)LanguageID.German);
        LangSPA = entry.GetLanguageFlag((int)LanguageID.Spanish);
        LangKOR = entry.GetLanguageFlag((int)LanguageID.Korean);
        LangCHS = entry.GetLanguageFlag((int)LanguageID.ChineseS);
        LangCHT = entry.GetLanguageFlag((int)LanguageID.ChineseT);
        _baseline = CaptureLegacy();
    }

    [RelayCommand]
    private void SaveCurrent()
    {
        if (_closed) return;
        FlushLegacy();
        if (!_session.TryCommit()) { Error = Localization.LocalizedStrings.Instance["LgpeTrainer_Conflict"]; return; }
        Error = string.Empty; LoadEntry();
    }
    private void FlushLegacy()
    {
        if (UsesDlcFormat || _loadedSpecies == 0 || _baseline is null) return;
        var old = _baseline;
        ushort species = _loadedSpecies;
        var entry = _zukan.DexPaldea.Get(species);
        if (IsCaught != old.Caught) entry.SetCaught(IsCaught);
        if (IsNew != old.New) entry.SetDisplayIsNew(IsNew);
        if (IsSeenMale != old.Male) entry.SetIsGenderSeen(0, IsSeenMale);
        if (IsSeenFemale != old.Female) entry.SetIsGenderSeen(1, IsSeenFemale);
        if (IsSeenGenderless != old.Genderless) entry.SetIsGenderSeen(2, IsSeenGenderless);
        if (IsSeenShiny != old.SeenShiny) entry.SetSeenIsShiny(IsSeenShiny);
        if (DisplayGender != old.Gender) entry.SetDisplayGender(DisplayGender);
        if (DisplayShiny != old.Shiny) entry.SetDisplayIsShiny(DisplayShiny);
        if (DisplayGenderDiff != old.Different) entry.SetDisplayGenderIsDifferent(DisplayGenderDiff);
        if (DisplayForm != old.Form) entry.SetDisplayForm(unchecked((uint)DisplayForm));
        bool[] languages = [LangJPN, LangENG, LangFRE, LangITA, LangGER, LangSPA, LangKOR, LangCHS, LangCHT];
        int[] ids = [1, 2, 3, 4, 5, 7, 8, 9, 10];
        for (int i = 0; i < ids.Length; i++) if (languages[i] != old.Languages[i]) entry.SetLanguageFlag(ids[i], languages[i]);
        _baseline = CaptureLegacy();
    }

    // Batch commands (Apply to current only, or all?)
    // Typically WinForms has "Seen None/All" for CURRENT species, and "Modify All" menu for entire dex.
    
    [RelayCommand]
    private void SeenAll()
    {
        if (SelectedSpecies is null) return;
        if (UsesDlcFormat)
        {
            foreach (var form in FormStates) { form.Seen = true; form.Heard = true; }
        }
        else
        {
            FlushLegacy(); var entry = _zukan.DexPaldea.Get((ushort)SelectedSpecies.Value); entry.SetSeen(true);
        }
        LoadEntry();
    }
    
    [RelayCommand]
    private void CaughtAll() // Actually Caught implies Seen
    {
        if (SelectedSpecies is null) return;
        if (UsesDlcFormat) foreach (var form in FormStates) { form.Obtained = true; form.Seen = true; form.Heard = true; }
        else { FlushLegacy(); _session.WriteLegacyCaught((ushort)SelectedSpecies.Value, true); }
        LoadEntry();
    }
    private LegacySnapshot CaptureLegacy() => new(IsCaught, IsNew, IsSeenMale, IsSeenFemale, IsSeenGenderless, IsSeenShiny,
        DisplayGender, DisplayShiny, DisplayGenderDiff, DisplayForm, [LangJPN, LangENG, LangFRE, LangITA, LangGER, LangSPA, LangKOR, LangCHS, LangCHT]);
    private sealed record LegacySnapshot(bool Caught, bool New, bool Male, bool Female, bool Genderless, bool SeenShiny,
        int Gender, bool Shiny, bool Different, int Form, bool[] Languages);
    [RelayCommand] private void Reset() { if (_closed) return; _session.Reset(); _baseline = null; _loadedSpecies = 0; LoadEntry(); Error = string.Empty; }
    public void Dispose() => _closed = true;
}
