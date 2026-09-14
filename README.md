![logo](https://raw.githubusercontent.com/shudnal/HipLantern/master/package/thunderstore/HipLantern/icon.png)
# HipLantern
Craft a little lantern and place it on your hip to cast away the darkness.

If you like darker nights you can also check [CircletExtended](https://thunderstore.io/c/valheim/p/shudnal/CircletExtended/) and [Firefly](https://thunderstore.io/c/valheim/p/shudnal/Firefly/) mods.

Must be installed on server in multiplayer. Configuration is synchronized with Conditional Config Sync.

EpicLoot integration supports both legacy releases and EpicLoot 0.13 or later.

## Features
You can change some item and light properties:
* crafting station and recipe
* fuel usage and refuel recipe
* light color
* light intensity, range and shadows strength (indoors and outdoors)
* lantern hip position and rotation

Item recipe is confurable: SurtlingCore x3, BronzeNails x10, FineWood x4.

Refuel recipe is configurable: SurtlingCore x1.

The Lantern takes its own slot. That slot ID is configurable to avoid potential incompatibilities. You can also configure it to use the standard Utility item type. Utility mode is compatible with EquipmentAndQuickSlots 3.x, including its additional Utility slots.

The goal of the mod is to make fighting easier at dungeons or at night. Default light intensity is only good to barely see your enemy up close.

It serves the same purpose as the Hand Lantern in Bloodborne and Elden ring.

## Fuel and crafting presets
* Default settings - craft at forge, refuel(recraft) at hands, can't be repaired
* To make lantern repairable - clear Refuel recipe (if refuel crafting station is set - it will be used as repair station)
* Crafting station minimum level is also level required for repair (vanilla behaviour)
* To make fuel infinite - set Fuel minutes to 0
* Fuel is set in minutes to better displayed in tooltip
* All crafting and item settings is applied on the fly

## Installation (manual)
Install Conditional Config Sync, then extract the HipLantern folder to your BepInEx\Plugins\ folder.

## Configurating
The best way to handle configs is configuration manager. Choose one that works best for you:

https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/

https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/

## Dependencies

- [BepInExPack Valheim 5.4.2350](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [ConditionalConfigSync 1.0.5](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync/)

Install ConditionalConfigSync as a separate dependency; do not copy its DLLs into this mod's package.

## Donation
[Buy Me a Coffee](https://buymeacoffee.com/shudnal)

## Discord
[Join server](https://discord.gg/e3UtQB8GFK)
