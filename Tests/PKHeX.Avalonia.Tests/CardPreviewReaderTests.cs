using PKHeX.Application.UseCases;
using PKHeX.Core;

namespace PKHeX.Avalonia.Tests;

public class CardPreviewReaderTests
{
    [Fact]
    public void MultiItemCardIncludesBothQuantitiesAndMetadataWithoutMutation()
    {
        var gift = new WC7 { IsItem = true, CardID = 123, GiftRepeatable = true, GiftUsed = true, GiftOncePerDay = true };
        gift.SetItem(0, 100);
        gift.SetQuantity(0, 2);
        gift.SetItem(1, 101);
        gift.SetQuantity(1, 3);
        var bytes = gift.Data.ToArray();
        var preview = new ReadCardPreviewUseCase().Execute(gift);
        Assert.Equal([new CardPreviewItem(100, 2), new CardPreviewItem(101, 3)], preview.Items);
        Assert.True(preview.Collected);
        Assert.True(preview.Repeatable);
        Assert.True(preview.OncePerDay);
        Assert.Equal(bytes, gift.Data.ToArray());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(70)]
    [InlineData(8)]
    [InlineData(80)]
    [InlineData(81)]
    [InlineData(9)]
    [InlineData(90)]
    public void PokemonTemplatesAreReadOnlyAcrossFormats(int format)
    {
        DataMysteryGift gift = format switch
        {
            5 => new PGF(), 6 => new WC6(), 7 => new WC7(), 70 => new WB7(),
            8 => new WC8(), 80 => new WA8(), 81 => new WB8(), 9 => new WC9(), _ => new WA9(),
        };
        gift.IsEntity = true;
        gift.Species = 25;
        gift.HeldItem = 100;
        var bytes = gift.Data.ToArray();
        var preview = new ReadCardPreviewUseCase().Execute(gift);
        Assert.Equal("Pokemon", preview.Kind);
        Assert.Contains(preview.Fields, field => field.Key == "Species" && field.Value == "25");
        Assert.Contains(preview.Fields, field => field.Key == "HeldItem" && field.Value == "100");
        Assert.Equal(bytes, gift.Data.ToArray());
    }
}
