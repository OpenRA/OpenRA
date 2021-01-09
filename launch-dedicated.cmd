:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set Mod=ra
set ListenPort=1234
set AdvertiseOnline=True
set Password=""
set RecordReplays=False

set RequireAuthentication=False
set ProfileIDBlacklist=""
set ProfileIDWhitelist=""

set EnableSingleplayer=False
set EnableSyncReports=False
set UseNewNetcode=False
set EnableGeoIP=True
set ShareAnonymizedIPs=True

set SupportDir=""

:loop

bin\OpenRA.Server.exe Engine.EngineDir=".." Game.Mod=%Mod% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password% Server.RecordReplays=%RecordReplays% Server.RequireAuthentication=%RequireAuthentication% Server.ProfileIDBlacklist=%ProfileIDBlacklist% Server.ProfileIDWhitelist=%ProfileIDWhitelist% Server.EnableSyncReports=%EnableSyncReports% Server.UseNewNetcode=%UseNewNetcode% Server.EnableGeoIP=%EnableGeoIP% Server.ShareAnonymizedIPs=%ShareAnonymizedIPs% Engine.SupportDir=%SupportDir%

goto loop
