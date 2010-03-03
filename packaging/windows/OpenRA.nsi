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
	Section "Red Alert: Aftermath" Aftermath
		SetOutPath "$INSTDIR\mods\aftermath"
		File /r "..\..\mods\aftermath\*.*"
	SectionEnd
	Section "Red Alert: Next Generation" RA_NG
		SetOutPath "$INSTDIR\mods\ra-ng"
		File /r "..\..\mods\ra-ng\*.*"
	SectionEnd
SectionGroupEnd

Section "Server" Server
	SetOutPath "$INSTDIR"
	File "..\..\OpenRA.Server\bin\Debug\OpenRA.Server.exe"
SectionEnd

Var previousSelection

Function .onInit
	IntOp $0 ${SF_SELECTED} | ${SF_RO}
	SectionSetFlags ${Client} $0
	
	IntOp $previousSelection ${SF_SELECTED} + 0
FunctionEnd

Function .onSelChange
	SectionGetFlags ${RA} $0
	IntOp $1 ${SF_SELECTED} & $0
	IntCmp $1 $previousSelection done

	IntCmp 0 $1 deselected selected
	deselected:
		SectionSetFlags ${Aftermath} ${SF_RO}
		SectionSetFlags ${RA_NG} ${SF_RO}
		Goto done
	selected:
		SectionSetFlags ${Aftermath} ${SF_SELECTED}
		SectionSetFlags ${RA_NG} ${SF_SELECTED}
		Goto done

	done:
		IntOp $previousSelection $1 + 0
FunctionEnd
