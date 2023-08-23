#!/bin/bash

DLL=JotunnModStub/bin/Release/JotunnModStub.dll
PLUGINS=JotunnModStub/Package/plugins
README=README.md
#TRANSLATIONS=Translations

VERSION=$1

# Check that source files exist and are readable
if [ ! -f "$DLL" ]; then
    echo "Error: $DLL does not exist or is not readable."
    exit 1
fi


# Check that target directory exists and is writable
if [ ! -d "$PLUGINS" ]; then
    echo "Error: $PLUGINS directory does not exist."
    exit 1
fi

if [ ! -w "$PLUGINS" ]; then
    echo "Error: $PLUGINS directory is not writable."
    exit 1
fi

if [ ! -f "$README" ]; then
    echo "Error: $README does not exist or is not readable."
    exit 1
fi

cp -f "$DLL" "$PLUGINS" || { echo "Error: Failed to copy $DLL"; exit 1; }
cp -f "$README" "$PLUGINS/../README.md" || { echo "Error: Failed to copy $README"; exit 1; }
#cp -rf "$TRANSLATIONS" "$PLUGINS/"  || { echo "Error: Failed to copy Translations"; exit 1; }

ZIPDESTINATION="../bin/Release/JotunnModStub.$VERSION.zip"

cd "$PLUGINS/.."
if [ ! -z "$VERSION" ]; then
    VERSION=".$VERSION"
fi
zip -r "$ZIPDESTINATION" .
