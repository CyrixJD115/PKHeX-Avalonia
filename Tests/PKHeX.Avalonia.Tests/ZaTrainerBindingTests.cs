using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PKHeX.Avalonia.Tests.Fixtures;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;

namespace PKHeX.Avalonia.Tests;

public class ZaTrainerBindingTests
{
    [AvaloniaFact]
    public void ComposedZAWorkspaceExposesFourSectionsAndKeepsApplyVisible()
    {
        using var app = new HeadlessAppFixture();
        var save = ZaTrainerWorkflowTests.CreateSave();
        var before = save.AllBlocks.ToDictionary(block => block.Key, block => block.Data.ToArray());
        app.LoadSaveInstance(save);
        var vm = app.ViewModel.TrainerEditor!;
        var host = new TrainerEditor { DataContext = vm };
        var window = new Window { Content = host, Width = 620, Height = 420 }; window.Show();
        try
        {
            Pump(window);
            var view = Assert.Single(host.GetVisualDescendants().OfType<TrainerZaWorkspace>());
            var tabs = view.FindControl<TabControl>("TrainerZaSections")!;
            Assert.Equal(4, tabs.Items.Count);
            for (int index = 0; index < 4; index++)
            {
                tabs.SelectedIndex = index; Pump(window);
                var apply = view.FindControl<Button>("TrainerZaApply")!;
                var point = apply.TranslatePoint(default, view)!.Value;
                Assert.True(point.Y >= 0 && point.Y + apply.Bounds.Height <= view.Bounds.Height);
            }
            Assert.Equal(3, vm.ZaImages.Count);
            Assert.True(vm.HasHyperspacePoints); Assert.True(vm.HasStreetName);
            vm.SaveCommand.Execute(null);
            foreach (var block in save.AllBlocks) Assert.True(before[block.Key].AsSpan().SequenceEqual(block.Data), $"Changed {block.Key:X8}; rotation VM {vm.ZaRotation}, source {save.Coordinates.Rotation}; position {vm.X}/{vm.Y}/{vm.Z}");
        }
        finally { window.Close(); }
    }

    private static void Pump(Window window)
    { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
}
