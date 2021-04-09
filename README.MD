A Valheim mod stub project including build tools for debugging your code in Visual Studio and publishing a mod including BepInEx as zip files for Windows and Unix. There is no actual plugin content included, just a bare minimum plugin class. 

# Development Environment Setup

How to setup the development enviroment for this project.

1. Install [Visual Studio 2019](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: https://mega.nz/file/hBM0BDBS#eAY26LfmSSVlIz2MZn0ox8WKspfKmeIssz7durp5SfM
3. Unpack into your Valheim root folder. You should now see a new folder called `<ValheimDir>\unstripped_corlib`.
4. [Compile](https://github.com/MrPurple6411/Bepinex-Tools) or [Download](https://mega.nz/file/oQxEjCJI#_XPXEjwLfv9zpcF2HRakYzepMwaUXflA9txxhx4tACA) Publicizers executable version. This will result in a new executable `AssemblyPublicizer.exe`.
5. Drag and drop all `assembly_*.dll` files from `<ValheimDir>\valheim_Data\Managed` onto `AssemblyPublicizer.exe`. You should get a new folder `<ValheimDir>\valheim_Data\Managed\publicized_assemblies` with all dragged dll files in it.
6. Clone this repository using git. That should create a new folder `ValheimModStub`.
7. Create a new user project file `ValheimModStub.csproj.user` alongside the project file within `<ValheimModStub>\ValheimModStub` to define some properties local to you. Paste this snippet and configure the paths as they are present on your computer.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Needs to be your path to the base Valheim folder -->
    <ValheimDir>F:\Steam\steamapps\common\Valheim</ValheimDir>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU'">
    <StartAction>Program</StartAction>
    <!-- Start valheim.exe after building Debug-->
    <StartProgram>$(ValheimDir)\valheim.exe</StartProgram>
    <!-- If you want to connect to a server automatically, add '+connect <ip-address>:<port>' as StartArguments -->
    <StartArguments>+console</StartArguments>
  </PropertyGroup>
</Project>
```
8. Open the Solution file `<ValheimModStub>\ValheimModStub.sln`. It should prompt you a message at the top that some NuGet-Packages are missing. Click "Restore" and restart Visual Studio when finished.

# Build automations

Included in this repo are a PowerShell script `publish.ps1` and a folder `resources`. The script is referenced in the project file as a build event. Depending on the chosen configuration in Visual Studio the script executes the following actions.

## Building Debug

* The contents of `<ValheimModStub>\resources\Local` are copied to `<ValheimDir>`. You can put there whatever you want to copy to the game directoy alongside the plugin dll.
* The BepInEx core libraries are copied from `<ValheimModStub>\packages` and `<ValheimModStub>\libraries\BepInEx` to `<ValheimDir>\BepInEx\core`.
* The compiled dll file for this project is copied to `<ValheimDir>\BepInEx\plugins`.
* A .mdb file is generated for the compiled project dll and copied to `<ValheimDir>\BepInEx\plugins`.
* `<ValheimModStub>\libraries\Debug\mono-2.0-bdwgc.dll` is copied to `<ValheimDir>\MonoBleedingEdge\EmbedRuntime` replacing the original file (a backup is created before).

## Building Release

* The contents of `<ValheimModStub>\resources\Windows` and `<ValheimModStub>\resources\Unix` are copied to respective folders in `<ValheimModStub>\release`.
* BepInEx core is created in both folders
* The project dll is copied.
* Both folders are compressed into a zip file and then deleted.

# Developing Assets with Unity

New Assets can be created with Unity and imported into Valheim using the mod. A Unity project is included in this repository under `<ValheimMod>\ValheimModUnity`.

## Editor Setup

1. [Download](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe) UnityHub directly from Unity or install it with the Visual Studio Installer via `Individual Components` -> `Visual Studio Tools for Unity`.
2. You will need an Unity account to register your PC and get a free licence. Create the account, login with it in Unity Hub and get your licence via `Settings` -> `Licence Management`.
3. Install Unity Editor version 2019.4.20f.
4. Open Unity and add the ValheimModUnity project.
5. Install the `AssetBundle Browser` package in the Unity Editor via `Window`-> `Package Manager`.
6. Create the folder `<ValheimMod>\ValheimModUnity\Packages\valheim` 
7. Copy all `assembly_*.dll` from `<ValheimDir>\valheim_Data\Managed` into it.
8. Create a new text file `package.json` in the same folder and paste the following content into it:
```json
{
  "name": "valheim",
  "author": {
    "name": "",
    "email": "",
    "url": ""
  },
  "displayName": "valheim",
  "version": "0.148.6",
  "unity": "2019.4",
  "description": "Imported Assets from game valheim"
}
```
9. Go back to the Unity Editor and press `Ctrl+R`. This should reload all files and add the valheim package under `Packages` in the project tree.

## Development

To build Assets with references to the main game libraries, some workarounds are necessary.

We can not distribute the copyrighted game assemblies, so every reference to e.g. the ItemDrop script would be gone after importing your local assemblies from the game to Unity. To circumvent this, there are "wrapper" classes included in the Unity project under `Assets\_Wrappers`. **Only reference these wrapper classes when creating new Assets which need game scripts as components.**

If a needed wrapper is missing, create a new wrapper class for the script component which extends the game scripts base class under the namespace `ValheimMod.UnityWrappers`. Take the wrapper for the ZNetView for example:
```csharp
namespace ValheimMod.UnityWrappers
{
    /// <summary>
    /// A wrapper for Valheim's <see cref="ZNetView" />. Put this on your prefabs instead of <see cref="ZNetView" />.
    /// </summary>
    /// <remarks>
    /// Since Valheim's assemblies can't be redistributed they'll get new GUIDs when they're imported.
    /// Wrapping the class avoids broken script references between machines.
    /// </remarks>
    public class ZNetViewWrapper : ZNetView
    {

    }
}
```
All wrapper classes are built with the project and added to ValheimMod automatically.

New assets should be bundled in groups according to the distinct function of that asset group. Don't add new resources to existing folders when they are not linked to the functionality of that folders contents. Instead create a new folder with all your assets needed for that distinct functionality.

The same goes for asset bundles. Create a distinct asset bundle for every distinct functionality created. To do this, open the `AssetBundle Browser` and create a new asset bundle for the distinct functionality. Add all needed assets to it and close the browser again. Unity will save the definition of all asset bundles internally.

All asset bundles are built with the project and added to the mod folder `<ValheimMod>\ValheimMod\AssetBundles` automatically. New bundles must be loaded to the mod manually by writing code (in an AssetLoader class for example).

## Building

To include the assets into the mod, a special build is necessary. There is an Unity Editor extension coded for that, found under `Assets\_Editor`. The extensions adds a new `ValheimMod` menu to the Editor. From there you can build everything at once or only create the assemblies or asset bundles alone. Normally you would only "Build Asset Bundles". The assembly should only be built when a new wrapper class is introduced. git will always declare the assembly as modified and add a new revision of it to the repo, although nothing actually changed in it, because of the file timestamp. So "Build All" or "Build Runtime Assemblies" only if really necessary.

# Debugging with dnSpy

Thanks to mono and unity-mono being open source, we patched and compiled our own mono runtime to enable actual live debugging of the game and the mod itself with dnSpy.

1. Download [dnSpy-net-win64](https://github.com/dnSpy/dnSpy/releases) and extract the exe.
2. Load all assemblies from `<ValheimDir>\unstripped_corlib` into dnSpy (just drag&drop the folder onto it).
3. Load all `assembly_*` from `<ValheimDir>\valheim_Data\Managed` into dnSpy (*do not load the publicized ones, they will not be loaded into the process and therefore can not be debugged*).
4. Load `ValheimModStub.dll` from `<ValheimDir>\BepInEx\plugins` into dnSpy.
5. Copy `<ValheimModStub>\libraries\Debug\mono-2.0-bdwgc.dll` into `<ValheimDir>\MonoBleedingEdge\EmbedRuntime` and overwrite the existing file.
6. Now go to `Debug` -> `Start Debugging` and select Unity debug engine. Select your valheim.exe as the executable and hit OK.
7. If you did set some breakpoints, the game will halt when it hits the breakpoint in memory and dnSpy will show you the objects in memory and much more useful stuff.

# Debugging with Visual Studio

Your own code can be debugged in source with Visual Studio itself. You cannot debug game disassemblies as with dnSpy, though.

1. Install Visual Studio Tools for Unity (can be done in Visual Studio installer via `Individual Components` -> `Visual Studio Tools for Unity`)
3. Build the project with target `Debug`. The publish.ps1 PowerShell script from this repo...
   * copies the generated mod .dll and .pdb to \<ValheimDir>\BepInEx\plugins after a successful build
   * automatically generates a ValheimModStub.dll.mdb file, which is needed for Unity/mono debugging. It should be in \<ValheimDir>\BepInEx\plugins, too.
   * copies the patched mono-2.0-bdwgc.dll from .\libraries\Debug to \<ValheimDir>\MonoBleedingEdge\EmbedRuntime and makes a backup copy of the original.
4. Start Valheim (either directly from Steam or hit F5 in Visual Studio when Steam is running)
5. Go to `Debug` -> `Attach Unity debugger`
6. Since the patched mono dll does not open the same port as Unity Dev builds, you have to click on `Input IP`. It should fill in your local IP automatically. you just have to change the port to `55555`and the debugger should connect.

# Actions after a game update

When Valheim updates it is likely that parts of the assembly files change. If this is the case, the references to the assembly files must be renewed in Visual Studio and Unity.

## Visual Studio actions

1. Make new versions of the publicized assemblies as described in [Development Environment Setup] step 4 and 5.
2. Go to VS and clean/build (or just rebuild) the project. The building process makes copies of the referenced dlls and stores them in the project folder. To get the newly created dlls it is important to grab the new publicized dlls from step 1.

## Unity actions

1. Copy all original `assembly_*.dll` as described in [Editor Setup] step 7 and 8.
2. Go to Unity Editor and press `Ctrl+R`. This reloads all files from the filesystem and "imports" the copied dlls into the project.