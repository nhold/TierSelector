# TierSelector

Can set config to the tier you want (White, Green, Red, Boss, Lunar, Void).

Currently the following things change their spawn to the selected tier of items:

+ Equipment
+ Shrines
+ 3D Printers
+ Chests
+ Lunar Buds
+ Void Thingos? (Not potentials)

TODO\Bugs:

+ Bosses still drop green\boss items.
+ Typed chests will drop any item from the selected tier, rather than the type of the chest.
+ Scavs will drop normal loot table.
+ Some 3D printers will ignore selected loot table??
+ AWU still drops red items
+ Add to risk of options

Thanks to RayDan, MarkTullius and huntergames084 for the idea, testing and feedback! and MarkTullius again for the icon!


## Installation

- Install [BepInEx Mod Pack](https://thunderstore.io/package/bbepis/BepInExPack/) (if you haven't already)
- Install [R2API](https://thunderstore.io/package/tristanmcpherson/R2API/) (if you haven't already)
- Place the mod in the Risk of Rain 2\BepInEx\plugins folder.

To configure, either go to BepInEx\config and edit Vorkiblo.TierSelector.cfg after running the game.

```
[ItemSelector]

## Which tier you want (White, Green, Red, Boss, Lunar, Void.
# Setting type: ItemType
# Default value: White
# Acceptable values: White, Green, Red, Boss, Lunar, Void
Selected Tier = Boss
```

or use r2modman config editor?

## Contact

[https://twitch.tv/raydans](https://twitch.tv/raydans) - In the chat is where I hang as Vorkiblo.

## Changelog

```
- 1.2.0: Add configuration for white, green, red, boss, lunar, void tiers.
- 1.0.3: Add back in last boss item (dunno what it was??)
- 1.0.0: Everything spawns boss items: Equipment, Shrines, 3D printers, Chests etc.
```