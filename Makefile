############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make [DEBUG=false]
#
# to compile with development tools, run:
#   make all [DEBUG=false]
#
# to check the official mods for erroneous yaml files, run:
#   make test
#
# to check the official mod dlls for StyleCop violations, run:
#   make check
#
# to generate documentation aimed at modders, run:
#   make docs
#
# to install, run:
#   make [prefix=/foo] [bindir=/bar/bin] install
#
# to install with development tools, run:
#   make [prefix=/foo] [bindir=/bar/bin] install-all
#
# to install Linux startup scripts, desktop files and icons:
#   make install-linux-shortcuts [DEBUG=false]
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
CSC         = dmcs
CSFLAGS     = -nologo -warn:4 -codepage:utf8 -unsafe -warnaserror
DEFINE      = TRACE
COMMON_LIBS = System.dll System.Core.dll System.Data.dll System.Data.DataSetExtensions.dll System.Drawing.dll System.Xml.dll thirdparty/download/ICSharpCode.SharpZipLib.dll thirdparty/download/FuzzyLogicLibrary.dll thirdparty/download/Mono.Nat.dll thirdparty/download/MaxMind.Db.dll thirdparty/download/MaxMind.GeoIP2.dll thirdparty/download/Eluant.dll thirdparty/download/SmarIrc4net.dll

DEBUG = true
ifeq ($(DEBUG), $(filter $(DEBUG),false no n off 0))
CSFLAGS   += -debug:pdbonly -optimize+
else
CSFLAGS   += -debug:full -optimize-
DEFINE    := DEBUG;$(DEFINE)
endif



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

# program targets
CORE = pdefault pnull game utility
TOOLS = gamemonitor
VERSION     = $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`)

# dependencies
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Darwin)
os-dependencies = osx-dependencies
else
os-dependencies = linux-dependencies
endif



######################## PROGRAM TARGET RULES ##########################
#
# Core binaries

game_SRCS := $(shell find OpenRA.Game/ -iname '*.cs')
game_TARGET = OpenRA.Game.exe
game_KIND = winexe
game_LIBS = $(COMMON_LIBS) $(game_DEPS) thirdparty/download/SharpFont.dll
game_FLAGS = -win32icon:OpenRA.Game/OpenRA.ico
PROGRAMS += game
game: $(game_TARGET)

# Platform dlls
pdefault_SRCS := $(shell find OpenRA.Platforms.Default/ -iname '*.cs')
pdefault_TARGET = OpenRA.Platforms.Default.dll
pdefault_KIND = library
pdefault_DEPS = $(game_TARGET)
pdefault_LIBS = $(COMMON_LIBS) thirdparty/download/SDL2-CS.dll $(pdefault_DEPS)

pnull_SRCS := $(shell find OpenRA.Platforms.Null/ -iname '*.cs')
pnull_TARGET = OpenRA.Platforms.Null.dll
pnull_KIND = library
pnull_DEPS = $(game_TARGET)
pnull_LIBS = $(COMMON_LIBS) $(pnull_DEPS)
PROGRAMS += pdefault pnull
platforms: $(pdefault_TARGET) $(pnull_TARGET)

# Mods Common
mod_common_SRCS := $(shell find OpenRA.Mods.Common/ -iname '*.cs')
mod_common_TARGET = mods/common/OpenRA.Mods.Common.dll
mod_common_KIND = library
mod_common_DEPS = $(game_TARGET)
mod_common_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) thirdparty/download/StyleCop.dll thirdparty/download/StyleCop.CSharp.dll thirdparty/download/StyleCop.CSharp.Rules.dll
PROGRAMS += mod_common
mod_common: $(mod_common_TARGET)

##### Official Mods #####

STD_MOD_LIBS	= $(game_TARGET)
STD_MOD_DEPS	= $(STD_MOD_LIBS)

# Red Alert
mod_ra_SRCS := $(shell find OpenRA.Mods.RA/ -iname '*.cs')
mod_ra_TARGET = mods/ra/OpenRA.Mods.RA.dll
mod_ra_KIND = library
mod_ra_DEPS = $(STD_MOD_DEPS) $(mod_common_TARGET)
mod_ra_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_common_TARGET)
PROGRAMS += mod_ra
mod_ra: $(mod_ra_TARGET)

# Command and Conquer
mod_cnc_SRCS := $(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
mod_cnc_TARGET = mods/cnc/OpenRA.Mods.Cnc.dll
mod_cnc_KIND = library
mod_cnc_DEPS = $(STD_MOD_DEPS) $(mod_common_TARGET)
mod_cnc_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_common_TARGET)
PROGRAMS += mod_cnc
mod_cnc: $(mod_cnc_TARGET)

# Dune 2000
mod_d2k_SRCS := $(shell find OpenRA.Mods.D2k/ -iname '*.cs')
mod_d2k_TARGET = mods/d2k/OpenRA.Mods.D2k.dll
mod_d2k_KIND = library
mod_d2k_DEPS = $(STD_MOD_DEPS) $(mod_common_TARGET)
mod_d2k_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_common_TARGET)
PROGRAMS += mod_d2k
mod_d2k: $(mod_d2k_TARGET)

# Tiberian Sun
mod_ts_SRCS := $(shell find OpenRA.Mods.TS/ -iname '*.cs')
mod_ts_TARGET = mods/ts/OpenRA.Mods.TS.dll
mod_ts_KIND = library
mod_ts_DEPS = $(STD_MOD_DEPS) $(mod_common_TARGET)
mod_ts_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_common_TARGET)
PROGRAMS += mod_ts
mod_ts: $(mod_ts_TARGET)

check-scripts:
	@echo
	@echo "Checking for Lua syntax errors..."
	@luac -p $(shell find mods/*/maps/* -iname '*.lua')
	@luac -p $(shell find lua/* -iname '*.lua')

check: utility mods
	@echo
	@echo "Checking for code style violations in OpenRA.Game..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Game
	@echo
	@echo "Checking for code style violations in OpenRA.Platforms.Default..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Platforms.Default
	@echo
	@echo "Checking for code style violations in OpenRA.Platforms.Null..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Platforms.Null
	@echo
	@echo "Checking for code style violations in OpenRA.GameMonitor..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.GameMonitor
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.Common..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.Common
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.RA..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.RA
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.Cnc..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.Cnc
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.D2k..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.D2k
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.TS..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Mods.TS
	@echo
	@echo "Checking for code style violations in OpenRA.Utility..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Utility
	@echo
	@echo "Checking for code style violations in OpenRA.Test..."
	@mono --debug OpenRA.Utility.exe ra --check-code-style OpenRA.Test

test: utility mods
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


##### Launchers / Utilities #####

gamemonitor_SRCS := $(shell find OpenRA.GameMonitor/ -iname '*.cs')
gamemonitor_TARGET = OpenRA.exe
gamemonitor_KIND = winexe
gamemonitor_DEPS = $(game_TARGET)
gamemonitor_LIBS = $(COMMON_LIBS) $(gamemonitor_DEPS) System.Windows.Forms.dll
gamemonitor_FLAGS = -win32icon:OpenRA.Game/OpenRA.ico
PROGRAMS += gamemonitor
gamemonitor: $(gamemonitor_TARGET)

# Backend for the launcher apps - queries game/mod info and applies actions to an install
utility_SRCS := $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET = OpenRA.Utility.exe
utility_KIND = exe
utility_DEPS = $(game_TARGET)
utility_LIBS = $(COMMON_LIBS) $(utility_DEPS) thirdparty/download/ICSharpCode.SharpZipLib.dll
PROGRAMS += utility
utility: $(utility_TARGET)

# Patches binary headers to work around a mono bug
fixheader.exe: packaging/fixheader.cs
	@echo CSC fixheader.exe
	@$(CSC) packaging/fixheader.cs $(CSFLAGS) -out:fixheader.exe -t:exe $(COMMON_LIBS:%=-r:%)

# Generate build rules for each target defined above in PROGRAMS
define BUILD_ASSEMBLY

$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS) fixheader.exe
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_EXTRA) \
		$$($(1)_SRCS)
	@mono fixheader.exe $$(@) > /dev/null
	@test `echo $$(@) | sed 's/^.*\.//'` = "dll" && chmod a-x $$(@) || ``
	@$$($(1)_EXTRA_CMDS)
endef

$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))



########################## MAKE/INSTALL RULES ##########################
#
default: core

core: game platforms mods utility

tools: gamemonitor

package: all-dependencies core tools docs version

mods: mod_common mod_ra mod_cnc mod_d2k mod_ts

all: dependencies core tools

clean:
	@-$(RM_F) *.exe *.dll *.dylib *.dll.config ./OpenRA*/*.dll ./OpenRA*/*.mdb *.mdb mods/**/*.dll mods/**/*.mdb *.resources
	@-$(RM_RF) ./*/bin ./*/obj
	@-$(RM_RF) ./thirdparty/download

distclean: clean

cli-dependencies:
	@./thirdparty/fetch-thirdparty-deps.sh
	@ $(CP_R) thirdparty/download/*.dll .
	@ $(CP_R) thirdparty/download/*.dll.config .
	@ $(CP) thirdparty/SDL2-CS.dll.config .

linux-dependencies: cli-dependencies linux-native-dependencies

linux-native-dependencies:
	@./thirdparty/configure-native-deps.sh

windows-dependencies:
	@./thirdparty/fetch-thirdparty-deps-windows.sh

osx-dependencies: cli-dependencies
	@./thirdparty/fetch-thirdparty-deps-osx.sh
	@ $(CP_R) thirdparty/download/osx/*.dylib .
	@ $(CP_R) thirdparty/download/osx/*.dll.config .

dependencies: $(os-dependencies)
	@./thirdparty/fetch-geoip-db.sh
	@ $(CP) thirdparty/download/GeoLite2-Country.mmdb.gz .

all-dependencies: cli-dependencies windows-dependencies osx-dependencies

version: mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/ts/mod.yaml mods/modchooser/mod.yaml mods/all/mod.yaml
	@for i in $? ; do \
		awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $${i} > $${i}.tmp && \
		mv -f $${i}.tmp $${i} ; \
	done

docs: utility mods version
	@mono --debug OpenRA.Utility.exe all --docs > DOCUMENTATION.md
	@mono --debug OpenRA.Utility.exe all --lua-docs > Lua-API.md

man-page: utility mods
	@mono --debug OpenRA.Utility.exe all --man-page > openra.6

install: install-core

install-all: install-core install-tools

install-linux-shortcuts: install-linux-scripts install-linux-icons install-linux-desktop

install-core: default
	@-echo "Installing OpenRA to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/common "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_common_TARGET) "$(DATA_INSTALL_DIR)/mods/common"
	@$(CP_R) mods/cnc "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_cnc_TARGET) "$(DATA_INSTALL_DIR)/mods/cnc"
	@$(CP_R) mods/ra "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_ra_TARGET) "$(DATA_INSTALL_DIR)/mods/ra"
	@$(CP_R) mods/d2k "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_d2k_TARGET) "$(DATA_INSTALL_DIR)/mods/d2k"
	@$(CP_R) mods/modchooser "$(DATA_INSTALL_DIR)/mods/"

	@$(INSTALL_DATA) "global mix database.dat" "$(DATA_INSTALL_DIR)/global mix database.dat"
	@$(INSTALL_DATA) "GeoLite2-Country.mmdb.gz" "$(DATA_INSTALL_DIR)/GeoLite2-Country.mmdb.gz"
	@$(INSTALL_DATA) AUTHORS "$(DATA_INSTALL_DIR)/AUTHORS"
	@$(INSTALL_DATA) COPYING "$(DATA_INSTALL_DIR)/COPYING"

	@$(CP_R) glsl "$(DATA_INSTALL_DIR)"
	@$(CP_R) lua "$(DATA_INSTALL_DIR)"
	@$(CP) SDL2-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) Eluant* "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) ICSharpCode.SharpZipLib.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) FuzzyLogicLibrary.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SharpFont.dll "$(DATA_INSTALL_DIR)"
	@$(CP) SharpFont.dll.config "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) Mono.Nat.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) MaxMind.Db.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) MaxMind.GeoIP2.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) Newtonsoft.Json.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) RestSharp.dll "$(DATA_INSTALL_DIR)"

ifneq ($(UNAME_S),Darwin)
	@$(CP) *.sh "$(DATA_INSTALL_DIR)"
endif

install-tools: tools
	@-echo "Installing OpenRA tools to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(foreach prog,$(TOOLS),$($(prog)_TARGET)) "$(DATA_INSTALL_DIR)"

install-linux-icons:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/icons/"
	@$(CP_R) packaging/linux/hicolor "$(DESTDIR)$(datadir)/icons/"

install-linux-desktop:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@$(INSTALL_DATA) packaging/linux/openra.desktop "$(DESTDIR)$(datadir)/applications"

install-linux-mime:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/mime/packages/"
	@$(INSTALL_DATA) packaging/linux/openra-mimeinfo.xml "$(DESTDIR)$(datadir)/mime/packages/openra.xml"

	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@$(INSTALL_DATA) packaging/linux/openra-join-servers.desktop "$(DESTDIR)$(datadir)/applications"
	@$(INSTALL_DATA) packaging/linux/openra-replays.desktop "$(DESTDIR)$(datadir)/applications"

install-linux-appdata:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/appdata/"
	@$(INSTALL_DATA) packaging/linux/openra.appdata.xml "$(DESTDIR)$(datadir)/appdata/"

install-man-page: man-page
	@$(INSTALL_DIR) "$(DESTDIR)$(mandir)/man6/"
	@$(INSTALL_DATA) openra.6 "$(DESTDIR)$(mandir)/man6/"

install-linux-scripts:
	@echo "#!/bin/sh" > openra
	@echo 'cd "$(gameinstalldir)"' >> openra
ifeq ($(DEBUG), $(filter $(DEBUG),false no n off 0))
	@echo 'mono OpenRA.Game.exe "$$@"' >> openra
else
	@echo 'mono --debug OpenRA.Game.exe "$$@"' >> openra
endif
	@echo 'if [ $$? != 0 -a $$? != 1 ]' >> openra
	@echo 'then' >> openra
	@echo 'ZENITY=`which zenity` || echo "OpenRA needs zenity installed to display a graphical error dialog. See ~/.openra. for log files."' >> openra
	@echo '$$ZENITY --question --title "OpenRA" --text "OpenRA has encountered a fatal error.\nLog Files are available in ~/.openra." --ok-label "Quit" --cancel-label "View FAQ" || xdg-open https://github.com/OpenRA/OpenRA/wiki/FAQ' >> openra
	@echo 'exit 1' >> openra
	@echo 'fi' >> openra

	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx openra "$(BIN_INSTALL_DIR)"
	@-$(RM) openra

uninstall:
	@-$(RM_R) "$(DATA_INSTALL_DIR)"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/16x16/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/64x64/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/128x128/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/mime/packages/openra.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/appdata/openra.appdata.xml"
	@-$(RM_F) "$(DESTDIR)$(mandir)/man6/openra.6"

help:
	@echo to compile, run:
	@echo \ \ make [DEBUG=false]
	@echo
	@echo to compile with development tools, run:
	@echo \ \ make all [DEBUG=false]
	@echo
	@echo to check the official mods for erroneous yaml files, run:
	@echo \ \ make test
	@echo
	@echo to generate documentation aimed at modders, run:
	@echo \ \ make docs
	@echo
	@echo to install, run:
	@echo \ \ make \[prefix=/foo\] \[bindir=/bar/bin\] install
	@echo
	@echo to install with development tools, run:
	@echo \ \ make \[prefix=/foo\] \[bindir=/bar/bin\] install-all
	@echo
	@echo to install Linux startup scripts, desktop files and icons
	@echo \ \ make install-linux-shortcuts [DEBUG=false]
	@echo
	@echo to uninstall, run:
	@echo \ \ make uninstall
	@echo
	@echo to start the game, run:
	@echo \ \ openra




########################### MAKEFILE SETTINGS ##########################
#
.DEFAULT_GOAL := default

.SUFFIXES:

.PHONY: core tools package all mods clean distclean dependencies version $(PROGRAMS)
