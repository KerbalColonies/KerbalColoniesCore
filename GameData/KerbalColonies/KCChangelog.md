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