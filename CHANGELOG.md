# Changelog

## 1.4.1

- Fixed clients not being able to place items on a Jester if it spawned inside while they were outside (or vice versa)
- Added several planned future counterplays to the README, based on a lot of ideas and suggestions by the community!

## 1.4.0

<ul>
 <li>Reworked Jester counterplay:
  <ul>
   <li>
    Items are now placed on top of a Jester by holding an item and interacting with its lid
   </li>
   <li>
    After its pop is prevented, the Jester now drops all items on its head, forcing players to keep an eye out and make sure there's always items on top of it before it finishes cranking
   </li>
   <li>
    Added some consequences for putting an excessive amount of weight on top of a Jester (configurable)
   </li>
   <li>
    Hitting a Jester with a shovel now causes it to drop all items placed on top of it
   </li>
   <li>
    Scanning a Jester now shows its total weight as its scan node's subtext
   </li>
   <li>
    New features can be disabled in the config file, if so desired
   </li>
   <li>
    <details>
     <summary>Spoiler (specific mechanics and configuration):</summary>
     <ul>
      <li>Any grabbable item can be deposited onto a Jester by holding the interact button while the prompt to place an item is visible (similar to the Ship's storage cabinet, the Cruiser's back storage, and the desk at the Company)
       <ul>
        <li>Items can no longer be physically dropped on top of it, but this will likely be readded in the future</li>
       </ul>
      </li>
      <li>If the Jester finishes cranking while its total weight exceeds the amount set by the <code>JesterPreventThreshold</code> setting (<b>60</b> pounds by default), its head visually pops out for a brief moment to drop its items before returning to its box, without actually chasing or killing players
       <ul>
        <li>Toggling the <code>ItemsStayOnLid</code> setting disables the Jester's ability to drop its items and keeps its head inside the box at all times, making it like how it used to work in previous versions of the mod</li>
       </ul>
      </li>
      <li>If the Jester's total weight exceeds the amount set by the <code>JesterEncumberThreshold</code> setting (<b>120</b> pounds by default), it'll no longer be able to follow you around due to being encumbered by the items
       <ul>
        <li>Threshold setting can be set to <code>0</code> to disable the Jester's encumbered state completely, or a small value (e.g. <code>0.1</code>) to allow almost any item to stop the Jester from moving</li>
       </ul>
      </li>
      <li>If the Jester's total weight exceeds the amount set by the <code>JesterPanicThreshold</code> setting (<b>200</b> pounds by default), it'll panic and begin cranking frantically before popping shortly after
       <ul>
        <li>Threshold setting can be set to <b>0</b> to disable the Jester's panicked state completely</li>
        <li>Minimum and maximum time that the Jester spends panicking can be configured via the <code>MinPanicTimer</code> and <code>MaxPanicTimer</code> settings, respectively</li>
        <li>Setting this threshold to a lower value than the <code>JesterPreventThreshold</code> setting functionally disables it as a counterplay, since the Jester's pop is not prevented while panicking</li>
        <li>Placing an item while over the panic threshold and while the Jester is cranking (even normally) will cause it to <b>skip its cranking and pop immediately</b></li>
       </ul>
      </li>
      <li>Whenever a Jester is hit by a shovel, it'll drop all its held items and reset its weight
       <ul>
        <li>Can be disabled by toggling the <code>DropItemsOnHit</code> setting</li>
       </ul>
      </li>
      <li>The total amount of weight the Jester is carrying is shown in the subtext of its scan node
       <ul>
        <li>Can be disabled by toggling the <code>ShowWeightOnScan</code> setting</li>
        <li>Might be slightly inaccurate due to integer rounding, or if an item is destroyed or otherwise removed by other means</li>
       </ul>
      </li>
     </ul>
    </details>
   </li>
  </ul>
 </li>
</ul>

- Fixed Jester getting stuck on its cranking animation after its pop is prevented
  - All animations should now be properly synced between all clients
- "Coilless Coilhead" material texture is now obtained from the Coilhead right as it spawns
  - Should make it compatible with some [EnemySkinKit](https://thunderstore.io/c/lethal-company/p/AntlerShed/EnemySkinKit) skins that don't modify the Coilhead model too much (e.g. [ColorfulEnemyVariety](https://thunderstore.io/c/lethal-company/p/DistinctBlaze/ColorfulEnemyVariety))
  - Material texture persists until reloading the save file, at which point it'll return to the vanilla Coilhead texture
- Fixed "Coilless Coilhead" item sound effects being heard globally by changing it from a 2D sound to a 3D one
- Added more information to the mod description, as well as short gifs showcasing some features

## 1.3.1

- Added several client-sided configuration settings for "lore accurate" Coilheads
- Fixed "lore accurate" Coilhead particle effects not showing up after reloading a lobby once
- Split configuration file entries into categories based on their respective entities

## 1.3.0

<ul>
 <li>Added <a href="https://thunderstore.io/c/lethal-company/p/Sigurd/CSync">CSync v5</a> as a dependency to ensure config parity between host and clients.</li>
 <li>Added "lore accurate" Coilhead counterplay (configurable):
  <ul>
   <li>
    <blockquote>
    <i>"They have been known to combust into flames when being dissected or even deactivated, and they carry dangerously high levels of radioactive particles."</i> - Sigurd's notes
    </blockquote>
   </li>
   <li>
    <b>Hint:</b> <i>You may find a Coilhead to be less volatile the more kinetic energy it releases when coming to a halt...</i>
   </li>
   <li>
    <details>
     <summary>Spoiler (specific mechanics and configuration):</summary>
     <ul>
      <li>Coilhead bodies now combust upon being decapitated, as their Bestiary entry suggests
       <ul>
        <li>Can be disabled by toggling the <code>LoreAccurateCoilheads</code> setting</li>
        <li>The range of the explosion damage is determined by the <code>ExplosionDamageRadius</code> setting, with the damage itself being set to the value of the <code>ExplosionDamage</code> setting</li>
        <li>Likewise, the <code>ExplosionKillRadius</code> setting determines the range around the explosion where it simply kills the player instead of dealing damage to them</li>
       </ul>
      </li>
      <li>Explosion timer is set to how long the Coilhead has moved since it last stopped, within configurable limits
       <ul>
        <li>Minimum and maximum time until exploding can be configured via the <code>MinExplosionTimer</code> and <code>MaxExplosionTimer</code> settings, respectively</li>
       </ul>
      </li>
      <li>Coilhead's head item is destroyed if its body explodes while it's still attached to its neck
       <ul>
        <li>Can be disabled by toggling the <code>ExplosionDestroysHead</code> setting, but it adds some interesting risk/reward by making players stay close to try and pick up the head before it explodes</li>
       </ul>
      </li>
     </ul>
    </details>
   </li>
  </ul>
 </li>
</ul>

- Coilheads can no longer be decapitated while in their cooldown state (they protect their necks!)
- Coilhead damage should now apply properly for all clients, whereas previously Coilheads would take damage separately even with the same settings
- Renamed Coilhead head item to "Coilless Coilhead"
- Changed "Coilless Coilhead" item sound effects a bit
- Fixed "Coilless Coilhead" item not being properly rotated when held
  - _You may discover it to not be as inanimate as it initially lets on..._
- Fixed several networking issues with the "Coilless Coilhead" item
  - New stuff should also be working reliably in multiplayer
- Reduced AssetBundle size significantly by adding large textures to prefabs at runtime instead of bundling them

## 1.2.3

- Coilhead's head item should no longer vanish from the ship after reloading the save file
- Coilhead's head item now spawns properly attached and facing the right way
- Coilhead's head item no longer floats above the ground when dropped/placed, and is upright
  - This also made it able to be properly placed inside the storage cabinet
- Coilhead's head item scale further reduced to 0.1763
  - Matches the size of the actual head
- Coilhead should no longer get stuck in its moving animation if killed while moving
  - Could happen previously while walking backwards into it while swinging the knife
- Coilhead's neck now wobbles one final time when killed, to indicate its death

## 1.2.2

- Dropped items on Jester should now attach properly (v56 compatibility)
- Items can no longer be placed on Jester while inside the ship
  - Should prevent Jester sneaking under the ship and stealing items in some custom moons
- Fixed error when removing items from the Jester through iteration
- Coilhead's head scale reduced to 0.2

## 1.2.1

- Added Head scrap item
- Coilheads now drop their head on death
- Fixed Coilhead's head disappear only for host bug

## 1.2.0

- Added Coilhead counterplay

## 1.1.0

- Added Turret counterplay

## 1.0.1

- Better README

## 1.0.0

- Added Jester counterplay
