!include "MUI2.nsh"
!include "ZipDLL.nsh"
!include "x64.nsh"

Name "OpenRA"
OutFile "OpenRA.exe"

InstallDir $PROGRAMFILES\OpenRA
SetCompressor lzma

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\COPYING"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
;!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;***************************
;Section Definitions
;***************************
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
		
		SetOutPath "$OUTDIR\packages"
		NSISdl::download http://open-ra.org/packages/ra-packages.zip ra-packages.zip
		ZipDLL::extractall "ra-packages.zip" "$OUTDIR"
		Delete ra-packages.zip
	SectionEnd
	Section "Command & Conquer" CNC
		SetOutPath "$INSTDIR\mods\cnc"
		File /r "..\..\mods\cnc\*.*"
		
		SetOutPath "$OUTDIR\packages"
		NSISdl::download http://open-ra.org/packages/cnc-packages.zip cnc-packages.zip
		ZipDLL::extractall "cnc-packages.zip" "$OUTDIR"
		Delete cnc-packages.zip
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

;***************************
;Dependency Sections
;***************************
Section "-OpenAl" OpenAl
	IfFileExists $SYSDIR\OpenAL32.dll done installal
	installal:
		SetOutPath "$TEMP"
		NSISdl::download http://connect.creativelabs.com/openal/Downloads/oalinst.zip oalinst.zip
		!insertmacro ZIPDLL_EXTRACT oalinst.zip OpenAL oalinst.exe
		ExecWait "$TEMP\OpenAL\oalinst.exe"
	done:
SectionEnd

Section "-Sdl" SDL
	IfFileExists $SYSDIR\SDL.dll done installsdl
	installsdl:
		SetOutPath "$TEMP"
		NSISdl::download http://www.libsdl.org/release/SDL-1.2.14-win32.zip sdl.zip
		!insertmacro ZIPDLL_EXTRACT sdl.zip $SYSDIR SDL.dll
	done:
SectionEnd

Section "-Freetype" Freetype
	IfFileExists $SYSDIR\freetype6.dll done installft
	installft:
		SetOutPath "$TEMP"
		NSISdl::download http://downloads.sourceforge.net/project/gnuwin32/freetype/2.3.5-1/freetype-2.3.5-1-bin.zip freetype.zip
		!insertmacro ZIPDLL_EXTRACT freetype.zip $OUTDIR bin\freetype6.dll
		;Epic hack to get around bug in CopyFiles
		${If} ${RunningX64}
			CopyFiles "$OUTDIR\bin\freetype6.dll" $WINDIR\SysWOW64
			Goto done
		${EndIf}
		CopyFiles "$OUTDIR\bin\freetype6.dll" $SYSDIR
	done:
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_Client ${LANG_ENGLISH} "OpenRA client and dependencies"
LangString DESC_Server ${LANG_ENGLISH} "OpenRA server"
LangString DESC_RA ${LANG_ENGLISH} "Base Red Alert mod"
LangString DESC_CNC ${LANG_ENGLISH} "Base Command and Conquer mod"
LangString DESC_Aftermath ${LANG_ENGLISH} "Red Alert: Aftermath expansion mod (depends on base Red Alert mod)"
LangString DESC_RA_NG ${LANG_ENGLISH} "Next-gen Red Alert mod (depends on base Red Alert mod)"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Client} $(DESC_Client)
	!insertmacro MUI_DESCRIPTION_TEXT ${Server} $(DESC_Server)
	!insertmacro MUI_DESCRIPTION_TEXT ${RA} $(DESC_RA)
	!insertmacro MUI_DESCRIPTION_TEXT ${CNC} $(DESC_CNC)
	!insertmacro MUI_DESCRIPTION_TEXT ${Aftermath} $(DESC_Aftermath)
	!insertmacro MUI_DESCRIPTION_TEXT ${RA_NG} $(DESC_RA_NG)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Functions
;***************************
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
