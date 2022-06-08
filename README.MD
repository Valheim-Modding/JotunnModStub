# JötunnModStub

A Valheim mod stub project using [Jötunn](https://github.com/Valheim-Modding/Jotunn) including build tools and a basic Unity project stub. There is no actual plugin content included, just a bare minimum plugin class. 

# Quick Setup Guide

These are quick setup steps with no context provided. Please see Jötunns [Github Pages](https://valheim-modding.github.io/Jotunn/guides/overview.html) for a more in depth guide and documentation.

## Development Environment Setup

How to setup the development enviroment for this project.

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: [BepInEx pack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
3. Unpack and copy the contents of `BepInExPack_Valheim` into your Valheim root folder. You should now see a new folder called `<ValheimDir>\unstripped_corlib` and more additional stuff.
4. Fork and clone this repository using git. That should create a new folder `JotunnModStub`. You can also [use the template function of github](https://github.com/Valheim-Modding/JotunnModStub/generate) to create a new, clean repo out of it and clone that.
5. Edit `DoPrebuild.props` in the project base path and change `ExecutePrebuild` to `true` if you want Jötunn to automatically generate publicized versions of the game dlls for you.
6. Open the Solution file `<JotunnModStub>\JotunnModStub.sln`. Right-click on the project or solution in the Solution Explorer and select `Manage NuGet packages...`. It should prompt you a message at the top that some NuGet-Packages are missing. Click "Restore" and restart Visual Studio when finished.
7. Rename the Solution/Project and everything related so that it resembles your own projects name. This includes the assembly information as well as the Unity project.

A new environment file `Environment.props` can be created in the projects base path `<JotunnModStub>`.
Make sure you are not in any subfolder.
Paste this snippet and change the paths accordingly.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Valheim install folder. This is normally found automatically, uncomment to overwrite it. Needs to be your path to the base Valheim folder. -->
    <!-- <VALHEIM_INSTALL>X:\PathToYourSteamLibary\steamapps\common\Valheim</VALHEIM_INSTALL>-->

    <!-- This is the folder where your build gets copied to when using the post-build automations -->
    <MOD_DEPLOYPATH>$(VALHEIM_INSTALL)\BepInEx\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```

### Post Build automations

Included in this repo is a PowerShell script `publish.ps1`. The script is referenced in the project file as a post-build event. Depending on the chosen configuration in Visual Studio the script executes the following actions.

### Building Debug

* The compiled dll file for this project is copied to `<ValheimDir>\BepInEx\plugins` (or whatever path you set as MOD_DEPLOYPATH).
* A .mdb file is generated for the compiled project dll and copied to `<ValheimDir>\BepInEx\plugins` (or whatever path you set as MOD_DEPLOYPATH).

### Building Release

* A compressed file with the binaries is created in `<JotunnModStub>\Packages`ready for upload to ThunderStore. Dont forget to include your information in the manifest.json and to change the project's readme file.

## Developing Assets with Unity

New Assets can be created with Unity and imported into Valheim using the mod. A Unity project is included in this repository under `<JotunnModStub>\JotunnModUnity`.

### Unity Editor Setup

1. [Download](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe) UnityHub directly from Unity or install it with the Visual Studio Installer via `Individual Components` -> `Visual Studio Tools for Unity`.
2. You will need an Unity account to register your PC and get a free licence. Create the account, login with it in Unity Hub and get your licence via `Settings` -> `Licence Management`.
3. Install Unity Editor version 2020.3.33f.
4. Copy all `assembly_*.dll` from `<ValheimDir>\valheim_Data\Managed` into `<JotunnModStub>\JotunnModUnity\Assets\Assemblies`. **Do this directly in the filesystem - don't open Unity first or import the dlls directly in Unity**.
5. **Warning:** These assembly files are copyrighted material and you can theoretically get into trouble when you distribute them in your github repository. To avoid that there is a .gitignore file in the Unity project folder. Keep that when you clone or copy this repository.
6. Open Unity Hub and add the JotunnModUnity project.
7. Open the project.
7. Install the `AssetBundle Browser` package in the Unity Editor via `Window`-> `Package Manager` for easy bundle creation.

## Debugging

You can enable remote debugging of your mod code at runtime via dnSpy or Visual Studio. Before being able to attach a remote debugger you will have to prepare your game install and turn it into a "Development Build" once:

1. Locate your Unity Editor installation from the previous step and navigate to `<UnityInstall>\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_development_mono`
2. Copy `UnityPlayer.dll` and `WinPixEventRuntime.dll` from that folder into your game installation folder. Overwrite existing files.
3. Open the `<Valheim>\valheim_Data\boot.config` with a text editor (Notepad++ for example) and add a new line `player-connection-debug=1` to it.
4. When starting up Valheim you should see a `Development Build` text at the lower-right corner of the screen.

### Debugging with Visual Studio

Your own code can be debugged in source with Visual Studio itself.

1. Install Visual Studio Tools for Unity (can be done in Visual Studio installer via `Individual Components` -> `Visual Studio Tools for Unity`)
3. Build the project with target `Debug`. The publish.ps1 PowerShell script from this repo...
   * copies the generated mod .dll and .pdb to \<ValheimDir>\BepInEx\plugins after a successful build
   * automatically generates a JotunnModStub.dll.mdb file, which is needed for Unity/mono debugging. It should be in \<ValheimDir>\BepInEx\plugins, too.
4. Start Valheim (either directly from Steam or hit F5 in Visual Studio when Steam is running)
5. Go to `Debug` -> `Attach Unity debugger`
6. You should see your local game instance listed as a target to attach the debugger to. If it is not there, try hitting `Refresh` as the debugger only appears after the game has loaded into the main menu.

## Actions after a game update

When Valheim updates it is likely that parts of the assembly files change. If this is the case, the references to the assembly files must be renewed in Visual Studio and Unity.

### Visual Studio actions

1. There is a file called DoPrebuild.props included in the solution. When you set its only value to true, Jötunn will automatically generate publicized assemblies for you. Otherwise you have to do this step manually.

### Unity actions

1. Copy all `assembly_*.dll` from `<ValheimDir>\valheim_Data\Managed` into `<JotunnModStub>\JotunnModUnity\Assets\Assemblies`. <br />
  **Do this directly in the filesystem - don't import the dlls in Unity**.
2. Go to Unity Editor and press `Ctrl+R`. This reloads all files from the filesystem and "re-imports" the copied dlls into the project.
