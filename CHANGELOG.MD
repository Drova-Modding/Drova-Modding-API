# Changelog

## [0.3.0 - 2024-11-29]
### Added
- Event System
- Controller Bindings
- Actor Spawning for creatures
- Input Access to Rewired (Thanks to HanHan)
- Area Name System, to know where the player is currently
- Save System to hook in
- GlobalFields added for common SceneNames

### Fixed
- Fixed a bug where option menu would not be open again after closing it

### Changed
- Changed from `KeyCode` to `InputAction` to support more input devices
- Added a Release Package and Debug Package (For User and Developer)

### Deprecated
- Deprecated the `KeyCode` approach in favor of the `InputAction` approach

### Known Issues
- In the console you will see 2 errors that something was not set properly regarding the Patches, it does not affect the Game from my investigations, but I will look into it in the future.

## [0.2.3 - 2024-11-04]
### Fixed
- Fixed a bug where the KeyBinding default value did not get saved and was not accessible at runtime

### Added
- A lot more null safety and better logging

## [0.2.2 - 2024-11-03]
### Fixed
- Fixed a bug where the KeyBindingSection would not refresh their conflict marker
- Fixed a bug where the KeyBindingSection would not display the conflict marker, when the keybinding in Drova have Spaces

## [0.2.1 - 2024-11-02]
### Fixed
- Fixed a bug where the KeyBindingSection would not refresh the keybindings when you change the language

## [0.2.0 - 2024-11-02]
### Added
- Button component
- PlayerAccess thanks to @HanHangit
- Addressable Access and Addressable Generation
- Added GameManager under ProviderAccess and related GameHandlers

### Changed
- OptionMenuAccess has now a method to get the builder for a common modding panel settings (instance method). This requires a identifier for your settings.
- OptionMenuAccess has now a method to get the builder for your own panel settings (static method)
- CreateKeyBindingSection has now a changed order of parameters. Title is now the second parameter and optional. Also will it now longer create a section if the title is null or empty. And it will put it into your corrosponding Panel.

### Fixed
- Fixed a bug where the OptionMenuAccess would not work display itself correctly when you are ingame
- Fixed a bug in the dictonary of the GameSettings where the key would already exist
- Fixed a lot of null checks in the OptionMenuAccess