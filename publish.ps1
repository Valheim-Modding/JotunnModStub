param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath
)

function Create-BepInEx{
    param (
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,

        [Parameter(Mandatory)]
        [ValidateSet('Windows','Unix','Local')]
        [System.String]$DistSystem
    )
    Write-Host "Creating BepInEx in $DistPath"

    # copy needed files for this target system
    Copy-Item -Path "$(Get-Location)\resources\$DistSystem\*" -Destination "$DistPath" -Recurse -Force
    
    # create \BepInEx
    $bepinex = $DistPath.CreateSubdirectory('BepInEx')
    
    # create \BepInEx\core and copy core dlls from build
    $core = $bepinex.CreateSubdirectory('core');
    Copy-Item -Path "$TargetPath\*" -Filter 'BepInEx*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter '*Harmony*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter 'Mono.Cecil*.dll' -Destination "$core" -Force
    Copy-Item -Path "$TargetPath\*" -Filter 'MonoMod*.dll' -Destination "$core" -Force

    # create \BepInEx\plugins and copy plugin dlls from build
    $plug = $bepinex.CreateSubdirectory('plugins');
    Write-Host "Plugins: $TargetAssembly"
    Copy-Item -Path "$TargetPath\*" -Include $TargetAssembly.Split(',') -Destination "$plug" -Force

    # copy debug files when dist system = Local
    if ($DistSystem.Equals("Local")) {
        Write-Host "Copy mono-2.0-bdwgc.dll to $ValheimPath\MonoBleedingEdge\EmbedRuntime"
        if (!(Test-Path -Path "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll.orig")) {
            Copy-Item -Path "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll" -Destination "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll.orig" -Force
        }
        Copy-Item -Path "$(Get-Location)\libraries\Debug\mono-2.0-bdwgc.dll" -Destination "$ValheimPath\MonoBleedingEdge\EmbedRuntime" -Force

        foreach($asm in $TargetAssembly.Split(',')){
            $pdb = "$TargetPath\" + ($asm -Replace('.dll','.pdb'))
            if (Test-Path -Path "$pdb") {
                Write-Host "Copy Debug files for plugin $asm"
                Copy-Item -Path "$pdb" -Destination "$plug" -Force
                start "$(Get-Location)\libraries\Debug\pdb2mdb.exe" "$plug\$asm"
            }
        }
        
        # set dnspy debugger env
        #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=y,no-hide-debugger'
        #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2','','User')
    }

    # return basepath as DirectoryInfo
    return $base
}

function Copy-Corlib{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath,
        
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$LibPath
    )
    Write-Host "Copy unstripped_corlib to $DistPath"

    $rel = $DistPath.CreateSubdirectory('unstripped_corlib')
    Copy-Item -Path "$LibPath\*" -Filter '*.dll' -Destination "$rel" -Force
}

function Make-Archive{
    param(
        [Parameter(Mandatory)]
        [System.IO.DirectoryInfo]$DistPath
    )

    $rel = $DistPath.Parent.FullName
    $zip = $DistPath.Name + ".zip"
    
    Write-Host "Creating archive $zip for $DistPath"

    Compress-Archive -Path "$DistPath\*" -DestinationPath "$rel\$zip" -Force
}

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$ValheimPath\unstripped_corlib",
 "$(Get-Location)\resources",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"

    $valheim = New-Item -ItemType Directory -Path "$ValheimPath" -Force
    Create-BepInEx -DistPath $valheim -DistSystem 'Local'
}

if ($Target.Equals("Release")) {
    $rel = New-Item -ItemType Directory -Path "$(Get-Location)\release" -Force
    $lib = Get-Item -Path "$ValheimPath\unstripped_corlib"

    Write-Host "Building release packages to $rel"
    
    # create all distros as folders and zip
    ('Windows','Unix') | % {
        $dist = New-Item -ItemType Directory -Path "$rel\$_" -Force;
        Create-BepInEx -DistPath $dist -DistSystem $_
        Copy-Corlib -DistPath $dist -LibPath $lib
        Make-Archive -DistPath $dist
        $dist.Delete($true);
    }
}

# Pop Location
Pop-Location