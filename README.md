<p align="center">
    <img src="http://i64.tinypic.com/2qtwqjt.png" alt="JecsTools" />
</p>

<p align="center">
  <a href="https://github.com/jecrell/JecsTools/releases/">
    <img src="https://img.shields.io/badge/release-1.0.9-4BC51D.svg?style=flat" alt="v1.0.9.1" />
  </a>
  <a href="https://github.com/roxxploxx/RimWorldModGuide/wiki">
    <img src="https://img.shields.io/badge/documentation-Wiki-4BC51D.svg?style=flat" alt="Documentation" />
  </a>
  <a href="https://www.patreon.com/jecrell">
    <img src="https://img.shields.io/badge/support%20me%20on-Patreon-red.svg?style=flat" alt="Support me on Patreon." />
    </a>
</p>

<p align="center">
 Adds modding components to RimWorld: vehicles, spell casting, weapon slots, oversized weapons, and more!
</p>

<hr>

**Note to players:** This mod will not change your game, but rather it lets modders do more, so you can have an even more amazing RimWorld experience.
	
**Note to modders:** This mod contains components that allow you to do many new and different things in RimWorld. Check out RoxxPloxx's guide for more info here: https://github.com/roxxploxx/RimWorldModGuide/wiki
	
Total list of components:

*CompAbilityUser*
 - Adds spell/ability casting to humanlikes.
 
*CompActivatableEffect*
 - Adds an activation graphic for weapons (e.g. lightsaber beam). 
 
*CompDeflector*
 - Allows the ability to knock projectiles away with melee weapons.
 
*CompExtraSounds*
 - Adds extra melee sounds to weapons.
 
*CompLumbering*
 - Gives pawns a staggered walking animation that cycles between two images. (e.g. ATST walking effect)
 
*CompOversizedWeapon*
 - Allows weapons to have graphic sizes that can be bigger than RimWorld's vanilla limits.
 
*CompSlotLoadable*
 - Adds slots to objects, weapons, apparel, etc that can be filled to have effects. (e.g. an ammo slot for guns with different kinds of ammunition, crystal slots for lightsabers, etc)
 
*CompVehicle (Experimental, Additions by Swenzi)*
 - Allows for a pawn to be treated as a vehicle that can be loaded with pilots, gunners, crew, and passengers.

*CompInstalledPart*
 - Allows for a part to be installable and uninstallable onto another thing. This is particularly useful for vehicle weapons.

*CompToggleDef (by Roxxploxx)*
 - A situational Component that allows you to toggle the ThingDef of a selected Thing via a radio button menu. ex. Change a ring to be for a pinky finger versus a ring or index finger.

*CompDelayedSpawner* 
- Allows us to create things or pawns after a set amount of time. For instance, I created an invisible spawner for the Star Vampire (as of this update) that uses this CompDelayedSpawner. This lets me trigger the Star Vampire incident, drop down some delayed spawners, and enjoy results after a short period of time. The CompDelayedSpawner is highly customizable for things, pawns, and even allows for setting mental states and hediffs.

Total List of Classes

*JecsTools.Hediff_TransformedPart*
 - Similar to added part, however, transformed parts will not remove the original parts when removed from the character. This allows for us to "transform" pawn parts. Such as having a colonists' hands turn into deadly claws.

*JecsTools.JobGiver_AIFirelessTrashColonyClose*
*JecsTools.JobGiver_AIFirelessTrashColonyDistant*
 - These classes lets us call a special jobgiver for raiders that does not include setting fire to objects. This is good for monstrous creatures that do not have the ability to start fires but still want to break things.
	
Additions by roxxploxx.
Additions by Swenzi.
Transpilers by Erdelf.
Extensive hours of testing, debugging, and fixes by Xen.
"Hey, should we make this into a public toolset for people to take advantage of all this cool stuff?" - Jecrell
"Hell yes - this is awesome stuff - people will love it!" - Xen
	
Special thanks to Pardeike's amazing non-destructive patching library, Harmony. Without his work, none of this would be possible.
<p align="center">
  <a href="https://github.com/pardeike/Harmony">
    <img src="https://s24.postimg.org/58bl1rz39/logo.png" alt="Harmony" />
    </a>
</p>


<hr>

<p align="center">
  <a href="mailto:matt.walls31@gmail.com">
    <img src="https://img.shields.io/badge/email-matt.walls31@gmail.com-blue.svg?style=flat" alt="Email: matt.walls31@gmail.com" />
  </a>
  <a href="https://raw.githubusercontent.com/jecrell/JecsTools/master/LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-lightgray.svg?style=flat" alt="MIT License" />
  </a>
</p> 
