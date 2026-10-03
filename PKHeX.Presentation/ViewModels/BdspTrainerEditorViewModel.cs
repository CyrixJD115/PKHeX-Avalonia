using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;
using System.Collections.ObjectModel;

namespace PKHeX.Presentation.ViewModels;

public partial class BdspTrainerEditorViewModel : ViewModelBase, IDisposable
{
    private readonly BdspTrainerDataSession _session;
    private SAV8BS _baseline;
    private bool _closed;
    [ObservableProperty] private string _trainerName = string.Empty;
    [ObservableProperty] private string _rivalName = string.Empty;
    [ObservableProperty] private int _gender;
    [ObservableProperty] private int _language;
    [ObservableProperty] private uint _displayTid;
    [ObservableProperty] private uint _displaySid;
    [ObservableProperty] private uint _money;
    [ObservableProperty] private int _playedHours;
    [ObservableProperty] private int _playedMinutes;
    [ObservableProperty] private int _playedSeconds;
    [ObservableProperty] private short _zoneId;
    [ObservableProperty] private int _x;
    [ObservableProperty] private float _y;
    [ObservableProperty] private int _height;
    [ObservableProperty] private float _rotation;
    [ObservableProperty] private GameVersion _version;
    [ObservableProperty] private string _error = string.Empty;
    public ObservableCollection<BdspBadgeViewModel> Badges { get; } = [];
    public BdspTrainerEditorViewModel(SAV8BS source)
    { _session = new(source); _baseline = (SAV8BS)_session.Staged.Clone(); Load(); }
    private void Load()
    {
        var save = _session.Staged;
        TrainerName = save.OT; RivalName = save.RivalName; Gender = save.Gender; Language = save.Language;
        DisplayTid = save.DisplayTID; DisplaySid = save.DisplaySID; Money = save.Money;
        PlayedHours = save.PlayedHours; PlayedMinutes = save.PlayedMinutes; PlayedSeconds = save.PlayedSeconds;
        ZoneId = save.ZoneID; X = save.MyStatus.X; Y = save.MyStatus.Height; Height = save.MyStatus.Y; Rotation = save.MyStatus.Rotation;
        Version = save.Version; Badges.Clear();
        for (int i = 0; i < 8; i++) Badges.Add(new(i, save.FlagWork.GetSystemFlag(124 + i)));
    }
    public bool CanSave => !_closed && TrainerName.Length <= _baseline.MaxStringLengthTrainer && RivalName.Length <= _baseline.MaxStringLengthTrainer &&
        (Gender == _baseline.Gender || Gender is 0 or 1) && (Language == _baseline.Language || Language is >= 1 and <= 10 && Language != 6) &&
        DisplayTid <= 999999 && (ulong)DisplaySid * 1000000 + DisplayTid <= uint.MaxValue &&
        (Money == _baseline.Money || Money <= _baseline.MaxMoney) && PlayedHours is >= 0 and <= ushort.MaxValue &&
        (PlayedMinutes == _baseline.PlayedMinutes || PlayedMinutes is >= 0 and <= 59) && (PlayedSeconds == _baseline.PlayedSeconds || PlayedSeconds is >= 0 and <= 59) &&
        Valid(Y, _baseline.MyStatus.Height) && Valid(Rotation, _baseline.MyStatus.Rotation) &&
        (Version == _baseline.Version || Version is GameVersion.BD or GameVersion.SP);
    private static bool Valid(float value, float original) => value.Equals(original) || float.IsFinite(value);
    private void Apply(SAV8BS save)
    {
        bool identity = TrainerName != _baseline.OT || DisplayTid != _baseline.DisplayTID || DisplaySid != _baseline.DisplaySID;
        if (TrainerName != _baseline.OT) save.OT = TrainerName;
        if (RivalName != _baseline.RivalName) save.RivalName = RivalName;
        if (Gender != _baseline.Gender) save.Gender = (byte)Gender;
        if (Language != _baseline.Language) save.Language = Language;
        if (DisplayTid != _baseline.DisplayTID || DisplaySid != _baseline.DisplaySID) save.SetDisplayID(DisplayTid, DisplaySid);
        if (Money != _baseline.Money) save.Money = Money;
        if (PlayedHours != _baseline.PlayedHours) save.PlayedHours = PlayedHours;
        if (PlayedMinutes != _baseline.PlayedMinutes) save.PlayedMinutes = PlayedMinutes;
        if (PlayedSeconds != _baseline.PlayedSeconds) save.PlayedSeconds = PlayedSeconds;
        if (ZoneId != _baseline.ZoneID) save.ZoneID = ZoneId;
        if (!X.Equals(_baseline.MyStatus.X)) save.MyStatus.X = X;
        if (!Y.Equals(_baseline.MyStatus.Height)) save.MyStatus.Height = Y;
        if (Height != _baseline.MyStatus.Y) save.MyStatus.Y = Height;
        if (!Rotation.Equals(_baseline.MyStatus.Rotation)) save.MyStatus.Rotation = Rotation;
        if (Version != _baseline.Version) save.Version = Version;
        foreach (var badge in Badges) if (badge.Value != _baseline.FlagWork.GetSystemFlag(124 + badge.Index)) save.FlagWork.SetSystemFlag(124 + badge.Index, badge.Value);
        if (identity && save.HasFirstSaveFileExpansion) save.RecordAdd.ReplaceOT(_baseline, save);
    }
    [RelayCommand] private void Save()
    {
        if (!CanSave) { Error = LocalizedStrings.Instance["Trainer7_InvalidValues"]; return; }
        if (!_session.TryCommit(Apply)) { Error = LocalizedStrings.Instance["LgpeTrainer_Conflict"]; return; }
        _baseline = (SAV8BS)_session.Staged.Clone(); Load(); Error = string.Empty;
    }
    [RelayCommand] private void Reset() { if (_closed) return; _session.Reset(); _baseline = (SAV8BS)_session.Staged.Clone(); Load(); Error = string.Empty; }
    public void Dispose() => _closed = true;
}

public partial class BdspBadgeViewModel(int index, bool value) : ObservableObject
{
    public int Index { get; } = index;
    [ObservableProperty] private bool _value = value;
}
