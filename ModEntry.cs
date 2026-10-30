using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MoreCustomers
{
    public class ModEntry : IMod
    {
        internal static ModLog log;
        internal static ModConfig config;
        internal static ConfigEntry<int> extraBuyers;
        internal static ConfigEntry<int> extraSellers;

        private GameObject uiObject;

        public void Init(ModManifest manifest)
        {
            log = new ModLog(manifest);

            config = ModConfig.For("more_customers");

            extraBuyers = config.Define(
                "extra_buyers", 3, "Spawns",
                () => "Extra buyers per day",
                min: 0, max: 10,
                tooltip: () => "How many extra customers come to buy from you each day.");

            extraSellers = config.Define(
                "extra_sellers", 2, "Spawns",
                () => "Extra sellers per day",
                min: 0, max: 10,
                tooltip: () => "How many extra sellers come to sell you items each day.");

            log.Log("More Customers and Sellers loaded.");
        }

        public void OnEnable()
        {
            uiObject = new GameObject("MoreCustomersUI");
            UnityEngine.Object.DontDestroyOnLoad(uiObject);
            uiObject.AddComponent<SettingsWindow>();

            log.Log("Extra buyers per day: " + extraBuyers.Value);
            log.Log("Extra sellers per day: " + extraSellers.Value);
            log.Log("Press F7 to open the settings window.");
        }

        public void OnDisable()
        {
            if (uiObject != null)
            {
                UnityEngine.Object.Destroy(uiObject);
            }
        }
    }

    // The F7 pop-up window
    public class SettingsWindow : MonoBehaviour
    {
        private bool visible = false;
        private Rect windowRect = new Rect(40, 40, 380, 0);

        void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F7)
            {
                visible = !visible;
                e.Use();
            }

            if (!visible)
            {
                return;
            }

            // Scale the window to the screen size (1.5x at 1080p, 2x at 1440p)
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            windowRect = GUILayout.Window(781230, windowRect, DrawWindow, "More Customers and Sellers");
        }

        void DrawWindow(int id)
        {
            int buyers = DrawSlider("Extra buyers per day", ModEntry.extraBuyers.Value);
            int sellers = DrawSlider("Extra sellers per day", ModEntry.extraSellers.Value);

            if (buyers != ModEntry.extraBuyers.Value)
            {
                ModEntry.extraBuyers.Set(buyers);
            }
            if (sellers != ModEntry.extraSellers.Value)
            {
                ModEntry.extraSellers.Set(sellers);
            }

            GUILayout.Space(8);
            GUILayout.Label("Changes apply from the next in-game day.");

            if (GUILayout.Button("Close (F7)"))
            {
                visible = false;
            }

            GUI.DragWindow();
        }

        int DrawSlider(string label, int value)
        {
            GUILayout.Label(label + ": " + value);
            float slider = GUILayout.HorizontalSlider(value, 0, 10);
            return Mathf.RoundToInt(slider);
        }
    }

    // Adds extra buyers and sellers after the game picks the day's normal customers
    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.HandleNormalClient))]
    public static class ExtraVisitorsPatch
    {
        static void Postfix(StoreClientManager __instance)
        {
            // Leave the story intro (first 13 days) as the devs intended
            if (!PlayerStore.Instance.skipIntro && StoreStation.Instance.dayCounter <= 13)
            {
                return;
            }

            int buyers = AddBuyers(__instance, Mathf.Clamp(ModEntry.extraBuyers.Value, 0, 10));
            int sellers = AddSellers(__instance, Mathf.Clamp(ModEntry.extraSellers.Value, 0, 10));

            ModEntry.log?.Log("Extra buyers added today: " + buyers
                + " | Extra sellers added today: " + sellers);
        }

        static int AddBuyers(StoreClientManager manager, int wanted)
        {
            if (wanted <= 0)
            {
                return 0;
            }

            int added = 0;

            // Round 1: a buyer who wants what you have the most of in stock
            if (TryAdd(manager, StoreClientManager.PickShopperClient()))
            {
                added++;
            }

            // Round 2: pure buyers from the general pool
            added += FillFromPool(manager, wanted - added,
                c => c.IsClientBuying() && !c.IsClientSelling(), 60);

            // Round 3: if still short, customers who both buy and sell
            added += FillFromPool(manager, wanted - added,
                c => c.IsClientBuying(), 40);

            return added;
        }

        static int AddSellers(StoreClientManager manager, int wanted)
        {
            if (wanted <= 0)
            {
                return 0;
            }

            int added = 0;

            // Round 1: the game's dedicated seller list
            int attempts = 0;
            while (added < wanted && attempts < 30)
            {
                attempts++;
                if (TryAdd(manager, StoreClientManager.PickSupplierClient()))
                {
                    added++;
                }
            }

            // Round 2: pure sellers from the general pool
            added += FillFromPool(manager, wanted - added,
                c => c.IsClientSelling() && !c.IsClientBuying(), 60);

            // Round 3: if still short, customers who both sell and buy
            added += FillFromPool(manager, wanted - added,
                c => c.IsClientSelling(), 40);

            return added;
        }

        // Picks random customers the same way the game does, keeping only those that match
        static int FillFromPool(StoreClientManager manager, int needed,
            Func<StoreClient, bool> matches, int maxAttempts)
        {
            int added = 0;
            int attempts = 0;

            while (added < needed && attempts < maxAttempts)
            {
                attempts++;
                string faction = manager.ComputeClientType();
                StoreClient client = manager.PickRandomValidClient(faction);
                if (client == null || !matches(client))
                {
                    continue;
                }
                if (TryAdd(manager, client))
                {
                    added++;
                }
            }

            return added;
        }

        // Adds the client unless they're null or already coming today
        static bool TryAdd(StoreClientManager manager, StoreClient client)
        {
            if (client == null)
            {
                return false;
            }

            bool alreadyComing = manager.clientStack.Any(
                c => c != null && c.identifier == client.identifier);
            if (alreadyComing)
            {
                return false;
            }

            manager.AddClient(client);
            return true;
        }
    }
}