#!/bin/sh
# example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated-Server for details

# Usage:
#  $ ./launch-dedicated.sh # Launch a dedicated server with default settings
#  $ Mod="d2k" ./launch-dedicated.sh # Launch a dedicated server with default settings but override the Mod
#  Read the file to see which settings you can override

set -o errexit || exit $?

ENGINEDIR=$(dirname "$0")
if command -v mono >/dev/null 2>&1 && [ "$(grep -c .NETCoreApp,Version= "${ENGINEDIR}/bin/OpenRA.Server.dll")" = "0" ]; then
	RUNTIME_LAUNCHER="mono --debug"
else
	RUNTIME_LAUNCHER="dotnet"
fi

Name="${Name:-"Dedicated Server"}"
Mod="${Mod:-"ra"}"
Map="${Map:-""}"
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
EnableLintChecks="${EnableLintChecks:-"True"}"
ShareAnonymizedIPs="${ShareAnonymizedIPs:-"True"}"

FloodLimitJoinCooldown="${FloodLimitJoinCooldown:-"5000"}"

SupportDir="${SupportDir:-""}"

while true; do
     ${RUNTIME_LAUNCHER} "${ENGINEDIR}/bin/OpenRA.Server.dll" Engine.EngineDir=".." Game.Mod="$Mod" \
     Server.Name="$Name" \
     Server.Map="$Map" \
     Server.ListenPort="$ListenPort" \
     Server.AdvertiseOnline="$AdvertiseOnline" \
     Server.EnableSingleplayer="$EnableSingleplayer" \
     Server.Password="$Password" \
     Server.RecordReplays="$RecordReplays" \
     Server.RequireAuthentication="$RequireAuthentication" \
     Server.ProfileIDBlacklist="$ProfileIDBlacklist" \
     Server.ProfileIDWhitelist="$ProfileIDWhitelist" \
     Server.EnableSyncReports="$EnableSyncReports" \
     Server.EnableGeoIP="$EnableGeoIP" \
     Server.EnableLintChecks="$EnableLintChecks" \
     Server.ShareAnonymizedIPs="$ShareAnonymizedIPs" \
     Server.FloodLimitJoinCooldown="$FloodLimitJoinCooldown" \
     Engine.SupportDir="$SupportDir" || :
done
