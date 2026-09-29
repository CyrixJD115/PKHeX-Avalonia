using System.Buffers.Binary;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Moq;
using PKHeX.Application.Abstractions;
using PKHeX.Application.UseCases;
using PKHeX.Avalonia.Tests.Harness;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.ViewModels;

namespace PKHeX.Avalonia.Tests;

public sealed class ChatterAudioWorkflowTests
{
    [Fact]
    public async Task NativePlaybackSmoke_WhenEnabled()
    {
        if (Environment.GetEnvironmentVariable("PKHEX_AUDIO_PLAYBACK_QA") != "1" || !OperatingSystem.IsWindows())
            return;
        var service = new global::PKHeX.Avalonia.Services.PlatformAudioPlaybackService();
        Assert.True(service.IsAvailable);
        var pcm = Enumerable.Repeat((byte)0x88, IChatter.SIZE_PCM).ToArray();
        Assert.True(await service.PlayWavAsync(ChatterAudioCodec.ToWav(pcm)));
    }

    private static SAV5B2W2 LoadWhite2()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../savefiles/gen5_white2.sav"));
        return Assert.IsType<SAV5B2W2>(FileUtil.GetSupportedFile(path));
    }

    [Fact]
    public void PackedPcmConvertsToTheExpectedMonoWav()
    {
        var pcm = new byte[IChatter.SIZE_PCM];
        pcm[0] = 0xAB;
        var wav = ChatterAudioCodec.ToWav(pcm);
        Assert.Equal(2044, wav.Length);
        Assert.Equal("RIFF"u8.ToArray(), wav[..4]);
        Assert.Equal("WAVE"u8.ToArray(), wav[8..12]);
        Assert.Equal((uint)2036, BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(4)));
        Assert.Equal((uint)2000, BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(24)));
        Assert.Equal((uint)2000, BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(40)));
        Assert.Equal((byte)0xB0, wav[44]);
        Assert.Equal((byte)0xA0, wav[45]);
        Assert.Throws<ArgumentException>(() => ChatterAudioCodec.ToWav(new byte[999]));
    }

    [Fact]
    public async Task ImportExportPlaybackAndCancelKeepTheSourceUntouched()
    {
        var save = LoadWhite2();
        var sourceBytes = save.Data.ToArray();
        var pcm = Enumerable.Range(0, IChatter.SIZE_PCM).Select(i => (byte)i).ToArray();
        var importPath = Path.Combine(Path.GetTempPath(), $"pkhex-chatter-import-{Guid.NewGuid():N}.pcm");
        var exportPath = Path.Combine(Path.GetTempPath(), $"pkhex-chatter-export-{Guid.NewGuid():N}.pcm");
        var wavPath = Path.Combine(Path.GetTempPath(), $"pkhex-chatter-export-{Guid.NewGuid():N}.wav");
        try
        {
            await File.WriteAllBytesAsync(importPath, pcm);
            var dialogs = new RecordingDialogService { OpenFileResult = importPath };
            byte[]? played = null;
            var audio = new Mock<IAudioPlaybackService>();
            audio.SetupGet(a => a.IsAvailable).Returns(true);
            audio.Setup(a => a.PlayWavAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(wav => played = wav).ReturnsAsync(true);
            var vm = new ChatterEditorViewModel(save, dialogs, audio.Object);

            await vm.ImportPcmCommand.ExecuteAsync(null);
            Assert.True(vm.HasRecording);
            Assert.True(vm.Initialized);
            Assert.Equal(sourceBytes, save.Data.ToArray());
            Assert.True(vm.PlayRecordingCommand.CanExecute(null));

            dialogs.SaveFileResult = exportPath;
            await vm.ExportPcmCommand.ExecuteAsync(null);
            Assert.Equal(pcm, await File.ReadAllBytesAsync(exportPath));
            dialogs.SaveFileResult = wavPath;
            await vm.ExportWavCommand.ExecuteAsync(null);
            var wavBytes = await File.ReadAllBytesAsync(wavPath);
            Assert.Equal(ChatterAudioCodec.ToWav(pcm), wavBytes);

            await vm.PlayRecordingCommand.ExecuteAsync(null);
            Assert.Equal(wavBytes, played);
            vm.CancelCommand.Execute(null);
            Assert.Equal(sourceBytes, save.Data.ToArray());
        }
        finally
        {
            foreach (var path in new[] { importPath, exportPath, wavPath })
                if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task InvalidImportDoesNotMutateAndValidImportSavesForGen4AndGen5()
    {
        var paths = new[]
        {
            "gen4_platinum.sav", "gen5_white2.sav",
        };
        foreach (var name in paths)
        {
            var fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../savefiles", name));
            var save = Assert.IsAssignableFrom<SaveFile>(FileUtil.GetSupportedFile(fixture));
            var sourceBytes = save.Data.ToArray();
            var file = Path.Combine(Path.GetTempPath(), $"pkhex-chatter-{Guid.NewGuid():N}.pcm");
            try
            {
                var dialogs = new RecordingDialogService { OpenFileResult = file };
                var vm = new ChatterEditorViewModel(save, dialogs);
                await File.WriteAllBytesAsync(file, new byte[999]);
                await vm.ImportPcmCommand.ExecuteAsync(null);
                Assert.Single(dialogs.Errors);
                Assert.Equal(sourceBytes, save.Data.ToArray());

                var pcm = Enumerable.Repeat((byte)0x7A, IChatter.SIZE_PCM).ToArray();
                await File.WriteAllBytesAsync(file, pcm);
                await vm.ImportPcmCommand.ExecuteAsync(null);
                vm.SaveCommand.Execute(null);
                Assert.True(save.State.Edited);
                Assert.Equal(pcm, GetChatter(save).Recording.ToArray());
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

    [Fact]
    public void ClearRecordingIsStagedUntilSave()
    {
        var save = LoadWhite2();
        GetChatter(save).Recording.Fill(0x7A);
        GetChatter(save).Initialized = true;
        var before = save.Data.ToArray();
        var vm = new ChatterEditorViewModel(save);
        vm.ClearRecordingCommand.Execute(null);
        vm.CancelCommand.Execute(null);
        Assert.Equal(before, save.Data.ToArray());

        vm = new ChatterEditorViewModel(save);
        vm.ClearRecordingCommand.Execute(null);
        vm.SaveCommand.Execute(null);
        Assert.False(GetChatter(save).Initialized);
        Assert.All(GetChatter(save).Recording.ToArray(), value => Assert.Equal((byte)0, value));
    }

    [AvaloniaFact]
    public void CompactViewShowsActionsAndDisablesPlaybackWithoutAudioService()
    {
        var vm = new ChatterEditorViewModel(LoadWhite2(), new RecordingDialogService());
        var view = new ChatterEditor { DataContext = vm, Width = 620, Height = 340 };
        var window = new Window { Content = view, Width = 640, Height = 360 };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.NotNull(view.FindControl<Button>("ChatterImportButton"));
            Assert.NotNull(view.FindControl<Button>("ChatterSaveButton"));
            Assert.False(vm.PlayRecordingCommand.CanExecute(null));
        }
        finally
        {
            window.Close();
        }
    }

    private static IChatter GetChatter(SaveFile save) => save switch
    {
        SAV4 sav4 => sav4.Chatter,
        SAV5 sav5 => sav5.Chatter,
        _ => throw new ArgumentException("Not a Chatter save."),
    };
}
