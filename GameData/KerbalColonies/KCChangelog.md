# v1.1.6 hotfix
- Fixed the performance issues at the spacecenter

# v1.1.5 hotfix
- Fixed the storage facility exception when being newly spawned, see issue [#46](https://github.com/KerbalColonies/KerbalColoniesCore/issues/46)

# v1.1.4
- Added a tech tree integration
- Added resource (transfer) limits to the mining and ISRU facilities
- Sorted the storage facility resources

# v1.1.3 hotfix
- Fixed the resource converter facility storage logic, see issue [#44](https://github.com/KerbalColonies/KerbalColoniesCore/issues/44)

# v1.1.2 hotfix
- Fixed the group change upgrades where they are not on the current body, see issue [#42](https://github.com/KerbalColonies/KerbalColoniesCore/issues/42)
- Fixed the harvest type only being changed for the placement preview, see issue [#43](https://github.com/KerbalColonies/KerbalColoniesCore/issues/43)
- Fixed the unit for the stored EC in the CAB overview

# v1.1.1 hotfix
- Fixed a bug where the CAB upgrade would be stopped when the scene was reloaded, see issue [#41](https://github.com/KerbalColonies/KerbalColoniesCore/issues/41)
- Set the isInSavegame flag for all new statics when upgrading a facility with group change mode, this would delete these statics upon reload

# v1.1.0
- Added an electricity system with an ec storage, wind turbines, fuel cells and fission and fusion reactors
- Reworked the commnet facility to now be either an unmanned commnet node or an (optionally) manned commnet node
- Reworked the launchpad facility to support multiple levels
- Reworked the saving system to store everything in scenario modules (except the settings), including the KK statics/groups
- Fixed the hangar facility size (a rework is planned for v1.2)
- Added a "CAB" tab to the cab window which now features a colony overview with the most important things like ec production/consumption/delta/stored
- Reworked the CAB facility to allow upgrades and added minimum CAB level options to the other facilities
- Added editor range limits which can change based on the CAB level, default is 1km
- Reworked the resource conversion list system to continue loading the other lists if one of them fails
- Resource conversion lists with the same name are now additive
- Added a category parameter to show a nice displayname for the types instead of the source code name\nThis also allows for splitting/merging of different types as it's set in each facility config
- Reworked the ISRU facility to allow the use of previous ISRU level counts with the same kerbal requirement and added a minimum kerbal parameter to allow higher levels with fewer kerbals. In the EPL config this results in a fully automated final level but only 2 ISRUs.

# v1.0.11 hotfix
- Fixed the launchpad facility OnGroupPlaced not generating a launchpad, see issue [#35](https://github.com/KerbalColonies/KerbalColoniesCore/issues/35) and [#36](https://github.com/KerbalColonies/KerbalColoniesCore/issues/36)

# v1.0.10 hotfix
- Fixed the mining facility disappearing, see issue [#31](https://github.com/KerbalColonies/KerbalColoniesCore/issues/31)
- Fixed a bug in the CAB loading that could delete the colony
- Fixed the hangar facility production display, see issue [#32](https://github.com/KerbalColonies/KerbalColoniesCore/issues/32)
- Fixed the incompatibility with certain mods, see issue [#33](https://github.com/KerbalColonies/KerbalColoniesCore/issues/33)
- Issue [#34](https://github.com/KerbalColonies/KerbalColoniesCore/issues/34) (no resources after launch from ksc) should be fixed
- Improved logging
- The single time windows are now shown once per game scene load

# v1.0.9
- Reworked the mining facility to use the resource density instead of fixed rates (this will most likely result in reduced rates)
- Added auto resource transfer options to the mining facility
- Small UI improvments for the mining facility
- Added an auto disable option to the isru facility that will disable the facility when not enough resources are available

# v1.0.8
- Reworked the kerbal gui to use the crew icons from the astronaut complex
- Reworked the production facility ui to group the available types together
- Reworked the CAB ui to group the facilities by type and added a brief overview of the facility
- A rightclick on the KC toolbar icon now toggles the click on building function

# v1.0.7
- Fixed the crewquarters kerbal transfer when switching the vessels, see issue [#23](https://github.com/KerbalColonies/KerbalColoniesCore/issues/23)
- Added facility/vessel cost/time multiplier settings and the max colonies per body setting with difficulty presets to the gameparameter settings
- Removed the hardcoded kerbin requirement for the base groups, the base body is now defined in the KC.cfg file or the homeworld if the config file does not contain it
- The insufficient resources are now displayed when the build colony failed

# v1.0.6
- Fixed the allowed/forbidden traits logic when transferring kerbals to not use the localized trait name
- Fixed the paths for the external config files
- Fixed the colony count when creating a colony
- Allow remote upgrades
- Only the KK groups that have a facility assigned are now enabled, this means when reverting to a point before a colony was made the colonies statics won't be visible
- KK groups on other bodies than the active body when loading are now disabled
- Added kerbal trait and level information to the transfer window
- All kerbals that are in colonies will now be marked as assigned in the crewroster (note: they won't be visible in the crewroster, only the count indicates that there are more kerbals there. Additionally if the crew respawn time is on then after leaving the crewroster the kerbals in colonies will be marked as missing until the next update of the crewquarters)

# v1.0.5
- Fixed the storage facility resource transfer (for real this time (I hope), the resources are now distributed evenly across the vessel)
- moved the resource conversion configs over to the config mod
- improved exception display in the main menu

# v1.0.4
- Fixed the storage facility resource transfer
- Added whitelist/blacklist options to the storage facility configs
- Reworked the mining facility to allow the use of any resource (the configs need to be updated)
- Removed Extraplanetary Launchpads as dependency for the core mod

# v1.0.3
- Fixed the auto colony display name which also caused an exception during the saving of the colony in the editor

# v1.0.2
- Reworked the storage facility to only show available resources
- Added this changelog with minimal markdown support
- Added the missing license file to release

# v1.0.1
- Seperated the facility configs into their own [mod](https://spacedock.info/mod/3899/KerbalColonies-ExtraplanetaryLaunchpadsConfig) (also on ckan) to allow easier replacement in the future
- Added options to rename facilities and colonies

# v1.0.0
- Initial release