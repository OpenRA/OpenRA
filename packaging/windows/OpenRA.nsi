; Copyright 2007-2015 OpenRA developers (see AUTHORS)
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
!include "FileFunc.nsh"
!include "WordFunc.nsh"

Name "OpenRA"
OutFile "OpenRA.Setup.exe"

InstallDir $PROGRAMFILES\OpenRA
InstallDirRegKey HKLM "Software\OpenRA" "InstallDir"

SetCompressor lzma

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SRCDIR}\COPYING"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKLM"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\OpenRA"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "OpenRA"

Var StartMenuFolder
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;***************************
;Section Definitions
;***************************
Section "-Reg" Reg

	; Installation directory
	WriteRegStr HKLM "Software\OpenRA" "InstallDir" $INSTDIR
	
	; Replay file association
	WriteRegStr HKLM "Software\Classes\.orarep" "" "OpenRA_replay"
	WriteRegStr HKLM "Software\Classes\OpenRA_replay\DefaultIcon" "" "$INSTDIR\OpenRA.ico,0"
	WriteRegStr HKLM "Software\Classes\OpenRA_replay\Shell\Open\Command" "" "$INSTDIR\OpenRA.exe Launch.Replay=$\"%1$\""
	
	; OpenRA URL Scheme
	WriteRegStr HKLM "Software\Classes\openra" "" "URL:OpenRA scheme"
	WriteRegStr HKLM "Software\Classes\openra" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra\DefaultIcon" "" "$INSTDIR\OpenRA.ico,0"
	WriteRegStr HKLM "Software\Classes\openra\Shell\Open\Command" "" "$INSTDIR\OpenRA.exe Launch.URI=%1"
	
SectionEnd

Section "Game" GAME
	SectionIn RO

	RMDir /r "$INSTDIR\mods"
	SetOutPath "$INSTDIR\mods"
	File /r "${SRCDIR}\mods\common"
	File /r "${SRCDIR}\mods\cnc"
	File /r "${SRCDIR}\mods\d2k"
	File /r "${SRCDIR}\mods\ra"
	File /r "${SRCDIR}\mods\modchooser"

	SetOutPath "$INSTDIR"
	File "${SRCDIR}\OpenRA.exe"
	File "${SRCDIR}\OpenRA.Game.exe"
	File "${SRCDIR}\OpenRA.Game.exe.config"
	File "${SRCDIR}\OpenRA.Utility.exe"
	File "${SRCDIR}\OpenRA.Server.exe"
	File "${SRCDIR}\OpenRA.Platforms.Default.dll"
	File "${SRCDIR}\ICSharpCode.SharpZipLib.dll"
	File "${SRCDIR}\FuzzyLogicLibrary.dll"
	File "${SRCDIR}\Open.Nat.dll"
	File "${SRCDIR}\AUTHORS"
	File "${SRCDIR}\COPYING"
	File "${SRCDIR}\README.html"
	File "${SRCDIR}\CHANGELOG.html"
	File "${SRCDIR}\CONTRIBUTING.html"
	File "${SRCDIR}\DOCUMENTATION.html"
	File "${SRCDIR}\OpenRA.ico"
	File "${SRCDIR}\SharpFont.dll"
	File "${SRCDIR}\SDL2-CS.dll"
	File "${SRCDIR}\OpenAL-CS.dll"
	File "${SRCDIR}\global mix database.dat"
	File "${SRCDIR}\MaxMind.Db.dll"
	File "${SRCDIR}\MaxMind.GeoIP2.dll"
	File "${SRCDIR}\Newtonsoft.Json.dll"
	File "${SRCDIR}\GeoLite2-Country.mmdb.gz"
	File "${SRCDIR}\eluant.dll"
	File "${SRCDIR}\SmarIrc4net.dll"
	File "${DEPSDIR}\soft_oal.dll"
	File "${DEPSDIR}\SDL2.dll"
	File "${DEPSDIR}\freetype6.dll"
	File "${DEPSDIR}\lua51.dll"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\OpenRA.lnk" $OUTDIR\OpenRA.exe "" \
			"$OUTDIR\OpenRA.exe" "" "" "" ""
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\README.lnk" $OUTDIR\README.html "" \
			"$OUTDIR\README.html" "" "" "" ""
	!insertmacro MUI_STARTMENU_WRITE_END

	SetOutPath "$INSTDIR\lua"
	File "${SRCDIR}\lua\*.lua"

	SetOutPath "$INSTDIR\glsl"
	File "${SRCDIR}\glsl\*.frag"
	File "${SRCDIR}\glsl\*.vert"
SectionEnd

Section "Desktop Shortcut" DESKTOPSHORTCUT
	SetOutPath "$INSTDIR"
	CreateShortCut "$DESKTOP\OpenRA.lnk" $INSTDIR\OpenRA.exe "" \
		"$INSTDIR\OpenRA.exe" "" "" "" ""
SectionEnd

;***************************
;Dependency Sections
;***************************
Section "-DotNet" DotNet
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" "Install"
	IfErrors error 0
	IntCmp $0 1 0 error 0
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Install"
	IfErrors error 0
	IntCmp $0 1 done error done
	error:
		MessageBox MB_YESNO ".NET Framework v4.5 or later is required to run OpenRA. $\n \
		Do you wish for the installer to launch your web browser in order to download and install it?" \
		IDYES download IDNO error2
	download:
		ExecShell "open" "http://www.microsoft.com/en-us/download/details.aspx?id=30653"
		Goto done
	error2:
		MessageBox MB_OK "Installation will continue, but be aware that OpenRA will not run unless .NET v4.5 \
		or later is installed."
	done:
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
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "Publisher" "OpenRA developers"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "URLInfoAbout" "http://openra.net"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoModify" "1"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "NoRepair" "1"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk" "$INSTDIR\uninstaller.exe" "" \
			"" "" "" "" "Uninstall OpenRA"
	!insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

!macro Clean UN
Function ${UN}Clean
	RMDir /r $INSTDIR\mods
	RMDir /r $INSTDIR\maps
	RMDir /r $INSTDIR\glsl
	RMDir /r $INSTDIR\lua
	Delete $INSTDIR\OpenRA.exe
	Delete $INSTDIR\OpenRA.Game.exe
	Delete $INSTDIR\OpenRA.Game.exe.config
	Delete $INSTDIR\OpenRA.Utility.exe
	Delete $INSTDIR\OpenRA.Server.exe
	Delete $INSTDIR\OpenRA.Platforms.Default.dll
	Delete $INSTDIR\ICSharpCode.SharpZipLib.dll
	Delete $INSTDIR\FuzzyLogicLibrary.dll
	Delete $INSTDIR\Open.Nat.dll
	Delete $INSTDIR\SharpFont.dll
	Delete $INSTDIR\AUTHORS
	Delete $INSTDIR\COPYING
	Delete $INSTDIR\README.html
	Delete $INSTDIR\CHANGELOG.html
	Delete $INSTDIR\CONTRIBUTING.html
	Delete $INSTDIR\DOCUMENTATION.html
	Delete $INSTDIR\OpenRA.ico
	Delete "$INSTDIR\global mix database.dat"
	Delete $INSTDIR\MaxMind.Db.dll
	Delete $INSTDIR\MaxMind.GeoIP2.dll
	Delete $INSTDIR\Newtonsoft.Json.dll
	Delete $INSTDIR\GeoLite2-Country.mmdb.gz
	Delete $INSTDIR\KopiLua.dll
	Delete $INSTDIR\soft_oal.dll
	Delete $INSTDIR\SDL2.dll
	Delete $INSTDIR\lua51.dll
	Delete $INSTDIR\eluant.dll
	Delete $INSTDIR\freetype6.dll
	Delete $INSTDIR\SDL2-CS.dll
	Delete $INSTDIR\OpenAL-CS.dll
	Delete $INSTDIR\SmarIrc4net.dll
	RMDir /r $INSTDIR\Support
	
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA"
	DeleteRegKey HKLM "Software\Classes\.orarep"
	DeleteRegKey HKLM "Software\Classes\OpenRA_replay"
	DeleteRegKey HKLM "Software\Classes\openra"
	
	Delete $INSTDIR\uninstaller.exe
	RMDir $INSTDIR
	
	!insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
	RMDir /r "$SMPROGRAMS\$StartMenuFolder"
	Delete $DESKTOP\OpenRA.lnk
	DeleteRegKey HKLM "Software\OpenRA"
FunctionEnd
!macroend

!insertmacro Clean ""
!insertmacro Clean "un."

Section "Uninstall"
	Call un.Clean
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_GAME ${LANG_ENGLISH} "OpenRA engine, official mods and dependencies"
LangString DESC_DESKTOPSHORTCUT ${LANG_ENGLISH} "Place shortcut on the Desktop."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${GAME} $(DESC_GAME)
	!insertmacro MUI_DESCRIPTION_TEXT ${DESKTOPSHORTCUT} $(DESC_DESKTOPSHORTCUT)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Callbacks
;***************************

Function .onInstFailed
	Call Clean
FunctionEnd
