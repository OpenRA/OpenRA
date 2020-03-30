#!/bin/bash
# Script to create DS_Store file for the macOS dmg package
# Requires macOS host

mkdir "dmgsrc"
mkdir "dmgsrc/OpenRA - Tiberian Dawn.app"
mkdir "dmgsrc/OpenRA - Red Alert.app"
mkdir "dmgsrc/OpenRA - Dune 2000.app"
cp background.tiff "dmgsrc/.background.tiff"

hdiutil create /tmp/OpenRA.dmg -format UDRW -volname "OpenRA" -fs HFS+ -srcfolder dmgsrc
DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "/tmp/OpenRA.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
sleep 2
ls -lah /Volumes/OpenRA
echo '
   tell application "Finder"
     tell disk "'OpenRA'"
           open
           set current view of container window to icon view
           set toolbar visible of container window to false
           set statusbar visible of container window to false
           set the bounds of container window to {400, 100, 1040, 580}
           set theViewOptions to the icon view options of container window
           set arrangement of theViewOptions to not arranged
           set icon size of theViewOptions to 72
           set background picture of theViewOptions to file ".background.tiff"
           make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
           set position of item "'OpenRA - Tiberian Dawn.app'" of container window to {160, 106}
           set position of item "'OpenRA - Red Alert.app'" of container window to {320, 106}
           set position of item "'OpenRA - Dune 2000.app'" of container window to {480, 106}
           set position of item "Applications" of container window to {320, 298}
           set position of item ".background.tiff" of container window to {160, 298}
           set position of item ".fseventsd" of container window to {160, 298}
           update without registering applications
           delay 5
           close
     end tell
   end tell
' | osascript

cp "/Volumes/OpenRA/.DS_Store" DS_Store

hdiutil detach ${DMG_DEVICE}
rm /tmp/OpenRA.dmg
rm -rf dmgsrc