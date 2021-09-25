using HarmonyLib;
using UnityEngine;

public class SadRevive : Mod
{
    private static SadRevive instance;

    private const string id = "vildaberper.SadRevive";

    private Harmony harmony = null;

    public void Start()
    {
        instance = this;

        (harmony = new Harmony(id)).PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

        Debug.Log("Mod SadRevive has been loaded!");
    }

    public void OnModUnload()
    {
        harmony.UnpatchAll(id);

        Debug.Log("Mod SadRevive has been unloaded!");
    }

    [ConsoleCommand(name: "heal", docs: "Heals you.")]
    public static void healCommand(string[] args)
    {
        RAPI.GetLocalPlayer().Stats.stat_thirst.Value = RAPI.GetLocalPlayer().Stats.stat_thirst.Max;
        RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Value = RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Max;
        RAPI.GetLocalPlayer().Stats.stat_health.Value = RAPI.GetLocalPlayer().Stats.stat_health.Max;
    }

    [ConsoleCommand(name: "revive", docs: "Revives you if you are dead. You need to have a bed on your raft for it to work.")]
    public static void reviveCommand(string[] args)
    {
        Bed[] beds = FindObjectsOfType<Bed>();

        if (beds.Length < 1)
        {
            Debug.Log("You do not have a bed on your raft.");
            return;
        }

        healCommand(null);

        RAPI.GetLocalPlayer().PlayerScript.StartRespawn(beds[0], false, false);
    }
}