############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make
#
# to compile with development tools, run:
#   make all
#
# to check the official mods for erroneous yaml files, run:
#   make test
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
#   make install-linux-shortcuts
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
CSFLAGS     = -nologo -warn:4 -debug:full -optimize- -codepage:utf8 -unsafe -warnaserror
DEFINE      = DEBUG;TRACE
COMMON_LIBS = System.dll System.Core.dll System.Drawing.dll System.Xml.dll thirdparty/ICSharpCode.SharpZipLib.dll thirdparty/FuzzyLogicLibrary.dll thirdparty/Mono.Nat.dll thirdparty/MaxMind.Db.dll thirdparty/MaxMind.GeoIP2.dll thirdparty/Eluant.dll



######################### UTILITIES/SETTINGS ###########################
#
# install locations
prefix ?= /usr/local
datarootdir ?= $(prefix)/share
datadir ?= $(datarootdir)
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

# build locations
OUTPUT_DIR = bin
ENSURE_OUTPUT_DIR = $(OUTPUT_DIR)/placeholder

# program targets
CORE = rsdl2 rnull game utility irc ralint
TOOLS = editor tsbuild crashdialog

VERSION     = $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`)



######################## PROGRAM TARGET RULES ##########################
#
# Core binaries

game_SRCS := $(shell find OpenRA.Game/ -iname '*.cs')
game_TARGET = $(OUTPUT_DIR)/OpenRA.Game.exe
game_KIND = winexe
game_LIBS = $(COMMON_LIBS) $(game_DEPS) thirdparty/SDL2-CS.dll thirdparty/SharpFont.dll
game_FLAGS = -win32icon:OpenRA.Game/OpenRA.ico
PROGRAMS += game
game: $(game_TARGET)

irc_SRCS := $(shell find OpenRA.Irc/ -iname '*.cs')
irc_TARGET = $(OUTPUT_DIR)/OpenRA.Irc.dll
irc_KIND = library
irc_DEPS = $(game_TARGET)
irc_LIBS = $(COMMON_LIBS) $(irc_DEPS)
PROGRAMS += irc
irc: $(irc_TARGET)

# Renderer dlls
rsdl2_SRCS := $(shell find OpenRA.Renderer.Sdl2/ -iname '*.cs')
rsdl2_TARGET = $(OUTPUT_DIR)/OpenRA.Renderer.Sdl2.dll
rsdl2_KIND = library
rsdl2_DEPS = $(game_TARGET)
rsdl2_LIBS = $(COMMON_LIBS) thirdparty/SDL2-CS.dll $(rsdl2_DEPS)

rnull_SRCS := $(shell find OpenRA.Renderer.Null/ -iname '*.cs')
rnull_TARGET = $(OUTPUT_DIR)/OpenRA.Renderer.Null.dll
rnull_KIND = library
rnull_DEPS = $(game_TARGET)
rnull_LIBS = $(COMMON_LIBS) $(rnull_DEPS)
PROGRAMS += rsdl2 rnull
renderers: $(rsdl2_TARGET) $(rnull_TARGET)

##### Official Mods #####

STD_MOD_LIBS	= $(game_TARGET) thirdparty/KopiLua.dll thirdparty/NLua.dll
STD_MOD_DEPS	= $(STD_MOD_LIBS) $(ralint_TARGET)

# Red Alert
mod_ra_SRCS := $(shell find OpenRA.Mods.RA/ -iname '*.cs')
mod_ra_TARGET = $(OUTPUT_DIR)/OpenRA.Mods.RA.dll
mod_ra_KIND = library
mod_ra_DEPS = $(STD_MOD_DEPS) $(irc_TARGET)
mod_ra_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(irc_TARGET)
PROGRAMS += mod_ra
mod_ra: $(mod_ra_TARGET)

# Command and Conquer
mod_cnc_SRCS := $(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
mod_cnc_TARGET = $(OUTPUT_DIR)/OpenRA.Mods.Cnc.dll
mod_cnc_KIND = library
mod_cnc_DEPS = $(STD_MOD_DEPS) $(mod_ra_TARGET)
mod_cnc_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_ra_TARGET)
PROGRAMS += mod_cnc
mod_cnc: $(mod_cnc_TARGET)

# Dune 2000
mod_d2k_SRCS := $(shell find OpenRA.Mods.D2k/ -iname '*.cs')
mod_d2k_TARGET = $(OUTPUT_DIR)/OpenRA.Mods.D2k.dll
mod_d2k_KIND = library
mod_d2k_DEPS = $(STD_MOD_DEPS) $(mod_ra_TARGET) $(mod_cnc_TARGET)
mod_d2k_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_ra_TARGET)
PROGRAMS += mod_d2k
mod_d2k: $(mod_d2k_TARGET)

# Tiberian Sun
mod_ts_SRCS := $(shell find OpenRA.Mods.TS/ -iname '*.cs')
mod_ts_TARGET = $(OUTPUT_DIR)/OpenRA.Mods.TS.dll
mod_ts_KIND = library
mod_ts_DEPS = $(STD_MOD_DEPS) $(mod_ra_TARGET)
mod_ts_LIBS = $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_ra_TARGET)
PROGRAMS += mod_ts
mod_ts: $(mod_ts_TARGET)

##### Tools #####

# Map Editor
editor_SRCS := $(shell find OpenRA.Editor/ -iname '*.cs')
editor_TARGET = $(OUTPUT_DIR)/OpenRA.Editor.exe
editor_KIND = winexe
editor_DEPS = $(game_TARGET)
editor_LIBS = System.Windows.Forms.dll System.Data.dll System.Drawing.dll $(editor_DEPS) thirdparty/Eluant.dll
editor_EXTRA = -resource:$(OUTPUT_DIR)/OpenRA.Editor.Form1.resources -resource:$(OUTPUT_DIR)/OpenRA.Editor.MapSelect.resources
editor_FLAGS = -win32icon:OpenRA.Editor/OpenRA.Editor.Icon.ico

PROGRAMS += editor
$(OUTPUT_DIR)/OpenRA.Editor.MapSelect.resources:
	resgen2 OpenRA.Editor/MapSelect.resx $(OUTPUT_DIR)/OpenRA.Editor.MapSelect.resources 1> /dev/null
$(OUTPUT_DIR)/OpenRA.Editor.Form1.resources:
	resgen2 OpenRA.Editor/Form1.resx $(OUTPUT_DIR)/OpenRA.Editor.Form1.resources 1> /dev/null
editor: $(OUTPUT_DIR)/OpenRA.Editor.MapSelect.resources $(OUTPUT_DIR)/OpenRA.Editor.Form1.resources $(editor_TARGET)

# Analyses mod yaml for easy to detect errors
ralint_SRCS := $(shell find OpenRA.Lint/ -iname '*.cs')
ralint_TARGET = $(OUTPUT_DIR)/OpenRA.Lint.exe
ralint_KIND = exe
ralint_DEPS = $(game_TARGET)
ralint_LIBS = $(COMMON_LIBS) $(ralint_DEPS)
PROGRAMS += ralint
ralint: $(ralint_TARGET)

test:
	@echo "OpenRA.Lint: checking Red Alert mod MiniYAML..."
	@mono --debug $(OUTPUT_DIR)/OpenRA.Lint.exe --verbose ra
	@echo "OpenRA.Lint: checking Tiberian Dawn mod MiniYAML..."
	@mono --debug $(OUTPUT_DIR)/OpenRA.Lint.exe --verbose cnc
	@echo "OpenRA.Lint: checking Dune 2000 mod MiniYAML..."
	@mono --debug $(OUTPUT_DIR)/OpenRA.Lint.exe --verbose d2k
	@echo "OpenRA.Lint: checking Tiberian Sun mod MiniYAML..."
	@mono --debug $(OUTPUT_DIR)/OpenRA.Lint.exe --verbose ts

# Builds and exports tilesets from a bitmap
tsbuild_SRCS := $(shell find OpenRA.TilesetBuilder/ -iname '*.cs')
tsbuild_TARGET = $(OUTPUT_DIR)/OpenRA.TilesetBuilder.exe
tsbuild_KIND = winexe
tsbuild_DEPS = $(game_TARGET)
tsbuild_LIBS = $(COMMON_LIBS) $(tsbuild_DEPS) System.Windows.Forms.dll
tsbuild_EXTRA = -resource:$(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormBuilder.resources -resource:$(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormNew.resources -resource:$(OUTPUT_DIR)/OpenRA.TilesetBuilder.Surface.resources
PROGRAMS += tsbuild
$(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormBuilder.resources:
	resgen2 OpenRA.TilesetBuilder/FormBuilder.resx $(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormBuilder.resources 1> /dev/null
$(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormNew.resources:
	resgen2 OpenRA.TilesetBuilder/frmNew.resx $(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormNew.resources 1> /dev/null
$(OUTPUT_DIR)/OpenRA.TilesetBuilder.Surface.resources:
	resgen2 OpenRA.TilesetBuilder/Surface.resx $(OUTPUT_DIR)/OpenRA.TilesetBuilder.Surface.resources 1> /dev/null
tsbuild: $(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormBuilder.resources $(OUTPUT_DIR)/OpenRA.TilesetBuilder.FormNew.resources $(OUTPUT_DIR)/OpenRA.TilesetBuilder.Surface.resources $(tsbuild_TARGET)


##### Launchers / Utilities #####

crashdialog_SRCS := $(shell find OpenRA.CrashDialog/ -iname '*.cs')
crashdialog_TARGET = $(OUTPUT_DIR)/OpenRA.CrashDialog.exe
crashdialog_KIND = exe
crashdialog_DEPS = $(game_TARGET)
crashdialog_LIBS = $(COMMON_LIBS) $(crashdialog_DEPS) System.Windows.Forms.dll
crashdialog_FLAGS = -win32icon:OpenRA.Game/OpenRA.ico
PROGRAMS += crashdialog
crashdialog: $(crashdialog_TARGET)

# Backend for the launcher apps - queries game/mod info and applies actions to an install
utility_SRCS := $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET = $(OUTPUT_DIR)/OpenRA.Utility.exe
utility_KIND = exe
utility_DEPS = $(game_TARGET) $(mod_ra_TARGET)
utility_LIBS = $(COMMON_LIBS) $(utility_DEPS) thirdparty/ICSharpCode.SharpZipLib.dll
PROGRAMS += utility
utility: $(utility_TARGET)


# Patches binary headers to work around a mono bug
$(OUTPUT_DIR)/fixheader.exe: $(ENSURE_OUTPUT_DIR) packaging/fixheader.cs
	@echo CSC $(OUTPUT_DIR)/fixheader.exe
	@$(CSC) packaging/fixheader.cs $(CSFLAGS) -out:$(OUTPUT_DIR)/fixheader.exe -t:exe $(COMMON_LIBS:%=-r:%)

# Generate build rules for each target defined above in PROGRAMS
define BUILD_ASSEMBLY

$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS) $(ENSURE_OUTPUT_DIR) $(OUTPUT_DIR)/fixheader.exe
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_EXTRA) \
		$$($(1)_SRCS)
	@mono $(OUTPUT_DIR)/fixheader.exe $$(@) > /dev/null
	@test `echo $$(@) | sed 's/^.*\.//'` = "dll" && chmod a-x $$(@) || ``
	@$$($(1)_EXTRA_CMDS)
endef

$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))



########################## MAKE/INSTALL RULES ##########################
#
default: cli-dependencies core

core: game renderers mods utility ralint

tools: editor tsbuild crashdialog

package: dependencies core editor crashdialog docs version

mods: mod_ra mod_cnc mod_d2k mod_ts

all: cli-dependencies core tools

clean:
	@-$(RM_F) $(OUTPUT_DIR)/* ./OpenRA*/*.dll ./OpenRA*/*.mdb *.mdb mods/**/*.dll mods/**/*.mdb *.resources
	@-$(RM_RF) ./*/bin ./*/obj

distclean: clean

platformdeps = "linux"
ifeq ($(shell uname),Darwin)
	platformdeps = "osx"
endif

dependencies: cli-dependencies native-dependencies

$(ENSURE_OUTPUT_DIR):
	@mkdir -p $(OUTPUT_DIR)
	@touch $(ENSURE_OUTPUT_DIR)

cli-dependencies: $(ENSURE_OUTPUT_DIR)
	@ $(CP_R) thirdparty/*.dll $(OUTPUT_DIR)/
	@ $(CP_R) thirdparty/*.dll.config $(OUTPUT_DIR)/

native-dependencies: $(ENSURE_OUTPUT_DIR)
	@ $(CP_R) thirdparty/${platformdeps}/*.dll.config $(OUTPUT_DIR)/

version: mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/modchooser/mod.yaml
	@for i in $? ; do \
		awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $${i} > $${i}.tmp && \
		mv -f $${i}.tmp $${i} ; \
	done

# Documentation (d2k depends on all mod libraries)
docs: utility
	@mono --debug $(OUTPUT_DIR)/OpenRA.Utility.exe --docs d2k > DOCUMENTATION.md
	@mono --debug $(OUTPUT_DIR)/OpenRA.Utility.exe --lua-docs ra > Lua-API.md

install: install-core

install-all: install-core install-tools

install-linux-shortcuts: install-linux-scripts install-linux-icons install-linux-desktop

install-core: default
	@-echo "Installing OpenRA to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)/mods"
	@$(CP_R) mods/common "$(DATA_INSTALL_DIR)/mods/"
	@$(CP_R) mods/cnc "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_cnc_TARGET) "$(DATA_INSTALL_DIR)/mods/cnc"
	@$(CP_R) mods/ra "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_ra_TARGET) "$(DATA_INSTALL_DIR)/mods/ra"
	@$(CP_R) mods/d2k "$(DATA_INSTALL_DIR)/mods/"
	@$(INSTALL_PROGRAM) $(mod_d2k_TARGET) "$(DATA_INSTALL_DIR)/mods/d2k"
	@$(CP_R) mods/modchooser "$(DATA_INSTALL_DIR)/mods/"

	@$(INSTALL_DATA) "global mix database.dat" "$(DATA_INSTALL_DIR)/global mix database.dat"
	@$(INSTALL_DATA) "GeoLite2-Country.mmdb" "$(DATA_INSTALL_DIR)/GeoLite2-Country.mmdb"
	@$(INSTALL_DATA) AUTHORS "$(DATA_INSTALL_DIR)/AUTHORS"
	@$(INSTALL_DATA) COPYING "$(DATA_INSTALL_DIR)/COPYING"

	@$(CP_R) glsl "$(DATA_INSTALL_DIR)"
	@$(CP_R) lua "$(DATA_INSTALL_DIR)"
	@$(CP) *.ttf "$(DATA_INSTALL_DIR)"
	@$(CP) $(OUTPUT_DIR)/SDL2-CS* "$(DATA_INSTALL_DIR)"
	@$(CP) $(OUTPUT_DIR)/Eluant* "$(DATA_INSTALL_DIR)"
	@$(CP) $(OUTPUT_DIR)/SharpFont.dll.config "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/ICSharpCode.SharpZipLib.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/FuzzyLogicLibrary.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/SharpFont.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/Mono.Nat.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/KopiLua.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/NLua.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/MaxMind.Db.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/MaxMind.GeoIP2.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/Newtonsoft.Json.dll "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(OUTPUT_DIR)/RestSharp.dll "$(DATA_INSTALL_DIR)"

ifeq ($(shell uname),Linux)
	@$(CP) *.sh "$(DATA_INSTALL_DIR)"
endif

install-tools: tools
	@-echo "Installing OpenRA tools to $(DATA_INSTALL_DIR)"
	@$(INSTALL_DIR) "$(DATA_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) $(foreach prog,$(TOOLS),$($(prog)_TARGET)) "$(DATA_INSTALL_DIR)"

install-linux-icons:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/icons/"
	@$(CP_R) packaging/linux/hicolor/ "$(DESTDIR)$(datadir)/icons"

install-linux-desktop:
	@$(INSTALL_DIR) "$(DESTDIR)$(datadir)/applications"
	@$(INSTALL_DATA) packaging/linux/openra.desktop "$(DESTDIR)$(datadir)/applications"
	@$(INSTALL_DATA) packaging/linux/openra-editor.desktop "$(DESTDIR)$(datadir)/applications"

install-linux-scripts:
	@echo "#!/bin/sh" > openra
	@echo 'cd "$(gameinstalldir)"' >> openra
	@echo 'exec mono OpenRA.Game.exe "$$@"' >> openra
	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx openra "$(BIN_INSTALL_DIR)"
	@-$(RM) openra

	@echo "#!/bin/sh" >  openra-editor
	@echo 'cd "$(gameinstalldir)"' >> openra-editor
	@echo 'exec mono OpenRA.Editor.exe "$$@"' >> openra-editor
	@$(INSTALL_DIR) "$(BIN_INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -m +rx openra-editor "$(BIN_INSTALL_DIR)"
	@-$(RM) openra-editor

uninstall:
	@-$(RM_R) "$(DATA_INSTALL_DIR)"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra"
	@-$(RM_F) "$(BIN_INSTALL_DIR)/openra-editor"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/applications/openra-editor.desktop"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/16x16/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/32x32/apps/openra-editor.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/48x48/apps/openra-editor.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/64x64/apps/openra.png"
	@-$(RM_F) "$(DESTDIR)$(datadir)/icons/hicolor/128x128/apps/openra.png"

help:
	@echo to compile, run:
	@echo \ \ make
	@echo
	@echo to compile with development tools, run:
	@echo \ \ make all
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
	@echo \ \ make install-linux-shortcuts
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
