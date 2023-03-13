:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated-Server for details

@echo on

set Name="Dedicated Server"
set Mod=ra
set Map=""
set ListenPort=1234
set AdvertiseOnline=True
set Password=""
set RecordReplays=False

set RequireAuthentication=False
set ProfileIDBlacklist=""
set ProfileIDWhitelist=""

set EnableSingleplayer=False
set EnableSyncReports=False
set EnableGeoIP=True
set EnableLintChecks=True
set ShareAnonymizedIPs=True

set FloodLimitJoinCooldown=5000

set SupportDir=""

:loop

bin\OpenRA.Server.exe Engine.EngineDir=".." Game.Mod=%Mod% Server.Name=%Name% Server.Map=%Map% Server.ListenPort=%ListenPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password% Server.RecordReplays=%RecordReplays% Server.RequireAuthentication=%RequireAuthentication% Server.ProfileIDBlacklist=%ProfileIDBlacklist% Server.ProfileIDWhitelist=%ProfileIDWhitelist% Server.EnableSyncReports=%EnableSyncReports% Server.EnableGeoIP=%EnableGeoIP% Server.EnableLintChecks=%EnableLintChecks% Server.ShareAnonymizedIPs=%ShareAnonymizedIPs% Server.FloodLimitJoinCooldown=%FloodLimitJoinCooldown% Engine.SupportDir=%SupportDir%

goto loop
