CSC     = gmcs
CSFLAGS  = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE  = DEBUG;TRACE

PROGRAMS	=	fileformats gl game ra cnc aftermath server seqed

COMMON_LIBS	:= System.dll System.Core.dll System.Drawing.dll System.Xml.dll

fileformats_SRCS	:=	$(shell find OpenRa.FileFormats/ -iname '*.cs')
fileformats_TARGET	:=	OpenRa.FileFormats.dll
fileformats_KIND	:=	library
fileformats_LIBS	:=	$(COMMON_LIBS)

gl_SRCS				:= $(shell find OpenRa.Gl/ -iname '*.cs')
gl_TARGET			:= OpenRa.Gl.dll
gl_KIND				:= library
gl_LIBS				:= $(COMMON_LIBS) System.Windows.Forms.dll \
						thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll \
						thirdparty/Tao/Tao.Platform.Windows.dll

game_SRCS			:=	$(shell find OpenRa.Game/ -iname '*.cs')
game_TARGET			:= OpenRa.Game.exe
game_KIND			:= winexe
game_LIBS			:= $(COMMON_LIBS) System.Windows.Forms.dll $(fileformats_TARGET) $(gl_TARGET) \
						thirdparty/Tao/Tao.OpenAl.dll
game_DEPS			:= $(fileformats_TARGET) $(gl_TARGET)
game_FLAGS			:= -win32icon:OpenRa.Game/OpenRa.ico

ra_SRCS				:=	$(shell find OpenRa.Mods.RA/ -iname '*.cs')
ra_TARGET			:=	mods/ra/OpenRa.Mods.RA.dll
ra_KIND				:=	library
ra_LIBS				:= $(COMMON_LIBS) $(fileformats_TARGET) $(game_TARGET)
ra_DEPS				:= $(fileformats_TARGET) $(game_TARGET)

cnc_SRCS			:=	$(shell find OpenRa.Mods.Cnc/ -iname '*.cs')
cnc_TARGET			:=	mods/cnc/OpenRa.Mods.Cnc.dll
cnc_KIND			:=	library
cnc_LIBS			:= $(COMMON_LIBS) $(fileformats_TARGET) $(game_TARGET)
cnc_DEPS			:= $(fileformats_TARGET) $(game_TARGET)

aftermath_SRCS		:=	$(shell find OpenRa.Mods.Aftermath/ -iname '*.cs')
aftermath_TARGET	:=	mods/cnc/OpenRa.Mods.Aftermath.dll
aftermath_KIND		:=	library
aftermath_LIBS		:= $(COMMON_LIBS) $(fileformats_TARGET) $(game_TARGET)
aftermath_DEPS		:= $(fileformats_TARGET) $(game_TARGET)

server_SRCS			:= $(shell find OpenRA.Server/ -iname '*.cs')
server_TARGET		:= OpenRA.Server.exe
server_KIND			:= winexe
server_LIBS			:= $(COMMON_LIBS) $(fileformats_TARGET)
server_DEPS			:= $(fileformats_TARGET)

seqed_SRCS			:= $(shell find SequenceEditor/ -iname '*.cs')
seqed_TARGET		:= SequenceEditor.exe
seqed_KIND			:= winexe
seqed_LIBS			:= $(COMMON_LIBS) System.Windows.Forms.dll $(fileformats_TARGET)
seqed_DEPS			:= $(fileformats_TARGET)

# -platform:x86

define BUILD_ASSEMBLY
$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS)
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_SRCS)
endef

$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))

.SUFFIXES:
.PHONY: clean all default mods server seqed

clean: 
	@-rm *.exe *.dll *.mdb mods/**/*.dll mods/**/*.mdb

mods: $(ra_TARGET) $(cnc_TARGET) $(aftermath_TARGET)

server: $(server_TARGET)
seqed: $(seqed_TARGET)
all: clean server default mods seqed

.DEFAULT: all

