# KerbalColonies
![Downloads](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fraw.githubusercontent.com%2FKSP-CKAN%2FCKAN-meta%2Frefs%2Fheads%2Fmaster%2Fdownload_counts.json&query=KerbalColonies&label=Downloads)
![Last commit](https://img.shields.io/github/last-commit/AMPW-german/KerbalColonies/master.svg)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/AMPW-german/KerbalColonies)
![Image](https://i.imgur.com/twFy677.jpeg)

Trailer:
https://www.youtube.com/watch?v=d6NffcYh760

## Main Goal
Our goal in future iterations is to change how you fundamentally play the game when it comes to colonies and the colonists themselves.
Unlike other colony mods KC uses KK groups as facilities, this means there are 0 parts in a colony.
KK has it's own facilities but only on single statics and they can only cost funds. They also don't use real kerbals so overall they aren't really a Colony.

One of the reasons why KC took so long was because it has a big focus on modularity.
The facilities are defined in a config file and contain the name of a KK group on Kerbin which will be copied to your current location when building the facility. The cost can be any resource in the PartResourceLibrary with any amount you want, funds are also supported (if available, otherwise ignored).
Currently there are 10 different facility types (including the CAB) but it's easily expandable, even other mods should be able to add their own types.
For more information please read the wiki.

## Performance:
While playing the performance is pretty much unaffected, only potentially with a ton of facilities and at very high timewarp it could be reduced (I haven't tested the limits) but most facilities do very little during the update.
All facilities are only updated once every 10 ingame seconds which further reduces the impact.
The only time where it will have a noticable impact is while switching the scene because there are multiple things done during that period, including:
* loading all of the facilities
* disabling the KK colony groups from other savegames

## Saving:
Kerbal Colonies uses an external config file that's used for cross game things and a scenario module for each game.
Scenario modules are stored per save, this means that a colony that's created at t+1000 won't be there in t+0, only the KK groups.


Kerbals that are in a vessel and a colony will be removed from the vessel.
When launching from the spacecenter view I can't forbid the launch so this necessary but when you are launching from the vab/sph you will get a warning about the kerbals that are in a colony.

Here's a video about [building vessels at colonies!](https://youtu.be/6lne_vgd7j8)

## Planned features:
* draining facility build cost
* electricity
* integration of life support mod(s)
* reworked mining facility (using the ore density)
* reworked commnet facility (requiring kerbals to work)
* cross colony resource and kerbal transport
* more included kk groups
* contracts
* auto grass color (based on the terrain color below the group)
* buildables (statics without a facility)


This mod is still in early development, while it's mostly bug free in our testing I can't and won't guarantee that there are no game breaking bugs or unexpected incompatibilities.
It works with existing saves but a backup is recommended.

Available on [SpaceDock](https://spacedock.info/mod/3896/Kerbal%20Colonies), GitHub and on CKAN.
This mod is fully licensed under the GNU GPLv3 license.

# Dependencies:
* KerbalKonstructs >= v1.10.0.0
* CustomPrelaunchChecks
* Click Through Blocker
* Extraplanetary launchpads (used for more resources, eventually there will be stock resource configs)
* OSSNTR
* paraterraforming

If you want to create new facility configs or an addon mod for more facility types I recommend reading the [wiki](https://github.com/AMPW-german/KerbalColonies/wiki)
Any help with the development of the plugin is appreciated.

If you encounter any bugs you can open a github issue or report it on the forums, suggestions are also always welcome.

Also, NO SUPPORT WITHOUT LOGS (preferably with debug mode enabled if it's repeatable).
