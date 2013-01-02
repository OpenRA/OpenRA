CSC         = gmcs
CSFLAGS     = -nologo -warn:4 -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE      = DEBUG;TRACE
COMMON_LIBS = System.dll System.Core.dll System.Drawing.dll System.Xml.dll thirdparty/ICSharpCode.SharpZipLib.dll thirdparty/FuzzyLogicLibrary.dll
PHONY       = core tools package all mods clean distclean

.SUFFIXES:
core: game renderers mods utility tsbuild
tools: editor ralint tsbuild
package: core editor
mods: mod_ra mod_cnc mod_d2k
all: core tools
clean: 
	@-rm -f *.exe *.dll *.mdb mods/**/*.dll mods/**/*.mdb *.resources
distclean: clean

#
# Core binaries
#
fileformats_SRCS	:= $(shell find OpenRA.FileFormats/ -iname '*.cs')
fileformats_TARGET	= OpenRA.FileFormats.dll
fileformats_KIND	= library
fileformats_LIBS	= $(COMMON_LIBS) thirdparty/Tao/Tao.Sdl.dll System.Windows.Forms.dll
PROGRAMS 			= fileformats
fileformats: $(fileformats_TARGET)

game_SRCS			:= $(shell find OpenRA.Game/ -iname '*.cs')
game_TARGET			= OpenRA.Game.exe
game_KIND			= winexe
game_DEPS			= $(fileformats_TARGET) 
game_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(game_DEPS) \
						thirdparty/Tao/Tao.OpenAl.dll thirdparty/Tao/Tao.FreeType.dll
game_FLAGS			= -win32icon:OpenRA.Game/OpenRA.ico
PROGRAMS 			+= game
game: $(game_TARGET)

#
# Renderer dlls
#
rsdl_SRCS			:= $(shell find OpenRA.Renderer.SdlCommon/ -iname '*.cs')
rsdl_TARGET			= OpenRA.Renderer.SdlCommon.dll
rsdl_KIND			= library
rsdl_DEPS			= $(fileformats_TARGET) $(game_TARGET)
rsdl_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
					$(rsdl_DEPS)

rcg_SRCS			:= $(shell find OpenRA.Renderer.Cg/ -iname '*.cs')
rcg_TARGET			= OpenRA.Renderer.Cg.dll
rcg_KIND			= library
rcg_DEPS			= $(fileformats_TARGET) $(game_TARGET) $(rsdl_TARGET)
rcg_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
					$(rcg_DEPS)

rgl_SRCS			:= $(shell find OpenRA.Renderer.Gl/ -iname '*.cs')
rgl_TARGET			= OpenRA.Renderer.Gl.dll
rgl_KIND			= library
rgl_DEPS			= $(fileformats_TARGET) $(game_TARGET) $(rsdl_TARGET)
rgl_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
					$(rgl_DEPS)

rnull_SRCS			:= $(shell find OpenRA.Renderer.Null/ -iname '*.cs')
rnull_TARGET			= OpenRA.Renderer.Null.dll
rnull_KIND			= library
rnull_DEPS			= $(fileformats_TARGET) $(game_TARGET)
rnull_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					$(rnull_DEPS)
PROGRAMS 			+= rcg rgl rnull rsdl
renderers: $(rcg_TARGET) $(rgl_TARGET) $(rnull_TARGET) $(rsdl_TARGET)

#
# Official Mods
#
# Red Alert

STD_MOD_LIBS	= $(fileformats_TARGET) $(game_TARGET)
STD_MOD_DEPS	= $(STD_MOD_LIBS) $(ralint_TARGET)

mod_ra_SRCS			:= $(shell find OpenRA.Mods.RA/ -iname '*.cs')
mod_ra_TARGET			= mods/ra/OpenRA.Mods.RA.dll
mod_ra_KIND			= library
mod_ra_DEPS			= $(STD_MOD_DEPS)
mod_ra_LIBS			= $(COMMON_LIBS) $(STD_MOD_LIBS)
mod_ra_EXTRA_CMDS		= mono --debug RALint.exe ra
PROGRAMS 			+= mod_ra
mod_ra: $(mod_ra_TARGET)

# Command and Conquer
mod_cnc_SRCS		:= $(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
mod_cnc_TARGET		= mods/cnc/OpenRA.Mods.Cnc.dll
mod_cnc_KIND		= library
mod_cnc_DEPS		= $(STD_MOD_DEPS) $(mod_ra_TARGET)
mod_cnc_LIBS		= $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_ra_TARGET)
mod_cnc_EXTRA_CMDS	= mono --debug RALint.exe cnc
PROGRAMS 		+= mod_cnc
mod_cnc: $(mod_cnc_TARGET)

# Dune 2000
mod_d2k_SRCS		:= $(shell find OpenRA.Mods.D2k/ -iname '*.cs')
mod_d2k_TARGET		= mods/d2k/OpenRA.Mods.D2k.dll
mod_d2k_KIND		= library
mod_d2k_DEPS		= $(STD_MOD_DEPS) $(mod_ra_TARGET) $(mod_cnc_TARGET) $(utility_TARGET)
mod_d2k_LIBS		= $(COMMON_LIBS) $(STD_MOD_LIBS) $(mod_ra_TARGET) $(utility_TARGET)
mod_d2k_EXTRA_CMDS	= mono --debug RALint.exe d2k
PROGRAMS 		+= mod_d2k
mod_d2k: $(mod_d2k_TARGET)

#
# Tools
#
# Map Editor
editor_SRCS			:= $(shell find OpenRA.Editor/ -iname '*.cs')
editor_TARGET			= OpenRA.Editor.exe
editor_KIND			= winexe
editor_DEPS			= $(fileformats_TARGET) $(game_TARGET)
editor_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll System.Data.dll $(editor_DEPS)
editor_EXTRA		= -resource:OpenRA.Editor.Form1.resources -resource:OpenRA.Editor.MapSelect.resources
editor_FLAGS		= -win32icon:OpenRA.Editor/OpenRA.Editor.Icon.ico

PROGRAMS 			+= editor
OpenRA.Editor.MapSelect.resources:
	resgen2 OpenRA.Editor/MapSelect.resx OpenRA.Editor.MapSelect.resources 1> /dev/null
OpenRA.Editor.Form1.resources:
	resgen2 OpenRA.Editor/Form1.resx OpenRA.Editor.Form1.resources 1> /dev/null
editor: OpenRA.Editor.MapSelect.resources OpenRA.Editor.Form1.resources $(editor_TARGET)

# Analyses mod yaml for easy to detect errors
ralint_SRCS			:= $(shell find RALint/ -iname '*.cs')
ralint_TARGET			= RALint.exe
ralint_KIND			= exe
ralint_DEPS			= $(fileformats_TARGET) $(game_TARGET)
ralint_LIBS			= $(COMMON_LIBS) $(ralint_DEPS)
ralint_EXTRA_CMDS		= cp thirdparty/FuzzyLogicLibrary.dll .
PROGRAMS 			+= ralint
ralint: $(ralint_TARGET)

# Builds and exports tilesets from a bitmap
tsbuild_SRCS		:= $(shell find OpenRA.TilesetBuilder/ -iname '*.cs')
tsbuild_TARGET		= OpenRA.TilesetBuilder.exe
tsbuild_KIND		= winexe
tsbuild_DEPS		= $(fileformats_TARGET) $(game_TARGET)
tsbuild_LIBS		= $(COMMON_LIBS) $(tsbuild_DEPS) System.Windows.Forms.dll
tsbuild_EXTRA		= -resource:OpenRA.TilesetBuilder.frmBuilder.resources -resource:OpenRA.TilesetBuilder.frmNew.resources -resource:OpenRA.TilesetBuilder.Surface.resources
PROGRAMS 			+= tsbuild
OpenRA.TilesetBuilder.frmBuilder.resources:
	resgen2 OpenRA.TilesetBuilder/frmBuilder.resx OpenRA.TilesetBuilder.frmBuilder.resources 1> /dev/null
OpenRA.TilesetBuilder.frmNew.resources:
	resgen2 OpenRA.TilesetBuilder/frmNew.resx OpenRA.TilesetBuilder.frmNew.resources 1> /dev/null
OpenRA.TilesetBuilder.Surface.resources:
	resgen2 OpenRA.TilesetBuilder/Surface.resx OpenRA.TilesetBuilder.Surface.resources 1> /dev/null
tsbuild: OpenRA.TilesetBuilder.frmBuilder.resources OpenRA.TilesetBuilder.frmNew.resources OpenRA.TilesetBuilder.Surface.resources $(tsbuild_TARGET)

#
# Launchers / Utilities
#
# Patches binary headers to work around a mono bug
fixheader.exe: packaging/fixheader.cs
	@echo CSC fixheader.exe
	@$(CSC) packaging/fixheader.cs $(CSFLAGS) -out:fixheader.exe -t:exe $(COMMON_LIBS:%=-r:%)

# Backend for the launcher apps - queries game/mod info and applies actions to an install
utility_SRCS		:= $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET		= OpenRA.Utility.exe
utility_KIND		= exe
utility_DEPS        = $(fileformats_TARGET) $(game_TARGET)
utility_LIBS        = $(COMMON_LIBS) $(utility_DEPS) thirdparty/ICSharpCode.SharpZipLib.dll System.Windows.Forms.dll
PROGRAMS 			+= utility
utility: $(utility_TARGET)

.PHONY: $(PHONY) $(PROGRAMS)

#
# Generate build rules for each target defined above in PROGRAMS
#
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


#
# Install / Uninstall for *nix
#
prefix ?= /usr/local
datarootdir ?= $(prefix)/share
datadir ?= $(datarootdir)
bindir ?= $(prefix)/bin
BIN_INSTALL_DIR = $(DESTDIR)$(bindir)
INSTALL_DIR = $(DESTDIR)$(datadir)/openra
INSTALL = install
INSTALL_PROGRAM = $(INSTALL)
CORE = fileformats rcg rgl rsdl rnull game editor utility tsbuild

install: all
	@-echo "Installing OpenRA to $(INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/cnc
	@$(INSTALL_PROGRAM) $(mod_cnc_TARGET) $(INSTALL_DIR)/mods/cnc
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/ra
	@$(INSTALL_PROGRAM) $(mod_ra_TARGET) $(INSTALL_DIR)/mods/ra
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/d2k
	@$(INSTALL_PROGRAM) $(mod_d2k_TARGET) $(INSTALL_DIR)/mods/d2k

	@-cp $(foreach f,$(shell ls mods/cnc --hide=*.dll),mods/cnc/$(f)) $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/maps $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/chrome $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/bits $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/rules $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/sequences $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/tilesets $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/uibits $(INSTALL_DIR)/mods/cnc

	@-cp $(foreach f,$(shell ls mods/ra --hide=*.dll),mods/ra/$(f)) $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/maps $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/bits $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/chrome $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/rules $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/tilesets $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/uibits $(INSTALL_DIR)/mods/ra
	
	@-cp $(foreach f,$(shell ls mods/d2k --hide=*.dll),mods/d2k/$(f)) $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/maps $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/bits $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/chrome $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/rules $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/tilesets $(INSTALL_DIR)/mods/d2k
	@cp -r mods/d2k/uibits $(INSTALL_DIR)/mods/d2k

	@cp -r glsl $(INSTALL_DIR)
	@cp -r cg $(INSTALL_DIR)
	@cp *.ttf $(INSTALL_DIR)
	@cp thirdparty/Tao/* $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) thirdparty/ICSharpCode.SharpZipLib.dll $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) thirdparty/FuzzyLogicLibrary.dll $(INSTALL_DIR)

	@echo "#!/bin/sh" > openra
	@echo "cd "$(datadir)"/openra" >> openra
	@echo "exec mono "$(datadir)"/openra/OpenRA.Game.exe \"$$""@\"" >> openra

	@echo "#!/bin/sh" > openra-editor
	@echo "cd "$(datadir)"/openra" >> openra-editor
	@echo "exec mono "$(datadir)"/openra/OpenRA.Editor.exe \"$$""@\"" >> openra-editor

	@$(INSTALL_PROGRAM) -d $(BIN_INSTALL_DIR)
	@$(INSTALL_PROGRAM) -m +rx openra $(BIN_INSTALL_DIR)
	@$(INSTALL_PROGRAM) -m +rx openra-editor $(BIN_INSTALL_DIR)

uninstall:
	@-rm -r $(INSTALL_DIR)
	@-rm $(DESTDIR)$(bindir)/openra
	@-rm $(DESTDIR)$(bindir)/openra-editor
