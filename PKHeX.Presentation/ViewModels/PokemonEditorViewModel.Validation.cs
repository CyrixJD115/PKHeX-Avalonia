
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;
using PKHeX.Presentation.Localization;

namespace PKHeX.Presentation.ViewModels;

public partial class PokemonEditorViewModel
{
    public string SpriteAccessibleName => $"{Title}: {PKHeX.Presentation.Models.SpriteStateDescription.Describe(_pk, default)}";
    public bool ShowLegalityStatus => Species != 0 && !IsHaXMode;
    [ObservableProperty]
    private bool _isLegal;

    [ObservableProperty]
    private string _legalityReport = string.Empty;

    private void Validate()
    {
        var pk = PreparePKM();
        var la = new LegalityAnalysis(pk, _sav.Personal);
        IsLegal = _haXMode || la.Valid;
        LegalityReport = la.Report();
        Sprite = _spriteRenderer.GetSprite(pk);
        OnPropertyChanged(nameof(SpriteAccessibleName));
    }

    [RelayCommand]
    private async Task ShowLegalityAsync()
    {
        Validate();
        await _windowService.ShowDialogAsync(new LegalityViewModel(LegalityReport), LocalizedStrings.Instance["PokemonEditor_LegalityAnalysisTitle"]);
    }
}
