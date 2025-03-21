﻿using StardewModdingAPI;
using HarmonyLib;
using EnemyOfTheValley.Patches;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using EnemyOfTheValley.Common;
using EOVPreconditions = EnemyOfTheValley.Common.Preconditions;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using SObject = StardewValley.Object;

namespace EnemyOfTheValley
{
    public class ModEntry : Mod
    {
        public static IMonitor Monitor;
        public static ITranslationHelper Translation;
        public static Texture2D? MiscSprites;  // do not reference directly in transpilers
        public static Texture2D? StandardSprites;
        public override void Entry(IModHelper helper)
        {
            Monitor = base.Monitor;
            Translation = helper.Translation;
            Harmony.DEBUG = true;

            Harmony harmony = new(ModManifest.UniqueID);
            FarmerPatches.Patch(harmony);
            SocialPagePatches.Patch(harmony);
            DialogueBoxPatches.Patch(harmony);
            NPCActionPatches.Patch(harmony);
            NPCDialoguePatches.Patch(harmony);
            ProfileMenuPatches.Patch(harmony);
            BeachPatches.Patch(harmony);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            LoadMiscSprites();
            LoadStandardSprites();

            helper.ConsoleCommands.Add("enemy", "Sets the specified NPC to be the player's enemy", SetEnemy);
            helper.ConsoleCommands.Add("archenemy", "Sets the specified NPC to be the player's archenemy", SetArchenemy);
            helper.ConsoleCommands.Add("exarchenemy", "Sets the specified NPC to be the player's ex-archenemy", SetExArchenemy);

            Event.RegisterPrecondition("NegativeFriendship", EOVPreconditions.NegativeFriendship);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("BarleyZP.EnemyOfTheValley/MiscSprites"))
            {
                e.LoadFromModFile<Texture2D>("assets/MiscSprites.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("BarleyZP.EnemyOfTheValley/StandardSprites"))
            {
                e.LoadFromModFile<Texture2D>("assets/StandardSprites.png", AssetLoadPriority.Medium);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // set the friendship status of each npc to what was stored in moddata
            foreach (string name in Game1.player.friendshipData.Keys)
            {
                NPC npc = Game1.getCharacterFromName(name);
                if (npc == null || !npc.modData.TryGetValue("BarleyZP.EnemyOfTheValley.FriendshipStatus", out string status)) continue;

                Relationships.SetRelationship(name, (FriendshipStatus)int.Parse(status));
            }

            // do the beach shards
            Beach beach = (Beach)Game1.getLocationFromName("Beach");
            if (Game1.wasRainingYesterday && Relationships.HasAnEnemyWithHeartLevel(Game1.player, -10) && !beach.IsRainingHere())
            {
                NPC oldMariner = Traverse.Create(beach).Field<NPC>("oldMariner").Value;
                if (oldMariner != null) return;  // somehow the mariner is here even though it isn't raining; we can't place the shards

                Vector2 marinerPos = new(80, 5);
                beach.overlayObjects.Remove(marinerPos);
                SObject shards = ItemRegistry.Create<SObject>("BarleyZP.EnemyOfTheValley.ShatteredAmulet");
                shards.TileLocation = marinerPos;
                shards.IsSpawnedObject = true;
                beach.overlayObjects.Add(marinerPos, shards);
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            // check if we need to send Qi mail
            if (Game1.player.mailReceived.Contains("enemyCake")) return;

            foreach (var item in Game1.player.friendshipData)
            {
                foreach (var friendship in item.Values)
                {
                    if (friendship.Points <= -2000)
                    {
                        Game1.player.mailForTomorrow.Add("enemyCake");
                        break;
                    }
                }
            }

            // get rid of door unlock for NPCs that fell below 0 hearts
            foreach (string name in Game1.player.friendshipData.Keys)
            {
                if (Game1.player.mailReceived.Contains("doorUnlock" + name) && Game1.player.friendshipData[name].Points < 0)
                {
                    Game1.player.mailReceived.Remove("doorUnlock" + name);
                }
            }

            // set the friendship status of each NPC to something safe
            foreach (string name in Game1.player.friendshipData.Keys)
            {
                NPC npc = Game1.getCharacterFromName(name);
                if (npc == null) continue;
                npc.modData["BarleyZP.EnemyOfTheValley.FriendshipStatus"] = ((int) Game1.player.friendshipData[name].Status).ToString();

                Relationships.SetRelationship(name, FriendshipStatus.Friendly);
            }

            // remove beach shards
            Beach beach = (Beach)Game1.getLocationFromName("Beach");
            Vector2 marinerPos = new(80, 5);
            beach.overlayObjects.Remove(marinerPos);
        }

        public static Texture2D LoadMiscSprites()
        {
            MiscSprites ??= Game1.content.Load<Texture2D>("BarleyZP.EnemyOfTheValley/MiscSprites");
            return MiscSprites;
        }

        public static Texture2D LoadStandardSprites()
        {
            StandardSprites ??= Game1.content.Load<Texture2D>("BarleyZP.EnemyOfTheValley/StandardSprites");
            return StandardSprites;
        }

        public static void SetEnemy(string command, string[] args) {
            Relationships.SetRelationship(args[0], Relationships.Enemy, printValidation: true);
        }

        public static void SetArchenemy(string command, string[] args)
        {
            Relationships.SetRelationship(args[0], Relationships.Archenemy, printValidation: true);
        }

        public static void SetExArchenemy(string command, string[] args)
        {
            Relationships.SetRelationship(args[0], Relationships.ExArchenemy, printValidation: true);
        }
    }
}
