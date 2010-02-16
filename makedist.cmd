@rmdir /s /q ..\openra-bin
@mkdir ..\openra-bin
@if "%1"=="--nomix" goto nomix
@copy *.mix ..\openra-bin\
:nomix
@copy *.ini ..\openra-bin\
@copy *.til ..\openra-bin\
@copy INSTALL ..\openra-bin\
@copy LICENSE ..\openra-bin\
@copy *.fx ..\openra-bin\
@copy OpenRA.Server\bin\debug\OpenRA.Server.exe ..\openra-bin\
@copy SequenceEditor\bin\debug\SequenceEditor.exe ..\openra-bin\
@copy OpenRA.Game\bin\debug\*.dll ..\openra-bin\
@copy OpenRA.Game\bin\debug\OpenRa.Game.exe ..\openra-bin\
@xcopy /E mods ..\openra-bin\mods\
@copy bogus.* ..\openra-bin\