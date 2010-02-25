source=./packaging/osx/dmgsource/
title=OpenRA
size=70m
dmgName=OpenRA.dmg

mv openra_server ${source}
mv OpenRA.app ${source}

hdiutil create -srcfolder "${source}" -volname "${title}" -fs HFS+ -fsargs "-c c=64,a=16,e=16" -format UDRW -size ${size} temp.dmg
sleep 2
device=$(hdiutil attach -readwrite -noverify -noautoopen "temp.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
echo '
   tell application "Finder"
     tell disk "'${title}'"
           open
           set current view of container window to icon view
           set toolbar visible of container window to false
           set statusbar visible of container window to false
           set the bounds of container window to {400, 100, 885, 430}
           set theViewOptions to the icon view options of container window
           set arrangement of theViewOptions to not arranged
           set icon size of theViewOptions to 72
           set background picture of theViewOptions to file ".background:bg.png"
           make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
           set position of item "OpenRA.app" of container window to {100, 90}
	       set position of item "openra_server" of container window to {100, 210}
           set position of item "Applications" of container window to {375, 150}
		   close
		   open
           update without registering applications
           delay 5
     end tell
   end tell
' | osascript
sleep 5
chmod -Rf go-w /Volumes/"${title}"
sync
hdiutil detach ${device}
hdiutil convert "./temp.dmg" -format UDZO -imagekey zlib-level=9 -o "${dmgName}"
rm -f ./temp.dmg
rm -rf ${source}OpenRA.app
rm -f ${source}openra_server