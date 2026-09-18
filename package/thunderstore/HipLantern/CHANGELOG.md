# 1.1.9
* Made lantern and light/heat switch sounds follow the game's Master and SFX volume settings.
* Added the client-side Heat / Enable heat sound option, enabled by default and applied immediately without changing heat or switch effects.
* Added client-side Heat sound volume and Heat sound pitch settings, both defaulting to 0.5 and applied immediately without restarting the heat sound.

# 1.1.8
* Fixed light and heat toggle state synchronization when the lantern uses the Utility slot, including EquipmentAndQuickSlots utility slots.
* Unified lantern fuel drain, heat multiplier and switched-off auto-charge rules across the custom slot, vanilla Utility slot and additional utility slots provided by compatible equipment mods.
* Preserved equipped state when switching between the custom lantern slot and Utility mode at runtime.
* Made Jewelcrafting item-type compatibility exception-safe and verified its current 2.0.9 target methods.

# 1.1.7
* Checked lantern shortcuts before invoking player input/UI checks on idle updates.
* Removed iterator-based inventory scans when applying lantern data after loading.

# 1.1.6
* Updated for the Valheim 1.0.7 release.
* Updated required dependencies to BepInExPack Valheim 5.4.2350 and ConditionalConfigSync 1.0.5.
* Fix lantern detection on item stands using the new item prefab hashes.

# 1.1.5
* added explicit runtime compatibility branches for EpicLoot versions before 0.13 and EpicLoot 0.13+
* migrated configuration synchronization from ServerSync to Conditional Config Sync

# 1.1.4
* fixed lantern auto charge over maximum durability

# 1.1.3
* fixed durability drain of the switched-off lantern
* added new config Auto charge speed of the switched-off lantern

# 1.1.2
* fixed incompatibility issue with DragonRider

# 1.1.1
* light and heat switch hotkeys made not server synced
* light and heat switch keypress is now registered only when you control your character (not in chat, map, console and such)

# 1.1.0
* light and heat switch
* more and now editable localizations

# 1.0.24
* distinct light settings for itemstand variant

# 1.0.23
* custom slots for AzuEPI and ExtraSlots can now be dynamically enabled and disabled (this fixes the issue when slot is disabled on a client and enabled on a server)
* utility and custom type value can now also be changed without restart

# 1.0.22
* minor improvements

# 1.0.21
* fixed potential technical issue with finding item by itemdata

# 1.0.20
* enchantments from EpicLoot and sockets from Jewelcrafting will persist after refueling

# 1.0.19
* new config option to make Lantern socketable by Jewelcrafting
* new config option to make Lantern enchantable by EpicLoot

# 1.0.18
* patch 0.220.3
* ServerSync updated

# 1.0.17
* Lantern no longer drain fuel if crafting station is opened

# 1.0.16
* ExtraSlotsCustomSlots support

# 1.0.15
* fix for MMHOOK dependent mods generating warnings on ExtraSlots API initialization

# 1.0.14
* ExtraSlots API rework

# 1.0.13
* ExtraSlots API removed

# 1.0.12
* occasional NRE fix

# 1.0.11
* more precise position on horizontal stand
* correct vertical position on vertical stand

# 1.0.10
* minor light tweaks

# 1.0.9
* bog witch patch

# 1.0.8
* lantern can now be placed on a stand

# 1.0.7
* fix for rare equipment issue

# 1.0.6
* fix for non AzuEPI users

# 1.0.5
* AzuEPI custom slot support

# 1.0.4
* consistent light state for other players in multiplayer

# 1.0.3
* fix for lantern staying equipped after refueling recraft
* disabled recycle option on refuel recipe

# 1.0.2
* Ashlands refinements

# 1.0.1
* configurable equip duration

# 1.0.0
* Initial release