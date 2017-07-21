############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make [DEBUG=false]
#
# to check unit tests (requires NUnit version >= 2.6), run:
#  make nunit [NUNIT_CONSOLE=<path-to/nunit[2]-console>] [NUNIT_LIBS_PATH=<path-to-libs-dir>] [NUNIT_LIBS=<nunit-libs>]
#      Use NUNIT_CONSOLE if nunit[3|2]-console was not downloaded by `make dependencies` nor is it in bin search paths
#      Use NUNIT_LIBS_PATH if NUnit libs are not in search paths. Include trailing /
#      Use NUNIT_LIBS if NUnit libs have different names (such as including a prefix or suffix)
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
# to install Linux startup scripts, desktop files and icons:
#   make install-linux-shortcuts [DEBUG=false]
#
# to install the engine and common mod files (omitting the default mods):
#   make install-engine
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
SDK         ?=
CSC         = mcs $(SDK)
CSFLAGS     = -nologo -warn:4 -codepage:utf8 -unsafe -warnaserror
DEFINE      = TRACE
COMMON_LIBS = System.dll System.Core.dll System.Data.dll System.Data.DataSetExtensions.dll System.Drawing.dll System.Xml.dll thirdparty/download/ICSharpCode.SharpZipLib.dll thirdparty/download/FuzzyLogicLibrary.dll thirdparty/download/MaxMind.Db.dll thirdparty/download/Eluant.dll thirdparty/download/SmarIrc4net.dll thirdparty/download/rix0rrr.BeaconLib.dll
NUNIT_LIBS_PATH :=
NUNIT_LIBS  := $(NUNIT_LIBS_PATH)nunit.framework.dll

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
CORE = pdefault game utility server
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
game_LIBS = $(COMMON_LIBS) $(game_DEPS) thirdparty/download/SharpFont.dll thirdparty/download/Open.Nat.dll
PROGRAMS += game
game: $(game_TARGET)

# Platform dlls
pdefault_SRCS := $(shell find OpenRA.Platforms.Default/ -iname '*.cs')
pdefault_TARGET = OpenRA.Platforms.Default.dll
pdefault_KIND = library
pdefault_DEPS = $(game_TARGET)
pdefault_LIBS = $(COMMON_LIBS) thirdparty/download/SDL2-CS.dll thirdparty/download/OpenAL-CS.dll $(pdefault_DEPS)
PROGRAMS += pdefault
platforms: $(pdefault_TARGET)

# Mods Common
mod_common_SRCS := $(shell find OpenRA.Mods.Common/ -iname '*.cs')
mod_common_TARGET = mods/common/OpenRA.Mods.Common.dll
mod_common_KIND = library
mod_common_DEPS = $(game_TARGET)
mod_common_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS)
PROGRAMS += mod_common
mod_common: $(mod_common_TARGET)

# NUnit testing
test_dll_SRCS := $(shell find OpenRA.Test/ -iname '*.cs')
test_dll_TARGET = OpenRA.Test.dll
test_dll_KIND = library
test_dll_DEPS = $(game_TARGET) $(mod_common_TARGET)
test_dll_FLAGS = -warn:1
test_dll_LIBS = $(COMMON_LIBS) $(game_TARGET) $(mod_common_TARGET) $(NUNIT_LIBS)
PROGRAMS += test_dll
test_dll: $(test_dll_TARGET)

##### Official Mods #####

STD_MOD_LIBS	= $(game_TARGET)
STD_MOD_DEPS	= $(STD_MOD_LIBS)

# Command and Conquer
mod_cnc_SRCS := $(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
mod_cnc_TARGET = mods/common/OpenRA.Mods.Cnc.dll
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

# Over Powered Mod
mod_yupgi_alert_SRCS := $(shell find OpenRA.Mods.yupgi_alert/ -iname '*.cs')
mod_yupgi_alert_TARGET = mods/yupgi_alert/OpenRA.Mods.yupgi_alert.dll
mod_yupgi_alert_KIND = library
mod_yupgi_alert_DEPS = $(STD_MOD_DEPS) $(mod_common_TARGET)
mod_yupgi_alert_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_common_TARGET)
PROGRAMS += mod_yupgi_alert
mods/yupgi_alert:
	mkdir -p mods/yupgi_alert
mod_yupgi_alert: mods/yupgi_alert $(mod_yupgi_alert_TARGET)

check-scripts:
	@echo
	@echo "Checking for Lua syntax errors..."
	@luac -p $(shell find mods/*/maps/* -iname '*.lua')
	@luac -p $(shell find lua/* -iname '*.lua')

check: utility stylecheck mods
	@echo
	@echo "Checking for explicit interface violations..."
	@mono --debug OpenRA.Utility.exe all --check-explicit-interfaces
	@echo
	@echo "Checking for code style violations in OpenRA.Game..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Game
	@echo
	@echo "Checking for code style violations in OpenRA.Platforms.Default..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Platforms.Default
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.Common..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Mods.Common
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.Cnc..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Mods.Cnc
	@echo
	@echo "Checking for code style violations in OpenRA.Mods.D2k..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Mods.D2k
	@echo
	@echo "Checking for code style violations in OpenRA.Utility..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Utility
	@echo
	@echo "Checking for code style violations in OpenRA.Test..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Test
	@echo
	@echo "Checking for code style violations in OpenRA.Server..."
	@mono --debug OpenRA.StyleCheck.exe OpenRA.Server

NUNIT_CONSOLE := $(shell test -f thirdparty/download/nunit3-console.exe && echo mono thirdparty/download/nunit3-console.exe || \
	which nunit3-console 2>/dev/null || which nunit2-console 2>/dev/null || which nunit-console 2>/dev/null)
nunit: test_dll
	@echo
	@echo "Checking unit tests..."
	@if [ "$(NUNIT_CONSOLE)" = "" ] ; then \
		echo 'nunit[3|2]-console not found!'; \
		echo 'Was "make dependencies" called or is NUnit installed?'>&2; \
		echo 'See "make help".'; \
		exit 1; \
	fi
	@if $(NUNIT_CONSOLE) --help | head -n 1 | grep -E "NUnit version (1|2\.[0-5])";then \
		echo 'NUnit version >= 2.6 required'>&2; \
		echo 'Try "make dependencies" first to use NUnit from NuGet.'>&2; \
		echo 'See "make help".'; \
		exit 1; \
	fi
	@$(NUNIT_CONSOLE) --noresult OpenRA.Test.nunit

test: utility mods
	@echo
	@echo "Over Powered Mod MiniYAML..."
	@mono --debug OpenRA.Utility.exe yupgi_alert --check-yaml


##### Launchers / Utilities #####

utility_SRCS := $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET = OpenRA.Utility.exe
utility_KIND = exe
utility_DEPS = $(game_TARGET)
utility_LIBS = $(COMMON_LIBS) $(utility_DEPS) thirdparty/download/ICSharpCode.SharpZipLib.dll
PROGRAMS += utility
utility: $(utility_TARGET)

stylecheck_SRCS := $(shell find OpenRA.StyleCheck/ -iname '*.cs')
stylecheck_TARGET = OpenRA.StyleCheck.exe
stylecheck_KIND = exe
stylecheck_LIBS = thirdparty/download/StyleCop.dll thirdparty/download/StyleCop.CSharp.dll thirdparty/download/StyleCop.CSharp.Rules.dll
PROGRAMS += stylecheck
stylecheck: $(stylecheck_TARGET)

# Dedicated server
server_SRCS := $(shell find OpenRA.Server/ -iname '*.cs')
server_TARGET = OpenRA.Server.exe
server_KIND = exe
server_DEPS = $(game_TARGET)
server_LIBS = $(COMMON_LIBS) $(server_DEPS)
PROGRAMS += server
server: $(server_TARGET)

# Patches binary headers to work around a mono bug
fixheader.exe: packaging/fixheader.cs
	@command -v $(CSC) >/dev/null || (echo "Mono is not installed. Please install Mono from http://www.mono-project.com/download/ before building OpenRA."; exit 1)
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

core: dependencies game platforms mods utility server

mods: mod_common mod_cnc mod_d2k mod_yupgi_alert

all: dependencies core stylecheck

clean:
	@-$(RM_F) *.exe *.dll *.dylib *.dll.config ./OpenRA*/*.dll ./OpenRA*/*.mdb *.mdb mods/**/*.dll mods/**/*.mdb *.resources
	@-$(RM_RF) ./*/bin ./*/obj
	@-$(RM_RF) ./thirdparty/download

distclean: clean

cli-dependencies:
	@./thirdparty/fetch-thirdparty-deps.sh
	@ $(CP_R) thirdparty/download/*.dll .
	@ $(CP_R) thirdparty/download/*.dll.config .

linux-dependencies: cli-dependencies geoip-dependencies linux-native-dependencies

linux-native-dependencies:
	@./thirdparty/configure-native-deps.sh

windows-dependencies: cli-dependencies geoip-dependencies
	@./thirdparty/fetch-thirdparty-deps-windows.sh

osx-dependencies: cli-dependencies geoip-dependencies
	@./thirdparty/fetch-thirdparty-deps-osx.sh
	@ $(CP_R) thirdparty/download/osx/*.dylib .
	@ $(CP_R) thirdparty/download/osx/*.dll.config .

geoip-dependencies:
	@./thirdparty/fetch-geoip-db.sh
	@ $(CP) thirdparty/download/GeoLite2-Country.mmdb.gz .

dependencies: $(os-dependencies)

all-dependencies: cli-dependencies windows-dependencies osx-dependencies geoip-dependencies

version: VERSION mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/ts/mod.yaml mods/modcontent/mod.yaml mods/all/mod.yaml
	@echo "$(VERSION)" > VERSION
	@for i in $? ; do \
		awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $${i} > $${i}.tmp && \
		awk '{sub("/[^/]*: User$$", "/$(VERSION): User"); print $0}' $${i}.tmp > $${i} && \
		rm $${i}.tmp; \
	done

man-page: utility mods
	@mono --debug OpenRA.Utility.exe all --man-page > openra.6

install: default install-core

install-linux-shortcuts: install-linux-scripts install-linux-icons install-linux-desktop

install-engine:
	@-echo "Installing OpenRA engine to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) "$(DATA_INSTALL_DIR)"

	@$(INSTALL_DATA) "GeoLite2-Country.mmdb.gz" "$(DATA_INSTALL_DIR)/GeoLite2-Country.mmdb.gz"
	@$(INSTALL_DATA) VERSION "$(DATA_INSTALL_DIR)/VERSION"
	@$(INSTALL_DATA) AUTHORS "$(DATA_INSTALL_DIR)/AUTHORS"
	@$(INSTALL_DATA) COPYING "$(DATA_INSTALL_DIR)/COPYING"

	@$(CP_R) glsl "$(DATA_INSTALL_DIR)"
	@$(CP_R) lua "$(DATA_INSTALL_DIR)"
	@$(CP) SDL2-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) OpenAL-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) Eluant* "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) ICSharpCode.SharpZipLib.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) FuzzyLogicLibrary.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SharpFont.dll "$(DATA_INSTALL_DIR)"
	@$(CP) SharpFont.dll.config "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) Open.Nat.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) MaxMind.Db.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) SmarIrc4net.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) rix0rrr.BeaconLib.dll "$(DATA_INSTALL_DIR)"

install-common-mod-files:
	@-echo "Installing OpenRA common mod files to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/common "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_common_TARGET) "$(DATA_INSTALL_DIR)/mods/common"
	@$(INSTALL_PROGRAM) $(mod_cnc_TARGET) "$(DATA_INSTALL_DIR)/mods/common"
	@$(INSTALL_DATA) "global mix database.dat" "$(DATA_INSTALL_DIR)/global mix database.dat"

install-default-mods:
	@-echo "Installing OpenRA default mods to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/cnc "$(DATA_INSTALL_DIR)/mods/"
	@$(CP_R) mods/ra "$(DATA_INSTALL_DIR)/mods/"
	@$(CP_R) mods/d2k "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_d2k_TARGET) "$(DATA_INSTALL_DIR)/mods/d2k"
	@$(CP_R) mods/modcontent "$(DATA_INSTALL_DIR)/mods/"

install-core: install-engine install-common-mod-files install-default-mods
	@$(CP) *.sh "$(DATA_INSTALL_DIR)"

install-linux-icons:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/icons/"
	@$(CP_R) packaging/linux/hicolor "$(DESTDIR)$(datadir)/icons/"

install-linux-desktop:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/ra/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Red Alert/g' > packaging/linux/openra-ra.desktop
	@$(INSTALL_DATA) packaging/linux/openra-ra.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/cnc/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Tiberian Dawn/g' > packaging/linux/openra-cnc.desktop
	@$(INSTALL_DATA) packaging/linux/openra-cnc.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/d2k/g' packaging/linux/openra.desktop.in | sed 's/{MODNAME}/Dune 2000/g' > packaging/linux/openra-d2k.desktop
	@$(INSTALL_DATA) packaging/linux/openra-d2k.desktop "$(DESTDIR)$(datadir)/applications"
	@-$(RM) packaging/linux/openra-ra.desktop packaging/linux/openra-cnc.desktop packaging/linux/openra-d2k.desktop

install-linux-mime:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/mime/packages/"

	@sed 's/{MOD}/ra/g' packaging/linux/openra-mimeinfo.xml.in | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-mimeinfo.xml
	@$(INSTALL_DATA) packaging/linux/openra-mimeinfo.xml "$(DESTDIR)$(datadir)/mime/packages/openra.xml"

	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/ra/g' packaging/linux/openra-join-servers.desktop.in | sed 's/{MODNAME}/Red Alert/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-ra-join-servers.desktop
	@$(INSTALL_DATA) packaging/linux/openra-ra-join-servers.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/cnc/g' packaging/linux/openra-join-servers.desktop.in | sed 's/{MODNAME}/Tiberian Dawn/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-cnc-join-servers.desktop
	@$(INSTALL_DATA) packaging/linux/openra-cnc-join-servers.desktop "$(DESTDIR)$(datadir)/applications"
	@sed 's/{MOD}/d2k/g' packaging/linux/openra-join-servers.desktop.in | sed 's/{MODNAME}/Dune 2000/g' | sed 's/{TAG}/$(VERSION)/g' > packaging/linux/openra-d2k-join-servers.desktop
	@$(INSTALL_DATA) packaging/linux/openra-d2k-join-servers.desktop "$(DESTDIR)$(datadir)/applications"
	@-$(RM) packaging/linux/openra-mimeinfo.xml packaging/linux/openra-ra-join-servers.desktop packaging/linux/openra-cnc-join-servers.desktop packaging/linux/openra-d2k-join-servers.desktop

install-linux-appdata:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/appdata/"
	@$(INSTALL_DATA) packaging/linux/openra.appdata.xml "$(DESTDIR)$(datadir)/appdata/"

install-man-page: man-page
	@$(INSTALL_DIR) "$(DESTDIR)$(mandir)/man6/"
	@$(INSTALL_DATA) openra.6 "$(DESTDIR)$(mandir)/man6/"

install-linux-scripts:
ifeq ($(DEBUG), $(filter $(DEBUG),false no n off 0))
	@sed 's/{DEBUG}//' packaging/linux/openra.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra.debug.in
	@sed 's/{DEBUG}//' packaging/linux/openra-server.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra-server.debug.in
else
	@sed 's/{DEBUG}/--debug/' packaging/linux/openra.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra.debug.in
	@sed 's/{DEBUG}/--debug/' packaging/linux/openra-server.in | sed 's|{GAME_INSTALL_DIR}|$(gameinstalldir)|' | sed 's|{BIN_DIR}|$(bindir)|' > packaging/linux/openra-server.debug.in
endif

	@sed 's/{MOD}/ra/g' packaging/linux/openra.debug.in | sed 's/{MODNAME}/Red Alert/g' > packaging/linux/openra-ra
	@sed 's/{MOD}/cnc/g' packaging/linux/openra.debug.in | sed 's/{MODNAME}/Tiberian Dawn/g' > packaging/linux/openra-cnc
	@sed 's/{MOD}/d2k/g' packaging/linux/openra.debug.in | sed 's/{MODNAME}/Dune 2000/g' > packaging/linux/openra-d2k

	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-ra "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-cnc "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx packaging/linux/openra-d2k "$(BIN_INSTALL_DIR)"
	@-$(RM) packaging/linux/openra-ra packaging/linux/openra-cnc packaging/linux/openra-d2k packaging/linux/openra.debug.in

	@sed 's/{MOD}/ra/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Red Alert/g' > packaging/linux/openra-ra-server
	@sed 's/{MOD}/cnc/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Tiberian Dawn/g' > packaging/linux/openra-cnc-server
	@sed 's/{MOD}/d2k/g' packaging/linux/openra-server.debug.in | sed 's/{MODNAME}/Dune 2000/g' > packaging/linux/openra-d2k-server

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
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-ra-join-servers.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-cnc-join-servers.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-d2k-join-servers.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/16x16/apps/openra-ra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra-ra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra-ra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/64x64/apps/openra-ra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/128x128/apps/openra-ra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/16x16/apps/openra-cnc.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra-cnc.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra-cnc.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/64x64/apps/openra-cnc.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/128x128/apps/openra-cnc.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/16x16/apps/openra-d2k.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra-d2k.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra-d2k.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/64x64/apps/openra-d2k.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/128x128/apps/openra-d2k.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/mime/packages/openra.xml"
	@-$(RM_F) "$(DESTDIR)$(datadir)/appdata/openra.appdata.xml"
	@-$(RM_F) "$(DESTDIR)$(mandir)/man6/openra.6"

help:
	@echo 'to compile, run:'
	@echo '  make [DEBUG=false]'
	@echo
	@echo 'to check unit tests (requires NUnit version >= 2.6), run:'
	@echo '  make nunit [NUNIT_CONSOLE=<path-to/nunit[3|2]-console>] [NUNIT_LIBS_PATH=<path-to-libs-dir>] [NUNIT_LIBS=<nunit-libs>]'
	@echo '     Use NUNIT_CONSOLE if nunit[3|2]-console was not downloaded by `make dependencies` nor is it in bin search paths'
	@echo '     Use NUNIT_LIBS_PATH if NUnit libs are not in search paths. Include trailing /'
	@echo '     Use NUNIT_LIBS if NUnit libs have different names (such as including a prefix or suffix)'
	@echo
	@echo 'to check the official mods for erroneous yaml files, run:'
	@echo '  make test'
	@echo
	@echo 'to generate documentation aimed at modders, run:'
	@echo '  make docs'
	@echo
	@echo 'to install, run:'
	@echo '  make [prefix=/foo] [bindir=/bar/bin] install'
	@echo
	@echo 'to install Linux startup scripts, desktop files and icons'
	@echo '  make install-linux-shortcuts [DEBUG=false]'
	@echo
	@echo 'to uninstall, run:'
	@echo '  make uninstall'
	@echo
	@echo 'to start the game, run:'
	@echo '  openra'




########################### MAKEFILE SETTINGS ##########################
#
.DEFAULT_GOAL := default

.SUFFIXES:

.PHONY: core package all mods clean distclean dependencies version $(PROGRAMS) nunit
