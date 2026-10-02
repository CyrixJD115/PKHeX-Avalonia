using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Moq;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class Pokedex9aBindingTests
{
    [AvaloniaFact]
    public void RealizedUnknownDisplayAndNoncanonicalFlagsSurviveNoOpApply()
    {
        using var app = new HeadlessAppFixture();
        var save = new SAV9ZA();
        var selected = save.Zukan.GetEntry((ushort)Species.Pikachu);
        selected.DisplayForm = 255; selected.DisplayGender = (DisplayGender9a)254;
        int offset = SpeciesConverter.GetInternal9((ushort)Species.Pikachu) * PokeDexEntry9a.SIZE;
        save.Zukan.Data[offset + 0x0A] = 0xA5; save.Zukan.Data[offset + 0x11] = 0x80;
        var before = save.Zukan.Data.ToArray(); save.State.Edited = false;
        using var vm = new Pokedex9aEditorViewModel(save, new Mock<IDialogService>().Object);
        vm.SelectedSpecies = vm.FilteredSpecies.Single(item => item.Value == (int)Species.Pikachu);
        var view = new Pokedex9aEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 620, Height = 420 }; window.Show();
        try
        {
            Pump(window);
            view.FindControl<ScrollViewer>("Dex9aDetails")!.ScrollToEnd(); Pump(window);
            Assert.Equal(255, vm.DisplayForm); Assert.Equal(254, vm.DisplayGender);
            Assert.True(vm.IsNew); Assert.Contains(vm.Flags, row => row.Value);
            var apply = view.FindControl<Button>("Dex9aApply")!;
            var point = apply.TranslatePoint(default, view)!.Value;
            Assert.True(point.Y >= 0 && point.Y + apply.Bounds.Height <= view.Bounds.Height);
            Assert.True(vm.CanSave); vm.SaveCommand.Execute(null);
            Assert.Equal(before, save.Zukan.Data.ToArray()); Assert.False(save.State.Edited);
        }
        finally { window.Close(); }
    }

    private static void Pump(Window window)
    { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
}
