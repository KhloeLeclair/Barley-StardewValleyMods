﻿using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace BzpAllergies.HarmonyPatches
{
    internal class Machine_Patches
    {
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.PlaceInMachine)),
                prefix: new HarmonyMethod(typeof(Machine_Patches), nameof(PlaceInMachine_Prefix)),
                postfix: new HarmonyMethod(typeof(Machine_Patches), nameof(PlaceInMachine_Postfix))
            );
        }

        // TODO replace with a transpiler
        public static IEnumerable<CodeInstruction> PlaceInMachine_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new(instructions);
            
            // plan: get the input item and any additional used items and use that to apply allergens to output item
            // make sure it works with EMC...
            
            return matcher.InstructionEnumeration();
        }
        
        public static void PlaceInMachine_Prefix(Farmer who, bool probe, out Dictionary<string, Item>? __state)
        {
            try
            {
                __state = !probe ? InventoryUtils.GetInventoryItemLookup(StardewValley.Object.autoLoadFrom ?? who.Items) : null;
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.Log($"Failed in {nameof(PlaceInMachine_Prefix)}:\n{ex}", LogLevel.Error);
                __state = null;
            }
        }

        public static void PlaceInMachine_Postfix(Farmer who, Dictionary<string, Item>? __state, StardewValley.Object __instance)
        {
            try
            {
                if (__state is null || __instance.heldObject.Value is null) return;

                Dictionary<string, Item> afterConsume = InventoryUtils.GetInventoryItemLookup(StardewValley.Object.autoLoadFrom ?? who.Items);
                List<Item> spentItems = InventoryUtils.InventoryUsedItems(__state, afterConsume);
                
                __instance.heldObject.Value.modData[Constants.ModDataMadeWith] = "";
                foreach (Item item in spentItems)
                {
                    if (item == null) continue;

                    // what allergens does it have?
                    ISet<string> allergens = AllergenManager.GetAllergensInObject(item);
                    foreach (string allergen in allergens)
                    {
                        AllergenManager.ModDataSetAdd(__instance.heldObject.Value, Constants.ModDataMadeWith, allergen);
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.Log($"Failed in {nameof(PlaceInMachine_Postfix)}:\n{ex}", LogLevel.Error);
            }
        }
    }
}
