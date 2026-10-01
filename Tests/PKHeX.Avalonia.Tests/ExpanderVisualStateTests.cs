using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public class ExpanderVisualStateTests
{
    [AvaloniaFact]
    public async Task GeneratorChevronSettlesToCollapsedAndExpandedAngles()
    {
        using var app = new HeadlessAppFixture();
        using var vm = new DonutEditorViewModel(new SAV9ZA());
        var view = new DonutEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 1000, Height = 760 };
        window.Show();
        try
        {
            var expander = view.FindControl<Expander>("DonutGenerator")!;
            foreach (bool expanded in new[] { false, true, false })
            {
                expander.IsExpanded = expanded;
                await Settle(window);
                var path = expander.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().Single(element => element.Name == "ExpandCollapseChevron");
                var rotation = Assert.IsType<RotateTransform>(path.RenderTransform);
                Assert.Equal(expanded ? 180 : 0, rotation.Angle, precision: 3);
            }
        }
        finally { window.Close(); }
    }

    internal static async Task Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(100);
        }
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
    }
}
