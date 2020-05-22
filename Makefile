############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make [DEBUG=true]
#
# to compile using system libraries for native dependencies, run:
#   make [DEBUG=true] TARGETPLATFORM=unix-generic
#
# to check the official mods for erroneous yaml files, run:
#   make test
#
# to check the engine and official mod dlls for code style violations, run:
#   make check
#
# to install, run:
#   make [prefix=/foo] [bindir=/bar/bin] install
#
# to install Linux startup scripts, desktop files and icons:
#   make install-linux-shortcuts [DEBUG=false]
#
# to install the engine and common mod files (omitting the default mods):
#   make install-engine
#   make install-dependencies
#   make install-common-mod-files
#
# to uninstall, run:
#   make uninstall
#
# for help, run:
#   make help
#
# to start the game, run:
#   openra

############################## TOOLCHAIN ###############################
#
# List of .NET assemblies that we can guarantee exist
# OpenRA.Game.dll is a harmless false positive that we can ignore
WHITELISTED_OPENRA_ASSEMBLIES = OpenRA.Game.exe OpenRA.Utility.exe OpenRA.Platforms.Default.dll OpenRA.Mods.Common.dll OpenRA.Mods.Cnc.dll OpenRA.Mods.D2k.dll OpenRA.Game.dll

# These are explicitly shipped alongside our core files by the packaging script
WHITELISTED_THIRDPARTY_ASSEMBLIES = ICSharpCode.SharpZipLib.dll FuzzyLogicLibrary.dll Eluant.dll BeaconLib.dll Open.Nat.dll SDL2-CS.dll OpenAL-CS.Core.dll

# These are shipped in our custom minimal mono runtime and also available in the full system-installed .NET/mono stack
# This list *must* be kept in sync with the files packaged by the AppImageSupport and OpenRALauncherOSX repositories
WHITELISTED_CORE_ASSEMBLIES = mscorlib.dll System.dll System.Configuration.dll System.Core.dll System.Numerics.dll System.Security.dll System.Xml.dll Mono.Security.dll netstandard.dll

NUNIT_LIBS_PATH :=
NUNIT_LIBS  := $(NUNIT_LIBS_PATH)nunit.framework.dll

######################### UTILITIES/SETTINGS ###########################
#
# install locations
prefix ?= /usr/local
datarootdir ?= $(prefix)/share
datadir ?= $(datarootdir)
mandir ?= $(datarootdir)/man/
bindir ?= $(prefix)/bin
libdir ?= $(prefix)/lib
gameinstalldir ?= $(libdir)/openra

BIN_INSTALL_DIR = $(DESTDIR)$(bindir)
DATA_INSTALL_DIR = $(DESTDIR)$(gameinstalldir)

# install tools
RM = rm
RM_R = $(RM) -r
RM_F = $(RM) -f
RM_RF = $(RM) -rf
CP = cp
CP_R = $(CP) -r
INSTALL = install
INSTALL_DIR = $(INSTALL) -d
INSTALL_PROGRAM = $(INSTALL) -m755
INSTALL_DATA = $(INSTALL) -m644

# Toolchain
MSBUILD = msbuild -verbosity:m -nologo

# Enable 32 bit builds while generating the windows installer
WIN32 = false

# dependencies
ifndef TARGETPLATFORM
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Darwin)
TARGETPLATFORM = osx-x64
else
ifeq ($(UNAME_M),x86_64)
TARGETPLATFORM = linux-x64
else
TARGETPLATFORM = unix-generic
endif
endif
endif

# program targets
VERSION     = $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`)

check-scripts:
	@echo
	@echo "Checking for Lua syntax errors..."
	@luac -p $(shell find mods/*/maps/* -iname '*.lua')
	@luac -p $(shell find lua/* -iname '*.lua')

check:
	@echo
	@echo "Compiling in debug mode..."
	@$(MSBUILD) -t:build -p:Configuration=Debug
	@echo
	@echo "Checking runtime assemblies..."
	@mono --debug OpenRA.Utility.exe all --check-runtime-assemblies $(WHITELISTED_OPENRA_ASSEMBLIES) $(WHITELISTED_THIRDPARTY_ASSEMBLIES) $(WHITELISTED_CORE_ASSEMBLIES)
	@echo
	@echo "Checking for explicit interface violations..."
	@mono --debug OpenRA.Utility.exe all --check-explicit-interfaces
	@echo
	@echo "Checking for incorrect conditional trait interface overrides..."
	@mono --debug OpenRA.Utility.exe all --check-conditional-trait-interface-overrides

test: core
	@echo
	@echo "Testing Tiberian Sun mod MiniYAML..."
	@mono --debug OpenRA.Utility.exe ts --check-yaml
	@echo
	@echo "Testing Dune 2000 mod MiniYAML..."
	@mono --debug OpenRA.Utility.exe d2k --check-yaml
	@echo
	@echo "Testing Tiberian Dawn mod MiniYAML..."
	@mono --debug OpenRA.Utility.exe cnc --check-yaml
	@echo
	@echo "Testing Red Alert mod MiniYAML..."
	@mono --debug OpenRA.Utility.exe ra --check-yaml

########################## MAKE/INSTALL RULES ##########################
#
all: core

core:
	@command -v $(firstword $(MSBUILD)) >/dev/null || (echo "OpenRA requires the '$(MSBUILD)' tool provided by Mono >= 5.18."; exit 1)
	@$(MSBUILD) -t:Build -restore -p:Configuration=Release -p:TargetPlatform=$(TARGETPLATFORM)
ifeq ($(TARGETPLATFORM), unix-generic)
	@./configure-system-libraries.sh
endif
	@./fetch-geoip.sh

clean:
	@-$(RM_F) *.config IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP
	@-$(RM_F) *.exe *.dll *.dll.config *.so *.dylib ./OpenRA*/*.dll *.pdb mods/**/*.dll mods/**/*.pdb *.resources
	@-$(RM_RF) ./*/bin ./*/obj
	@ $(MSBUILD) -t:clean

version: VERSION mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/ts/mod.yaml mods/modcontent/mod.yaml mods/all/mod.yaml
	@echo "$(VERSION)" > VERSION
	@for i in $? ; do \
		awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $${i} > $${i}.tmp && \
		awk '{sub("/[^/]*: User$$", "/$(VERSION): User"); print $0}' $${i}.tmp > $${i} && \
		rm $${i}.tmp; \
	done

install: core install-core

install-linux-shortcuts: install-linux-scripts install-linux-icons install-linux-desktop

install-dependencies:
ifeq ($(TARGETPLATFORM), $(filter $(TARGETPLATFORM),win-x86 win-x64))
	@-echo "Installing OpenRA dependencies to $(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) soft_oal.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SDL2.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) freetype6.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) lua51.dll "$(DATA_INSTALL_DIR)"
endif
ifeq ($(TARGETPLATFORM), linux-x64)
	@-echo "Installing OpenRA dependencies to $(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) soft_oal.so "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SDL2.so "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) freetype6.so "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) lua51.so "$(DATA_INSTALL_DIR)"
endif
ifeq ($(TARGETPLATFORM), osx-x64)
	@-echo "Installing OpenRA dependencies to $(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) soft_oal.dylib "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SDL2.dylib "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) freetype6.dylib "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) lua51.dylib "$(DATA_INSTALL_DIR)"
endif

install-engine:
	@-echo "Installing OpenRA engine to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) OpenRA.Game.exe "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) OpenRA.Server.exe "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) OpenRA.Utility.exe "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) OpenRA.Platforms.Default.dll "$(DATA_INSTALL_DIR)"

ifneq ($(TARGETPLATFORM), $(filter $(TARGETPLATFORM),win-x86 win-x64))
	@$(INSTALL_DATA) OpenRA.Platforms.Default.dll.config "$(DATA_INSTALL_DIR)"
endif
	@$(INSTALL_DATA) VERSION "$(DATA_INSTALL_DIR)/VERSION"
	@$(INSTALL_DATA) AUTHORS "$(DATA_INSTALL_DIR)/AUTHORS"
	@$(INSTALL_DATA) COPYING "$(DATA_INSTALL_DIR)/COPYING"
	@$(INSTALL_DATA) IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP "$(DATA_INSTALL_DIR)/IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP"

	@$(CP_R) glsl "$(DATA_INSTALL_DIR)"
	@$(CP_R) lua "$(DATA_INSTALL_DIR)"
	@$(CP) SDL2-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) OpenAL-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) Eluant* "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) ICSharpCode.SharpZipLib.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) FuzzyLogicLibrary.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) Open.Nat.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) BeaconLib.dll "$(DATA_INSTALL_DIR)"

install-common-mod-files:
	@-echo "Installing OpenRA common mod files to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/common "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) mods/common/OpenRA.Mods.Common.dll "$(DATA_INSTALL_DIR)/mods/common"
	@$(INSTALL_PROGRAM) mods/common/OpenRA.Mods.Cnc.dll "$(DATA_INSTALL_DIR)/mods/common"
	@$(INSTALL_DATA) "global mix database.dat" "$(DATA_INSTALL_DIR)/global mix database.dat"

install-default-mods:
	@-echo "Installing OpenRA default mods to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/cnc "$(DATA_INSTALL_DIR)/mods/"
	@$(CP_R) mods/ra "$(DATA_INSTALL_DIR)/mods/"
	@$(CP_R) mods/d2k "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) mods/d2k/OpenRA.Mods.D2k.dll "$(DATA_INSTALL_DIR)/mods/d2k"
	@$(CP_R) mods/modcontent "$(DATA_INSTALL_DIR)/mods/"

install-core: install-engine install-common-mod-files install-default-mods
	@$(CP) *.sh "$(DATA_INSTALL_DIR)"

install-linux-icons:
	for SIZE in 16x16 32x32 48x48 64x64 128x128; do \
		$(INSTALL_DIR) "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps"; \
		$(INSTALL_DATA) packaging/linux/icons/ra_$$SIZE.png "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-ra.png"; \
		$(INSTALL_DATA) packaging/linux/icons/cnc_$$SIZE.png "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-cnc.png"; \
		$(INSTALL_DATA) packaging/linux/icons/d2k_$$SIZE.png "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-d2k.png"; \
	done
	$(INSTALL_DIR) "$(DESTDIR)$(datadir)/icons/hicolor/scalable/apps"
	$(INSTALL_DATA) packaging/linux/icons/ra_scalable.svg "$(DESTDIR)$(datadir)/icons/hicolor/scalable/apps/openra-ra.svg"
	$(INSTALL_DATA) packaging/linux/icons/cnc_scalable.svg "$(DESTDIR)$(datadir)/icons/hicolor/scalable/apps/openra-cnc.svg"

install-linux-desktop:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MODID}/ra/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Red Alert/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-ra.desktop
	@$(INSTALL_DATA) packaging/linux/openra-ra.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MODID}/cnc/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Tiberian Dawn/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-cnc.desktop
	@$(INSTALL_DATA) packaging/linux/openra-cnc.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MODID}/d2k/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Dune 2000/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-d2k.desktop
	@$(INSTALL_DATA) packaging/linux/openra-d2k.desktop "$(DESTDIR)$(datadir)/applications"
	@-$(RM) packaging/linux/openra-ra.desktop packaging/linux/openra-cnc.desktop packaging/linux/openra-d2k.desktop

install-linux-mime:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/mime/packages/"
	@sed 's/{MODID}/ra/g' packaging/linux/openra-mimeinfo.xml.in | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-mimeinfo.xml
	@$(INSTALL_DATA) packaging/linux/openra-mimeinfo.xml "$(DESTDIR)$(datadir)/mime/packages/openra-ra.xml"
	@sed 's/{MODID}/cnc/g' packaging/linux/openra-mimeinfo.xml.in | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-mimeinfo.xml
	@$(INSTALL_DATA) packaging/linux/openra-mimeinfo.xml "$(DESTDIR)$(datadir)/mime/packages/openra-cnc.xml"
	@sed 's/{MODID}/d2k/g' packaging/linux/openra-mimeinfo.xml.in | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-mimeinfo.xml
	@$(INSTALL_DATA) packaging/linux/openra-mimeinfo.xml "$(DESTDIR)$(datadir)/mime/packages/openra-d2k.xml"

install-linux-appdata:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/appdata/"
	@sed 's/{MODID}/ra/g' packaging/linux/openra.appdata.xml.in | sed 's/{MOD_NAME}/Red Alert/g' | sed 's/{SCREENSHOT_RA}/ type="default"/g' | sed 's/{SCREENSHOT_CNC}//g' | sed 's/{SCREENSHOT_D2K}//g'> packaging/linux/openra-ra.appdata.xml
	@$(INSTALL_DATA) packaging/linux/openra-ra.appdata.xml "$(DESTDIR)$(datadir)/appdata/"
	@sed 's/{MODID}/cnc/g' packaging/linux/openra.appdata.xml.in | sed 's/{MOD_NAME}/Tiberian Dawn/g' | sed 's/{SCREENSHOT_RA}//g' | sed 's/{SCREENSHOT_CNC}/ type="default"/g' | sed 's/{SCREENSHOT_D2K}//g'> packaging/linux/openra-cnc.appdata.xml
	@$(INSTALL_DATA) packaging/linux/openra-cnc.appdata.xml "$(DESTDIR)$(datadir)/appdata/"
	@sed 's/{MODID}/d2k/g' packaging/linux/openra.appdata.xml.in | sed 's/{MOD_NAME}/Dune 2000/g' | sed 's/{SCREENSHOT_RA}//g' | sed 's/{SCREENSHOT_CNC}//g' | sed 's/{SCREENSHOT_D2K}/ type="default"/g'> packaging/linux/openra-d2k.appdata.xml
	@$(INSTALL_DATA) packaging/linux/openra-d2k.appdata.xml "$(DESTDIR)$(datadir)/appdata/"
	@-$(RM) packaging/linux/openra-ra.appdata.xml packaging/linux/openra-cnc.appdata.xml packaging/linux/openra-d2k.appdata.xml

install-man-page:
	@$(INSTALL_DIR) "$(DESTDIR)$(mandir)/man6/"
	@mono --debug OpenRA.Utility.exe all --man-page > openra.6
	@$(INSTALL_DATA) openra.6 "$(DESTDIR)$(mandir)/man6/"
	@-$(RM) openra.6

install-linux-scripts:
ifeq ($(DEBUG), $(filter $(DEBUG),false no n off 0))
	@sed 's/{DEBUG}//' packaging/linux/openra.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra.debug.in
	@sed 's/{DEBUG}//' packaging/linux/openra-server.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra-server.debug.in
else
	@sed 's/{DEBUG}/--debug/' packaging/linux/openra.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra.debug.in
	@sed 's/{DEBUG}/--debug/' packaging/linux/openra-server.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra-server.debug.in
endif

	@sed 's/{MODID}/ra/g' packaging/linux/openra.debug.in  | sed 's/{TAG}/$(VERSION)/g' | sed 's/{MODNAME}/Red Alert/g' > packaging/linux/openra-ra
	@sed 's/{MODID}/cnc/g' packaging/linux/openra.debug.in | sed 's/{TAG}/$(VERSION)/g' | sed 's/{MODNAME}/Tiberian Dawn/g' > packaging/linux/openra-cnc
	@sed 's/{MODID}/d2k/g' packaging/linux/openra.debug.in | sed 's/{TAG}/$(VERSION)/g' | sed 's/{MODNAME}/Dune 2000/g' > packaging/linux/openra-d2k

	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-ra "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-cnc "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-d2k "$(BIN_INSTALL_DIR)"
	@-$(RM) packaging/linux/openra-ra packaging/linux/openra-cnc packaging/linux/openra-d2k packaging/linux/openra.debug.in

	@sed 's/{MODID}/ra/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Red Alert/g' > packaging/linux/openra-ra-server
	@sed 's/{MODID}/cnc/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Tiberian Dawn/g' > packaging/linux/openra-cnc-server
	@sed 's/{MODID}/d2k/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Dune 2000/g' > packaging/linux/openra-d2k-server

	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-ra-server "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-cnc-server "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-d2k-server "$(BIN_INSTALL_DIR)"
	@-$(RM) packaging/linux/openra-ra-server packaging/linux/openra-cnc-server packaging/linux/openra-d2k-server packaging/linux/openra-server.debug.in

uninstall:
	@-$(RM_R) "$(DATA_INSTALL_DIR)"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-ra"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-ra-server"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-cnc"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-cnc-server"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-d2k"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-d2k-server"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-ra.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-cnc.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-d2k.desktop"
	@-for SIZE in 16x16 32x32 48x48 64x64 128x128; do \
		$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-ra.png"; \
		$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-cnc.png"; \
		$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/$$SIZE/apps/openra-d2k.png"; \
	done
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/scalable/apps/openra-ra.svg"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/scalable/apps/openra-cnc.svg"
	@-$(RM_F) "$(DESTDIR)$(datadir)/mime/packages/openra-ra.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/mime/packages/openra-cnc.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/mime/packages/openra-d2k.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/appdata/openra-ra.appdata.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/appdata/openra-cnc.appdata.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/appdata/openra-d2k.appdata.xml"
	@-$(RM_F) "$(DESTDIR)$(mandir)/man6/openra.6"

help:
	@echo 'to compile, run:'
	@echo '  make [DEBUG=true]'
	@echo
	@echo 'to compile using system libraries for native dependencies, run:'
	@echo '  make [DEBUG=true] TARGETPLATFORM=unix-generic'
	@echo
	@echo 'to check the official mods for erroneous yaml files, run:'
	@echo '  make test'
	@echo
	@echo 'to check the engine and official mod dlls for code style violations, run:'
	@echo '  make test'
	@echo
	@echo 'to install, run:'
	@echo '  make [prefix=/foo] [bindir=/bar/bin] install'
	@echo
	@echo 'to install Linux startup scripts, desktop files and icons'
	@echo '  make install-linux-shortcuts [DEBUG=false]'
	@echo
	@echo ' to install the engine and common mod files (omitting the default mods):'
	@echo '   make install-engine'
	@echo '   make install-dependencies'
	@echo '   make install-common-mod-files'
	@echo
	@echo 'to uninstall, run:'
	@echo '  make uninstall'
	@echo
	@echo 'to start the game, run:'
	@echo '  openra'

########################### MAKEFILE SETTINGS ##########################
#
.DEFAULT_GOAL := all

.SUFFIXES:

.PHONY: check-scripts check test all core clean version install install-linux-shortcuts install-dependencies install-engine install-common-mod-files install-default-mods install-core install-linux-icons install-linux-desktop install-linux-mime install-linux-appdata install-man-page install-linux-scripts uninstall help
