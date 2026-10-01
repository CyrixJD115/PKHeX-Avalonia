using System.Globalization;
using PKHeX.Core;

namespace PKHeX.Application.UseCases;

public sealed record CardPreviewField(string Key, string Value);
public sealed record CardPreviewItem(int Id, int Quantity);
public sealed record CardPreview(int Id, string Title, string Format, string Kind,
    bool? Collected, bool? Repeatable, bool? OncePerDay,
    IReadOnlyList<CardPreviewItem> Items, IReadOnlyList<CardPreviewField> Fields);

/// <summary>Reads the stored template without generating a Pokémon or normalizing card bytes.</summary>
public sealed class ReadCardPreviewUseCase
{
    public CardPreview Execute(DataMysteryGift gift)
    {
        var items = new List<CardPreviewItem>();
        var fields = new List<CardPreviewField>();
        void Add(string key, object value) => fields.Add(new(key, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
        string kind = gift.IsEntity ? (gift.IsEgg ? "Egg" : "Pokemon") : gift.IsItem ? "Item" : "Other";
        if (gift.IsEntity)
        {
            Add("Species", gift.Species);
            Add("Form", gift.Form);
            Add("Level", gift.Level);
            Add("Gender", gift.Gender);
            Add("Ball", gift.Ball);
            Add("HeldItem", gift.HeldItem);
            Add("OriginalTrainer", gift.OriginalTrainerName);
            Add("TrainerId", gift.DisplayTID);
            Add("SecretId", gift.DisplaySID);
            Add("Location", gift.Location);
            Add("EggLocation", gift.EggLocation);
            var moves = gift.Moves;
            Add("Moves", string.Join(",", new[] { moves.Move1, moves.Move2, moves.Move3, moves.Move4 }));
            Add("Ability", gift.Ability);
            try { Add("Shiny", gift.Shiny); }
            catch (ArgumentOutOfRangeException) { Add("Shiny", string.Empty); }
            if (gift is INature nature) Add("Nature", (int)nature.Nature);
            int[] ivs = new int[6];
            gift.GetIVs(ivs);
            Add("IVs", string.Join(" / ", ivs));
            if (gift is WC7 w7 && w7.AdditionalItem != 0) Add("AdditionalItem", w7.AdditionalItem);
        }
        else if (gift.IsItem)
        {
            int count = gift switch { WC7 => (gift.Data.Length - 0x68) / 4, WB7 or WC8 or WB8 or WA8 or WC9 or WA9 => 6, _ => 1 };
            for (int i = 0; i < count; i++)
            {
                int id = gift switch { WC7 x => x.GetItem(i), WB7 x => x.GetItem(i), WC8 x => x.GetItem(i), WB8 x => x.GetItem(i), WA8 x => x.GetItem(i), WC9 x => x.GetItem(i), WA9 x => x.GetItem(i), _ => gift.ItemID };
                int quantity = gift switch { WC7 x => x.GetQuantity(i), WB7 x => x.GetQuantity(i), WC8 x => x.GetQuantity(i), WB8 x => x.GetQuantity(i), WA8 x => x.GetQuantity(i), WC9 x => x.GetQuantity(i), WA9 x => x.GetQuantity(i), _ => gift.Quantity };
                if (id == 0) break;
                items.Add(new(id, quantity));
            }
        }
        else
        {
            kind = gift switch
            {
                WC7 { IsBP: true } => "BattlePoints", WC7 { IsBean: true } => "Beans",
                PGF { IsPower: true } => "Power",
                WC8 x => x.CardType.ToString(), WB8 x => x.CardType.ToString(),
                WA8 x => x.CardType.ToString(), WC9 x => x.CardType.ToString(), WA9 x => x.CardType.ToString(),
                _ => "Other",
            };
            Add("PayloadId", gift.ItemID);
            Add("Quantity", gift.Quantity);
            if (gift is PGT pgt) Add("OtherType", (int)pgt.GiftType);
            if (gift is PCD pcd) Add("OtherType", (int)pcd.Gift.GiftType);
        }
        bool? collected = gift is PGF or WC6 or WC7 or WB7 ? gift.GiftUsed : null;
        bool? repeatable = gift switch
        {
            PGF x => x.MultiObtain, WC6 x => x.MultiObtain, WC7 x => x.GiftRepeatable,
            WB7 x => x.GiftRepeatable, WC8 x => x.GiftRepeatable, WB8 x => x.GiftRepeatable,
            WA8 x => x.GiftRepeatable, WC9 x => x.GiftRepeatable, WA9 x => x.GiftRepeatable, _ => null,
        };
        bool? daily = gift switch { WC7 x => x.GiftOncePerDay, WB7 x => x.GiftOncePerDay, WB8 x => x.GiftOncePerDay, _ => null };
        return new(gift.CardID, gift.CardTitle, gift.Type, kind, collected, repeatable, daily, items, fields);
    }
}
