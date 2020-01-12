:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set Mod=ra
set ListenPort=1234
set AdvertiseOnline=True
set Password=""

set GeoIPDatabase=""

set RequireAuthentication=False
set ProfileIDBlacklist=""
set ProfileIDWhitelist=""

set EnableSingleplayer=False
set EnableSyncReports=False
set ShareAnonymizedIPs=True

:loop

OpenRA.Server.exe Game.Mod=%Mod% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password% Server.GeoIPDatabase=%GeoIPDatabase% Server.RequireAuthentication=%RequireAuthentication% Server.ProfileIDBlacklist=%ProfileIDBlacklist% Server.ProfileIDWhitelist=%ProfileIDWhitelist% Server.EnableSyncReports=%EnableSyncReports% Server.ShareAnonymizedIPs=%ShareAnonymizedIPs%

goto loop
