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
                await Settle(window, () =>
                {
                    var chevron = expander.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().Single(element => element.Name == "ExpandCollapseChevron");
                    return chevron.RenderTransform is RotateTransform transform && Math.Abs(transform.Angle - (expanded ? 180 : 0)) < 0.0001;
                });
                var header = expander.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.ToggleButton>().Single(element => element.Name == "ExpanderHeader");
                Assert.Equal(expanded, header.IsChecked);
                Assert.Equal(expanded ? "expanded" : "collapsed", header.Tag);
                var path = expander.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().Single(element => element.Name == "ExpandCollapseChevron");
                var rotation = Assert.IsType<RotateTransform>(path.RenderTransform);
                Assert.Equal(expanded ? 180 : 0, rotation.Angle, precision: 3);
            }
        }
        finally { window.Close(); }
    }

    internal static async Task Settle(Window window, Func<bool>? settled = null)
    {
        int consecutive = 0;
        for (int i = 0; i < (settled is null ? 3 : 50); i++)
        {
            // Request a UI animation frame explicitly: ForceRenderTimerTick can be a no-op
            // when the headless render timer has no active subscription.
            window.RequestAnimationFrame(_ => { });
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
            await Task.Delay(100);
            if (settled is not null)
            {
                consecutive = settled() ? consecutive + 1 : 0;
                if (consecutive == 2) break;
            }
        }
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
    }
}
