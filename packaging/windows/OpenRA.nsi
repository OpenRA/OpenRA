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
!include "FileFunc.nsh"
!include "WordFunc.nsh"

Name "OpenRA"
OutFile "OpenRA.exe"

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

;!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Var DownloadCount
!macro DownloadDependency name saveas
	IntOp $DownloadCount 0 + 1
	download:
		NSISdl::download "http://open-ra.org/get-dependency.php?file=${name}" ${saveas}
		Pop $R0
		StrCmp $R0 "success" success
		IntCmp $DownloadCount 3 failure retry
	failure:
		MessageBox MB_OK "Download of ${saveas} did not succeed. Aborting installation. $\n$\n$R0"
		Abort
	retry:
		IntOp $DownloadCount $DownloadCount + 1
		Goto download
	success:
!macroend

;***************************
;Section Definitions
;***************************
Section "-Reg" Reg
	WriteRegStr HKLM "Software\OpenRA" "InstallDir" $INSTDIR
SectionEnd

Section "Client" Client
	SetOutPath "$INSTDIR"
	File "${SRCDIR}\OpenRA.Game.exe"
	File "${SRCDIR}\OpenRA.Utility.exe"
	File "${SRCDIR}\OpenRA.FileFormats.dll"
	File "${SRCDIR}\OpenRA.Renderer.SdlCommon.dll"
	File "${SRCDIR}\OpenRA.Renderer.Gl.dll"
	File "${SRCDIR}\OpenRA.Renderer.Cg.dll"
	File "${SRCDIR}\OpenRA.Renderer.Null.dll"
	File "${SRCDIR}\ICSharpCode.SharpZipLib.dll"
	File "${SRCDIR}\FuzzyLogicLibrary.dll"
	File "${SRCDIR}\Mono.Nat.dll"
	File "${SRCDIR}\COPYING"
	File "${SRCDIR}\HACKING"
	File "${SRCDIR}\INSTALL"
	File "${SRCDIR}\*.ttf"
	File "${SRCDIR}\OpenRA.ico"
	File "${SRCDIR}\Tao.*.dll"
	File "${SRCDIR}\SharpFont.*.dll"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\OpenRA.lnk" $OUTDIR\OpenRA.Game.exe "" \
			"$OUTDIR\OpenRA.Game.exe" "" "" "" ""
	!insertmacro MUI_STARTMENU_WRITE_END

	SetOutPath "$INSTDIR\cg"
	File "${SRCDIR}\cg\*.fx"
	SetOutPath "$INSTDIR\glsl"
	File "${SRCDIR}\glsl\*.frag"
	File "${SRCDIR}\glsl\*.vert"
SectionEnd

Section "Editor" Editor
	SetOutPath "$INSTDIR"
	File "${SRCDIR}\OpenRA.Editor.exe"
	
	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\OpenRA Editor.lnk" $OUTDIR\OpenRA.Editor.exe "" \
			"$OUTDIR\OpenRA.Editor.exe" "" "" "" ""
	!insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

SectionGroup /e "Mods"
	Section "Red Alert" RA
		RMDir /r "$INSTDIR\mods\ra"
		SetOutPath "$INSTDIR\mods\ra"
		File "${SRCDIR}\mods\ra\*.*"
		File /r "${SRCDIR}\mods\ra\maps"
		File /r "${SRCDIR}\mods\ra\chrome"
		File /r "${SRCDIR}\mods\ra\bits"
		File /r "${SRCDIR}\mods\ra\rules"
		File /r "${SRCDIR}\mods\ra\tilesets"
		File /r "${SRCDIR}\mods\ra\uibits"
	SectionEnd
	Section "Command & Conquer" CNC
		RMDir /r "$INSTDIR\mods\cnc"
		SetOutPath "$INSTDIR\mods\cnc"
		File "${SRCDIR}\mods\cnc\*.*"
		File /r "${SRCDIR}\mods\cnc\maps"
		File /r "${SRCDIR}\mods\cnc\chrome"
		File /r "${SRCDIR}\mods\cnc\bits"
		File /r "${SRCDIR}\mods\cnc\rules"
		File /r "${SRCDIR}\mods\cnc\sequences"
		File /r "${SRCDIR}\mods\cnc\tilesets"
		File /r "${SRCDIR}\mods\cnc\uibits"
	SectionEnd
	Section "Dune 2000" D2K
		RMDir /r "$INSTDIR\mods\d2k"
		SetOutPath "$INSTDIR\mods\d2k"
		File "${SRCDIR}\mods\d2k\*.*"
		File /r "${SRCDIR}\mods\d2k\maps"
		File /r "${SRCDIR}\mods\d2k\chrome"
		File /r "${SRCDIR}\mods\d2k\bits"
		File /r "${SRCDIR}\mods\d2k\rules"
		File /r "${SRCDIR}\mods\d2k\tilesets"
		File /r "${SRCDIR}\mods\d2k\uibits"
	SectionEnd
SectionGroupEnd

;***************************
;Dependency Sections
;***************************
Section "-OpenAl" OpenAl
	AddSize 768
	ClearErrors
	${GetFileVersion} $SYSDIR\OpenAL32.dll $0
	IfErrors installal 0
	${VersionCompare} $0 "6.14.357.24" $1
	IntCmp $1 1 done done installal
	installal:
		SetOutPath "$TEMP"
		NSISdl::download http://connect.creativelabs.com/openal/Downloads/oalinst.zip oalinst.zip
		Pop $R0
		StrCmp $R0 "success" +2
			Abort
		!insertmacro ZIPDLL_EXTRACT oalinst.zip OpenAL oalinst.exe
		ExecWait "$TEMP\OpenAL\oalinst.exe"
	done:
SectionEnd

Section "-Sdl" SDL
	AddSize 317
	IfFileExists $INSTDIR\SDL.dll done installsdl
	installsdl:
		SetOutPath "$TEMP"
		NSISdl::download http://www.libsdl.org/release/SDL-1.2.14-win32.zip sdl.zip
		!insertmacro ZIPDLL_EXTRACT sdl.zip $INSTDIR SDL.dll
	done:
SectionEnd

Section "-Freetype" Freetype
	AddSize 583
	SetOutPath "$TEMP"
	IfFileExists $INSTDIR\zlib1.dll done installfreetype
	installfreetype:
		!insertmacro DownloadDependency "freetype" "freetype-zlib.zip"
		ZipDLL::extractall "freetype-zlib.zip" "$INSTDIR"
	done:
SectionEnd

Section "-Cg" Cg
	AddSize 1500
	SetOutPath "$TEMP"
	IfFileExists $INSTDIR\cg.dll done installcg
	installcg:
		!insertmacro DownloadDependency "cg" "cg-win32.zip"
		ZipDLL::extractall "cg-win32.zip" "$INSTDIR"
	done:
SectionEnd

Section "-DotNet" DotNet
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Install"
	IfErrors error 0
	IntCmp $0 1 0 error 0
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "SP"
	IfErrors error 0
	IntCmp $0 1 done error done
	error: 
		MessageBox MB_YESNO ".NET Framework v3.5 SP1 or later is required to run OpenRA. $\n \
		Do you wish for the installer to launch your web browser in order to download and install it?" \
		IDYES download IDNO error2
	download:
		ExecShell "open" "http://www.microsoft.com/downloads/en/details.aspx?familyid=ab99342f-5d1a-413d-8319-81da479ab0d7"
		Goto done
	error2:
		MessageBox MB_OK "Installation will continue but be aware that OpenRA will not run unless .NET v3.5 SP1 \
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
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "Publisher" "IJW Software (New Zealand)"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenRA" "URLInfoAbout" "http://open-ra.org"
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
	RMDir /r $INSTDIR\cg
	RMDir /r $INSTDIR\glsl
	Delete $INSTDIR\OpenRA.Launcher.exe
	Delete $INSTDIR\OpenRA.Game.exe
	Delete $INSTDIR\OpenRA.Utility.exe
	Delete $INSTDIR\OpenRA.Editor.exe
	Delete $INSTDIR\OpenRA.FileFormats.dll
	Delete $INSTDIR\OpenRA.Renderer.Gl.dll
	Delete $INSTDIR\OpenRA.Renderer.Cg.dll
	Delete $INSTDIR\OpenRA.Renderer.Null.dll
	Delete $INSTDIR\OpenRA.Renderer.SdlCommon.dll
	Delete $INSTDIR\ICSharpCode.SharpZipLib.dll
	Delete $INSTDIR\FuzzyLogicLibrary.dll
	Delete $INSTDIR\Mono.Nat.dll
	Delete $INSTDIR\Tao.*.dll
	Delete $INSTDIR\SharpFont.*.dll
	Delete $INSTDIR\COPYING
	Delete $INSTDIR\HACKING
	Delete $INSTDIR\INSTALL
	Delete $INSTDIR\OpenRA.ico
	Delete $INSTDIR\*.ttf
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
LangString DESC_Client ${LANG_ENGLISH} "OpenRA client and dependencies"
LangString DESC_RA ${LANG_ENGLISH} "Base Red Alert mod"
LangString DESC_CNC ${LANG_ENGLISH} "Base Command and Conquer mod"
LangString DESC_D2K ${LANG_ENGLISH} "Base Dune 2000 mod"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Client} $(DESC_Client)
	!insertmacro MUI_DESCRIPTION_TEXT ${RA} $(DESC_RA)
	!insertmacro MUI_DESCRIPTION_TEXT ${CNC} $(DESC_CNC)
	!insertmacro MUI_DESCRIPTION_TEXT ${D2K} $(DESC_D2K)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Callbacks
;***************************

Function .onInstFailed
	Call Clean
FunctionEnd
