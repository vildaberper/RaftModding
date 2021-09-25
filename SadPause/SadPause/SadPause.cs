using HarmonyLib;
using UnityEngine;

public class SadPause : Mod
{

    private static SadPause instance;

    private const string id = "vildaberper.SadPause";

    private bool wasPaused = false;
    private float stat_thirst, stat_hunger;

    private Harmony harmony = null;

    public void Start()
    {
        instance = this;

        (harmony = new Harmony(id)).PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

        Debug.Log("Mod SadPause has been loaded!");
    }

    public void OnModUnload()
    {
        harmony.UnpatchAll(id);

        Debug.Log("Mod SadPause has been unloaded!");
    }

    public void Update()
    {
        bool paused = CanvasHelper.ActiveMenu.Equals(MenuType.PauseMenu);

        if (paused)
        {
            if (!wasPaused)
            {
                stat_thirst = RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.Value;
                stat_hunger = RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Value;
            }
            else
            {
                stat_thirst = Mathf.Max(stat_thirst, RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.Value);
                stat_hunger = Mathf.Max(stat_hunger, RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Value);
            }

            RAPI.GetLocalPlayer().Stats.stat_thirst.Normal.Value = stat_thirst;
            RAPI.GetLocalPlayer().Stats.stat_hunger.Normal.Value = stat_hunger;
        }

        wasPaused = paused;
    }

}
