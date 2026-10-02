using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// Misc editor for Gen 7 saves covering Battle Tree, Poké Finder, Stamps, and Fly destinations.
/// </summary>
public partial class Misc7EditorViewModel : ViewModelBase, IDisposable
{
    private SAV7 _sav;
    private SAV7 _baseline;
    private readonly TrainerBlockDataSession<SAV7> _session;
    private bool _closed;
    public bool HasError => Error.Length != 0;
    [ObservableProperty] private string _error = string.Empty;

    public Misc7EditorViewModel(SAV7 sav)
    {
        _session = new(sav);
        _sav = _session.Staged;
        _baseline = (SAV7)_sav.Clone();
        LoadTrainerFields();
        LoadBattleTree();
        LoadPokeFinder();
        LoadStamps();
        LoadFlyDestinations();
        if (sav is SAV7USUM)
            LoadUltraData();
    }

    public bool IsUSUM => _sav is SAV7USUM;
    public bool IsSM => _sav is SAV7SM;

    #region Battle Tree

    // Regular mode (current/max)
    [ObservableProperty] private int _singleCurrentStreak;
    [ObservableProperty] private int _singleMaxStreak;
    [ObservableProperty] private int _doubleCurrentStreak;
    [ObservableProperty] private int _doubleMaxStreak;
    [ObservableProperty] private int _multiCurrentStreak;
    [ObservableProperty] private int _multiMaxStreak;

    // Super mode (current/max)
    [ObservableProperty] private int _superSingleCurrentStreak;
    [ObservableProperty] private int _superSingleMaxStreak;
    [ObservableProperty] private int _superDoubleCurrentStreak;
    [ObservableProperty] private int _superDoubleMaxStreak;
    [ObservableProperty] private int _superMultiCurrentStreak;
    [ObservableProperty] private int _superMultiMaxStreak;

    // Unlock flags
    [ObservableProperty] private bool _superSingleUnlocked;
    [ObservableProperty] private bool _superDoubleUnlocked;
    [ObservableProperty] private bool _superMultiUnlocked;

    private void LoadBattleTree()
    {
        var bt = _sav.BattleTree;

        // Regular
        SingleCurrentStreak = bt.GetTreeStreak(0, super: false, max: false);
        SingleMaxStreak = bt.GetTreeStreak(0, super: false, max: true);
        DoubleCurrentStreak = bt.GetTreeStreak(1, super: false, max: false);
        DoubleMaxStreak = bt.GetTreeStreak(1, super: false, max: true);
        MultiCurrentStreak = bt.GetTreeStreak(2, super: false, max: false);
        MultiMaxStreak = bt.GetTreeStreak(2, super: false, max: true);

        // Super
        SuperSingleCurrentStreak = bt.GetTreeStreak(0, super: true, max: false);
        SuperSingleMaxStreak = bt.GetTreeStreak(0, super: true, max: true);
        SuperDoubleCurrentStreak = bt.GetTreeStreak(1, super: true, max: false);
        SuperDoubleMaxStreak = bt.GetTreeStreak(1, super: true, max: true);
        SuperMultiCurrentStreak = bt.GetTreeStreak(2, super: true, max: false);
        SuperMultiMaxStreak = bt.GetTreeStreak(2, super: true, max: true);

        // Unlock flags
        SuperSingleUnlocked = _sav.EventWork.GetEventFlag(333);
        SuperDoubleUnlocked = _sav.EventWork.GetEventFlag(334);
        SuperMultiUnlocked = _sav.EventWork.GetEventFlag(335);
    }

    private void SaveBattleTree()
    {
        var bt = _sav.BattleTree;

        // Regular
        if (SingleCurrentStreak != _baseline.BattleTree.GetTreeStreak(0, super: false, max: false)) bt.SetTreeStreak(SingleCurrentStreak, 0, super: false, max: false);
        if (SingleMaxStreak != _baseline.BattleTree.GetTreeStreak(0, super: false, max: true)) bt.SetTreeStreak(SingleMaxStreak, 0, super: false, max: true);
        if (DoubleCurrentStreak != _baseline.BattleTree.GetTreeStreak(1, super: false, max: false)) bt.SetTreeStreak(DoubleCurrentStreak, 1, super: false, max: false);
        if (DoubleMaxStreak != _baseline.BattleTree.GetTreeStreak(1, super: false, max: true)) bt.SetTreeStreak(DoubleMaxStreak, 1, super: false, max: true);
        if (MultiCurrentStreak != _baseline.BattleTree.GetTreeStreak(2, super: false, max: false)) bt.SetTreeStreak(MultiCurrentStreak, 2, super: false, max: false);
        if (MultiMaxStreak != _baseline.BattleTree.GetTreeStreak(2, super: false, max: true)) bt.SetTreeStreak(MultiMaxStreak, 2, super: false, max: true);

        // Super
        if (SuperSingleCurrentStreak != _baseline.BattleTree.GetTreeStreak(0, super: true, max: false)) bt.SetTreeStreak(SuperSingleCurrentStreak, 0, super: true, max: false);
        if (SuperSingleMaxStreak != _baseline.BattleTree.GetTreeStreak(0, super: true, max: true)) bt.SetTreeStreak(SuperSingleMaxStreak, 0, super: true, max: true);
        if (SuperDoubleCurrentStreak != _baseline.BattleTree.GetTreeStreak(1, super: true, max: false)) bt.SetTreeStreak(SuperDoubleCurrentStreak, 1, super: true, max: false);
        if (SuperDoubleMaxStreak != _baseline.BattleTree.GetTreeStreak(1, super: true, max: true)) bt.SetTreeStreak(SuperDoubleMaxStreak, 1, super: true, max: true);
        if (SuperMultiCurrentStreak != _baseline.BattleTree.GetTreeStreak(2, super: true, max: false)) bt.SetTreeStreak(SuperMultiCurrentStreak, 2, super: true, max: false);
        if (SuperMultiMaxStreak != _baseline.BattleTree.GetTreeStreak(2, super: true, max: true)) bt.SetTreeStreak(SuperMultiMaxStreak, 2, super: true, max: true);

        // Unlock flags
        if (SuperSingleUnlocked != _baseline.EventWork.GetEventFlag(333)) _sav.EventWork.SetEventFlag(333, SuperSingleUnlocked);
        if (SuperDoubleUnlocked != _baseline.EventWork.GetEventFlag(334)) _sav.EventWork.SetEventFlag(334, SuperDoubleUnlocked);
        if (SuperMultiUnlocked != _baseline.EventWork.GetEventFlag(335)) _sav.EventWork.SetEventFlag(335, SuperMultiUnlocked);
    }

    [RelayCommand]
    private void UnlockAllBattleTreeModes()
    {
        SuperSingleUnlocked = true;
        SuperDoubleUnlocked = true;
        SuperMultiUnlocked = true;
    }

    #endregion

    #region Poké Finder

    [ObservableProperty] private uint _snapCount;
    [ObservableProperty] private uint _thumbsTotal;
    [ObservableProperty] private uint _thumbsRecord;
    [ObservableProperty] private int _cameraVersion;
    [ObservableProperty] private bool _gyroEnabled;

    public IReadOnlyList<ComboItem> CameraVersions
    {
        get
        {
            string name = Localization.LocalizedStrings.Instance["Misc7Editor_PokeFinder"];
            var values = new List<ComboItem> { new(name, 0), new(name + " 2.0", 1), new(name + " 2.1", 2) };
            if (CameraVersion > 2) values.Add(new(Localization.LocalizedStrings.Instance.Format("RaidSession_UnknownType", CameraVersion), CameraVersion));
            return values;
        }
    }

    private void LoadPokeFinder()
    {
        SnapCount = _sav.PokeFinder.SnapCount;
        ThumbsTotal = _sav.PokeFinder.ThumbsTotalValue;
        ThumbsRecord = _sav.PokeFinder.ThumbsHighValue;
        CameraVersion = _sav.PokeFinder.CameraVersion;
        GyroEnabled = _sav.PokeFinder.GyroFlag;
    }

    private void SavePokeFinder()
    {
        if (SnapCount != _baseline.PokeFinder.SnapCount) _sav.PokeFinder.SnapCount = SnapCount;
        if (ThumbsTotal != _baseline.PokeFinder.ThumbsTotalValue) _sav.PokeFinder.ThumbsTotalValue = ThumbsTotal;
        if (ThumbsRecord != _baseline.PokeFinder.ThumbsHighValue) _sav.PokeFinder.ThumbsHighValue = ThumbsRecord;
        if (CameraVersion != _baseline.PokeFinder.CameraVersion) _sav.PokeFinder.CameraVersion = (ushort)CameraVersion;
        if (GyroEnabled != _baseline.PokeFinder.GyroFlag) _sav.PokeFinder.GyroFlag = GyroEnabled;
    }

    [RelayCommand]
    private void MaxPokeFinder()
    {
        SnapCount = 999999;
        ThumbsTotal = 9999999;
        ThumbsRecord = 9999999;
        CameraVersion = 2;
    }

    #endregion

    #region Stamps

    [ObservableProperty]
    private ObservableCollection<StampViewModel> _stamps = [];

    private void LoadStamps()
    {
        var stampNames = Enum.GetNames<Stamp7>();
        uint stampBits = _sav.Misc.Stamps;

        Stamps.Clear();
        for (int i = 0; i < stampNames.Length; i++)
        {
            bool obtained = (stampBits & (1u << i)) != 0;
            Stamps.Add(new StampViewModel(i, Localization.LocalizedStrings.Instance["Trainer7_Stamp_" + i], obtained));
        }
    }

    private void SaveStamps()
    {
        uint bits = 0;
        for (int i = 0; i < Stamps.Count; i++)
        {
            if (Stamps[i].IsObtained)
                bits |= 1u << i;
        }
        if (bits != _baseline.Misc.Stamps) _sav.Misc.Stamps = bits;
    }

    [RelayCommand]
    private void UnlockAllStamps()
    {
        foreach (var stamp in Stamps)
            stamp.IsObtained = true;
    }

    #endregion

    #region Fly Destinations

    [ObservableProperty]
    private ObservableCollection<FlyDestination7ViewModel> _flyDestinations = [];

    public ObservableCollection<FlyDestination7ViewModel> MapUnmask { get; } = [];

    private void LoadFlyDestinations()
    {
        FlyDestinations.Clear(); MapUnmask.Clear();
        foreach (var item in Gen7TrainerMapFlags.GetFlyDestinations(_sav))
            FlyDestinations.Add(new(item.FlagIndex, GetMapFlagName(item), _sav.EventWork.GetEventFlag(item.FlagIndex)));
        foreach (var item in Gen7TrainerMapFlags.GetMapUnmask(_sav))
            MapUnmask.Add(new(item.FlagIndex, GetMapFlagName(item), _sav.EventWork.GetEventFlag(item.FlagIndex)));
    }

    private static string GetMapFlagName(Gen7TrainerMapFlag item)
    {
        if (item.AlternateName != Gen7MapAlternateName.None)
            return Localization.LocalizedStrings.Instance["Trainer7_Map_" + item.AlternateName];
        return GameInfo.GetLocationList(GameVersion.US, EntityContext.Gen7, false).FirstOrDefault(location => location.Value == item.LocationId)?.Text
            ?? Localization.LocalizedStrings.Instance.Format("RaidSession_UnknownType", item.LocationId);
    }

    private void SaveFlyDestinations()
    {
        foreach (var row in FlyDestinations.Concat(MapUnmask))
            if (row.IsUnlocked != _baseline.EventWork.GetEventFlag(row.Index))
                _sav.EventWork.SetEventFlag(row.Index, row.IsUnlocked);
    }

    [RelayCommand]
    private void UnlockAllFlyDestinations()
    {
        foreach (var dest in FlyDestinations)
            dest.IsUnlocked = true;
    }

    #endregion

    #region Ultra Data (USUM only)

    [ObservableProperty] private int _mantineSurf0;
    [ObservableProperty] private int _mantineSurf1;
    [ObservableProperty] private int _mantineSurf2;
    [ObservableProperty] private int _mantineSurf3;

    private void LoadUltraData()
    {
        MantineSurf0 = _sav.Misc.GetSurfScore(0);
        MantineSurf1 = _sav.Misc.GetSurfScore(1);
        MantineSurf2 = _sav.Misc.GetSurfScore(2);
        MantineSurf3 = _sav.Misc.GetSurfScore(3);
    }

    private void SaveUltraData()
    {
        if (_sav is not SAV7USUM) return;

        if (MantineSurf0 != _baseline.Misc.GetSurfScore(0)) _sav.Misc.SetSurfScore(0, MantineSurf0);
        if (MantineSurf1 != _baseline.Misc.GetSurfScore(1)) _sav.Misc.SetSurfScore(1, MantineSurf1);
        if (MantineSurf2 != _baseline.Misc.GetSurfScore(2)) _sav.Misc.SetSurfScore(2, MantineSurf2);
        if (MantineSurf3 != _baseline.Misc.GetSurfScore(3)) _sav.Misc.SetSurfScore(3, MantineSurf3);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Save()
    {
        if (_closed) return;
        if (!CanSave) { Error = Localization.LocalizedStrings.Instance["Trainer7_InvalidValues"]; return; }
        if (!_session.TryCommit(staged =>
        {
            _sav = staged;
            SaveTrainerFields(); SaveBattleTree(); SavePokeFinder(); SaveStamps(); SaveFlyDestinations(); SaveUltraData();
        }))
        { _sav = _session.Staged; Error = Localization.LocalizedStrings.Instance["LgpeTrainer_Conflict"]; return; }
        _sav = _session.Staged; _baseline = (SAV7)_sav.Clone();
        LoadTrainerFields(); LoadBattleTree(); LoadPokeFinder(); LoadStamps(); LoadFlyDestinations(); if (IsUSUM) LoadUltraData();
        Error = string.Empty;
    }

    [RelayCommand]
    private void Reset()
    {
        if (_closed) return;
        _session.Reset(); _sav = _session.Staged; _baseline = (SAV7)_sav.Clone();
        LoadTrainerFields(); LoadBattleTree(); LoadPokeFinder(); LoadStamps(); LoadFlyDestinations();
        if (IsUSUM) LoadUltraData(); Error = string.Empty;
    }
    public void Dispose() => _closed = true;
    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    #endregion
}

public partial class StampViewModel : ObservableObject
{
    public StampViewModel(int index, string name, bool isObtained)
    {
        Index = index;
        Name = name;
        _isObtained = isObtained;
    }

    public int Index { get; }
    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty]
    private bool _isObtained;
}

public partial class FlyDestination7ViewModel : ObservableObject
{
    public FlyDestination7ViewModel(int index, string name, bool isUnlocked)
    {
        Index = index;
        Name = name;
        _isUnlocked = isUnlocked;
    }

    public int Index { get; }
    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty]
    private bool _isUnlocked;
}
