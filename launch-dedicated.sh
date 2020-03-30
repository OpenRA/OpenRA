#!/bin/sh
# example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

# Usage:
#  $ ./launch-dedicated.sh # Launch a dedicated server with default settings
#  $ Mod="d2k" ./launch-dedicated.sh # Launch a dedicated server with default settings but override the Mod
#  Read the file to see which settings you can override

Name="${Name:-"Dedicated Server"}"
Mod="${Mod:-"ra"}"
ListenPort="${ListenPort:-"1234"}"
AdvertiseOnline="${AdvertiseOnline:-"True"}"
Password="${Password:-""}"

RequireAuthentication="${RequireAuthentication:-"False"}"
ProfileIDBlacklist="${ProfileIDBlacklist:-""}"
ProfileIDWhitelist="${ProfileIDWhitelist:-""}"

EnableSingleplayer="${EnableSingleplayer:-"False"}"
EnableSyncReports="${EnableSyncReports:-"False"}"

while true; do
<<<<<<< HEAD
     mono --debug OpenRA.Server.exe Game.Mod=$Mod \
     Server.Name="$Name" Server.ListenPort=$ListenPort \
     Server.AdvertiseOnline=$AdvertiseOnline \
     Server.EnableSingleplayer=$EnableSingleplayer Server.Password=$Password
=======
     mono --debug OpenRA.Server.exe Game.Mod="$Mod" \
     Server.Name="$Name" \
     Server.ListenPort="$ListenPort" \
     Server.AdvertiseOnline="$AdvertiseOnline" \
     Server.EnableSingleplayer="$EnableSingleplayer" \
     Server.Password="$Password" \
     Server.RequireAuthentication="$RequireAuthentication" \
     Server.ProfileIDBlacklist="$ProfileIDBlacklist" \
     Server.ProfileIDWhitelist="$ProfileIDWhitelist" \
     Server.EnableSyncReports="$EnableSyncReports"
>>>>>>> e82c30fbabc008a988936025f3250729d9a22b4c
done
