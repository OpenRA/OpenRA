CSC         = gmcs
CSFLAGS     = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE      = DEBUG;TRACE
COMMON_LIBS	= System.dll System.Core.dll System.Drawing.dll System.Xml.dll
PHONY		= core tools package all mods clean distclean

.SUFFIXES:
core: game renderers mod_ra mod_cnc
tools: editor ralint seqed filex tsbuild utility
package: fixheader core editor utility
mods: mod_ra mod_cnc
all: core tools winlaunch
clean: 
	@-rm *.exe *.dll *.mdb mods/**/*.dll mods/**/*.mdb *.resources
distclean: clean

#
# Core binaries
#
fileformats_SRCS	= $(shell find OpenRA.FileFormats/ -iname '*.cs')
fileformats_TARGET	= OpenRA.FileFormats.dll
#fileformats_DEPS	= fixheader
fileformats_KIND	= library
fileformats_LIBS	= $(COMMON_LIBS) thirdparty/Tao/Tao.Sdl.dll System.Windows.Forms.dll thirdparty/ICSharpCode.SharpZipLib.dll
PROGRAMS 			= fileformats
fileformats: $(fileformats_TARGET)

game_SRCS			= $(shell find OpenRA.Game/ -iname '*.cs')
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
rcg_SRCS			= $(shell find OpenRA.Renderer.Cg/ -iname '*.cs')
rcg_TARGET			= OpenRA.Renderer.Cg.dll
rcg_KIND			= library
rcg_DEPS			= $(fileformats_TARGET) $(game_TARGET)
rcg_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
					$(rcg_DEPS) $(game_TARGET)

rgl_SRCS			= $(shell find OpenRA.Renderer.Gl/ -iname '*.cs')
rgl_TARGET			= OpenRA.Renderer.Gl.dll
rgl_KIND			= library
rgl_DEPS			= $(fileformats_TARGET) $(game_TARGET)
rgl_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
					$(rgl_DEPS) $(game_TARGET)

rnull_SRCS			= $(shell find OpenRA.Renderer.Null/ -iname '*.cs')
rnull_TARGET		= OpenRA.Renderer.Null.dll
rnull_KIND			= library
rnull_DEPS			= $(fileformats_TARGET) $(game_TARGET)
rnull_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll \
					$(rnull_DEPS) $(game_TARGET)
PROGRAMS 			+= rcg rgl rnull
renderers: $(rcg_TARGET) $(rgl_TARGET) $(rnull_TARGET)

#
# Official Mods
#
# Red Alert
mod_ra_SRCS			= $(shell find OpenRA.Mods.RA/ -iname '*.cs')
mod_ra_TARGET		= mods/ra/OpenRA.Mods.RA.dll
mod_ra_KIND			= library
mod_ra_DEPS			= $(fileformats_TARGET) $(game_TARGET)
mod_ra_LIBS			= $(COMMON_LIBS) $(mod_ra_DEPS)
PROGRAMS 			+= mod_ra
mod_ra: $(mod_ra_TARGET)
#	mono RALint.exe ra

# Command and Conquer
mod_cnc_SRCS		= $(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
mod_cnc_TARGET		= mods/cnc/OpenRA.Mods.Cnc.dll
mod_cnc_KIND		= library
mod_cnc_DEPS		= $(fileformats_TARGET) $(game_TARGET) $(mod_ra_TARGET)
mod_cnc_LIBS		= $(COMMON_LIBS) $(mod_cnc_DEPS)
PROGRAMS 			+= mod_cnc
mod_cnc: $(mod_cnc_TARGET)
#	mono RALint.exe cnc

#
# Tools
#
# Sequence editor (defunct)
seqed_SRCS			= $(shell find SequenceEditor/ -iname '*.cs')
seqed_TARGET		= SequenceEditor.exe
seqed_KIND			= winexe
seqed_DEPS			= $(fileformats_TARGET)
seqed_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(seqed_DEPS)
seqed_EXTRA			= -resource:SequenceEditor.Form1.resources
PROGRAMS 			+= seqed
SequenceEditor.Form1.resources:
	resgen2 SequenceEditor/Form1.resx SequenceEditor.Form1.resources 1> /dev/null
seqed: SequenceEditor.Form1.resources $(seqed_TARGET)

# Map Editor
editor_SRCS			= $(shell find OpenRA.Editor/ -iname '*.cs')
editor_TARGET		= OpenRA.Editor.exe
editor_KIND			= winexe
editor_DEPS			= $(fileformats_TARGET) $(game_TARGET)
editor_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll System.Data.dll $(editor_DEPS)
editor_EXTRA		= -resource:OpenRA.Editor.Form1.resources -resource:OpenRA.Editor.MapSelect.resources
PROGRAMS 			+= editor
OpenRA.Editor.MapSelect.resources:
	resgen2 OpenRA.Editor/MapSelect.resx OpenRA.Editor.MapSelect.resources 1> /dev/null
OpenRA.Editor.Form1.resources:
	resgen2 OpenRA.Editor/Form1.resx OpenRA.Editor.Form1.resources 1> /dev/null
editor: OpenRA.Editor.MapSelect.resources OpenRA.Editor.Form1.resources $(editor_TARGET)

# Analyses mod yaml for easy to detect errors
ralint_SRCS			= $(shell find RALint/ -iname '*.cs')
ralint_TARGET		= RALint.exe
ralint_KIND			= exe
ralint_DEPS			= $(fileformats_TARGET) $(game_TARGET)
ralint_LIBS			= $(COMMON_LIBS) $(ralint_DEPS)
PROGRAMS 			+= ralint
ralint: $(ralint_TARGET)

# Extracts files from packages (mixfiles, zips, etc)
filex_SRCS			= $(shell find FileExtractor/ -iname '*.cs')
filex_TARGET		= FileExtractor.exe
filex_KIND			= exe
filex_DEPS			= $(fileformats_TARGET)
filex_LIBS			= $(COMMON_LIBS) $(filex_DEPS)
PROGRAMS 			+= filex
filex: $(filex_TARGET)

# Builds and exports tilesets from a bitmap
tsbuild_SRCS		= $(shell find OpenRA.TilesetBuilder/ -iname '*.cs')
tsbuild_TARGET		= TilesetBuilder.exe
tsbuild_KIND		= winexe
tsbuild_DEPS		= $(fileformats_TARGET) $(game_TARGET)
tsbuild_LIBS		= $(COMMON_LIBS) $(tsbuild_DEPS) System.Windows.Forms.dll
tsbuild_EXTRA		= -resource:OpenRA.TilesetBuilder.Form1.resources
PROGRAMS 			+= tsbuild
OpenRA.TilesetBuilder.Form1.resources:
	resgen2 OpenRA.TilesetBuilder/Form1.resx OpenRA.TilesetBuilder.Form1.resources 1> /dev/null
tsbuild: OpenRA.TilesetBuilder.Form1.resources $(tsbuild_TARGET)

#
# Launchers / Utilities
#
# Patches binary headers to work around a mono bug
fixheader: packaging/fixheader.cs
	@$(CSC) packaging/fixheader.cs $(CSFLAGS) -out:fixheader.exe -t:exe $(COMMON_LIBS:%=-r:%)
PHONY += fixheader

# Backend for the launcher apps - queries game/mod info and applies actions to an install
utility_SRCS		= $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET		= OpenRA.Utility.exe
utility_KIND		= exe
utility_DEPS        = $(fileformats_TARGET) thirdparty/ICSharpCode.SharpZipLib.dll
utility_LIBS        = $(COMMON_LIBS) $(utility_DEPS)
PROGRAMS 			+= utility
utility: $(utility_TARGET)

# Windows launcher
winlaunch_SRCS		= $(shell find OpenRA.Launcher/ -iname '*.cs')
winlaunch_TARGET	= OpenRA.Launcher.exe
winlaunch_KIND		= winexe
winlaunch_LIBS		= $(COMMON_LIBS) System.Windows.Forms.dll
winlaunch_EXTRA		= -resource:OpenRA.Launcher.Launcher.resources
PROGRAMS 			+= winlaunch
OpenRA.Launcher.Launcher.resources:
	resgen2 OpenRA.Launcher/Launcher.resx OpenRA.Launcher.Launcher.resources 1> /dev/null
winlaunch: OpenRA.Launcher.Launcher.resources $(winlaunch_TARGET)

.PHONY: $(PHONY) $(PROGRAMS)

#
# Generate build rules for each target defined above in PROGRAMS
#
define BUILD_ASSEMBLY

$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS)
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_EXTRA) \
		$$($(1)_SRCS)
	@test -e fixheader.exe && mono fixheader.exe $$(@) || ``
endef

$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))


#
# Install / Uninstall for *nix
#
prefix = /usr/local
datarootdir = $(prefix)/share
datadir = $(datarootdir)
bindir = $(prefix)/bin
BIN_INSTALL_DIR = $(DESTDIR)$(bindir)
INSTALL_DIR = $(DESTDIR)$(datadir)/openra
INSTALL = install
INSTALL_PROGRAM = $(INSTALL)
CORE = fileformats rcg rgl rnull game editor utility winlaunch
install: all
	@-echo "Installing OpenRA to $(INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) $(INSTALL_DIR)
		
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/cnc
	@$(INSTALL_PROGRAM) $(mod_cnc_TARGET) $(INSTALL_DIR)/mods/cnc
	@-cp $(foreach f,$(shell ls mods/cnc --hide=*.dll),mods/cnc/$(f)) $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/maps $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/chrome $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/bits $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/rules $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/sequences $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/tilesets $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/uibits $(INSTALL_DIR)/mods/cnc
	
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/ra
	@$(INSTALL_PROGRAM) $(mod_ra_TARGET) $(INSTALL_DIR)/mods/ra
	@-cp $(foreach f,$(shell ls mods/ra --hide=*.dll),mods/ra/$(f)) $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/maps $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/bits $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/chrome $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/rules $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/tilesets $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/uibits $(INSTALL_DIR)/mods/ra
	
	@cp -r glsl $(INSTALL_DIR)
	@cp -r cg $(INSTALL_DIR)
	@cp *.ttf $(INSTALL_DIR)
	@cp --parents -r thirdparty/Tao $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) thirdparty/ICSharpCode.SharpZipLib.dll $(INSTALL_DIR)
	@-$(INSTALL_PROGRAM) VERSION $(INSTALL_DIR)
	
	@echo "#!/bin/sh" > openra
	@echo "cd "$(datadir)"/openra" >> openra
	@echo "mono "$(datadir)"/openra/OpenRA.Game.exe SupportDir=~/.openra \"$$""@\"" >> openra
	@$(INSTALL_PROGRAM) -d $(BIN_INSTALL_DIR)
	@$(INSTALL_PROGRAM) -m +rx openra $(BIN_INSTALL_DIR)
	
	@echo "OpenRA is now installed. You will now want to download"
	@echo "http://open-ra.org/get-dependency.php?file=ra-packages and"
	@echo "http://open-ra.org/get-dependency.php?file=cnc-packages"
	@echo "and extract their contents to"
	@echo "$(INSTALL_DIR)/mods/ra/packages and "
	@echo "$(INSTALL_DIR)/mods/cnc/packages respectively."
	@echo "It is also advised to install the contents of $(INSTALL_DIR)/thirdparty to the Mono Global Assembly Cache \
	with gacutil."

uninstall:
	@-rm -r $(INSTALL_DIR)
	@-rm $(DESTDIR)$(bindir)/openra