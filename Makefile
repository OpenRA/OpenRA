CSC     = gmcs
CSFLAGS  = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE  = DEBUG;TRACE
PROGRAMS	=fileformats gl game ra cnc seqed editor ralint filex tsbuild utility
prefix = /usr/local
datarootdir = $(prefix)/share
datadir = $(datarootdir)
bindir = $(prefix)/bin
BIN_INSTALL_DIR = $(DESTDIR)$(bindir)
INSTALL_DIR = $(DESTDIR)$(datadir)/openra
INSTALL = install
INSTALL_PROGRAM = $(INSTALL)

COMMON_LIBS	= System.dll System.Core.dll System.Drawing.dll System.Xml.dll

fileformats_SRCS	=	$(shell find OpenRA.FileFormats/ -iname '*.cs')
fileformats_TARGET	=	OpenRA.FileFormats.dll
fileformats_KIND	=	library
fileformats_LIBS	=	$(COMMON_LIBS) thirdparty/Tao/Tao.Sdl.dll thirdparty/WindowsBase.dll

gl_SRCS				= $(shell find OpenRA.Gl/ -iname '*.cs')
gl_TARGET			= OpenRA.Gl.dll
gl_KIND				= library
gl_DEPS				= $(fileformats_TARGET) $(game_TARGET)
gl_LIBS				= $(COMMON_LIBS) System.Windows.Forms.dll \
						thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
						$(gl_DEPS) $(game_TARGET)

game_SRCS			=	$(shell find OpenRA.Game/ -iname '*.cs')
game_TARGET			= OpenRA.Game.exe
game_KIND			= winexe
game_DEPS			= $(fileformats_TARGET) 
game_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(game_DEPS) \
						thirdparty/Tao/Tao.OpenAl.dll thirdparty/Tao/Tao.FreeType.dll
game_FLAGS			= -win32icon:OpenRA.Game/OpenRA.ico

ra_SRCS				=	$(shell find OpenRA.Mods.RA/ -iname '*.cs')
ra_TARGET			=	mods/ra/OpenRA.Mods.RA.dll
ra_KIND				=	library
ra_DEPS				= $(fileformats_TARGET) $(game_TARGET)
ra_LIBS				= $(COMMON_LIBS) $(ra_DEPS)

cnc_SRCS			=	$(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
cnc_TARGET			=	mods/cnc/OpenRA.Mods.Cnc.dll
cnc_KIND			=	library
cnc_DEPS			= $(fileformats_TARGET) $(game_TARGET) $(ra_TARGET)
cnc_LIBS			= $(COMMON_LIBS) $(cnc_DEPS)

seqed_SRCS			= $(shell find SequenceEditor/ -iname '*.cs')
seqed_TARGET		= SequenceEditor.exe
seqed_KIND			= winexe
seqed_DEPS			= $(fileformats_TARGET)
seqed_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(seqed_DEPS)
seqed_EXTRA			= -resource:SequenceEditor.Form1.resources

editor_SRCS			= $(shell find OpenRA.Editor/ -iname '*.cs')
editor_TARGET		= OpenRA.Editor.exe
editor_KIND			= winexe
editor_DEPS			= $(fileformats_TARGET) $(game_TARGET)
editor_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll System.Data.dll $(editor_DEPS)
editor_EXTRA		= -resource:OpenRA.Editor.Form1.resources -resource:OpenRA.Editor.MapSelect.resources

ralint_SRCS		= $(shell find RALint/ -iname '*.cs')
ralint_TARGET	= RALint.exe
ralint_KIND		= exe
ralint_DEPS		= $(fileformats_TARGET) $(game_TARGET)
ralint_LIBS		= $(COMMON_LIBS) $(ralint_DEPS)

filex_SRCS		= $(shell find FileExtractor/ -iname '*.cs')
filex_TARGET	= FileExtractor.exe
filex_KIND		= exe
filex_DEPS		= $(fileformats_TARGET)
filex_LIBS		= $(COMMON_LIBS) $(filex_DEPS)

tsbuild_SRCS		= $(shell find OpenRA.TilesetBuilder/ -iname '*.cs')
tsbuild_TARGET		= TilesetBuilder.exe
tsbuild_KIND		= winexe
tsbuild_DEPS		= $(fileformats_TARGET) $(game_TARGET)
tsbuild_LIBS		= $(COMMON_LIBS) $(tsbuild_DEPS) System.Windows.Forms.dll
tsbuild_EXTRA		= -resource:OpenRA.TilesetBuilder.Form1.resources

utility_SRCS		= $(shell find OpenRA.Utility/ -iname '*.cs')
utility_TARGET		= OpenRA.Utility.exe
utility_KIND		= exe
utility_DEPS        = $(fileformats_TARGET) thirdparty/ICSharpCode.SharpZipLib.dll
utility_LIBS        = $(COMMON_LIBS) $(utility_DEPS)


# -platform:x86

.SUFFIXES:
.PHONY: clean all game tool default mods mod_ra mod_cnc install uninstall editor_res editor tsbuild ralint seqed filex utility

game: $(fileformats_TARGET) $(gl_TARGET) $(game_TARGET) $(ra_TARGET) $(cnc_TARGET) $(utility_TARGET)

clean: 
	@-rm *.exe *.dll *.mdb mods/**/*.dll mods/**/*.mdb *.resources

distclean: clean

CORE = fileformats gl game editor utility

install: all
	@-echo "Installing OpenRA to $(INSTALL_DIR)"
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) $(foreach prog,$(CORE),$($(prog)_TARGET)) $(INSTALL_DIR)
		
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/cnc
	@$(INSTALL_PROGRAM) $(cnc_TARGET) $(INSTALL_DIR)/mods/cnc
	@-cp $(foreach f,$(shell ls mods/cnc --hide=*.dll),mods/cnc/$(f)) $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/maps $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/chrome $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/bits $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/rules $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/sequences $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/tilesets $(INSTALL_DIR)/mods/cnc
	@cp -r mods/cnc/uibits $(INSTALL_DIR)/mods/cnc
	
	@$(INSTALL_PROGRAM) -d $(INSTALL_DIR)/mods/ra
	@$(INSTALL_PROGRAM) $(ra_TARGET) $(INSTALL_DIR)/mods/ra
	@-cp $(foreach f,$(shell ls mods/ra --hide=*.dll),mods/ra/$(f)) $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/maps $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/bits $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/chrome $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/rules $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/tilesets $(INSTALL_DIR)/mods/ra
	@cp -r mods/ra/uibits $(INSTALL_DIR)/mods/ra
	
	@cp -r shaders $(INSTALL_DIR)
	@cp *.ttf $(INSTALL_DIR)
	@cp --parents -r thirdparty/Tao $(INSTALL_DIR)
	@$(INSTALL_PROGRAM) thirdparty/WindowsBase.dll $(INSTALL_DIR)
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

mod_ra: $(ra_TARGET) $(ralint_TARGET)
	mono RALint.exe ra
mod_cnc: $(cnc_TARGET) $(ralint_TARGET)
	mono RALint.exe cnc
mods: mod_ra mod_cnc

OpenRA.Editor.MapSelect.resources:
	resgen2 OpenRA.Editor/MapSelect.resx OpenRA.Editor.MapSelect.resources 1> /dev/null

OpenRA.Editor.Form1.resources:
	resgen2 OpenRA.Editor/Form1.resx OpenRA.Editor.Form1.resources 1> /dev/null
editor: OpenRA.Editor.MapSelect.resources OpenRA.Editor.Form1.resources $(editor_TARGET)
ralint: $(ralint_TARGET)
seqed: SequenceEditor.Form1.resources $(seqed_TARGET)
SequenceEditor.Form1.resources:
	resgen2 SequenceEditor/Form1.resx SequenceEditor.Form1.resources 1> /dev/null
filex: $(filex_TARGET)
tsbuild: OpenRA.TilesetBuilder.Form1.resources $(tsbuild_TARGET)
OpenRA.TilesetBuilder.Form1.resources:
	resgen2 OpenRA.TilesetBuilder/Form1.resx OpenRA.TilesetBuilder.Form1.resources 1> /dev/null
tools: editor ralint seqed filex tsbuild
all: game tools

fixheader: packaging/fixheader.cs
	@$(CSC) packaging/fixheader.cs $(CSFLAGS) -out:fixheader.exe -t:exe $(COMMON_LIBS:%=-r:%)

define BUILD_ASSEMBLY

$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS) fixheader
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_EXTRA) \
		$$($(1)_SRCS)
	@mono fixheader.exe $$(@)
endef

$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))
