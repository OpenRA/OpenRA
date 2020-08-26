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
RecordReplays="${RecordReplays:-"False"}"

RequireAuthentication="${RequireAuthentication:-"False"}"
ProfileIDBlacklist="${ProfileIDBlacklist:-""}"
ProfileIDWhitelist="${ProfileIDWhitelist:-""}"

EnableSingleplayer="${EnableSingleplayer:-"False"}"
EnableSyncReports="${EnableSyncReports:-"False"}"
EnableGeoIP="${EnableGeoIP:-"True"}"
ShareAnonymizedIPs="${ShareAnonymizedIPs:-"True"}"

SupportDir="${SupportDir:-""}"

while true; do
     mono --debug bin/OpenRA.Server.dll Engine.EngineDir=".." Game.Mod="$Mod" \
     Server.Name="$Name" \
     Server.ListenPort="$ListenPort" \
     Server.AdvertiseOnline="$AdvertiseOnline" \
     Server.EnableSingleplayer="$EnableSingleplayer" \
     Server.Password="$Password" \
     Server.RecordReplays="$RecordReplays" \
     Server.GeoIPDatabase="$GeoIPDatabase" \
     Server.RequireAuthentication="$RequireAuthentication" \
     Server.ProfileIDBlacklist="$ProfileIDBlacklist" \
     Server.ProfileIDWhitelist="$ProfileIDWhitelist" \
     Server.EnableSyncReports="$EnableSyncReports" \
     Server.EnableGeoIP="$EnableGeoIP" \
     Server.ShareAnonymizedIPs="$ShareAnonymizedIPs" \
     Engine.SupportDir="$SupportDir"
done
