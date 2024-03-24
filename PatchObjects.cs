﻿using BZP_Allergies.Config;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using static BZP_Allergies.AllergenManager;
using StardewModdingAPI.Utilities;

namespace BZP_Allergies
{
    internal sealed class PatchObjects
    {

        public static void AddAllergen (AssetRequestedEventArgs e, Allergens allergen, ModConfig config)
        {
            string path = PathUtilities.NormalizeAssetName("Data/Objects");
            if (e.NameWithoutLocale.IsEquivalentTo(path))
            {
                bool allergic = FarmerIsAllergic(allergen, config);

                e.Edit(asset =>
                {
                    // update items containing allergens
                    var editor = asset.AsDictionary<string, ObjectData>();

                    ISet<string> idsToEdit = GetObjectsWithAllergen(allergen, editor);
                    foreach (string id in idsToEdit)
                    {
                        // add context tags
                        ObjectData objectData = editor.Data[id];
                        if (objectData.ContextTags == null)
                        {
                            objectData.ContextTags = new List<string>();
                        }
                        objectData.ContextTags.Add(GetAllergenContextTag(allergen));

                        // update descriptions
                        if (!objectData.Description.Contains("Allergens: "))
                        {
                            objectData.Description += "\nAllergens: ";
                        }
                        else
                        {
                            objectData.Description += ", ";
                        }
                        objectData.Description += GetAllergenReadableString(allergen);
                    }

                    // add new object assets
                    ObjectData allergyMedicine = new()
                    {
                        Name = "Allergy Medicine",
                        DisplayName = "Allergy Medicine",
                        Description = "Drink for relief from an allergic reaction. Can be consumed even when nauseous.",
                        Type = "Crafting",
                        Category = 0,
                        Price = 500,
                        Texture = PathUtilities.NormalizeAssetName("Mods/BarleyZP.BzpAllergies/AllergyMedicine"),
                        SpriteIndex = 0,
                        Edibility = 20,
                        IsDrink = true,
                        Buffs = null,
                        GeodeDropsDefaultItems = false,
                        GeodeDrops = null,
                        ArtifactSpotChances = null,
                        ExcludeFromFishingCollection = false,
                        ExcludeFromShippingCollection = false,
                        ExcludeFromRandomSale = false,
                        ContextTags = new List<string>()
                        {
                            "medicine_item",
                            "color_dark_purple",
                            "ginger_item"  // so can be eaten when nauseous
                        },
                        CustomFields = null
                    };

                    editor.Data["BzpAllergies_AllergyMedicine"] = allergyMedicine;
                });
            }
        }

    }
}
