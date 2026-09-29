using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;

namespace PKHeX.Presentation.ViewModels;

public partial class Geonet4EditorViewModel : ViewModelBase, ICloseableDialog
{
    private readonly SAV4? _source;
    private readonly SAV4? _working;
    private readonly Geonet4? _geonet;
    private readonly List<GeonetLocationRow> _allLocations = [];
    private readonly string[] _countryNames = Util.GetStringList("gen4_countries");
    private readonly int _languageColumn = Math.Max(1, GeoLocation.GetLanguageIndex(GameInfo.CurrentLanguage) + 1);

    public Action? CloseRequested { get; set; }
    public bool IsSupported => _geonet is not null;
    public ObservableCollection<GeonetLocationRow> Locations { get; } = [];
    public IReadOnlyList<GeonetPointOption> PointOptions { get; } =
    [
        new(GeonetPoint.None, LocalizedStrings.Instance["Geonet4Editor_PointNone"]),
        new(GeonetPoint.Blue, LocalizedStrings.Instance["Geonet4Editor_PointBlue"]),
        new(GeonetPoint.Yellow, LocalizedStrings.Instance["Geonet4Editor_PointYellow"]),
        new(GeonetPoint.Red, LocalizedStrings.Instance["Geonet4Editor_PointRed"]),
    ];

    [ObservableProperty] private bool _globalFlag;
    [ObservableProperty] private string _filterText = string.Empty;

    public Geonet4EditorViewModel(SaveFile sav)
    {
        _source = sav as SAV4;
        if (_source is null)
            return;

        // Geonet4.GlobalFlag writes its SAV4 immediately. Keep all edits on a clone so Cancel
        // leaves the live save byte-for-byte unchanged, including bulk actions and the flag.
        var editedBeforeClone = _source.State.Edited;
        _working = (SAV4)_source.Clone();
        _source.State.Edited = editedBeforeClone;
        _geonet = new Geonet4(_working);
        _globalFlag = _geonet.GlobalFlag;

        for (byte country = 1; country <= LocaleNDS4.CountryCount; country++)
        {
            var regionCount = Geonet4.GetSubregionCount(country);
            if (regionCount == 0)
                AddLocation(country, 0, null);
            else
            {
                var regionNames = Util.GetStringList($"gen4_sr_{country:000}");
                for (byte region = 1; region <= regionCount; region++)
                    AddLocation(country, region, regionNames);
            }
        }
        RefreshFilter();
    }

    private void AddLocation(byte country, byte region, string[]? regionNames)
    {
        // The 3DS GeoLocation table uses different country IDs. Read the Gen 4 NDS resources
        // used by the upstream Geonet editor so an ID never displays another country's name.
        var countryName = GetLocalizedName(_countryNames, country);
        var regionName = regionNames is null ? string.Empty : GetLocalizedName(regionNames, region);
        var point = _geonet!.GetCountrySubregion(country, region);
        _allLocations.Add(new GeonetLocationRow(country, region,
            $"{countryName} ({country})", regionName,
            PointOptions, PointOptions[(int)point], option => _geonet.SetCountrySubregion(country, region, option.Value)));
    }

    private string GetLocalizedName(string[] rows, byte id)
    {
        if (id >= rows.Length) return id.ToString();
        var columns = rows[id].Split('\t');
        return _languageColumn < columns.Length && !string.IsNullOrWhiteSpace(columns[_languageColumn])
            ? columns[_languageColumn]
            : id.ToString();
    }

    partial void OnFilterTextChanged(string value) => RefreshFilter();

    private void RefreshFilter()
    {
        Locations.Clear();
        foreach (var row in _allLocations)
        {
            if (string.IsNullOrWhiteSpace(FilterText)
                || row.Country.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase)
                || row.Region.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
                Locations.Add(row);
        }
    }

    partial void OnGlobalFlagChanged(bool value)
    {
        if (_geonet is not null)
            _geonet.GlobalFlag = value;
    }

    private void RefreshPoints()
    {
        foreach (var row in _allLocations)
            row.SetFromGeonet(PointOptions[(int)_geonet!.GetCountrySubregion(row.CountryId, row.RegionId)]);
        GlobalFlag = _geonet!.GlobalFlag;
    }

    [RelayCommand]
    private void SetAllLocations()
    {
        _geonet?.SetAll();
        if (_geonet is not null) RefreshPoints();
    }

    [RelayCommand]
    private void SetAllLegalLocations()
    {
        _geonet?.SetAllLegal();
        if (_geonet is not null) RefreshPoints();
    }

    [RelayCommand]
    private void ClearAllLocations()
    {
        _geonet?.ClearAll();
        if (_geonet is not null) RefreshPoints();
    }

    [RelayCommand]
    private void Save()
    {
        if (_source is null || _working is null || _geonet is null) return;
        _geonet.Save();
        var length = LocaleNDS4.CountryCount * 16;
        var offset = _source.Geonet + 3;
        var changed = _source.GeonetGlobalFlag != _working.GeonetGlobalFlag
            || !_source.General.Slice(offset, length).SequenceEqual(_working.General.Slice(offset, length));
        if (!changed)
        {
            CloseRequested?.Invoke();
            return;
        }
        _source.GeonetGlobalFlag = _working.GeonetGlobalFlag;
        _source.SetData(_source.General.Slice(offset, length), _working.General.Slice(offset, length));
        _source.State.Edited = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}

public sealed record GeonetPointOption(GeonetPoint Value, string Name);

public partial class GeonetLocationRow(
    byte countryId, byte regionId, string country, string region,
    IReadOnlyList<GeonetPointOption> pointOptions,
    GeonetPointOption selectedPoint, Action<GeonetPointOption> onChanged) : ObservableObject
{
    public byte CountryId { get; } = countryId;
    public byte RegionId { get; } = regionId;
    public string Country { get; } = country;
    public string Region { get; } = region;
    public IReadOnlyList<GeonetPointOption> PointOptions { get; } = pointOptions;

    [ObservableProperty] private GeonetPointOption _selectedPoint = selectedPoint;

    partial void OnSelectedPointChanged(GeonetPointOption value) => onChanged(value);

    public void SetFromGeonet(GeonetPointOption value) => SelectedPoint = value;
}
