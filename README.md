# More Customers and Sellers

A mod for [Probably Stolen](https://store.steampowered.com/app/4348910) that adds extra customers and sellers to your shop every day, with an in-game settings window to control how busy it gets.

**Steam Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3830266605

![Preview](preview.png)

## Features

- **Extra customers**: up to 10 more customers at your counter each day
- **Extra sellers**: up to 10 more suppliers bringing you stock to buy
- **F7 settings window**: open it anywhere (main menu or mid-shift) and drag the sliders
- Settings are saved automatically through the game's `ModConfig` system

## Usage

1. Subscribe on the Steam Workshop (or copy the built mod into `<game>/Mods/more_customers/`)
2. Activate it in the game's **Mods** menu, then restart the game
3. Press **F7** and set the sliders (0 = vanilla, 10 = very busy)
4. Changes apply from the **next in-game day**

## Good to know

- This mod changes the game's economy: more trade means more money and stock than the game is balanced for
- Extra sellers only appear after the story intro (day 14+). Story and scripted visitors are untouched
- Built for game version **0.46D**

## How it works

I found the customer logic by decompiling the game's `Assembly-CSharp.dll` with ILSpy and tracing how each day's customers are generated in `StoreClientManager`.

| What | How |
|---|---|
| Extra customers | Harmony **postfix** on `StoreClientManager.GetNormalCustomerCount()` adds the configured amount to the daily count (the game normally caps this at 6) |
| Extra sellers | Harmony **postfix** on `StoreClientManager.HandleNormalClient()` calls the game's own `PickSupplierClient()` and queues sellers who aren't already coming that day |
| Settings | Two `ConfigEntry<int>` values registered with the game's `ModConfig` API (range 0–10), saved automatically |
| F7 window | A small Unity IMGUI window drawn by a `MonoBehaviour`, scaled to the screen resolution |

**Possible conflicts:** other mods that patch `GetNormalCustomerCount` or `HandleNormalClient`.

## Building from source

**Requirements**
- Probably Stolen installed (the project references the game's DLLs from your own install; none are included here)
- [.NET SDK](https://dotnet.microsoft.com/download) 6.0 or later

**Steps**
1. Clone this repo into `<game folder>/Modding/more_customers/`
2. In that folder, run:
   ```
   dotnet build -c Release
   ```
3. The build automatically installs the mod into `<game folder>/Mods/more_customers/`

If your project is somewhere else, change `GameDir` in `more_customers.csproj` to point to your game folder.

## Credits

- Made by **Luviein**
- Built with the official [Probably Stolen modding guide](https://github.com/dragonpan2/probably-stolen-modding)
- Uses [Harmony](https://github.com/pardeike/Harmony), which ships with the game

## License

[MIT](LICENSE)
