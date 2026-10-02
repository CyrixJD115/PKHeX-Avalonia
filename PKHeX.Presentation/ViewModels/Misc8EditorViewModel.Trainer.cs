using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;

namespace PKHeX.Presentation.ViewModels;

public partial class Misc8EditorViewModel
{
    [ObservableProperty] private string _trainerName = string.Empty;
    [ObservableProperty] private uint _money = 0;
    [ObservableProperty] private int _gender = 0;
    [ObservableProperty] private int _language = 0;
    [ObservableProperty] private uint _displayTid = 0;
    [ObservableProperty] private uint _displaySid = 0;
    [ObservableProperty] private int _playedHours = 0;
    [ObservableProperty] private int _playedMinutes = 0;
    [ObservableProperty] private int _playedSeconds = 0;
    [ObservableProperty] private string _leagueCardName = string.Empty;
    [ObservableProperty] private string _uniformNumber = string.Empty;
    [ObservableProperty] private int _leagueTrainerId = 0;
    [ObservableProperty] private int _rotoRallyScore = 0;
    [ObservableProperty] private ulong _mapId = 0;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private double _z;
    [ObservableProperty] private double _sX;
    [ObservableProperty] private double _sY;
    [ObservableProperty] private double _sZ;
    [ObservableProperty] private double _rotation;
    [ObservableProperty] private int _skinColor;
    [ObservableProperty] private DateTimeOffset? _startedDate;
    [ObservableProperty] private DateTimeOffset? _lastSavedDate;
    [ObservableProperty] private TimeSpan? _lastSavedTime;

    private void LoadTrainerFields()
    {
        TrainerName = _sav.OT;
        Money = _sav.Money;
        Gender = _sav.Gender;
        Language = _sav.Language;
        DisplayTid = _sav.DisplayTID;
        DisplaySid = _sav.DisplaySID;
        PlayedHours = _sav.PlayedHours;
        PlayedMinutes = _sav.PlayedMinutes;
        PlayedSeconds = _sav.PlayedSeconds;
        LeagueCardName = _sav.TrainerCard.OT;
        UniformNumber = _sav.TrainerCard.Number;
        LeagueTrainerId = _sav.TrainerCard.TrainerID;
        RotoRallyScore = _sav.TrainerCard.RotoRallyScore;
        MapId = _sav.Coordinates.M;
        X = _sav.Coordinates.X;
        Y = _sav.Coordinates.Y;
        Z = _sav.Coordinates.Z;
        SX = _sav.Coordinates.SX;
        SY = _sav.Coordinates.SY;
        SZ = _sav.Coordinates.SZ;
        Rotation = Math.Atan2(_sav.Coordinates.RZ, _sav.Coordinates.RW) * 360 / Math.PI;
        SkinColor = (int)PlayerSkinColor8Extensions.GetSkinColorFromSkin(_sav.MyStatus.Skin);
        StartedDate = null;
        try { StartedDate = new DateTimeOffset(_sav.TrainerCard.StartedYear, _sav.TrainerCard.StartedMonth, _sav.TrainerCard.StartedDay, 0, 0, 0, TimeSpan.Zero); } catch (ArgumentOutOfRangeException) { }
        var last = ReadLastSaved(_sav); LastSavedDate = last is { } date ? new DateTimeOffset(date.Date, TimeSpan.Zero) : null; LastSavedTime = last?.TimeOfDay;
    }

    private static DateTime? ReadLastSaved(SAV8SWSH save)
    { try { return save.Played.LastSavedDate; } catch (ArgumentOutOfRangeException) { return null; } }

    private void SaveTrainerFields()
    {
        if (TrainerName != _baseline.OT) _sav.OT = TrainerName;
        if (Money != _baseline.Money) _sav.Money = Money;
        if (Gender != _baseline.Gender) _sav.Gender = (byte)Gender;
        if (Language != _baseline.Language) _sav.Language = Language;
        if (PlayedHours != _baseline.PlayedHours) _sav.PlayedHours = PlayedHours;
        if (PlayedMinutes != _baseline.PlayedMinutes) _sav.PlayedMinutes = PlayedMinutes;
        if (PlayedSeconds != _baseline.PlayedSeconds) _sav.PlayedSeconds = PlayedSeconds;
        if (LeagueCardName != _baseline.TrainerCard.OT) _sav.TrainerCard.OT = LeagueCardName;
        if (LeagueTrainerId != _baseline.TrainerCard.TrainerID) _sav.TrainerCard.TrainerID = LeagueTrainerId;
        if (RotoRallyScore != _baseline.TrainerCard.RotoRallyScore) _sav.TrainerCard.RotoRallyScore = RotoRallyScore;
        if (MapId != _baseline.Coordinates.M) _sav.Coordinates.M = MapId;
        if (DisplayTid != _baseline.DisplayTID || DisplaySid != _baseline.DisplaySID) _sav.SetDisplayID(DisplayTid, DisplaySid);
        if (UniformNumber != _baseline.TrainerCard.Number) { _sav.TrainerCard.Number = UniformNumber; _sav.MyStatus.Number = UniformNumber; }
        if (!X.Equals((double)_baseline.Coordinates.X)) _sav.Coordinates.X = (float)X;
        if (!Y.Equals((double)_baseline.Coordinates.Y)) _sav.Coordinates.Y = (float)Y;
        if (!Z.Equals((double)_baseline.Coordinates.Z)) _sav.Coordinates.Z = (float)Z;
        if (!SX.Equals((double)_baseline.Coordinates.SX)) _sav.Coordinates.SX = (float)SX;
        if (!SY.Equals((double)_baseline.Coordinates.SY)) _sav.Coordinates.SY = (float)SY;
        if (!SZ.Equals((double)_baseline.Coordinates.SZ)) _sav.Coordinates.SZ = (float)SZ;
        if (Math.Abs(Rotation - Math.Atan2(_baseline.Coordinates.RZ, _baseline.Coordinates.RW) * 360 / Math.PI) > 1e-8)
        { double angle = Rotation * Math.PI / 360; _sav.Coordinates.RX = 0; _sav.Coordinates.RY = 0; _sav.Coordinates.RZ = (float)Math.Sin(angle); _sav.Coordinates.RW = (float)Math.Cos(angle); }
        if (SkinColor != (int)PlayerSkinColor8Extensions.GetSkinColorFromSkin(_baseline.MyStatus.Skin)) _sav.MyStatus.SetSkinColor((PlayerSkinColor8)SkinColor);
        if (StartedDate is { } date && (date.Year != _baseline.TrainerCard.StartedYear || date.Month != _baseline.TrainerCard.StartedMonth || date.Day != _baseline.TrainerCard.StartedDay))
        { _sav.TrainerCard.StartedYear = (ushort)date.Year; _sav.TrainerCard.StartedMonth = (byte)date.Month; _sav.TrainerCard.StartedDay = (byte)date.Day; }
        if (LastSavedDate is { } saved && LastSavedTime is { } time && saved.Date + new TimeSpan(time.Hours, time.Minutes, 0) != ReadLastSaved(_baseline))
            _sav.Played.LastSavedDate = saved.Date + new TimeSpan(time.Hours, time.Minutes, 0);
    }

    public bool CanSave => !_closed && TrainerName.Length <= 12 && LeagueCardName.Length <= 12 && UniformNumber.Length <= 3 && UniformNumber.All(c => c is >= '0' and <= '9' or '\0') &&
        DisplayTid <= 999999 && (ulong)DisplaySid * 1000000 + DisplayTid <= uint.MaxValue &&
        (Money == _baseline.Money || Money <= _sav.MaxMoney) &&
        (Gender == _baseline.Gender || Gender is 0 or 1) && (Language == _baseline.Language || Language is >= 1 and <= 10 && Language != 6) &&
        PlayedHours is >= 0 and <= 65535 && (PlayedMinutes == _baseline.PlayedMinutes || PlayedMinutes is >= 0 and <= 59) &&
        (PlayedSeconds == _baseline.PlayedSeconds || PlayedSeconds is >= 0 and <= 59) &&
        (LeagueTrainerId == _baseline.TrainerCard.TrainerID || LeagueTrainerId is >= 0 and <= 999999) &&
        ValidFloat(X, _baseline.Coordinates.X) && ValidFloat(Y, _baseline.Coordinates.Y) && ValidFloat(Z, _baseline.Coordinates.Z) &&
        ValidFloat(SX, _baseline.Coordinates.SX) && ValidFloat(SY, _baseline.Coordinates.SY) && ValidFloat(SZ, _baseline.Coordinates.SZ) &&
        (Rotation.Equals(Math.Atan2(_baseline.Coordinates.RZ, _baseline.Coordinates.RW) * 360 / Math.PI) || double.IsFinite(Rotation) && Math.Abs(Rotation) <= 360) &&
        (StartedDate is null || StartedDate.Value.Year <= ushort.MaxValue) &&
        (LastSavedDate.HasValue == LastSavedTime.HasValue) && (LastSavedTime is null || LastSavedTime >= TimeSpan.Zero && LastSavedTime < TimeSpan.FromDays(1)) &&
        (Watts == _baseline.MyStatus.Watt || Watts <= MyStatus8.MaxWatt) && (Bp == _baseline.Misc.BP || Bp is >= 0 and <= 9999) &&
        (RotoRallyScore == _baseline.TrainerCard.RotoRallyScore || RotoRallyScore is >= 0 and <= TrainerCard8.RotoRallyScoreMax) &&
        (SkinColor == (int)PlayerSkinColor8Extensions.GetSkinColorFromSkin(_baseline.MyStatus.Skin) || SkinColor is >= 0 and <= 7);
    private static bool ValidFloat(double value, float original) => value.Equals((double)original) || double.IsFinite(value) && Math.Abs(value) <= float.MaxValue;

    private async Task RunActionAsync(string kind, Action<SAV8SWSH> action)
    {
        if (_closed || _dialogs is null) return; int epoch = _epoch;
        if (!await _dialogs.ShowConfirmationAsync(LocalizedStrings.Instance["Trainer8_Action" + kind], LocalizedStrings.Instance["Trainer8_Confirm" + kind], LocalizedStrings.Instance["TrainerEditor_ApplyChanges"], LocalizedStrings.Instance["Common_Cancel"])) return;
        if (_closed || epoch != _epoch) return;
        _session.ApplyAction(action); _sav = _session.Staged; _epoch++; OnPropertyChanged(nameof(CanUndo));
    }
    [RelayCommand] private Task ResetAppearance()
    {
        if (SkinColor is < 0 or > 7 || Gender is not (0 or 1)) return Task.CompletedTask;
        int skin = SkinColor, gender = Gender;
        return RunActionAsync("Appearance", save =>
        {
            byte original = save.Gender;
            try { save.Gender = (byte)gender; save.MyStatus.ResetAppearance((PlayerSkinColor8)skin); }
            finally { save.Gender = original; }
        });
    }
    [RelayCommand] private void Undo() { if (_closed) return; _session.Undo(); _sav = _session.Staged; _epoch++; OnPropertyChanged(nameof(CanUndo)); }
    [RelayCommand] private void Reset() { if (_closed) return; _session.Reset(); _sav = _session.Staged; _baseline = (SAV8SWSH)_sav.Clone(); _epoch++; LoadTrainerFields(); LoadMisc(); LoadBattleTower(); Error = string.Empty; OnPropertyChanged(nameof(CanUndo)); }
    public void Dispose() { _closed = true; _epoch++; }
    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
}
