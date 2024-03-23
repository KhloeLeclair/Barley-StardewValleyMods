﻿using StardewValley.GameData.Objects;
using StardewModdingAPI;
using BZP_Allergies.Config;
using StardewValley;
using System.Text.RegularExpressions;
using StardewValley.ItemTypeDefinitions;
using System.Runtime.CompilerServices;

namespace BZP_Allergies
{
    internal class AllergenManager
    {
        private static IMonitor Monitor;
        private static ModConfig Config;

        public enum Allergens {
            EGG,
            WHEAT,
            FISH,
            SHELLFISH,
            TREE_NUTS,
            DAIRY
        }

        public const string ALLERIC_REACTION_DEBUFF = "bzp_allergies_1";

        private static readonly Dictionary<Allergens, ISet<string>> ENUM_TO_ALLERGEN_OBJECTS = new()
        {
            { Allergens.EGG, new HashSet<string>{
                "194", "195", "201", "203", "211", "213", "220", "221", "223", "234", "240", "648",
                "732"
            }},
            { Allergens.WHEAT, new HashSet<string>{
                "198", "201", "202", "203", "206", "211", "214", "216", "220", "221", "222", "223",
                "224", "234", "239", "241", "604", "608", "611", "618", "651", "731", "732", "246",
                "262"
            }},
            { Allergens.FISH, new HashSet<string>{
                "198", "202", "204", "212", "213", "214", "219", "225", "226", "227", "228", "242",
                "265", "447", "445", "812", "SmokedFish"
            }},
            { Allergens.SHELLFISH, new HashSet<string>{
                "203", "218", "227", "228", "727", "728", "729", "730", "732", "733", "447", "812",
                "SmokedFish"
            }},
            { Allergens.TREE_NUTS, new HashSet<string>{
                "239", "607", "408"
            }},
            { Allergens.DAIRY, new HashSet<string>{
                "195", "197", "199", "201", "206", "215", "232", "233", "236", "240", "243", "605",
                "608", "727", "730", "904", "424", "426"
            }}
        };

        private static readonly Dictionary<Allergens, string> ENUM_TO_CONTEXT_TAG = new()
        {
            { Allergens.EGG, "bzp_allergies_egg" },
            { Allergens.WHEAT, "bzp_allergies_wheat" },
            { Allergens.FISH, "bzp_allergies_fish" },
            { Allergens.SHELLFISH, "bzp_allergies_shellfish" },
            { Allergens.TREE_NUTS, "bzp_allergies_treenuts" },
            { Allergens.DAIRY, "bzp_allergies_dairy" }
        };

        private static readonly Dictionary<Allergens, string> ENUM_TO_STRING = new()
        {
            { Allergens.EGG, "Eggs" },
            { Allergens.WHEAT, "Wheat" },
            { Allergens.FISH, "Fish" },
            { Allergens.SHELLFISH, "Shellfish" },
            { Allergens.TREE_NUTS, "Tree Nuts" },
            { Allergens.DAIRY, "Dairy" }
        };

        // call this method from your Entry class
        public static void Initialize(IMonitor monitor, ModConfig config)
        {
            Monitor = monitor;
            Config = config;
        }

        public static string GetAllergenContextTag(Allergens allergen)
        {
            string result = ENUM_TO_CONTEXT_TAG.GetValueOrDefault(allergen, "");
            if (result.Equals(""))
            {
                throw new Exception("No context tags were defined for the allergen " + allergen.ToString());
            }
            return result;
        }

        public static string GetAllergenReadableString(Allergens allergen)
        {
            string result = ENUM_TO_STRING.GetValueOrDefault(allergen, "");
            if (result.Equals(""))
            {
                throw new Exception("No readable string was defined for the allergen " + allergen.ToString());
            }
            return result;
        }

        public static ISet<string> GetObjectsWithAllergen(Allergens allergen, IAssetDataForDictionary<string, ObjectData> data)
        {
            // labeled cooked items
            ISet<string> result = ENUM_TO_ALLERGEN_OBJECTS.GetValueOrDefault(allergen, new HashSet<string>());

            // raw ingredient items
            if (allergen == Allergens.EGG)
            {
                ISet<string> rawEggItems = GetItemsWithContextTags(new List<string> { "egg_item", "mayo_item", "large_egg_item" }, data);
                result.UnionWith(rawEggItems);
            }
            else if (allergen == Allergens.FISH)
            {
                ISet<string> fishItems = GetItemsWithContextTags(
                    new List<string> { "fish_ocean", "fish_legendary", "fish_lake", "fish_river", "fish_freshwater", "fish_pond",
                                       "fish_secret_pond", "fish_swamp", "fish_bug_lair", "fish_semi_rare", "fish_night_market",
                                       "fish_legendary_family", "id_(o)smokedfish", "id_o_smokedfish" },
                    data,
                    new List<string> { "fish_crab_pot" }
                );
                result.UnionWith(fishItems);
            }
            else if (allergen == Allergens.DAIRY)
            {
                ISet<string> dairyItems = GetItemsWithContextTags(new List<string> { "milk_item", "large_milk_item", "cow_milk_item", "goat_milk_item" }, data);
                result.UnionWith(dairyItems);
            }

            if (result.Count == 0)
            {
                throw new Exception("No objects have been assigned the allergen " + allergen.ToString());
            }
            return result;

            
        }
        public static bool FarmerIsAllergic(Allergens allergen, ModConfig config)
        {
            switch (allergen)
            {
                case Allergens.EGG:
                    return config.Farmer.EggAllergy;
                case Allergens.WHEAT:
                    return config.Farmer.WheatAllergy;
                case Allergens.FISH:
                    return config.Farmer.FishAllergy;
                case Allergens.SHELLFISH:
                    return config.Farmer.ShellfishAllergy;
                case Allergens.TREE_NUTS:
                    return config.Farmer.TreenutAllergy;
                case Allergens.DAIRY:
                    return config.Farmer.DairyAllergy;
                default:
                    return false;
            }
        }

        public static bool FarmerIsAllergic (StardewValley.Object @object, ModConfig config, IGameContentHelper helper)
        {
            Monitor.Log(@object.QualifiedItemId, LogLevel.Debug);
            // special case: roe, aged roe, or smoked fish
            // need to differentiate fish vs shellfish ingredient
            List<string> fishShellfishDifferentiation = new() { "(O)447", "(O)812", "(O)SmokedFish" };
            if (fishShellfishDifferentiation.Contains(@object.QualifiedItemId))
            {
                try
                {
                    // get context tags
                    ISet<string> tags = @object.GetContextTags();

                    // find the "preserve_sheet_index_{id}" tag
                    Regex rx = new(@"^preserve_sheet_index_\d+$");
                    List<string> filtered_tags = tags.Where(t => rx.IsMatch(t)).ToList();
                    string preserve_sheet_tag = filtered_tags[0];

                    // get the id of the object it was made from
                    Match m = Regex.Match(preserve_sheet_tag, @"\d+");
                    if (!m.Success)
                    {
                        throw new Exception("No regex match for item id in preserve_sheet_index context tag");
                    }

                    string madeFromId = m.Value;
                    // load Data/Objects for context tags
                    IDictionary<string, ObjectData> objData = helper.Load<Dictionary<string, ObjectData>>("Data/Objects");

                    // !isShellfish = isFish since these can only be made from one of the two
                    bool isShellfish = objData[madeFromId].ContextTags.Contains("fish_crab_pot");

                    if (isShellfish && FarmerIsAllergic(Allergens.SHELLFISH, config))
                    {
                        return true;
                    }
                    else
                    {
                        return !isShellfish && FarmerIsAllergic(Allergens.FISH, config);
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed in {nameof(FarmerIsAllergic)}:\n{ex}", LogLevel.Error);
                    Monitor.Log("Unable to determine whether eaten Object was fish or shellfish");
                    // we failed to determine, so let's just fall through and
                    // return whether the farmer is allergic to fish or shellfish
                }
            }

            // check each of the allergens
            foreach (Allergens a in Enum.GetValues<Allergens>())
            {
                if (@object.HasContextTag(GetAllergenContextTag(a)) && FarmerIsAllergic(a, config))
                {
                    return true;
                }
            }

            return false;
        }

        public static ISet<string> GetItemsWithContextTags (List<string> tags, IAssetDataForDictionary<string, ObjectData> data, List<string>? rejectTags = null)
        {
            ISet<string> result = new HashSet<string>();

            rejectTags ??= new List<string>();

            foreach (var item in data.Data)
            {
                ObjectData v = item.Value;
                foreach (string tag in tags)
                {
                    if (v.ContextTags != null && v.ContextTags.Contains(tag))
                    {
                        result.Add(item.Key);
                    }
                }

                foreach (string rejectTag in rejectTags)
                {
                    if (v.ContextTags != null && v.ContextTags.Contains(rejectTag))
                    {
                        result.Remove(item.Key);
                    }
                }
            }

            return result;
        }
    }
}
