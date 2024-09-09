param([string]$cmdarg = "")

#Contibuted by Kpro#3271
#uncomment to enable logging
#Start-Transcript -OutputDirectory "C:\transcripts\"

Write-Host ""
Write-Host ""
Write-Host "WELCOME TO JOTUNNMODSTUB RENAMING UTILITY"
Write-Host "-----------------------------------------"
Write-Host ""
Write-Host "This script will do the following:"
Write-Host ""
Write-Host ""
Write-Host "1. Change the file names, folder names, and project references from the JotunnModStub to your custom solution name."
Write-Host ""
Write-Host "2. Change the DoPrebuild.props file."
Write-Host ""
Write-Host "3. Create the Environment.props file."


$modstubpath = Get-Location
$modstubfoldername = Split-Path -Path $modstubpath -leaf
Write-Host ""

if ($cmdarg -eq "-nocopy") {
	$Name = $modstubfoldername
} else {
	#Check for rename and copy use-case
	Write-Host "Step 1. Choose one of the following options:"
	Write-Host ""
	Write-Host "1. Create a solution in this folder named '$modstubfoldername'"
	Write-Host ""
	Write-Host "2. Copy this folder to a new folder with a solution name I will choose"
	Write-Host ""
	$yn = Read-Host "Select (1/2)?"
	if ($yn -eq "1") 
	{
		$Name = $modstubfoldername
	}
	else
	{
		Write-Host ""
		Write-Host "Got it. A copy of this folder will be created with a new folder and solution name that you choose"
		$Name = Read-Host "Enter a new name for the solution."
		Write-Host ""

		#Copy modstub folder
		Copy-Item ..\$modstubfoldername -Destination "..\$Name" -Recurse 
		Set-Location ..\$Name
		Write-Host "Current location set to: (Get-Location)"
	}
}

#Rename folders and files

if ($Name -ne "")
{

Write-Host "     . . . Renaming files and folders to '$Name' . . ."
Move-Item -Path ..\$Name\JotunnModStub -Destination ..\$Name\$Name
$unity = $Name + "Unity"
Move-Item -Path ..\$Name\JotunnModUnity -Destination ..\$Name\$unity
Get-ChildItem -Path ..\$Name\ -File -Recurse | % -Process{if($_.Name -ne "JotunnModStub.zip") {Rename-Item -Path $_.PSPath -NewName $_.Name.replace("JotunnModStub", $Name)}}
}
else{
Write-Host "Error: empty solution name"
Read-Host "Enter to exit"
Exit 0
}

#Rename internal references
$msg = "     . . . Replacing internal references to 'JotunnModStub' with " + '($Name)' + " . . ."
Write-Host $msg
((Get-Content -path ..\$Name\$Name.sln -Raw) -replace 'JotunnModStub',$Name) | Set-Content -Path ..\$Name\$Name.sln
((Get-Content -path ..\$Name\$Name\$Name.cs -Raw) -replace 'JotunnModStub',$Name) | Set-Content -Path ..\$Name\$Name\$Name.cs 
$landed = $Name + " has landed"
((Get-Content -path ..\$Name\$Name\$Name.cs -Raw) -replace 'ModStub has landed',$landed) | Set-Content -Path ..\$Name\$Name\$Name.cs 
((Get-Content -path ..\$Name\$Name\$Name.csproj -Raw) -replace 'JotunnModStub',$Name) | Set-Content -Path ..\$Name\$Name\$Name.csproj
((Get-Content -path ..\$Name\$Name\$Name.csproj -Raw) -replace 'JotunnModUnity',$unity) | Set-Content -Path ..\$Name\$Name\$Name.csproj
((Get-Content -path ..\$Name\$Name\Properties\AssemblyInfo.cs -Raw) -replace 'JotunnModStub',$Name) | Set-Content -Path ..\$Name\$Name\Properties\AssemblyInfo.cs



#setting DoPrebuild.props to true
Write-Host ""
Write-Host "Step 2. . . . setting DoPrebuid.props <ExecutePrebuild> to true..."
((Get-Content -path ..\$Name\DoPreBuild.props -Raw) -replace 'False','True') | Set-Content -Path ..\$Name\DoPreBuild.props

Write-Host ""


#Test whether Environment.props exists in parent directory
if (Test-Path -Path ..\Environment.props) {	
	Write-Host ""
    $yn = Read-Host "Step 3. . . . An Environment.Props file is dectected in the parent directory. Copy this file to solution directory (y/n)?"
	 if ($yn -eq "y") {
		Copy-Item "..\Environment.props" -Destination ".\"
	 } 	 else {
		Write-Host ""
		Write-Host "WARNING"
		Write-Host "-------"
		Write-Host ""	
		Write-Host "You must create an environment.props file inside the solution directory PRIOR to building the solution."
		Write-Host "Instructions for creating this file can be found at"
		Write-Host "https://github.com/Valheim-Modding/JotunnModStub"
		Write-Host ""
		Write-Host ""
		Read-Host "Hit Enter to Exit"
		Exit 1
	 }
} else {
		Write-Host ""
		Write-Host "Step 3 . . . . You must create an environment.props file inside the solution directory PRIOR to building the solution."
		Write-Host ""
$yn3 = Read-Host " -- Would you like to create an environment.props file now (y/n)"
if ($yn3 -eq "y")
{
	#Initially assume typical install location
	$TypicalInstallFolder = "c:\Program Files (x86)\Steam\steamapps\common\Valheim"
	
	#Test for existence of typical install	
	if (Test-Path -Path $TypicalInstallFolder) {
		$ValheimFolder = $TypicalInstallFolder
	} else {
		$ValheimFolder = "$env:SystemDrive"
	}
	

	#Ask user to verify install folder
	
	function Get-InstallFolder($initialDirectory="") {   
		[void] [System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms')
		
		$OpenFolderDialog = New-Object System.Windows.Forms.FolderBrowserDialog
		$OpenFolderDialog.SelectedPath = $initialDirectory
		$OpenFolderDialog.Description = "Select Valheim Install Folder"
		$OpenFolderDialog.rootfolder = "MyComputer"
		[void] $OpenFolderDialog.ShowDialog()
		return $OpenFolderDialog.SelectedPath
	}
	
	Write-Host ""
	Write-Host " -- Hit ENTER to select your Valheim install folder"
	
	$verifiedinstallfolder = Get-InstallFolder($ValheimFolder)
	
	#Build environment.props
	
	New-Item .\environment.props
	Add-Content -Path .\environment.props -Value '<?xml version="1.0" encoding="utf-8"?>'
	Add-Content -Path .\environment.props -Value '<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
	Add-Content -Path .\environment.props -Value '  <PropertyGroup>'
 	Add-Content -Path .\environment.props -Value '   <!-- Needs to be your path to the base Valheim folder -->'
	
	#Build Install Path
	$installpath = "    <VALHEIM_INSTALL>$verifiedinstallfolder</VALHEIM_INSTALL>"
	
	Add-Content -Path .\environment.props -Value $installpath
	Add-Content -Path .\environment.props -Value '    <!-- This is the folder where your build gets copied to when using the post-build automations -->'
	Add-Content -Path .\environment.props -Value '    <MOD_DEPLOYPATH>$(VALHEIM_INSTALL)\BepInEx\plugins</MOD_DEPLOYPATH>'
	Add-Content -Path .\environment.props -Value '  </PropertyGroup>'
	Add-Content -Path .\environment.props -Value '</Project>'
	
	#Copy environment.props to parent folder for reuse
	Copy-Item ".\environment.props" -Destination "..\"
	
	#check for existence of BepInEx folder
	if (Test-Path -Path $verifiedinstallfolder\BepInEx) {
		
	} else {
		Write-Host "WARNING: BepInEx is not installed."
	}
}

}

#Exit

Write-Host ""
Write-Host ""
Write-Host "Success"
Write-Host "-------"
Write-Host ""
Write-Host "The process is complete."
Write-Host "Note that, as stated in the wiki, the compiler will generate reference errors the first time you build the solution."
Write-Host "This is normal. Close VS2019/2022. Reopen the solution. Build. The errors should be resolved."
Write-Host ""
Write-Host ""
Read-Host "Hit Enter to Exit"
Write-Host ""
Write-Host ""
Exit 0
