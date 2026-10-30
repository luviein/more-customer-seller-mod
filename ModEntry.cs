using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MoreCustomers
{
    public class ModEntry : IMod
    {
        internal static ModLog log;
        internal static ModConfig config;
        internal static ConfigEntry<int> extraCustomers;
        internal static ConfigEntry<int> extraSellers;

        private GameObject uiObject;

        public void Init(ModManifest manifest)
        {
            log = new ModLog(manifest);

            config = ModConfig.For("more_customers");

            extraCustomers = config.Define(
                "extra_customers", 3, "Spawns",
                () => "Extra customers per day",
                min: 0, max: 10,
                tooltip: () => "How many extra customers visit your shop each day.");

            extraSellers = config.Define(
                "extra_sellers", 2, "Spawns",
                () => "Extra sellers per day",
                min: 0, max: 10,
                tooltip: () => "How many extra sellers come to sell you items each day.");

            log.Log("More Customers loaded.");
        }

        public void OnEnable()
        {
            // Create an invisible object that draws our window and listens for F7
            uiObject = new GameObject("MoreCustomersUI");
            UnityEngine.Object.DontDestroyOnLoad(uiObject);
            uiObject.AddComponent<SettingsWindow>();

            log.Log("Extra customers per day: " + extraCustomers.Value);
            log.Log("Extra sellers per day: " + extraSellers.Value);
            log.Log("Press F7 to open the More Customers window.");
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
            // Toggle the window with F7
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

               windowRect = GUILayout.Window(781230, windowRect, DrawWindow, "More customers and sellers");
        }

        void DrawWindow(int id)
        {
            int customers = DrawSlider("Extra customers per day", ModEntry.extraCustomers.Value);
            int sellers = DrawSlider("Extra sellers per day", ModEntry.extraSellers.Value);

            // Save only when something changed
              if (customers != ModEntry.extraCustomers.Value)
            {
                ModEntry.extraCustomers.Set(customers);
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

            // Lets you drag the window around by its title bar
            GUI.DragWindow();
        }

        int DrawSlider(string label, int value)
        {
            GUILayout.Label(label + ": " + value);
            float slider = GUILayout.HorizontalSlider(value, 0, 10);
            return Mathf.RoundToInt(slider);
        }
    }

    // Part 1: more customers in general
    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.GetNormalCustomerCount))]
    public static class CustomerCountPatch
    {
        static void Postfix(ref int __result)
        {
            int extra = Mathf.Clamp(ModEntry.extraCustomers.Value, 0, 10);
            int before = __result;
            __result += extra;
            ModEntry.log?.Log("Normal customers today: " + before + " -> " + __result);
        }
    }

    // Part 2: extra sellers
    [HarmonyPatch(typeof(StoreClientManager), nameof(StoreClientManager.HandleNormalClient))]
    public static class ExtraSellersPatch
    {
        static void Postfix(StoreClientManager __instance)
        {
            if (!PlayerStore.Instance.skipIntro && StoreStation.Instance.dayCounter <= 13)
            {
                return;
            }

            int wanted = Mathf.Clamp(ModEntry.extraSellers.Value, 0, 10);
            int added = 0;
            int attempts = 0;

            while (added < wanted && attempts < 30)
            {
                attempts++;
                StoreClient seller = StoreClientManager.PickSupplierClient();
                if (seller == null)
                {
                    continue;
                }

                bool alreadyComing = __instance.clientStack.Any(
                    c => c != null && c.identifier == seller.identifier);
                if (alreadyComing)
                {
                    continue;
                }

                __instance.AddClient(seller);
                added++;
            }

            ModEntry.log?.Log("Extra sellers added today: " + added);
        }
    }
}