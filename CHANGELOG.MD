# Changelog

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