#!/bin/sh
# example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

# Usage:
#  $ ./launch-dedicated-appimage.sh # Launch a dedicated server with default settings
#  $ Mod="d2k" ./launch-dedicated-appimage.sh # Launch a dedicated server with default settings but override the Mod
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

if [ "$Mod" = "d2k" ]; then
    App="OpenRA-Dune-2000-x86_64.AppImage"
elif [ "$Mod" = "cnc" ]; then
    App="OpenRA-Tiberian-Dawn-x86_64.AppImage"
elif [ "$Mod" = "ra" ]; then
    App="OpenRA-Red-Alert-x86_64.AppImage"
else
    printf "Invalid Mod value %s provided. Allowed values are ra, cnc and d2k." $Mod
    exit 1
fi

if [ ! -f "$App" ]; then
    printf "Could not find AppImage %s for Mod %s in the current folder. Has it been downloaded?" $App $Mod
    exit 1
fi

eval "./$App" --server \
Server.Name="$Name" \
Server.ListenPort="$ListenPort" \
Server.AdvertiseOnline="$AdvertiseOnline" \
Server.EnableSingleplayer="$EnableSingleplayer" \
Server.Password="$Password" \
Server.RequireAuthentication="$RequireAuthentication" \
Server.ProfileIDBlacklist="$ProfileIDBlacklist" \
Server.ProfileIDWhitelist="$ProfileIDWhitelist" \
Server.EnableSyncReports="$EnableSyncReports"
