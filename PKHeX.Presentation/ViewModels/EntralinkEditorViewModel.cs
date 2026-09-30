using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;

namespace PKHeX.Presentation.ViewModels;

public partial class EntralinkEditorViewModel : ViewModelBase, ICloseableDialog
{
    private readonly SAV5 _source;
    private readonly SAV5 _sav;
    private readonly SAV5B2W2? _b2w2;
    private readonly SAV5BW? _bw;
    private readonly Entralink5 _entralink;
    private readonly EntreeForest _forest;
    private readonly FestaBlock5? _festa;

    public Action? CloseRequested { get; set; }

    public EntralinkEditorViewModel(SaveFile sav)
    {
        _source = (SAV5)sav;
        var editedBeforeClone = _source.State.Edited;
        _sav = (SAV5)_source.Clone();
        _source.State.Edited = editedBeforeClone;
        _b2w2 = _sav as SAV5B2W2;
        _bw = _sav as SAV5BW;
        _entralink = _sav.Entralink;
        _forest = _sav.EntreeForest;
        _festa = _b2w2?.Festa;

        IsB2W2 = _b2w2 is not null;
        IsBW = _bw is not null;

        // Initialize Lists
        var ppValues = Enum.GetValues<PassPower5>();
        var ppNames = Enum.GetNames<PassPower5>(); // Fallback to enum names if no translation, or implement translation similarly to WinForms if needed.
        PassPowers = IsB2W2 ? ppValues.Cast<PassPower5>().Select((v, i) => new ComboItem(ppNames[i], (int)v)).ToList() : [];

        // Species
        var species = GameInfo.Strings.Species;
        SpeciesList = Enumerable.Range(0, species.Count).Select(i => new ComboItem(species[i], i)).ToList();
        
        // Moves
        var moves = GameInfo.Strings.Move;
        MoveList = Enumerable.Range(0, moves.Count).Select(i => new ComboItem(moves[i], i)).ToList();
        
        LoadData();
    }

    public bool IsB2W2 { get; }
    public bool IsBW { get; }

    public IReadOnlyList<ComboItem> PassPowers { get; }
    public IReadOnlyList<ComboItem> SpeciesList { get; }
    public IReadOnlyList<ComboItem> MoveList { get; }
    public IReadOnlyList<ComboItem> GenderList { get; } = [
        new ComboItem("Male", 0),
        new ComboItem("Female", 1),
        new ComboItem("Genderless", 2)
    ];
    
    // Entralink Levels
    [ObservableProperty] private int _whiteLevel;
    [ObservableProperty] private int _blackLevel;
    
    partial void OnWhiteLevelChanged(int value) => _entralink.WhiteForestLevel = (ushort)value;
    partial void OnBlackLevelChanged(int value) => _entralink.BlackCityLevel = (ushort)value;

    // Pass Powers (B2W2)
    [ObservableProperty] private int _passPower1;
    [ObservableProperty] private int _passPower2;
    [ObservableProperty] private int _passPower3;

    partial void OnPassPower1Changed(int value)
    {
        if (_b2w2 != null) ((Entralink5B2W2)_entralink).PassPower1 = (byte)value;
    }
    partial void OnPassPower2Changed(int value)
    {
        if (_b2w2 != null) ((Entralink5B2W2)_entralink).PassPower2 = (byte)value;
    }
    partial void OnPassPower3Changed(int value)
    {
        if (_b2w2 != null) ((Entralink5B2W2)_entralink).PassPower3 = (byte)value;
    }

    // Festa Missions (B2W2)
    [ObservableProperty] private int _festaHosted;
    [ObservableProperty] private int _festaParticipated;
    [ObservableProperty] private int _festaCompleted;
    [ObservableProperty] private int _festaScore;
    [ObservableProperty] private int _festaMostParticipants;

    partial void OnFestaHostedChanged(int value) { if (_festa != null) _festa.Hosted = (ushort)value; }
    partial void OnFestaParticipatedChanged(int value) { if (_festa != null) _festa.Participated = (ushort)value; }
    partial void OnFestaCompletedChanged(int value) { if (_festa != null) _festa.Completed = (ushort)value; }
    partial void OnFestaScoreChanged(int value) { if (_festa != null) _festa.TopScores = (ushort)value; }
    partial void OnFestaMostParticipantsChanged(int value) { if (_festa != null) _festa.Participants = (byte)Math.Clamp(value, 0, byte.MaxValue); }

    public ObservableCollection<FunfestMissionRow> Missions { get; } = [];
    public ObservableCollection<FunfestMissionRow> FilteredMissions { get; } = [];
    [ObservableProperty] private FunfestMissionRow? _selectedMission;
    [ObservableProperty] private string _missionFilter = string.Empty;

    partial void OnMissionFilterChanged(string value) => RefreshMissionFilter();
    partial void OnSelectedMissionChanged(FunfestMissionRow? value) => UnlockSelectedMissionCommand.NotifyCanExecuteChanged();

    private bool CanUnlockSelectedMission => SelectedMission is { IsUnlocked: false };

    private void RefreshMissionFilter()
    {
        FilteredMissions.Clear();
        foreach (var mission in Missions)
            if (string.IsNullOrWhiteSpace(MissionFilter)
                || mission.Name.Contains(MissionFilter, StringComparison.CurrentCultureIgnoreCase))
                FilteredMissions.Add(mission);
        if (SelectedMission is null || !FilteredMissions.Contains(SelectedMission))
            SelectedMission = FilteredMissions.FirstOrDefault();
    }

    private void LoadMissions()
    {
        if (_festa is null) return;
        Missions.Clear();
        for (var index = 0; index <= FestaBlock5.MaxMissionIndex; index++)
        {
            var id = index;
            var mission = (Funfest5Mission)index;
            var name = LocalizedStrings.Instance[$"Funfest5Mission_{mission}"];
            Missions.Add(new FunfestMissionRow(id, name, _festa.GetMissionRecord(index),
                _festa.IsFunfestMissionUnlocked(index), score => _festa.SetMissionRecord(id, score)));
        }
        RefreshMissionFilter();
    }

    [RelayCommand(CanExecute = nameof(CanUnlockSelectedMission))]
    private void UnlockSelectedMission()
    {
        if (_festa is null || SelectedMission is null) return;
        _festa.UnlockFunfestMission(SelectedMission.Index);
        RefreshMissionLocks();
    }

    private void RefreshMissionLocks()
    {
        if (_festa is null) return;
        foreach (var mission in Missions)
            mission.IsUnlocked = _festa.IsFunfestMissionUnlocked(mission.Index);
        UnlockSelectedMissionCommand.NotifyCanExecuteChanged();
    }


    // Forest
    [ObservableProperty] private ObservableCollection<EntreeAreaViewModel> _areas = [];
    [ObservableProperty] private EntreeAreaViewModel? _selectedArea;
    
    [ObservableProperty] private int _unlockedAreas;
    partial void OnUnlockedAreasChanged(int value) => _forest.Unlock38Areas = value; // 0-6 maps to Areas 3-8

    [ObservableProperty] private bool _unlock9thArea;
    partial void OnUnlock9thAreaChanged(bool value) => _forest.Unlock9thArea = value;

    public void LoadData()
    {
        WhiteLevel = _entralink.WhiteForestLevel;
        BlackLevel = _entralink.BlackCityLevel;

        if (_b2w2 != null)
        {
            var el = (Entralink5B2W2)_entralink;
            PassPower1 = el.PassPower1;
            PassPower2 = el.PassPower2;
            PassPower3 = el.PassPower3;

            if (_festa != null)
            {
                FestaHosted = _festa.Hosted;
                FestaParticipated = _festa.Participated;
                FestaCompleted = _festa.Completed;
                FestaScore = _festa.TopScores;
                FestaMostParticipants = _festa.Participants;
                LoadMissions();
            }
        }

        UnlockedAreas = _forest.Unlock38Areas;
        Unlock9thArea = _forest.Unlock9thArea;

        LoadForest();
    }

    private void LoadForest()
    {
        _forest.StartAccess();
        var slots = _forest.Slots;
        
        Areas.Clear();
        var areaGroups = slots.GroupBy(s => s.Area & ~(EntreeForestArea.Center | EntreeForestArea.Left | EntreeForestArea.Right));
        
        foreach (var group in areaGroups)
        {
            var name = GetAreaName(group.Key);
            var vm = new EntreeAreaViewModel(name, group.Select(s => new EntreeSlotViewModel(s)).ToList());
            Areas.Add(vm);
        }

        if (Areas.Count > 0)
            SelectedArea = Areas[0];
    }
    
    private string GetAreaName(EntreeForestArea area)
    {
        if (area.HasFlag(EntreeForestArea.Deepest)) return "Deepest Clearing";
        if (area.HasFlag(EntreeForestArea.Ninth)) return "Area 9 (Sky)";
        if (area.HasFlag(EntreeForestArea.First)) return "Area 1";
        if (area.HasFlag(EntreeForestArea.Second)) return "Area 2";
        if (area.HasFlag(EntreeForestArea.Third)) return "Area 3";
        if (area.HasFlag(EntreeForestArea.Fourth)) return "Area 4";
        if (area.HasFlag(EntreeForestArea.Fifth)) return "Area 5";
        if (area.HasFlag(EntreeForestArea.Sixth)) return "Area 6";
        if (area.HasFlag(EntreeForestArea.Seventh)) return "Area 7";
        if (area.HasFlag(EntreeForestArea.Eighth)) return "Area 8";
        return "Unknown Area";
    }

    [RelayCommand]
    private void UnlockAllMissions()
    {
        if (_festa is null) return;
        // Core's bulk helper excludes the last mission; call the public per-mission API for
        // the full inclusive range without changing the mirrored Core source.
        for (var index = 0; index <= FestaBlock5.MaxMissionIndex; index++)
            _festa.UnlockFunfestMission(index);
        RefreshMissionLocks();
    }

    [RelayCommand]
    private void Save()
    {
        // Slots are decrypted for editing. Re-encrypt the staged forest before committing any
        // bytes so merely opening and saving this dialog cannot corrupt the forest block.
        _forest.EndAccess();
        if (!_source.Data.SequenceEqual(_sav.Data))
        {
            _source.CopyChangesFrom(_sav);
            _source.State.Edited = true;
        }
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
    
    [RelayCommand]
    private void UnlockAllAreasCmd()
    {
        _forest.UnlockAllAreas();
        UnlockedAreas = _forest.Unlock38Areas;
        Unlock9thArea = _forest.Unlock9thArea;
    }
}

public partial class EntreeAreaViewModel : ObservableObject
{
    public string Name { get; }
    public ObservableCollection<EntreeSlotViewModel> Slots { get; }

    public EntreeAreaViewModel(string name, IEnumerable<EntreeSlotViewModel> slots)
    {
        Name = name;
        Slots = new ObservableCollection<EntreeSlotViewModel>(slots);
    }
}

public partial class EntreeSlotViewModel : ViewModelBase
{
    private readonly EntreeSlot _slot;

    public EntreeSlotViewModel(EntreeSlot slot)
    {
        _slot = slot;
        Species = _slot.Species;
        Move = _slot.Move;
        Gender = _slot.Gender;
        Form = _slot.Form;
        Animation = (int)_slot.Animation;
    }

    public string SlotPosition => GetPositionName(_slot.Area);

    private string GetPositionName(EntreeForestArea area)
    {
        if (area.HasFlag(EntreeForestArea.Left)) return "Left";
        if (area.HasFlag(EntreeForestArea.Right)) return "Right";
        if (area.HasFlag(EntreeForestArea.Center)) return "Center";
        return "Center";
    }

    [ObservableProperty] private ushort _species;
    partial void OnSpeciesChanged(ushort value)
    {
        _slot.Species = value;
        OnPropertyChanged(nameof(SpeciesName));
    }
    
    public string SpeciesName => GameInfo.Strings.Species[Species];

    [ObservableProperty] private ushort _move;
    partial void OnMoveChanged(ushort value) => _slot.Move = value;

    [ObservableProperty] private int _gender;
    partial void OnGenderChanged(int value) => _slot.Gender = (byte)value;

    [ObservableProperty] private int _form;
    partial void OnFormChanged(int value) => _slot.Form = (byte)value;
    
    [ObservableProperty] private int _animation;
    partial void OnAnimationChanged(int value) => _slot.Animation = (EntreeForestAnimation)value;
}

public partial class FunfestMissionRow : ObservableObject
{
    private readonly Action<Funfest5Score> _onChanged;
    private Funfest5Score _record;

    public int Index { get; }
    public string Name { get; }
    public string StatusText => LocalizedStrings.Instance[IsUnlocked ? "EntralinkEditor_MissionUnlocked" : "EntralinkEditor_MissionLocked"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isUnlocked;
    [ObservableProperty] private int _bestTotal;
    [ObservableProperty] private int _bestScore;
    [ObservableProperty] private int _level;
    [ObservableProperty] private bool _isNew;

    public FunfestMissionRow(int index, string name, Funfest5Score record, bool unlocked, Action<Funfest5Score> onChanged)
    {
        Index = index;
        Name = name;
        _record = record;
        _onChanged = onChanged;
        _isUnlocked = unlocked;
        _bestTotal = record.Total;
        _bestScore = record.Score;
        _level = record.Level;
        _isNew = record.IsNew;
    }

    partial void OnBestTotalChanged(int value)
    {
        _record.Total = Math.Clamp(value, 0, 0x3FFF);
        _onChanged(_record);
    }

    partial void OnBestScoreChanged(int value)
    {
        _record.Score = Math.Clamp(value, 0, 0x3FFF);
        _onChanged(_record);
    }

    partial void OnLevelChanged(int value)
    {
        _record.Level = Math.Clamp(value, 0, 7);
        _onChanged(_record);
    }

    partial void OnIsNewChanged(bool value)
    {
        _record.IsNew = value;
        _onChanged(_record);
    }
}
