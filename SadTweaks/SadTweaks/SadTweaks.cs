using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SadTweaks : Mod
{

    private static SadTweaks instance;

    private const string id = "vildaberper.SadTweaks";

    private Harmony harmony = null;

    private IDictionary<int, Tuple<int, int>> maxUses = null;

    private readonly static System.Func<Item_Base, bool> hasMaxUsesItem = item =>
    {
        return item.MaxUses >= 10 && item.MaxUses < 9999;
    };

    private readonly static System.Func<Slot, bool> hasMaxUsesSlot = slot =>
    {
        return slot.HasValidItemInstance() && hasMaxUsesItem.Invoke(slot.itemInstance.baseItem);
    };

    public void Start()
    {
        instance = this;

        (harmony = new Harmony(id)).PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

        if (maxUses == null)
        {
            maxUses = new Dictionary<int, Tuple<int, int>>();
            foreach (var item in ItemManager.GetAllItems().Where(hasMaxUsesItem))
            {
                var t = Tuple.Create(item.MaxUses, item.MaxUses * 3);

                maxUses.Add(item.UniqueIndex, t);
                AccessTools.Field(typeof(Item_Base), "maxUses").SetValue(item, t.Item2);
            }
        }

        Debug.Log("Mod SadTweaks has been loaded!");
    }

    public void OnModUnload()
    {
        harmony.UnpatchAll(id);

        if (maxUses != null)
        {
            foreach (var item in maxUses) AccessTools.Field(typeof(Item_Base), "maxUses").SetValue(ItemManager.GetItemByIndex(item.Key), item.Value.Item1);
            maxUses = null;
        }

        Debug.Log("Mod SadTweaks has been unloaded!");
    }

    [HarmonyPatch(typeof(Seagull), "SwitchState")]
    public static class Seagull_SwitchState_Patch
    {
        private static void Postfix(Seagull __instance, ref SeagullState newState)
        {
            if (!RAPI.GetLocalPlayer().IsLocalPlayer || __instance.targetCropplot == null) return;

            bool allow = true;

            var scarecrows = FindObjectsOfType<Scarecrow>();
            if (scarecrows.Length > 0)
            {
                Vector3 plotLocation = __instance.targetCropplot.gameObject.transform.position;

                foreach (var scarecrow in scarecrows)
                {
                    if (!scarecrow.Destroyed && Vector3.Distance(plotLocation, scarecrow.gameObject.transform.position) <= 7.0f)
                    {
                        allow = false;
                        break;
                    }
                }
            }

            if (allow)
            {
                allow = false;
                foreach (var slot in __instance.targetCropplot.plantationSlots.Where(slot => slot.plant != null))
                {
                    if (!slot.plant.item.UniqueName.Contains("Flower"))
                    {
                        allow = true;
                        break;
                    }
                }
            }

            if (!allow)
            {
                __instance.targetCropplot = null;
                __instance.SwitchState(SeagullState.Hover);
            }
        }
    }

    [HarmonyPatch(typeof(AI_State_Attack_Block_Shark), "FindBlockToAttack")]
    public static class AIStateAttackBlockShark_FindBlockToAttack_Patch
    {
        private static void Postfix(ref Block __result)
        {
            if (RAPI.GetLocalPlayer().IsLocalPlayer && RaftWeightManager.FoundationWeight >= 50) __result = null;
        }
    }

    [HarmonyPatch(typeof(PlayerSeat), "TryTakingSeat")]
    public static class Chair_TryTakingSeat_Patch
    {
        private static Item_Base item;

        [HarmonyPrefix]
        private static void Prefix(Network_Player player)
        {
            if (!player.IsLocalPlayer) return;

            item = player.PlayerItemManager.useItemController.GetCurrentItemInHand();
        }

        [HarmonyPostfix]
        private static void Postfix(ref bool __result, Network_Player player)
        {
            if (!__result || !player.IsLocalPlayer) return;

            PlayerItemManager.IsBusy = false;
            if (item != null) player.PlayerItemManager.SelectUsable(item);
        }
    }

    [HarmonyPatch(typeof(PlayerStats), "Consume")]
    public static class PlayerStats_Consume_Patch
    {
        private static void Postfix(PlayerStats __instance, ref Item_Base edibleItem)
        {
            if (__instance.Equals(RAPI.GetLocalPlayer().Stats) && (edibleItem.name.StartsWith("Claybowl_") || edibleItem.name.StartsWith("ClayPlate_")))
            {
                RAPI.GetLocalPlayer().Inventory.AddItem(ItemManager.GetItemByName("Claybowl_Empty").UniqueName, 1);
            }
        }
    }

    /*
     * Solar Panels (and POSSIBLY other similar mods) dont properly work
     * courtesy of Aidanamite
     */
    [HarmonyPatch(typeof(ItemInstance), MethodType.Constructor, new Type[] { typeof(Item_Base), typeof(int), typeof(int), typeof(string) })]
    public static class Battery_Insert_Patch
    {
        private static void Prefix(ref Item_Base itemBase)
        {
            if (itemBase.UniqueName == "Battery")
                itemBase = ItemManager.GetItemByName("Battery");
        }
    }


    [ConsoleCommand(name: "weight", docs: "Tells you the raft weight.")]
    public static void weightCommand(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null) return;

        Debug.Log("Foundations: " + RaftWeightManager.FoundationWeight + ", Total: " + RaftWeightManager.TotallWeight);
    }

    [ConsoleCommand(name: "heal", docs: "Heals you.")]
    public static void healCommand(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null) return;

        RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.Value = RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.Max;
        RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Value = RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Max;
        RAPI.GetLocalPlayer().Stats.stat_health.Value = RAPI.GetLocalPlayer().Stats.stat_health.Max;
    }

    [ConsoleCommand(name: "revive", docs: "Revives you if you are dead. You need to have a bed on your raft for it to work.")]
    public static void reviveCommand(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null) return;

        var beds = FindObjectsOfType<Bed>();
        if (beds.Length < 1)
        {
            Debug.Log("You do not have a bed on your raft.");
            return;
        }

        healCommand(null);

        RAPI.GetLocalPlayer().PlayerScript.StartRespawn(beds[0], false, false);
    }

    [ConsoleCommand(name: "maxuses", docs: "Updates all items to match extended durability.")]
    public static void maxusesCommand(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null || instance.maxUses == null) return;

        List<Slot> slots = new List<Slot>();

        slots.AddRange(RAPI.GetLocalPlayer().Inventory.allSlots.Where(hasMaxUsesSlot));
        slots.AddRange(RAPI.GetLocalPlayer().Inventory.backpackSlots.Where(hasMaxUsesSlot));
        slots.AddRange(RAPI.GetLocalPlayer().Inventory.equipSlots.Where(hasMaxUsesSlot));

        if (RAPI.GetLocalPlayer().IsLocalPlayer)
        {
            foreach (var storage in StorageManager.allStorages)
            {
                slots.AddRange(storage.GetInventoryReference().allSlots.Where(hasMaxUsesSlot));
            }
        }

        int updated = 0;
        foreach (var slot in slots)
        {
            ItemInstance item = slot.itemInstance;
            Tuple<int, int> t;

            if (!instance.maxUses.TryGetValue(item.baseItem.UniqueIndex, out t) || item.Uses > t.Item1) continue;

            slot.SetUses(item.Uses * (t.Item2 / t.Item1));
            ++updated;
        }

        Tuple<int, int> bt;
        if (RAPI.GetLocalPlayer().IsLocalPlayer && instance.maxUses.TryGetValue(ItemManager.GetItemByName("Battery").UniqueIndex, out bt))
        {
            var batteries = FindObjectsOfType<Battery>();
            foreach (var battery in batteries.Where(battery => battery.BatteryUses > 0))
            {
                if (battery.BatteryUses > bt.Item1) continue;

                battery.Update(battery.BatteryUses * (bt.Item2 / bt.Item1));
                ++updated;
            }
        }

        Debug.Log("Updated durability of " + updated + " existing items.");
    }
}
