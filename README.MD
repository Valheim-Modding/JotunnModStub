# JötunnModStub

A Valheim mod stub project using [JötunnLib](https://github.com/Valheim-Modding/Jotunn) including build tools for debugging your code in Visual Studio and a basic Unity project stub. There is no actual plugin content included, just a bare minimum plugin class. 

# Quick Setup Guide

These are quick setup steps with no context provided. Please see [Github Pages](https://valheim-modding.github.io/Jotunn/tutorials/getting-started.html) for more in depth documentation.

## Development Environment Setup

How to setup the development enviroment for this project.

1. Install [Visual Studio 2019](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: [BepInEx pack for Valheim](https://valheim.thunderstore.io/package/1F31A/BepInEx_Valheim_Full/)
3. Unpack into your Valheim root folder. You should now see a new folder called `<ValheimDir>\unstripped_corlib` and more additional stuff.
4. [Compile](https://github.com/CabbageCrow/AssemblyPublicizer) or [Download](https://github.com/CabbageCrow/AssemblyPublicizer/releases) AssemblyPublicizers executable version. This will result in a new executable `AssemblyPublicizer.exe`.
5. Drag and drop all `assembly_*.dll` files from `<ValheimDir>\valheim_Data\Managed` onto `AssemblyPublicizer.exe`. You should get a new folder `<ValheimDir>\valheim_Data\Managed\publicized_assemblies` with all dragged dll files in it.
6. Fork and clone this repository using git. That should create a new folder `JotunnModStub`.
7. Create a new environment file `Environment.props` in the projects base path `<JotunnModStub>`. Paste this snippet and change the path according to your local Valheim installation.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Needs to be your path to the base Valheim folder -->
    <VALHEIM_INSTALL>F:\Steam\steamapps\common\Valheim</VALHEIM_INSTALL>
  </PropertyGroup>
</Project>
```
7. Create a new user project file `JotunnModStub.csproj.user` alongside the project file within `<JotunnModStub>\JotunnModStub` if you want to start Valheim directly after building Debug. Paste this snippet into the file.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'">
    <StartAction>Program</StartAction>
    <!-- Start valheim.exe after building Debug-->
    <StartProgram>$(ValheimDir)\valheim.exe</StartProgram>
    <!-- If you want to connect to a server automatically, add '+connect <ip-address>:<port>' as StartArguments -->
    <StartArguments>-console</StartArguments>
  </PropertyGroup>
</Project>
```
8. Open the Solution file `<JotunnModStub>\JotunnModStub.sln`. Right-click on the project or solution in the Solution Explorer and select `Manage NuGet packages...`. It should prompt you a message at the top that some NuGet-Packages are missing. Click "Restore" and restart Visual Studio when finished.
9. Rename the Solution/Project and everything related so that it resembles your own projects name.

### Build automations

Included in this repo is a PowerShell script `publish.ps1`. The script is referenced in the project file as a post-build event. Depending on the chosen configuration in Visual Studio the script executes the following actions.

### Building Debug

* The compiled dll file for this project is copied to `<ValheimDir>\BepInEx\plugins`.
* A .mdb file is generated for the compiled project dll and copied to `<ValheimDir>\BepInEx\plugins`.
* `<JotunnModStub>\libraries\Debug\mono-2.0-bdwgc.dll` is copied to `<ValheimDir>\MonoBleedingEdge\EmbedRuntime` replacing the original file (a backup is created before).

### Building Release

* A compressed file with the binaries is created in `<JotunnModStub>\Packages`ready for upload to ThunderStore.

## Developing Assets with Unity

New Assets can be created with Unity and imported into Valheim using the mod. A Unity project is included in this repository under `<JotunnModStub>\JotunnModUnity`.

### Unity Editor Setup

1. [Download](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe) UnityHub directly from Unity or install it with the Visual Studio Installer via `Individual Components` -> `Visual Studio Tools for Unity`.
2. You will need an Unity account to register your PC and get a free licence. Create the account, login with it in Unity Hub and get your licence via `Settings` -> `Licence Management`.
3. Install Unity Editor version 2019.4.20f.
4. Open Unity and add the JotunnModUnity project.
5. Install the `AssetBundle Browser` package in the Unity Editor via `Window`-> `Package Manager`.
6. Copy all `assembly_*.dll` from `<ValheimDir>\valheim_Data\Managed` into `<JotunnModStub>\JotunnModUnity\Assets\Assemblies`. **Do this directly in the filesystem - don't import the dlls in Unity**.
7. Go back to the Unity Editor and press `Ctrl+R`. This should reload all files and add the Valheim assemblies under `Assets\Assemblies`

## Debugging with dnSpy

Thanks to mono and unity-mono being open source, we patched and compiled our own mono runtime to enable actual live debugging of the game and the mod itself with dnSpy.

1. Download [dnSpy-net-win64](https://github.com/dnSpy/dnSpy/releases) and extract the exe.
2. Load all assemblies from `<ValheimDir>\unstripped_corlib` into dnSpy (just drag&drop the folder onto it).
3. Load all `assembly_*` from `<ValheimDir>\valheim_Data\Managed` into dnSpy (*do not load the publicized ones, they will not be loaded into the process and therefore can not be debugged*).
4. Load `JotunnModStub.dll` from `<ValheimDir>\BepInEx\plugins` into dnSpy.
5. Copy `<JotunnModStub>\libraries\Debug\mono-2.0-bdwgc.dll` into `<ValheimDir>\MonoBleedingEdge\EmbedRuntime` and overwrite the existing file.
6. Now go to `Debug` -> `Start Debugging` and select Unity debug engine. Select your valheim.exe as the executable and hit OK.
7. If you did set some breakpoints, the game will halt when it hits the breakpoint in memory and dnSpy will show you the objects in memory and much more useful stuff.

## Debugging with Visual Studio

Your own code can be debugged in source with Visual Studio itself. You cannot debug game disassemblies as with dnSpy, though.

1. Install Visual Studio Tools for Unity (can be done in Visual Studio installer via `Individual Components` -> `Visual Studio Tools for Unity`)
3. Build the project with target `Debug`. The publish.ps1 PowerShell script from this repo...
   * copies the generated mod .dll and .pdb to \<ValheimDir>\BepInEx\plugins after a successful build
   * automatically generates a JotunnModStub.dll.mdb file, which is needed for Unity/mono debugging. It should be in \<ValheimDir>\BepInEx\plugins, too.
   * copies the patched mono-2.0-bdwgc.dll from .\libraries\Debug to \<ValheimDir>\MonoBleedingEdge\EmbedRuntime and makes a backup copy of the original.
4. Start Valheim (either directly from Steam or hit F5 in Visual Studio when Steam is running)
5. Go to `Debug` -> `Attach Unity debugger`
6. Since the patched mono dll does not open the same port as Unity Dev builds, you have to click on `Input IP`. It should fill in your local IP automatically. you just have to change the port to `55555`and the debugger should connect.

## Actions after a game update

When Valheim updates it is likely that parts of the assembly files change. If this is the case, the references to the assembly files must be renewed in Visual Studio and Unity.

### Visual Studio actions

1. Make new versions of the publicized assemblies as described in [Development Environment Setup] step 5.
2. Go to VS and clean/build (or just rebuild) the project.

### Unity actions

1. Copy all original `assembly_*.dll` as described in [Unity Editor Setup] step 6 and 7.
2. Go to Unity Editor and press `Ctrl+R`. This reloads all files from the filesystem and "re-imports" the copied dlls into the project.
