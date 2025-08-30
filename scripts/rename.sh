#!/bin/bash

if [ $# -ne 2 ]; then
  echo "Usage: $0 <old string> <new string>"
  exit 1
fi

directory="."
oldstring="$1"
newstring="$2"

# Replace string in file and folder names
find "$directory" -depth  -not -path '*/.git/*' -name "*$oldstring*" -execdir bash -c 'mv -- "$1" "${1//'"$oldstring"'/'"$newstring"'}"' _ {} \;

# Replace string in file contents
find "$directory" -not -path '*/.git/*' -type f -exec sed -i "s/$oldstring/$newstring/g" {} +
