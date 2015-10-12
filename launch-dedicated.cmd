@echo on

set Name="Dedicated Server"
set Mod=ra
set Dedicated=True
set DedicatedLoop=True
set ListenPort=1234
set ExternalPort=1234
set AdvertiseOnline=True
set Map=
set Password=

:loop

OpenRA.Game.exe Game.Mod=%Mod% Server.Dedicated=%Dedicated% Server.DedicatedLoop=%DedicatedLoop% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.ExternalPort=%ExternalPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.Map=%Map% Server.Password=%Password% 

goto loop