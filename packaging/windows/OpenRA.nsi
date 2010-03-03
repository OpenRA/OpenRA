!include "MUI2.nsh"

Name "OpenRA"
OutFile "OpenRA.exe"

InstallDir $PROGRAMFILES\OpenRA
SetCompressor lzma

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\COPYING"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Client" Client
	SetOutPath "$INSTDIR"
	File "..\..\OpenRA.Game\bin\Debug\OpenRA.Game.exe"
	File "..\..\OpenRA.Game\bin\Debug\OpenRA.FileFormats.dll"
	File "..\..\OpenRA.Gl.dll"
	File "..\..\COPYING"
	File "..\..\HACKING"
	File "..\..\INSTALL"
	File "..\..\settings-netplay-cnc.ini"
	File "..\..\settings-netplay-ra.ini"
	File "..\..\FreeSans.ttf"
	File "..\..\FreeSansBold.ttf"
	
	File /r "..\..\thirdparty\Tao\*.dll"
	
	SetOutPath "$INSTDIR\shaders"
	File /r "..\..\shaders\*.fx"
SectionEnd

SectionGroup /e "Mods"
	Section "Red Alert" RA
		SetOutPath "$INSTDIR\mods\ra"
		File /r "..\..\mods\ra\*.*"
	SectionEnd
	Section "Command & Conquer" CNC
		SetOutPath "$INSTDIR\mods\cnc"
		File /r "..\..\mods\cnc\*.*"
	SectionEnd
SectionGroupEnd

Section "Server" Server
	SetOutPath "$INSTDIR"
	File "..\..\OpenRA.Server\bin\Debug\OpenRA.Server.exe"
SectionEnd

Function .onInit
	IntOp $0 ${SF_SELECTED} | ${SF_RO}
	SectionSetFlags ${Client} $0
FunctionEnd
