@rmdir /s /q ..\openra-bin
@mkdir ..\openra-bin
@if "%1"=="--nomix" goto nomix
@copy *.mix ..\openra-bin\
:nomix
@copy *.ini ..\openra-bin\
@copy *.rem ..\openra-bin\
@copy *.til ..\openra-bin\
@copy INSTALL ..\openra-bin\
@copy LICENSE ..\openra-bin\
@copy *.fx ..\openra-bin\
@copy *.xml ..\openra-bin\
@copy specialbin.png ..\openra-bin\
@copy OpenRA.Server\bin\debug\OpenRA.Server.exe ..\openra-bin\
@copy SequenceEditor\bin\x86\debug\SequenceEditor.exe ..\openra-bin\
@copy OpenRA.Game\bin\x86\debug\*.dll ..\openra-bin\
@copy OpenRA.Game\bin\x86\debug\OpenRa.Game.exe ..\openra-bin\
