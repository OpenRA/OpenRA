#!/bin/sh
macpack -mode:console -n "OpenRa Server" -r OpenRa.FileFormats.dll,packaging/osx/OpenRa.icns OpenRa.Server.exe
cp packaging/osx/Info_server.plist "OpenRa Server.app/Contents/Info.plist"
macpack -mode:winforms -n OpenRA -r OpenRa.FileFormats.dll,OpenRa.Gl.dll,libglfw.dylib,allies.mix,conquer.mix,expand2.mix,general.mix,hires.mix,interior.mix,redalert.mix,russian.mix,snow.mix,sounds.mix,temperat.mix,packaging/osx/settings.ini,line.fx,chrome-shp.fx,chrome-rgba.fx,bogus.sno,bogus.tem,world-shp.fx,tileSet.til,templates.ini,packaging/osx/OpenRa.icns,mods OpenRa.Game.exe
cp packaging/osx/Info_game.plist "OpenRa.app/Contents/Info.plist"
