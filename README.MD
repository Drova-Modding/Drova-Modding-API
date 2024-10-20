# Unofficial Drova Modding API

This is an unofficial API for Drova. It is not supported by Drova native. It is a community project to make modding Drova easier.

This is a growing project and will be updated with more features step by step.

## Installation Instructions for Developers

Follow Instructions for https://melonwiki.xyz/#/README?id=automated-installation

After that: https://melonwiki.xyz/#/modders/quickstart (Be aware that Drova is a IL2Cpp Project and follow the instrcutions for that)

## Usage
Put into your Drova Installation Mods the .dll file (you can find it under Releases or build it yourself).

Add to your mod project:

```csharp

// other assembly infos...
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]
public class MyMod : MelonMod
{
}
```

And to your csproj: 
```csproj
<Reference Include="DrovaModdingAPI">
	<HintPath>C:\Program Files (x86)\Steam\steamapps\common\Drova - Forsaken Kin\Mods\Drova_Modding_API.dll</HintPath>
</Reference>
```


## Features
- Easier access to OptionsMenu, you can create your own Panel!
- Create your own loc files for your mods
- Easier access to the Drova Providers

### Contributing
Feel free to contribute to this project. If you have any questions or ideas, feel free to ask.
