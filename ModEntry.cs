﻿using BZP_Allergies.Apis;
using BZP_Allergies.AssetPatches;
using BZP_Allergies.Config;
using BZP_Allergies.ContentPackFramework;
using BZP_Allergies.HarmonyPatches;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using static BZP_Allergies.AllergenManager;

namespace BZP_Allergies
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

        private Harmony Harmony;
        public static ModConfigModel Config;
        private IModHelper ModHelper;

        public static readonly ISet<string> NpcsThatReactedToday = new HashSet<string>();

        public static readonly string MOD_ID = "BarleyZP.BzpAllergies";

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper modHelper)
        {
            ModHelper = modHelper;
            Initializable.Initialize(Monitor, ModHelper.GameContent, ModHelper.ModContent);

            // allergen manager
            AllergenManager.InitDefault();

            // events
            modHelper.Events.GameLoop.GameLaunched += OnGameLaunched;
            modHelper.Events.Content.AssetRequested += OnAssetRequested;
            modHelper.Events.GameLoop.DayStarted += OnDayStarted;
            modHelper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            // config
            Config = Helper.ReadConfig<ModConfigModel>();

            // harmony patches
            Harmony = new(ModManifest.UniqueID);
            Harmony.PatchAll();

            // console commands
            modHelper.ConsoleCommands.Add("bzpa_list_allergens", "Get a list of all possible allergens.", ListAllergens);
            modHelper.ConsoleCommands.Add("bzpa_get_held_allergens", "Get the allergens of the currently-held item.", GetAllergensOfHeldItem);
            modHelper.ConsoleCommands.Add("bzpa_reload", "Reload all content packs.", ReloadPacks);
            modHelper.ConsoleCommands.Add("bzpa_player_allergies", "Get the player's allergies.", GetPlayerAllergies);
        }


        /*********
        ** Private methods
        *********/

        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                foreach (string a in ALLERGEN_DATA.Keys)
                {
                    PatchObjects.AddAllergen(e, a);
                }
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/BarleyZP.BzpAllergies/Sprites"))
            {
                e.LoadFromModFile<Texture2D>(PathUtilities.NormalizePath(@"assets/Sprites.png"), AssetLoadPriority.Medium);
            }
        }

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                Monitor.Log("No mod config menu API found.", LogLevel.Debug);
                return;
            }

            // content packs
            LoadContentPacks.LoadPacks(Helper.ContentPacks.GetOwned(), Config);

            // config
            configMenu.Register(
                mod: ModManifest,
                reset: () => {
                    Config = new ModConfigModel();
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    Config = Helper.ReadConfig<ModConfigModel>();
                }
            );

            ConfigMenuInit.SetupMenuUI(configMenu, ModManifest);
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // make sure all the allergens the player "has" and "discovered" still exist
            ISet<string> has = AllergenManager.ModDataSetGet(Game1.player, Constants.ModDataHas);
            ISet<string> discovered = AllergenManager.ModDataSetGet(Game1.player, Constants.ModDataDiscovered);
            foreach (string id in has)
            {
                if (!AllergenManager.ALLERGEN_DATA.ContainsKey(id))
                {
                    AllergenManager.ModDataSetRemove(Game1.player, Constants.ModDataHas, id);
                }
            }

            foreach (string id in discovered)
            {
                if (!AllergenManager.ALLERGEN_DATA.ContainsKey(id))
                {
                    AllergenManager.ModDataSetRemove(Game1.player, Constants.ModDataDiscovered, id);
                }
            }
        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            NpcsThatReactedToday.Clear();
        }

        private void ListAllergens(string command, string[] args) {

            string result = "\n{Allergen Id}: {Allergen Display Name}";

            foreach (var item in AllergenManager.ALLERGEN_DATA)
            {
                result += "\n  " + item.Key + ": " + item.Value.DisplayName;
            }

            Monitor.Log(result, LogLevel.Info);
        }

        private void GetAllergensOfHeldItem(string command, string[] args)
        {
            ISet<string> result = new HashSet<string>();
            Item currItem = Game1.player.CurrentItem;

            if (currItem is StardewValley.Object currObj)
            {
                result = GetAllergensInObject(currObj);
            }

            Monitor.Log(string.Join(", ", result), LogLevel.Info);
        }

        private void ReloadPacks(string command, string[] args)
        {
            AllergenManager.InitDefault();
            LoadContentPacks.LoadPacks(Helper.ContentPacks.GetOwned(), Config);
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache(asset => asset.NameWithoutLocale.StartsWith("Characters/Dialogue/"));
        }

        private void GetPlayerAllergies(string command, string[] args)
        {
            ISet<string> has = ModDataSetGet(Game1.player, Constants.ModDataHas);
            ISet<string> discovered = ModDataSetGet(Game1.player, Constants.ModDataDiscovered);

            string result = "\n{Allergen Id}: {Discovered}";
            foreach (string a in has)
            {
                result += "\n  " + a + ": " + discovered.Contains(a);
            }
            
            Monitor.Log(result, LogLevel.Info);
        }
    }
}