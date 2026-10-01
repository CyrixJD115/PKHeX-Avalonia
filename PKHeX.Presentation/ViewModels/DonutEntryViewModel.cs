using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;

namespace PKHeX.Presentation.ViewModels;

public sealed record DonutProfileValue(string Name, int Value);
public partial class DonutEntryViewModel : ViewModelBase
{
    private readonly Donut9a _donut;
    private readonly Func<bool> _canEdit;
    private readonly Action _changed;
    private bool _editingHex;
    public int Index { get; }
    public ulong MillisecondsSince1970 => _donut.MillisecondsSince1970;
    public bool IsOccupied => DonutDataSession.IsOccupied(_donut);
    public string DisplayName => IsOccupied ? LocalizedStrings.Instance.Format("DonutFlow_SlotOccupied", Index + 1, TypeName, Stars) : LocalizedStrings.Instance.Format("DonutFlow_SlotEmpty", Index + 1);
    public string TypeName => (uint)DonutType < GameInfo.Strings.donutName.Length ? GameInfo.Strings.donutName[DonutType] : LocalizedStrings.Instance.Format("DonutFlow_UnknownId", DonutType);
    public IReadOnlyList<ComboItem> DonutOptions { get; private set; } = [];
    public IReadOnlyList<ComboItem> BerryOptions { get; private set; } = [];
    public IReadOnlyList<DonutProfileValue> Profile { get; private set; } = [];
    public bool HasError => Flavor0Error.Length != 0 || Flavor1Error.Length != 0 || Flavor2Error.Length != 0 || NumberError.Length != 0;
    public string NumberError => Stars is < 0 or > 255 || Calories is < 0 or > 65535 || LevelBoost is < 0 or > 255 || DonutType is < 0 or > 65535
        || new[] { BerryName, Berry1, Berry2, Berry3, Berry4, Berry5, Berry6, Berry7, Berry8 }.Any(value => value is < 0 or > 65535)
        ? LocalizedStrings.Instance["DonutFlow_NumberError"] : string.Empty;
    public DonutEntryViewModel(int index, Donut9a donut, Func<bool>? canEdit = null, Action? changed = null)
    {
        Index = index; _donut = donut; _canEdit = canEdit ?? (() => true); _changed = changed ?? (() => { });
        _stars = donut.Stars; _calories = donut.Calories; _levelBoost = donut.LevelBoost; _donutType = donut.Donut;
        _berryName = donut.BerryName; _berry1 = donut.Berry1; _berry2 = donut.Berry2; _berry3 = donut.Berry3; _berry4 = donut.Berry4;
        _berry5 = donut.Berry5; _berry6 = donut.Berry6; _berry7 = donut.Berry7; _berry8 = donut.Berry8;
        _flavor0 = donut.Flavor0; _flavor1 = donut.Flavor1; _flavor2 = donut.Flavor2;
        _flavor0Text = donut.Flavor0.ToString("X16"); _flavor1Text = donut.Flavor1.ToString("X16"); _flavor2Text = donut.Flavor2.ToString("X16");
        RefreshLanguage();
    }
    [ObservableProperty] private int _stars;
    partial void OnStarsChanged(int value)
    { if (_canEdit() && value >= 0 && value <= byte.MaxValue) _donut.Stars = (byte)value; RefreshDetails(); }
    [ObservableProperty] private int _calories;
    partial void OnCaloriesChanged(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Calories = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _levelBoost;
    partial void OnLevelBoostChanged(int value)
    { if (_canEdit() && value >= 0 && value <= byte.MaxValue) _donut.LevelBoost = (byte)value; RefreshDetails(); }
    [ObservableProperty] private int _donutType;
    partial void OnDonutTypeChanged(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Donut = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berryName;
    partial void OnBerryNameChanged(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.BerryName = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry1;
    partial void OnBerry1Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry1 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry2;
    partial void OnBerry2Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry2 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry3;
    partial void OnBerry3Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry3 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry4;
    partial void OnBerry4Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry4 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry5;
    partial void OnBerry5Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry5 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry6;
    partial void OnBerry6Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry6 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry7;
    partial void OnBerry7Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry7 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private int _berry8;
    partial void OnBerry8Changed(int value)
    { if (_canEdit() && value >= 0 && value <= ushort.MaxValue) _donut.Berry8 = (ushort)value; RefreshDetails(); }
    [ObservableProperty] private ulong _flavor0;
    [ObservableProperty] private string _flavor0Text = string.Empty;
    [ObservableProperty] private string _flavor0Error = string.Empty;
    public string Flavor0Name => LocalizeFlavor(Flavor0);
    partial void OnFlavor0TextChanged(string value)
    {
        if (!_canEdit() || _editingHex) return;
        var text = value.Trim(); if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        if (text.Length is < 1 or > 16 || !ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hash))
            Flavor0Error = LocalizedStrings.Instance["DonutFlow_HexError"];
        else { Flavor0Error = string.Empty; _editingHex = true; Flavor0 = hash; _editingHex = false; }
        RefreshDetails();
    }
    partial void OnFlavor0Changed(ulong value)
    {
        if (_canEdit()) _donut.Flavor0 = value;
        if (!_editingHex) { _editingHex = true; Flavor0Text = value.ToString("X16"); Flavor0Error = string.Empty; _editingHex = false; }
        OnPropertyChanged(nameof(Flavor0Name)); RefreshDetails();
    }
    [ObservableProperty] private ulong _flavor1;
    [ObservableProperty] private string _flavor1Text = string.Empty;
    [ObservableProperty] private string _flavor1Error = string.Empty;
    public string Flavor1Name => LocalizeFlavor(Flavor1);
    partial void OnFlavor1TextChanged(string value)
    {
        if (!_canEdit() || _editingHex) return;
        var text = value.Trim(); if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        if (text.Length is < 1 or > 16 || !ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hash))
            Flavor1Error = LocalizedStrings.Instance["DonutFlow_HexError"];
        else { Flavor1Error = string.Empty; _editingHex = true; Flavor1 = hash; _editingHex = false; }
        RefreshDetails();
    }
    partial void OnFlavor1Changed(ulong value)
    {
        if (_canEdit()) _donut.Flavor1 = value;
        if (!_editingHex) { _editingHex = true; Flavor1Text = value.ToString("X16"); Flavor1Error = string.Empty; _editingHex = false; }
        OnPropertyChanged(nameof(Flavor1Name)); RefreshDetails();
    }
    [ObservableProperty] private ulong _flavor2;
    [ObservableProperty] private string _flavor2Text = string.Empty;
    [ObservableProperty] private string _flavor2Error = string.Empty;
    public string Flavor2Name => LocalizeFlavor(Flavor2);
    partial void OnFlavor2TextChanged(string value)
    {
        if (!_canEdit() || _editingHex) return;
        var text = value.Trim(); if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        if (text.Length is < 1 or > 16 || !ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hash))
            Flavor2Error = LocalizedStrings.Instance["DonutFlow_HexError"];
        else { Flavor2Error = string.Empty; _editingHex = true; Flavor2 = hash; _editingHex = false; }
        RefreshDetails();
    }
    partial void OnFlavor2Changed(ulong value)
    {
        if (_canEdit()) _donut.Flavor2 = value;
        if (!_editingHex) { _editingHex = true; Flavor2Text = value.ToString("X16"); Flavor2Error = string.Empty; _editingHex = false; }
        OnPropertyChanged(nameof(Flavor2Name)); RefreshDetails();
    }
    public static string LocalizeFlavor(ulong hash)
    {
        if (hash == 0) return LocalizedStrings.Instance["DonutFlow_None"];
        for (int i = 0; i < DonutInfo.Flavors.Count; i++)
            if (DonutInfo.Flavors[i].Hash == hash)
                return i < GameInfo.Strings.donutFlavor.Length ? GameInfo.Strings.donutFlavor[i] : DonutInfo.Flavors[i].Name;
        return LocalizedStrings.Instance.Format("DonutFlow_UnknownHash", hash.ToString("X16"));
    }
    private void RefreshDetails()
    {
        var stats = new int[5]; _donut.RecalculateDonutFlavors(stats);
        string[] keys = ["Spicy", "Fresh", "Sweet", "Bitter", "Sour"];
        Profile = keys.Select((key, i) => new DonutProfileValue(LocalizedStrings.Instance["DonutFlow_" + key], stats[i])).ToArray();
        OnPropertyChanged(nameof(Profile)); OnPropertyChanged(nameof(DisplayName)); OnPropertyChanged(nameof(TypeName));
        OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(NumberError)); _changed();
    }
    public void RefreshLanguage()
    {
        DonutOptions = GameInfo.Strings.donutName.Select((name, id) => new ComboItem(name, id)).ToArray();
        if (DonutType >= DonutOptions.Count) DonutOptions = [.. DonutOptions, new ComboItem(LocalizedStrings.Instance.Format("DonutFlow_UnknownId", DonutType), DonutType)];
        var names = GameInfo.Strings.Item;
        var items = DonutInfo.Berries.Select(berry => (int)berry.Item).Concat(new[] { 0, BerryName, Berry1, Berry2, Berry3, Berry4, Berry5, Berry6, Berry7, Berry8 }).Distinct().Order();
        BerryOptions = items.Select(id => new ComboItem(id == 0 ? LocalizedStrings.Instance["DonutFlow_None"] : (uint)id < names.Count ? names[id] : LocalizedStrings.Instance.Format("DonutFlow_UnknownId", id), id)).ToArray();
        OnPropertyChanged(nameof(DonutOptions)); OnPropertyChanged(nameof(BerryOptions));
        OnPropertyChanged(nameof(Flavor0Name)); OnPropertyChanged(nameof(Flavor1Name)); OnPropertyChanged(nameof(Flavor2Name)); RefreshDetails();
    }
    [RelayCommand] private void Recalculate()
    {
        if (!_canEdit()) return;
        _donut.RecalculateDonutStats(); Stars = _donut.Stars; Calories = _donut.Calories; LevelBoost = _donut.LevelBoost; RefreshDetails();
    }
}
