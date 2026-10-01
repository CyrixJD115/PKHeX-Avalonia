using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PKHeX.Application.Abstractions;
using PKHeX.Avalonia.Controls;
using PKHeX.Avalonia.Views;
using PKHeX.Avalonia.Tests.Fixtures;
using PKHeX.Core;
using PKHeX.Infrastructure.GiftRecords;
using PKHeX.Presentation.Localization;
using PKHeX.Presentation.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace PKHeX.Avalonia.Tests.Harness;

/// <summary>
/// Opt-in visual-evidence captures of feature states, mirroring the headless capture pattern in
/// <see cref="HeadlessGiftRecordTests"/>. These write a PNG of a real editor view and are skipped
/// unless the process was started with <c>PKHEX_HEADLESS_CAPTURE=1</c> and the Skia headless app
/// builder (frames are only meaningful when drawing is enabled; see Harness/README.md).
/// </summary>
public sealed class HeadlessFeatureCaptureTests(ITestOutputHelper output)
{
    [AvaloniaFact]
    public void CaptureDlc5Images_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled()) return;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                foreach (var tab in new[] { 0, 2, 5 })
                {
                    var save = LoadCaptureSave<SAV5>(language == "en" ? "gen5_black.sav" : "gen5_white2.sav");
                    global::PKHeX.Avalonia.Tests.Dlc5ImageLayoutTests.Seed(save);
                    var vm = new DLC5EditorViewModel(save, new RecordingDialogService(), new global::PKHeX.Avalonia.Services.PngImageCodec()) { SelectedTab = tab };
                    CaptureAuxiliaryView(new DLC5Editor { DataContext = vm }, $"dlc5-{language}-tab{tab}.png",
                        language == "en" ? 780 : 620, language == "en" ? 700 : 420, "Gen 5 DLC image workflow");
                }
            }
        }
        finally { LocalizedStrings.Instance.SetLanguage(previous); }
    }

    [AvaloniaFact]
    public void CaptureFestivalPlaza_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled()) return;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                foreach (var tab in new[] { 0, 1, 2 })
                {
                    var save = language == "en" ? LoadCaptureSave<SAV7>("gen7_sun.main") : LoadCaptureSave<SAV7>("gen7_ultrasun.main");
                    using var vm = new FestivalPlazaEditorViewModel(save, new RecordingDialogService()) { SelectedTab = tab };
                    CaptureAuxiliaryView(new FestivalPlazaEditor { DataContext = vm }, $"festival-plaza-{language}-tab{tab}.png",
                        language == "en" ? 740 : 480, language == "en" ? 620 : 360, "Festival Plaza staged workspace");
                }
            }
        }
        finally { LocalizedStrings.Instance.SetLanguage(previous); }
    }

    [AvaloniaFact]
    public void CaptureSvRaidRegionsAndRecords_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled()) return;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                foreach (var tab in new[] { 0, 1 })
                {
                    var save = global::PKHeX.Avalonia.Tests.SvRaidDataSessionTests.CreateSave();
                    foreach (var region in new[] { save.RaidPaldea, save.RaidKitakami, save.RaidBlueberry })
                    {
                        region.GetRaid(0).AreaID = 1; region.GetRaid(0).LotteryGroup = 2;
                        region.GetRaid(0).SpawnPointID = 3; region.GetRaid(0).Seed = 0xABCDEF01;
                        region.GetRaid(0).Content = TeraRaidContentType.Might7; region.GetRaid(0).IsEnabled = true;
                    }
                    save.RaidSevenStar.GetRaid(0).Identifier = 20260930;
                    save.RaidSevenStar.GetRaid(0).Captured = true;
                    using var vm = new Raid9EditorViewModel(save, dialogs: new RecordingDialogService(), initialTab: tab);
                    CaptureAuxiliaryView(new Raid9Editor { DataContext = vm }, $"sv-raids-{language}-tab{tab}.png",
                        language == "en" ? 780 : 480, language == "en" ? 650 : 360, "SV shared raid and seven-star session");
                }
            }
        }
        finally { LocalizedStrings.Instance.SetLanguage(previous); }
    }

    [AvaloniaFact]
    public void CaptureBoxReportColumns_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled()) return;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                var save = new SAV6XY();
                save.SetBoxSlotAtIndex(new PK6 { Species = 94, Nickname = "Long-name example", Ability = 130, Move1 = 421, CurrentLevel = 55 }, 0);
                var vm = new BoxReportViewModel(save, new RecordingDialogService());
                vm.Columns[11].IsVisible = true;
                CaptureAuxiliaryView(new BoxReportView { DataContext = vm }, $"box-report-{language}.png", language == "en" ? 1100 : 700, language == "en" ? 600 : 400, "Box report column workflow");
            }
        }
        finally { LocalizedStrings.Instance.SetLanguage(previous); }
    }

    [AvaloniaFact]
    public void CaptureRaid8Regions_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled()) return;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                var vm = new RaidEditorViewModel(global::PKHeX.Avalonia.Tests.Raid8TransactionTests.CreateSave());
                vm.SelectedRegion = vm.Regions[2];
                vm.SelectedDen!.WattsHarvested = true;
                CaptureAuxiliaryView(new RaidEditor { DataContext = vm }, $"raids8-{language}.png", language == "en" ? 700 : 480, language == "en" ? 650 : 360, "SWSH staged raid regions");
            }
        }
        finally { LocalizedStrings.Instance.SetLanguage(previous); }
    }

    [AvaloniaFact]
    public void CaptureGeonetLocationEditor_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        CaptureAuxiliaryView(
            new Geonet4Editor { DataContext = new Geonet4EditorViewModel(new SAV4HGSS()) },
            "geonet-location-editor.png", 620, 570, "Gen 4 Geonet location editor");
    }

    [AvaloniaFact]
    public void CaptureChatterAudioActions_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        var save = LoadCaptureSave<SAV5B2W2>("gen5_white2.sav");
        save.Chatter.Recording.Fill(0x7A);
        save.Chatter.Initialized = true;
        var previous = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                foreach (var (width, height) in new[] { (620, 350), (420, 300) })
                {
                    CaptureAuxiliaryView(
                        new ChatterEditor
                        {
                            DataContext = new ChatterEditorViewModel(save,
                                new RecordingDialogService(), Mock.Of<IAudioPlaybackService>()),
                        },
                        $"chatter-audio-{language}-{width}.png", width, height, "Chatter audio actions");
                }
            }
        }
        finally
        {
            LocalizedStrings.Instance.SetLanguage(previous);
        }
    }

    [AvaloniaFact]
    public void CapturePortugueseFashionCatalogs_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        var priorLanguage = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            LocalizedStrings.Instance.SetLanguage("pt-BR");
            CaptureAuxiliaryView(
                new Fashion9Editor { DataContext = new Fashion9EditorViewModel(LoadCaptureSave<SAV9SV>("gen9_scarlet.main")) },
                "fashion9-sv-pt-BR.png", 900, 680, "Portuguese SV fashion catalog");
            CaptureAuxiliaryView(
                new Fashion9Editor { DataContext = new Fashion9EditorViewModel(LoadCaptureSave<SAV9ZA>("gen9a_legendsza.main")) },
                "fashion9-za-pt-BR.png", 900, 680, "Portuguese ZA fashion catalog");
        }
        finally
        {
            LocalizedStrings.Instance.SetLanguage(priorLanguage);
        }
    }

    [AvaloniaFact]
    public void CaptureUnityTowerDetailTabs_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        CaptureAuxiliaryView(
            new UnityTower5Editor { DataContext = new UnityTower5EditorViewModel(new SAV5B2W2()) },
            "unity-tower-locations.png", 620, 620, "Gen 5 Unity Tower locations");
        var floors = new UnityTower5Editor { DataContext = new UnityTower5EditorViewModel(new SAV5B2W2()) };
        floors.FindControl<TabControl>("DetailTabs")!.SelectedIndex = 1;
        CaptureAuxiliaryView(floors,
            "unity-tower-floors.png", 620, 620, "Gen 5 Unity Tower floors");
    }

    [AvaloniaFact]
    public void CapturePssContactPreview_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        CaptureAuxiliaryView(
            new PssExportView
            {
                DataContext = new PssExportViewModel(
                    global::PKHeX.Avalonia.Tests.PssExportTests.MakeMultiRecordSave(),
                    Mock.Of<IClipboardService>(), new RecordingDialogService()),
            },
            "pss-contact-preview.png", 620, 520, "Gen 6 PSS contact preview");
    }

    [AvaloniaFact]
    public void CaptureFunfestMissions_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        var view = new EntralinkEditor { DataContext = new EntralinkEditorViewModel(new SAV5B2W2()) };
        view.FindControl<TabControl>("EntralinkTabs")!.SelectedIndex = 1;
        CaptureAuxiliaryView(view, "funfest-missions.png", 640, 550, "B2W2 Funfest mission editor");
    }

    [AvaloniaFact]
    public void CaptureEntreeForestPreview_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;
        var priorLanguage = LocalizedStrings.Instance.CurrentLanguage;
        try
        {
            foreach (var language in new[] { "en", "de", "pt-BR" })
            {
                LocalizedStrings.Instance.SetLanguage(language);
                var save = LoadCaptureSave<SAV5B2W2>("gen5_white2.sav");
                var renderer = new global::PKHeX.Avalonia.Services.AvaloniaSpriteRenderer(new global::PKHeX.Application.Services.AppSettings());
                renderer.Initialize(save);
                var vm = new EntralinkEditorViewModel(save, renderer, new RecordingDialogService());
                var selected = vm.SelectedEntreeSlot!;
                selected.Species = 25;
                selected.Move = 33;
                selected.Form = 0;
                selected.Gender = 0;
                selected.Animation = 0;
                var view = new EntralinkEditor { DataContext = vm };
                view.FindControl<TabControl>("EntralinkTabs")!.SelectedIndex = 2;
                CaptureAuxiliaryView(view, $"entree-forest-{language}.png", 640, 550,
                    $"Gen 5 Entree Forest {language} preview");
            }
        }
        finally
        {
            LocalizedStrings.Instance.SetLanguage(priorLanguage);
        }
    }

    [AvaloniaFact]
    public void CapturePokeRadar_Misc4Editor_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        // Poke Radar is a Gen 4 Platinum key item toggled from the Misc editor's checkbox.
        var sav = new SAV4Pt();
        var vm = new Misc4EditorViewModel(sav);
        vm.PokeRadar = true;
        vm.SaveCommand.Execute(null);

        var view = new Misc4Editor { DataContext = vm };
        var window = new Window { Content = view, Width = 520, Height = 400 };
        window.Show();
        PumpToStableLayout(window);

        var path = Path.Combine(CaptureDirectory(), "poke-radar.png");
        var saved = CaptureWindow(window, path);
        if (saved is null)
        {
            output.WriteLine("Skipped: headless drawing mode produced no frame.");
            return;
        }

        Assert.Equal(path, saved);
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        output.WriteLine($"Saved Pt Poke Radar editor screenshot to {path}");
    }

    [AvaloniaFact]
    public void CaptureHyperTrainingAndPokerus_PokemonEditor_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        // A level-100 Gen 9 PKM so hyper training is available (Gen 9 unlocks at level 50), with the
        // ATK hyper-training flag and Pokerus infection set — both should render as checked.
        var sav = new SAV9SV();
        var pk = new PK9 { Species = (ushort)Species.Sprigatito, CurrentLevel = 100 };
        var (vm, _, _) = TestHelpers.CreateTestViewModel(pk, sav);
        Assert.True(vm.CanHyperTrain);
        vm.HyperTrainedATK = true;
        vm.IsPokerusInfected = true;

        var view = new PokemonEditor { DataContext = vm };
        // Match the minimum width of the editor pane in MainWindow so this capture is a meaningful
        // regression artifact for the compact headers and Hyper Training column.
        var window = new Window { Content = view, Width = 360, Height = 620 };
        window.Show();
        PumpToStableLayout(window);

        // The editor opens on the Main tab, which shows neither feature: the hyper-training
        // checkboxes are on the Stats tab (index 1) and the Pokerus checkboxes on the OT/Misc tab
        // (index 4). Select each tab in turn and capture one PNG per feature so the checked state
        // is actually visible in the rendered frame.
        var tabs = view.GetVisualDescendants().OfType<TabControl>().Single();

        tabs.SelectedIndex = 1; // Stats
        PumpToStableLayout(window);
        if (CaptureOrSkip(window, "hyper-training.png", "hyper training") is null)
            return;

        tabs.SelectedIndex = 4; // OT/Misc
        PumpToStableLayout(window);
        CaptureOrSkip(window, "pokerus.png", "Pokerus");
    }

    [AvaloniaFact]
    public void CaptureNativeControls_PokemonEditor_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var sav = new SAV9SV();
        var pk = new PK9 { Species = (ushort)Species.Pikachu, CurrentLevel = 55 };
        var (vm, _, _) = TestHelpers.CreateTestViewModel(pk, sav);
        var view = new PokemonEditor { DataContext = vm };
        var window = new Window { Content = view, Width = 620, Height = 720 };
        window.Show();

        try
        {
            PumpToStableLayout(window);

            // Open a real native ComboBox so the artifact proves the composed field and popup
            // surfaces, not only their closed-state colors. Select the gender list by its actual
            // ComboItem content rather than relying on the order of the editor's hidden fields.
            var gender = view.GetVisualDescendants()
                .OfType<ComboBox>()
                .First(combo => combo.Items.Cast<object>()
                    .OfType<ComboItem>()
                    .Any(item => item.Text == "Male"));
            gender.IsDropDownOpen = true;
            PumpToStableLayout(window);

            CaptureOrSkip(window, "native-controls-pokemon-editor.png", "native control system");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CapturePkmDatabaseScanningState_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var sav = new SAV9SV();
        var vm = new PKMDatabaseViewModel(
            sav,
            new Mock<ISpriteRenderer>().Object,
            new Mock<IDialogService>().Object)
        {
            IsSearching = true,
            SearchProgress = 42,
        };

        var view = new PKMDatabaseView { DataContext = vm };
        var window = new Window { Content = view, Width = 960, Height = 620 };
        window.Show();
        PumpToStableLayout(window);

        CaptureOrSkip(window, "pkm-database-scanning.png", "PKM Database scanning state");
    }

    [AvaloniaFact]
    public void CaptureAuxiliaryEditorStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var dialog = new Mock<IDialogService>().Object;
        CaptureAuxiliaryView(
            new EventFlagsEditor { DataContext = new EventFlagsEditorViewModel(new SAV3E()) },
            "event-flags-editor.png",
            960,
            620,
            "Event Flags editor");
        CaptureAuxiliaryView(
            new MysteryGiftEditor
            {
                DataContext = new MysteryGiftEditorViewModel(new SAV9SV(), dialog, new GiftRecordProvider()),
            },
            "mystery-gift-editor.png",
            960,
            620,
            "Mystery Gift editor");
        CaptureAuxiliaryView(
            new BatchEditor { DataContext = new BatchEditorViewModel(new SAV3E(), dialog) },
            "batch-editor.png",
            980,
            720,
            "Batch editor");
        CaptureAuxiliaryView(
            new Misc3Editor { DataContext = new Misc3EditorViewModel(new SAV3E()) },
            "misc3-editor.png",
            800,
            680,
            "Gen 3 Misc editor");
        CaptureAuxiliaryView(
            new Misc4Editor { DataContext = new Misc4EditorViewModel(new SAV4Pt()) },
            "misc4-editor.png",
            800,
            680,
            "Gen 4 Misc editor");

        using var host = new HeadlessAppFixture();
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        host.LoadSave(Path.Combine(saveDirectory!, "gen9_scarlet.main"));
        var save = host.Save as SAV9SV
            ?? throw new InvalidOperationException("The Gen 9 capture save could not be loaded.");
        CaptureAuxiliaryView(
            new Misc9Editor
            {
                DataContext = new Misc9EditorViewModel(save),
            },
            "misc9-editor.png",
            800,
            680,
            "Gen 9 Misc editor");
    }

    [AvaloniaFact]
    public void CaptureLegacyMiscEditorStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        CaptureAuxiliaryView(
            new Misc2Editor
            {
                DataContext = new Misc2EditorViewModel(
                    LoadCaptureSave<SAV2>("gen2_crystal.sav")),
            },
            "misc2-editor.png",
            800,
            620,
            "Gen 2 Misc editor");
        CaptureAuxiliaryView(
            new Misc5Editor
            {
                DataContext = new Misc5EditorViewModel(
                    LoadCaptureSave<SAV5>("gen5_black.sav")),
            },
            "misc5-editor.png",
            920,
            700,
            "Gen 5 Misc editor");
        CaptureAuxiliaryView(
            new Misc7Editor
            {
                DataContext = new Misc7EditorViewModel(
                    LoadCaptureSave<SAV7>("gen7_sun.main")),
            },
            "misc7-editor.png",
            920,
            700,
            "Gen 7 Misc editor");
        CaptureAuxiliaryView(
            new Misc7bEditor
            {
                DataContext = new Misc7bEditorViewModel(new SAV7b()),
            },
            "misc7b-editor.png",
            800,
            620,
            "LGPE Misc editor");
        CaptureAuxiliaryView(
            new Misc8Editor
            {
                DataContext = new Misc8EditorViewModel(new SAV8SWSH()),
            },
            "misc8-editor.png",
            800,
            620,
            "Gen 8 Misc editor");
        CaptureAuxiliaryView(
            new Misc8bEditor
            {
                DataContext = new Misc8bEditorViewModel(new SAV8BS()),
            },
            "misc8b-editor.png",
            900,
            700,
            "BDSP Misc editor");
    }

    [AvaloniaFact]
    public void CaptureComplexEditorStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var dialog = new Mock<IDialogService>().Object;
        var spriteRenderer = new Mock<ISpriteRenderer>().Object;
        CaptureAuxiliaryView(
            new PokeathlonEditor
            {
                DataContext = new PokeathlonEditorViewModel(LoadCaptureSave<SAV4HGSS>("gen4_heartgold.sav"), spriteRenderer),
            },
            "pokeathlon-editor.png",
            980,
            760,
            "Pokéathlon editor");
        CaptureAuxiliaryView(
            new JoinAvenueEditor
            {
                DataContext = new JoinAvenueEditorViewModel(LoadCaptureSave<SAV5B2W2>("gen5_white2.sav"), spriteRenderer, dialog),
            },
            "join-avenue-editor.png",
            980,
            760,
            "Join Avenue editor");
        CaptureAuxiliaryView(
            new GlobalLink5Editor
            {
                DataContext = new GlobalLink5EditorViewModel(LoadCaptureSave<SAV5>("gen5_white2.sav"), spriteRenderer),
            },
            "global-link-editor.png",
            760,
            720,
            "Global Link editor");
        CaptureAuxiliaryView(
            new MedalEditorView
            {
                DataContext = new MedalEditorViewModel(LoadCaptureSave<SaveFile>("gen5_white2.sav"), dialog),
            },
            "medal-editor.png",
            980,
            720,
            "Medal editor");
        CaptureAuxiliaryView(
            new FashionEditorView
            {
                DataContext = new FashionEditorViewModel(new SAV8SWSH()),
            },
            "fashion-editor.png",
            760,
            620,
            "Fashion editor");
        CaptureAuxiliaryView(
            new DonutEditor
            {
                DataContext = new DonutEditorViewModel(LoadCaptureSave<SaveFile>("gen9a_legendsza.main")),
            },
            "donut-editor.png",
            820,
            700,
            "Donut editor");
    }

    [AvaloniaFact]
    public void CaptureReviewFindingEditors_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var dialog = new Mock<IDialogService>().Object;
        var spriteRenderer = new Mock<ISpriteRenderer>().Object;
        CaptureAuxiliaryView(
            new BoxLayoutEditor { DataContext = new BoxLayoutEditorViewModel(new SAV4Pt()) },
            "review-box-layout-editor.png",
            980,
            720,
            "Box Layout editor review state");
        CaptureAuxiliaryView(
            new DaycareEditorView
            {
                DataContext = new DaycareEditorViewModel(new SAV8SWSH(), spriteRenderer),
            },
            "review-daycare-editor.png",
            620,
            520,
            "Daycare editor review state");
        CaptureAuxiliaryView(
            new ZygardeCellEditor
            {
                DataContext = new ZygardeCellEditorViewModel(LoadCaptureSave<SAV7USUM>("gen7_ultrasun.main")),
            },
            "review-totem-sticker-editor.png",
            900,
            720,
            "Totem Sticker editor review state");
        CaptureAuxiliaryView(
            new LegalityAuditView
            {
                DataContext = new LegalityAuditViewModel(LoadCaptureSave<SAV3E>("gen3_emerald.sav"), dialog),
            },
            "review-legality-audit.png",
            1100,
            700,
            "Legality Audit review state");
    }

    [AvaloniaFact]
    public void CaptureIssueSweepNextEditors_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var pokemon = new PK9
        {
            Species = (ushort)Species.Pikachu,
            CurrentLevel = 50,
            EncryptionConstant = 0x12345678,
        };
        CaptureAuxiliaryView(
            new SealStickers8bEditor { DataContext = new SealStickers8bEditorViewModel(new SAV8BS()) },
            "next-seal-stickers-editor.png",
            900,
            650,
            "BDSP Seal Stickers editor");
        CaptureAuxiliaryView(
            new TechRecordEditor { DataContext = new TechRecordEditorViewModel((ITechRecord)pokemon, pokemon) },
            "next-tech-record-editor.png",
            760,
            620,
            "Technical Records editor");
        CaptureAuxiliaryView(
            new RibbonEditor { DataContext = new RibbonEditorViewModel(pokemon) },
            "next-ribbon-editor.png",
            760,
            680,
            "Ribbon editor");
        CaptureAuxiliaryView(
            new Pokedex8bEditor { DataContext = new Pokedex8bEditorViewModel(new SAV8BS()) },
            "next-pokedex8b-editor.png",
            1000,
            720,
            "BDSP Pokédex editor");
    }

    [AvaloniaFact]
    public void CaptureRemainingDialogStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        var spriteRenderer = new Mock<ISpriteRenderer>().Object;
        CaptureAuxiliaryView(
            new OPowerEditor { DataContext = new OPowerEditorViewModel(new SAV6XY()) },
            "o-power-editor.png",
            820,
            620,
            "O-Power editor");
        CaptureAuxiliaryView(
            new PokebeanEditor { DataContext = new PokebeanEditorViewModel(new SAV7SM()) },
            "pokebean-editor.png",
            820,
            620,
            "Poké Beans editor");
        CaptureAuxiliaryView(
            new PoketchEditorView { DataContext = new PoketchEditorViewModel(new SAV4Pt()) },
            "poketch-editor.png",
            820,
            620,
            "Pokétch editor");
        CaptureAuxiliaryView(
            new RaidEditor { DataContext = new RaidEditorViewModel(new SAV8SWSH()) },
            "raid-editor.png",
            820,
            620,
            "Raid editor");
        CaptureAuxiliaryView(
            new Raid9Editor { DataContext = new Raid9EditorViewModel(LoadCaptureSave<SAV9SV>("gen9_scarlet.main")) },
            "raid9-editor.png",
            820,
            620,
            "Gen 9 Raid editor");
        CaptureAuxiliaryView(
            new SecretBaseEditor
            {
                DataContext = new SecretBaseEditorViewModel(new SAV6XY(), spriteRenderer),
            },
            "secret-base-editor.png",
            900,
            680,
            "Secret Base editor");
    }

    [AvaloniaFact]
    public void CaptureLegacyUtilityStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        CaptureAuxiliaryView(
            new AccessorEditor { DataContext = new AccessorEditorViewModel(new SAV6XY()) },
            "accessor-editor.png",
            900,
            620,
            "Accessor editor");
        CaptureAuxiliaryView(
            new BoxListEditor { DataContext = new BoxListEditorViewModel(new SAV6XY()) },
            "box-list-editor.png",
            720,
            560,
            "Box list editor");
        CaptureAuxiliaryView(
            new EventWorkEditor { DataContext = new EventWorkEditorViewModel(new SAV7b()) },
            "event-work-editor.png",
            820,
            620,
            "Event work editor");
        CaptureAuxiliaryView(
            new Misc8aEditor { DataContext = new Misc8aEditorViewModel(LoadCaptureSave<SAV8LA>("gen8a_legendsarceus.main")) },
            "misc8a-editor.png",
            820,
            620,
            "Legends: Arceus Misc editor");
        CaptureAuxiliaryView(
            new SecretBase3Editor { DataContext = new SecretBase3EditorViewModel(new SAV3E()) },
            "secret-base3-editor.png",
            820,
            620,
            "Gen 3 Secret Base editor");
        CaptureAuxiliaryView(
            new SealStickers8bEditor { DataContext = new SealStickers8bEditorViewModel(new SAV8BS()) },
            "seal-stickers8b-editor.png",
            820,
            620,
            "BDSP Seal Stickers editor");
        CaptureAuxiliaryView(
            new Underground8bEditor { DataContext = new Underground8bEditorViewModel(new SAV8BS()) },
            "underground8b-editor.png",
            900,
            620,
            "BDSP Underground editor");
    }

    [AvaloniaFact]
    public void CaptureRemainingUtilityDialogStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        CaptureAuxiliaryView(
            new EventFlags2Editor { DataContext = new EventFlags2EditorViewModel(new SAV2()) },
            "event-flags2-editor.png",
            900,
            680,
            "Gen 2 Event Flags editor");
        CaptureAuxiliaryView(
            new EventReset1Editor { DataContext = new EventReset1EditorViewModel(new SAV1()) },
            "event-reset1-editor.png",
            900,
            680,
            "Gen 1 Event Reset editor");
        CaptureAuxiliaryView(
            new LegalityView { DataContext = new LegalityViewModel("Legal\nNo issues found.") },
            "legality-editor.png",
            700,
            500,
            "Legality report");
        CaptureAuxiliaryView(
            new Fashion9Editor { DataContext = new Fashion9EditorViewModel(LoadCaptureSave<SAV9SV>("gen9_scarlet.main")) },
            "fashion9-editor.png",
            900,
            680,
            "Gen 9 Fashion editor");
        CaptureAuxiliaryView(
            new MailBoxEditor { DataContext = new MailBoxEditorViewModel(new SAV2()) },
            "mail-box-editor.png",
            900,
            680,
            "Mail Box editor");
        CaptureAuxiliaryView(
            new PokepuffEditor { DataContext = new PokepuffEditorViewModel(new SAV6XY()) },
            "poke-puff-editor.png",
            900,
            680,
            "Poké Puff editor");
        CaptureAuxiliaryView(
            new TrashEditor
            {
                DataContext = new TrashEditorViewModel("PIKA", null, new PK4 { Species = 25 }, 4, EntityContext.Gen4),
            },
            "trash-editor.png",
            820,
            620,
            "Trash editor");
    }

    [AvaloniaFact]
    public void CaptureDatabaseToolStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        using var host = new HeadlessAppFixture();
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        host.LoadSave(Path.Combine(saveDirectory!, "gen9_scarlet.main"));
        var save = host.Save ?? throw new InvalidOperationException("The database capture save could not be loaded.");
        var spriteRenderer = host.Services.GetRequiredService<ISpriteRenderer>();

        CaptureAuxiliaryView(
            new PKMDatabaseView
            {
                DataContext = new PKMDatabaseViewModel(save, spriteRenderer, host.Dialogs),
            },
            "pkm-database-editor.png",
            1100,
            700,
            "PKM Database");
        CaptureAuxiliaryView(
            new EncounterDatabaseView
            {
                DataContext = new EncounterDatabaseViewModel(save, spriteRenderer, host.Dialogs, _ => { }),
            },
            "encounter-database-editor.png",
            900,
            650,
            "Encounter Database");
        CaptureAuxiliaryView(
            new BoxReportView { DataContext = new BoxReportViewModel(save, host.Dialogs) },
            "box-report-editor.png",
            1100,
            600,
            "Box Data Report");
        CaptureAuxiliaryView(
            new LegalityAuditView { DataContext = new LegalityAuditViewModel(save, host.Dialogs) },
            "legality-audit-editor.png",
            1100,
            600,
            "Legality Audit");
        CaptureAuxiliaryView(
            new MysteryGiftDatabaseView
            {
                DataContext = new MysteryGiftDatabaseViewModel(save, spriteRenderer, host.Dialogs),
            },
            "mystery-gift-database-editor.png",
            1100,
            700,
            "Mystery Gift Database");
    }

    [AvaloniaFact]
    public void CapturePokedexEditorStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        CaptureAuxiliaryView(
            new Pokedex6Editor { DataContext = new Pokedex6EditorViewModel(new SAV6XY()) },
            "pokedex6-editor.png",
            1000,
            680,
            "Gen 6 Pokédex editor");
        CaptureAuxiliaryView(
            new Pokedex7bEditor { DataContext = new Pokedex7bEditorViewModel(new SAV7b()) },
            "pokedex7b-editor.png",
            920,
            650,
            "LGPE Pokédex editor");

        using var host = new HeadlessAppFixture();
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        host.LoadSave(Path.Combine(saveDirectory!, "gen9_scarlet.main"));
        var save = host.Save as SAV9SV
            ?? throw new InvalidOperationException("The Gen 9 Pokédex capture save could not be loaded.");
        CaptureAuxiliaryView(
            new PokedexGen9Editor { DataContext = new PokedexGen9EditorViewModel(save) },
            "pokedex-gen9-editor.png",
            900,
            620,
            "Gen 9 Pokédex editor");
        CaptureAuxiliaryView(
            new PokedexLAEditor { DataContext = new PokedexLAEditorViewModel(BlankSaveFile.Get(GameVersion.PLA)) },
            "pokedex-la-editor.png",
            1000,
            680,
            "Legends Pokédex editor");
    }

    [AvaloniaFact]
    public void CaptureDensityModes_MainWindow_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        using var app = new HeadlessAppFixture();
        app.Window.Width = 1024;
        app.Window.Height = 720;
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        var density = app.Services.GetRequiredService<IUiDensityService>();
        // The declarative application resources already carry the Compact compatibility default.
        // Capture that initial frame without a same-value resource notification; later mode changes
        // are captured after the explicit repaint barrier in CaptureDensityMode.
        app.LoadSave(Path.Combine(saveDirectory!, "gen9a_legendsza.main"));

        try
        {
            CaptureCurrentDensity(app, "mainwindow-density-compact.png");
            CaptureDensityMode(app, density, AppDensity.Comfortable, "mainwindow-density-comfortable.png");
        }
        finally
        {
            // Do not leave the process-wide resource dictionary in the capture-only mode for any
            // later opt-in captures running in the same test process.
            density.ApplyDensity(AppDensity.Compact);
        }
    }

    [AvaloniaFact]
    public void CaptureTaskAwareShellStates_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        using var app = new HeadlessAppFixture();
        app.Window.Width = 1024;
        app.Window.Height = 720;
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        app.LoadSave(Path.Combine(saveDirectory!, "gen9a_legendsza.main"));
        app.ClickSlot(0, 0);
        var boxView = app.Find<BoxViewer>();
        Assert.NotNull(boxView);
        app.Focus(boxView!);
        app.PressKey(PhysicalKey.Enter);
        app.Pump();

        // Use a fresh top-level surface for every shell state so each visual artifact is a complete
        // repaint, including the initial Pokémon workspace after the save-loaded tree is realized.
        app.Pump();
        CaptureFreshShellState(app, "shell-pokemon.png", "Pokémon workspace");

        app.ViewModel.SelectedWorkspaceIndex = 1;
        app.Pump();
        CaptureFreshShellState(app, "shell-party.png", "Party workspace");
        app.ViewModel.SelectedWorkspaceIndex = 0;
        app.Pump();

        app.ViewModel.ActiveWorkspace = MainWorkspace.Save;
        app.Pump();
        CaptureFreshShellState(app, "shell-save.png", "Save workspace");

        app.ViewModel.SelectedWorkspaceIndex = 3;
        app.Pump();
        CaptureFreshShellState(app, "shell-inventory.png", "Inventory workspace");

        app.ViewModel.ActiveWorkspace = MainWorkspace.Reports;
        app.Pump();
        CaptureFreshShellState(app, "shell-reports.png", "Reports workspace");

        app.ViewModel.IsToolLauncherOpen = true;
        app.Pump();
        CaptureFreshShellState(app, "shell-launcher.png", "Tool launcher");
        app.ViewModel.IsToolLauncherOpen = false;
    }

    [AvaloniaFact]
    public void CaptureToolsMenu_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        using var app = new HeadlessAppFixture();
        app.Window.Width = 1024;
        app.Window.Height = 720;
        app.LoadSaveInstance(new SAV6XY());

        var toolsMenu = app.Window.GetVisualDescendants()
            .OfType<MenuItem>()
            .Single(menu => Equals(menu.Header, LocalizedStrings.Instance["Menu_Tools"]));
        toolsMenu.IsSubMenuOpen = true;
        app.Pump();

        CaptureShellState(app.Window, "tools-menu.png", "capability-driven Tools menu");
        toolsMenu.IsSubMenuOpen = false;
    }

    [AvaloniaFact]
    public void CaptureThemeVariants_WhenEnabled_WritesPng()
    {
        if (SkipWhenCaptureDisabled())
            return;

        using var app = new HeadlessAppFixture();
        app.Window.Width = 1024;
        app.Window.Height = 720;
        var saveDirectory = SaveFileFixture.FindSaveFilesPath();
        Assert.NotNull(saveDirectory);
        app.LoadSave(Path.Combine(saveDirectory!, "gen9a_legendsza.main"));

        var theme = app.Services.GetRequiredService<IThemeService>();
        try
        {
            foreach (var (variant, fileName) in new[]
                     {
                         (AppTheme.Dark, "shell-theme-dark.png"),
                         (AppTheme.Light, "shell-theme-light.png"),
                     })
            {
                theme.ApplyTheme(variant);
                app.Pump();
                CaptureFreshShellState(app, fileName, $"{variant} theme");
            }
        }
        finally
        {
            theme.ApplyTheme(AppTheme.Dark);
        }
    }

    private bool SkipWhenCaptureDisabled()
    {
        if (Environment.GetEnvironmentVariable("PKHEX_HEADLESS_CAPTURE") == "1")
            return false;
        output.WriteLine("Skipped: set PKHEX_HEADLESS_CAPTURE=1 with the Skia headless app builder.");
        return true;
    }

    private static string CaptureDirectory() =>
        Environment.GetEnvironmentVariable("PKHEX_HEADLESS_CAPTURE_DIR")
        ?? Path.Combine(Path.GetTempPath(), "pkhex-headless-frames");

    private static void PumpToStableLayout(Window window)
    {
        for (var i = 0; i < 10; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            // Commit the visual tree into the server-side composition scene.
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    private string? CaptureOrSkip(Window window, string fileName, string featureLabel)
    {
        var path = Path.Combine(CaptureDirectory(), fileName);
        var saved = CaptureWindow(window, path);
        if (saved is null)
        {
            output.WriteLine("Skipped: headless drawing mode produced no frame.");
            return null;
        }

        Assert.Equal(path, saved);
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        output.WriteLine($"Saved Pokemon editor ({featureLabel}) screenshot to {path}");
        return saved;
    }

    private void CaptureDensityMode(HeadlessAppFixture app, IUiDensityService density, AppDensity mode, string fileName)
    {
        var changed = density.CurrentDensity != mode;
        if (changed)
            density.ApplyDensity(mode);
        var captureWindow = changed
            ? new MainWindow
            {
                DataContext = app.ViewModel,
                Width = app.Window.Width,
                Height = app.Window.Height,
            }
            : app.Window;

        try
        {
            if (changed)
                captureWindow.Show();
            app.Pump();
            // A fresh top-level surface gives Skia a complete repaint after a DynamicResource
            // replacement. This avoids treating a valid runtime reflow as a partial screenshot.
            if (changed)
                PumpToStableLayout(captureWindow);

            var path = Path.Combine(CaptureDirectory(), fileName);
            var saved = CaptureWindow(captureWindow, path);
            if (saved is null)
            {
                output.WriteLine("Skipped: headless drawing mode produced no frame.");
                return;
            }

            Assert.Equal(path, saved);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            output.WriteLine($"Saved {mode} density screenshot to {path}");
        }
        finally
        {
            if (changed)
                captureWindow.Close();
        }
    }

    private void CaptureCurrentDensity(HeadlessAppFixture app, string fileName)
    {
        app.Pump();

        var path = Path.Combine(CaptureDirectory(), fileName);
        var saved = CaptureWindow(app.Window, path);
        if (saved is null)
        {
            output.WriteLine("Skipped: headless drawing mode produced no frame.");
            return;
        }

        Assert.Equal(path, saved);
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        output.WriteLine($"Saved initial Compact density screenshot to {path}");
    }

    private void CaptureAuxiliaryView(Control view, string fileName, double width, double height, string stateLabel)
    {
        var window = new Window { Content = view, Width = width, Height = height };
        window.Show();
        try
        {
            PumpToStableLayout(window);
            var path = Path.Combine(CaptureDirectory(), fileName);
            var saved = CaptureWindow(window, path);
            if (saved is null)
            {
                output.WriteLine("Skipped: headless drawing mode produced no frame.");
                return;
            }

            Assert.Equal(path, saved);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
            output.WriteLine($"Saved {stateLabel} screenshot to {path}");
        }
        finally
        {
            window.Close();
        }
    }

    private static TSave LoadCaptureSave<TSave>(string fileName)
        where TSave : SaveFile
    {
        var saveDirectory = SaveFileFixture.FindSaveFilesPath()
            ?? throw new InvalidOperationException("The capture save directory could not be found.");
        return SaveFileFixture.LoadSave(Path.Combine(saveDirectory, fileName)) as TSave
            ?? throw new InvalidOperationException($"The capture save {fileName} could not be loaded as {typeof(TSave).Name}.");
    }

    private void CaptureFreshShellState(HeadlessAppFixture app, string fileName, string stateLabel)
    {
        var window = new MainWindow
        {
            DataContext = app.ViewModel,
            Width = app.Window.Width,
            Height = app.Window.Height,
        };
        window.Show();
        try
        {
            PumpToStableLayout(window);
            CaptureShellState(window, fileName, stateLabel);
        }
        finally
        {
            window.Close();
        }
    }

    private void CaptureShellState(Window window, string fileName, string stateLabel)
    {
        var path = Path.Combine(CaptureDirectory(), fileName);
        var saved = CaptureWindow(window, path);
        if (saved is null)
        {
            output.WriteLine("Skipped: headless drawing mode produced no frame.");
            return;
        }

        Assert.Equal(path, saved);
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        output.WriteLine($"Saved {stateLabel} screenshot to {path}");
    }

    private static string? CaptureWindow(Window window, string pngPath)
    {
        WriteableBitmap? frame;
        try
        {
            // Throws NotSupportedException under the default headless drawing mode (no real pixels);
            // only succeeds when the assembly's app builder enables Skia + UseHeadlessDrawing = false.
            frame = window.GetLastRenderedFrame();
        }
        catch (NotSupportedException)
        {
            return null;
        }
        if (frame is null)
            return null;
        Directory.CreateDirectory(Path.GetDirectoryName(pngPath)!);
        using var fs = File.Create(pngPath);
        frame.Save(fs);
        return pngPath;
    }
}
