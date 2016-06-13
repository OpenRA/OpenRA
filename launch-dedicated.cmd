:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set Mod=ra
set ListenPort=1234
set ExternalPort=1234
set AdvertiseOnline=True
set AllowPortForward=False
set DisableSinglePlayer=True
set Password=""

:loop

OpenRA.Server.exe Game.Mod=%Mod% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.ExternalPort=%ExternalPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.AllowPortForward=%AllowPortForward% Server.DisableSinglePlayer=%DisableSinglePlayer% Server.Password=%Password%

goto loop