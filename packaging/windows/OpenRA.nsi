; Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
; This file is part of OpenRA.
; 
;  OpenRA is free software: you can redistribute it and/or modify
;  it under the terms of the GNU General Public License as published by
;  the Free Software Foundation, either version 3 of the License, or
;  (at your option) any later version.
; 
;  OpenRA is distributed in the hope that it will be useful,
;  but WITHOUT ANY WARRANTY; without even the implied warranty of
;  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;  GNU General Public License for more details.
; 
;  You should have received a copy of the GNU General Public License
;  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.


!include "MUI2.nsh"
!include "ZipDLL.nsh"

Name "OpenRA"
OutFile "OpenRA.exe"

InstallDir $PROGRAMFILES\OpenRA
SetCompressor lzma

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\COPYING"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\OpenRA"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "OpenRA"

Var StartMenuFolder
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
;!insertmacro MUI_PAGE_FINISH

;!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

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
	
	File "..\..\OpenRA.Game\OpenRA.ico"
	
	File "..\..\thirdparty\Tao\*.dll"
	
	File "cg.dll"
	File "cgGL.dll"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\OpenRA.lnk" $OUTDIR\OpenRA.Game.exe "" \
			"$OUTDIR\OpenRA.ico" "" "" "" "OpenRA Client"
	!insertmacro MUI_STARTMENU_WRITE_END
	
	SetOutPath "$INSTDIR\shaders"
	File "..\..\shaders\*.fx"
SectionEnd

SectionGroup /e "Mods"
	Section "Red Alert" RA
		SetOutPath "$INSTDIR\mods\ra"
		File "..\..\mods\ra\*.*"
		MessageBox MB_YESNO "Setup will now download and install the Red Alert packages.$\n\
			The size of the download will be approximately 7MB in size.$\n\
			If you do not wish to download them at this time, you can find instructions on how to \
			download the packages in the INSTALL file found in the OpenRA program directory$\n$\n\
			Continue?" IDYES download IDNO done
		download:
			AddSize 10137
			SetOutPath "$OUTDIR\packages"
			NSISdl::download http://open-ra.org/packages/ra-packages.zip ra-packages.zip
			ZipDLL::extractall "ra-packages.zip" "$OUTDIR"
			Delete ra-packages.zip
		done:
	SectionEnd
	Section "Command & Conquer" CNC
		SetOutPath "$INSTDIR\mods\cnc"
		File "..\..\mods\cnc\*.*"
		
		MessageBox MB_YESNO "Setup will now download and install the Command and Conquer packages.$\n\
			The size of the download will be approximately 6MB in size.$\n\
			If you do not wish to download them at this time, you can find instructions on how to \
			download the packages in the INSTALL file found in the OpenRA program directory$\n$\n\
			Continue?" IDYES download IDNO done
		download:
			AddSize 9431
			SetOutPath "$OUTDIR\packages"
			NSISdl::download http://open-ra.org/packages/cnc-packages.zip cnc-packages.zip
			ZipDLL::extractall "cnc-packages.zip" "$OUTDIR"
			Delete cnc-packages.zip
		done:
	SectionEnd
	Section "Red Alert: Aftermath" Aftermath
		SetOutPath "$INSTDIR\mods\aftermath"
		File "..\..\mods\aftermath\*.*"
	SectionEnd
	Section "Red Alert: Next Generation" RA_NG
		SetOutPath "$INSTDIR\mods\ra-ng"
		File "..\..\mods\ra-ng\*.*"
	SectionEnd
SectionGroupEnd

;***************************
;Dependency Sections
;***************************
Section "-OpenAl" OpenAl
	AddSize 768
	IfFileExists $SYSDIR\OpenAL32.dll done installal
	installal:
		SetOutPath "$TEMP"
		NSISdl::download http://connect.creativelabs.com/openal/Downloads/oalinst.zip oalinst.zip
		!insertmacro ZIPDLL_EXTRACT oalinst.zip OpenAL oalinst.exe
		ExecWait "$TEMP\OpenAL\oalinst.exe"
	done:
SectionEnd

Section "-Sdl" SDL
	AddSize 317
	SetOutPath "$TEMP"
	NSISdl::download http://www.libsdl.org/release/SDL-1.2.14-win32.zip sdl.zip
	!insertmacro ZIPDLL_EXTRACT sdl.zip $INSTDIR SDL.dll
SectionEnd

Section "-Freetype" Freetype
	AddSize 583
	SetOutPath "$TEMP"
	NSISdl::download http://downloads.sourceforge.net/project/gnuwin32/freetype/2.3.5-1/freetype-2.3.5-1-bin.zip freetype.zip
	!insertmacro ZIPDLL_EXTRACT freetype.zip $OUTDIR bin\freetype6.dll
	CopyFiles "$OUTDIR\bin\freetype6.dll" $INSTDIR
	NSISdl::download http://www.zlib.net/zlib123-dll.zip zlib.zip
	!insertmacro ZIPDLL_EXTRACT zlib.zip $OUTDIR zlib1.dll
	CopyFiles "$OUTDIR\zlib1.dll" $INSTDIR
SectionEnd

;***************************
;Uninstaller Sections
;***************************
Section "-Uninstaller"
	WriteUninstaller $INSTDIR\uninstaller.exe
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "DisplayName" "OpenRA"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "UninstallString" "$INSTDIR\uninstaller.exe"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "InstallLocation" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "DisplayIcon" "$INSTDIR\OpenRA.ico"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "Publisher" "IJW Software (New Zealand)"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "URLInfoAbout" "http://open-ra.org"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoModify" "1"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoRepair" "1"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk" "$INSTDIR\uninstaller.exe" "" \
			"" "" "" "" "Uninstall OpenRA"
	!insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

Section "Uninstall"
	RMDir /r $INSTDIR\mods
	RMDir /r $INSTDIR\shaders
	Delete $INSTDIR\OpenRA.Game.exe
	Delete $INSTDIR\OpenRA.FileFormats.dll
	Delete $INSTDIR\OpenRA.Gl.dll
	Delete $INSTDIR\Tao.*.dll
	Delete $INSTDIR\COPYING
	Delete $INSTDIR\HACKING
	Delete $INSTDIR\INSTALL
	Delete $INSTDIR\OpenRA.ico
	Delete $INSTDIR\*.ttf
	Delete $INSTDIR\settings-netplay-*.ini
	Delete $INSTDIR\freetype6.dll
	Delete $INSTDIR\SDL.dll
	Delete $INSTDIR\cg.dll
	Delete $INSTDIR\cgGL.dll
	Delete $INSTDIR\zlib1.dll
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA"
	Delete $INSTDIR\uninstaller.exe
	RMDir $INSTDIR
	
	!insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
	RMDir /r "$SMPROGRAMS\$StartMenuFolder"
	DeleteRegKey HKCU "Software\OpenRA"
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_Client ${LANG_ENGLISH} "OpenRA client and dependencies"
LangString DESC_RA ${LANG_ENGLISH} "Base Red Alert mod"
LangString DESC_CNC ${LANG_ENGLISH} "Base Command and Conquer mod"
LangString DESC_Aftermath ${LANG_ENGLISH} "Red Alert: Aftermath expansion mod (depends on base Red Alert mod)"
LangString DESC_RA_NG ${LANG_ENGLISH} "Next-gen Red Alert mod (depends on base Red Alert mod)"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Client} $(DESC_Client)
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
		SectionSetFlags ${Aftermath} 0
		SectionSetFlags ${RA_NG} 0
		Goto done

	done:
		IntOp $previousSelection $1 + 0
FunctionEnd
