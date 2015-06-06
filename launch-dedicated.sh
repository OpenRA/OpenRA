#!/bin/sh
# example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details
Name="${Name:-"Dedicated Server"}"
Mod="${Mod:-"ra"}"
Dedicated="True"
DedicatedLoop="True"
ListenPort="${ListenPort:-"1234"}"
ExternalPort="${ExternalPort:-"1234"}"
AdvertiseOnline="${AdvertiseOnline:-"False"}"

while true; do
     mono --debug OpenRA.Game.exe Game.Mod=$Mod Server.Dedicated=$Dedicated Server.DedicatedLoop=$DedicatedLoop \
     Server.Name="$Name" Server.ListenPort=$ListenPort Server.ExternalPort=$ExternalPort \
     Server.AdvertiseOnline=$AdvertiseOnline
done
