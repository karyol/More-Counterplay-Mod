# More Counterplay for Lethal Company enemies

Mod is fully configurable so you can disable or edit counterplays that you don't like.

- Uses [CSync v5.0.1](https://thunderstore.io/c/lethal-company/p/Sigurd/CSync) by [Lordfirespeed](https://github.com/Lordfirespeed) to sync configuration settings between host and clients.

## Supported Enemies

### Jester

<details>
<summary>Spoiler</summary>

Items may be placed on top of a Jester by holding an item and interacting with its lid, it'll carry them around until the next time it pops. If the total weight of items on its head exceeds a certain (configurable) amount, the Jester will be too exhausted to chase you after popping and its head will return to its box immediately.

<div style="text-align: center;">
 <img alt="A lamp being placed on top of a Jester." src="https://files.catbox.moe/jkqrv8.gif" width=256>
 <img alt="A Jester with several items on its head popping and returning to its box immediately." src="https://files.catbox.moe/49ut0j.gif" width=256>
 <h2 style="font-weight: bold; color: firebrick; text-shadow: 0 0 3px black">— [Warning] —</h2>

 > Expecting a Jester to comply with carrying an excessive amount of weight may lead to disastrous consequences, as a desperate Jester's pop is much stronger than normal.

 **Hint:** _A Jester **will** let you know when you've exceeded its limits, and there's still a short window to correct your mistake..._

 <img alt="A Jester with an anvil on its head chasing and killing the player after popping." src="https://files.catbox.moe/2cebnk.gif" width=256>

 <details>
 <summary>Spoiler (specific mechanics and configuration):</summary>

 <div style="text-align: left;">

- Any grabbable item can be deposited onto a Jester by holding the interact button while the prompt to place an item is visible (similar to the Ship's storage cabinet, the Cruiser's back storage, and the desk at the Company).
  - Every placed item's weight is added to the Jester's total weight, which forms the basis for its counterplay.
- If the Jester finishes cranking while its total weight exceeds the amount set by the `JesterPreventThreshold` setting (**60** pounds by default), its head visually pops out for a brief moment to drop its items before returning to its box, without actually chasing or killing players.
  - Toggling the `ItemsStayOnLid` setting disables the Jester's ability to drop its items and keeps its head inside the box at all times, making it like how it used to work in previous versions of the mod.
- If the Jester's total weight exceeds the amount set by the `JesterEncumberThreshold` setting (**120** pounds by default), it'll no longer be able to follow you around due to being encumbered by the items.
  - Threshold setting can be set to `0` to disable the Jester's encumbered state completely, or a small value (e.g. `0.1`) to allow almost any item to stop the Jester from moving.
- If the Jester's total weight exceeds the amount set by the `JesterPanicThreshold` setting (**200** pounds by default), it'll panic and begin cranking frantically before popping shortly after.
  - Threshold setting can be set to `0` to disable the Jester's panicked state completely.
  - Minimum and maximum time that the Jester spends panicking can be configured via the `MinPanicTimer` and `MaxPanicTimer` settings, respectively.
  - Setting this threshold to a lower value than the `JesterPreventThreshold` setting functionally disables it as a counterplay, since the Jester's pop is not prevented while panicking.
  - Placing an item while over the panic threshold and while the Jester is cranking (even normally) will cause it to **skip its cranking and pop immediately**.
- Whenever a Jester is hit by a shovel, it'll drop all its held items and reset its weight.
  - Can be disabled by toggling the `DropItemsOnHit` setting.
- The total amount of weight the Jester is carrying is shown in the subtext of its scan node.
  - Can be disabled by toggling the `ShowWeightOnScan` setting.
  - Might be slightly inaccurate due to integer rounding, or if an item is destroyed or otherwise removed by other means.

 </div>
 </details>
</div>
</details>

<details>
 <summary>Configs</summary>

- `EnableJesterCounterplay` - Add counterplay for Jesters.
- `JesterPreventThreshold` - Minimum weight of items needed to prevent the Jester from popping.
- `JesterEncumberThreshold` - Minimum weight of items needed to prevent the Jester from walking at all.
- `JesterPanicThreshold` - See above for more info.
- `ItemsStayOnLid` - Allow items to stay on top of the Jester after preventing it from popping.
- `DropItemsOnHit` - Drop all items on top of the Jester when hitting it with a shovel.
- `ShowWeightOnScan` - Shows the total weight on top of the Jester as the subtext of its scan node.

</details>

---

### Turret

<details>
 <summary>Spoiler</summary>

 Turrets can be disabled by cutting their wires with a knife.
 When you hit Turret using knife it will enter berserker mode and after that it will disable permanently!
</details>

<details>
 <summary>Configs</summary>

- `EnableTurretCounterplay` - Toggle Turret counterplay.

</details>

---

### Coilhead

<details>
<summary>Spoiler</summary>

You can cut off a Coilhead's head using the [Knife](https://lethal.miraheze.org/wiki/Kitchen_knife) dropped by the [Butler](https://lethal.miraheze.org/wiki/Butler), which deactivates it permanently. Its head will drop as a scrap item that can be sold to the Company, but it must first be detached from its neck.

<div style="text-align: center;">
 <img alt="A player chopping off a Coilhead's head and picking it up." src="https://files.catbox.moe/2k34ho.gif" width=256>
 <img alt="A player trapped in a room by a deactivated Coilhead blocking the door, about to be killed by a Jester." src="https://files.catbox.moe/uqq80x.gif" width=256>
 <h2 style="font-weight: bold; color: firebrick; text-shadow: 0 0 3px black">— [Warning] —</h2>

 > _"They have been known to combust into flames when being dissected or even deactivated, and they carry dangerously high levels of radioactive particles."_ - Sigurd's notes

 **Hint:** _You may find a Coilhead to be less volatile the more kinetic energy it releases when coming to a halt..._

 <img alt="A player chopping off a Coilhead's head, picking it up, and then dying due to being too close to the explosion." src="https://files.catbox.moe/7ij65e.gif" width=256>

 <details>
 <summary>Spoiler (specific mechanics and configuration):</summary>

 <div style="text-align: left;">

- Coilhead bodies combust upon being decapitated, as their Bestiary entry suggests
  - Can be disabled by toggling the `LoreAccurateCoilheads` setting.
  - The range of the explosion damage is determined by the `ExplosionDamageRadius` setting, with the damage itself being set to the value of the `ExplosionDamage` setting.
  - Likewise, the `ExplosionKillRadius` setting determines the range around the explosion where it simply kills the player instead of dealing damage to them.
- Explosion timer is set to how long the Coilhead has moved since it last stopped, within configurable limits.
  - Minimum and maximum time until exploding can be configured via the `MinExplosionTimer` and `MaxExplosionTimer` settings, respectively
- Coilhead's head item is destroyed if its body explodes while it's still attached to its neck.
  - Can be disabled by toggling the `ExplosionDestroysHead` setting, but it adds some interesting risk/reward by making players stay close to try and pick up the head before it explodes.
- Client-side configuration settings:
  - `ExplosionFire` - Enable green fire effect for Coilheads that are about to explode.
  - `ExplosionParticles` - Enable radioactive particles effect for Coilheads that are about to explode.
  - `ExplosionWarnVolume` - Adjust volume of the sound effect played right before exploding (**NOT** the actual explosion).
  - `EnableCoilheadScanNode` - Enable scanning Coilheads that have been killed.
  - `ModifyCoilheadScanNode` - Add extra text/subtext to a killed Coilhead's scan node (requires `EnableCoilheadScanNode`).

 </div>
 </details>
</div>
</details>

<details>
 <summary>Configs</summary>

- `EnableCoilheadCounterplay` - Add counterplay for Coilheads.
- `SpringDurability` - Set Coilhead health points.
- `CoilheadDefaultDamage` - Amount of damage that Coilheads take from any source not specified below.
- `CoilheadKnifeDamage` - Amount of damage that Coilheads take from Knife.
- `CoilheadShovelDamage` - Amount of damage that Coilheads take from Shovel.
- `DropHeadAsScrap` - Enable the Coilhead head scrap item ('Coilless Coilhead') spawning on death.
- `MinHeadValue` - Minimum value of head item.
- `MaxHeadValue` - Maximum value of head item.
- `LoreAccurateCoilheads` - See above for more info.

</details>

---

### Ghost Girl (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Implements the popular myth involving the Shower furniture item, where taking a shower is said to reduce insanity levels and repel the Ghost Girl, as an actual gameplay mechanic.
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableGhostGirlCounterplay` - Add counterplay for the Ghost Girl.

</details>

---

### Barber (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Adds the ability to spray paint Barbers in order to visually notice and keep track of them without fully revealing them, with the paint likely slowly fading away after a configurable amount of time.
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableBarberCounterplay` - Add counterplay for the Barber.

</details>

---

### Baboon Hawk (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Adds the ability to shout at Baboon Hawks to briefly scare them away, similar to blowing an airhorn in front of them. Don't think the leader will let you get away with this, though...
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableBaboonHawkCounterplay` - Add counterplay for the Baboon Hawk.

</details>

---

### Forest Giant (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Adds the ability to escape a Forest Giant's grasp by frantically jumping, at the cost of a configurable amount of stamina (certain number of jumps required). Intended to be most useful when immediately grabbed after exiting the facility, which can be quite unfair if the Forest Giant's location is not known.
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableForestGiantCounterplay` - Add counterplay for the Forest Giant.

</details>

---

### Old Bird (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Makes Old Birds deactivate for a configurable amount of time if they happen to be struck by lightning. _Possibly_ also an interaction with Circuit Bees, if a player's brave enough...
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableOldBirdCounterplay` - Add counterplay for the Old Bird.

</details>

---

### Maneater (planned)

<details>
 <summary>Spoiler (WIP)</summary>

 Adds a configurable list of items (e.g. `Candy`, or `Red soda`) that, when fed to a Maneater, will soothe it a lot more effectively and/or reset its crying timer.

 Also, if I can figure out a good way to implement it, adding the ability to soothe a Maneater by humming, in addition to rocking it.
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableManeaterCounterplay` - Add counterplay for the Maneater.

</details>

---

### Mask Hornets (planned, _maybe_)

<details>
 <summary>Spoiler (WIP)</summary>

 [StopBeesOpeningDoors](https://thunderstore.io/c/lethal-company/p/necrowing/StopBeesOpeningDoors) already exists, but if I can think of a good enough drawback to it, it'll definitely be added to this mod!
</details>

<details>
 <summary>Configs (WIP)</summary>

- `EnableMaskHornetsCounterplay` - Add counterplay for Mask Hornets.

</details>

---

## Bug Reports / Suggestions

If you come across any issues or mod incompatibilities, or simply have an interesting idea you'd like to see implemented, feel free to drop a message in the [relevant thread](https://discord.com/channels/1168655651455639582/1212542584610881557) in the [Lethal Company Modding Discord server](https://discord.com/invite/lcmod), or [open an issue on GitHub](https://github.com/karyol/More-Counterplay-Mod/issues)!

## Credits

- [Baron Drakula](https://github.com/karyol)
- [pacoito123](https://github.com/pacoito123)
- Society for testing and giving new ideas.

### If you are enjoying my mods please consider supporting me [here](https://ko-fi.com/baron_drakula)
