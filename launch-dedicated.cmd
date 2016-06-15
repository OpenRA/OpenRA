:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set Mod=ra
set ListenPort=1234
set ExternalPort=1234
set AdvertiseOnline=True
set AllowPortForward=False
set EnableSingleplayer=False
set Password=""

:loop

OpenRA.Server.exe Game.Mod=%Mod% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.ExternalPort=%ExternalPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.AllowPortForward=%AllowPortForward% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password%

goto loop