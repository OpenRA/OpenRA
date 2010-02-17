CSC     = gmcs

CSFLAGS  = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe

DEFINE  = DEBUG;TRACE

RESC       = resgen2

default: OpenRa.Game.exe

PROGRAMS	=	fileformats gl game

COMMON_LIBS	:= System.dll System.Core.dll System.Drawing.dll System.Xml.dll

fileformats_SRCS	:=	$(shell find OpenRa.FileFormats/ -iname '*.cs')
fileformats_TARGET	:=	OpenRa.FileFormats.dll
fileformats_KIND	:=	library
fileformats_LIBS	:=	$(COMMON_LIBS)
fileformats_DEPS	:=	
fileformats_FLAGS	:=	

gl_SRCS				:= $(shell find OpenRa.Gl/ -iname '*.cs')
gl_TARGET			:= OpenRa.Gl.dll
gl_KIND				:= library
gl_LIBS				:= $(COMMON_LIBS) System.Windows.Forms.dll System.Xml.Linq.dll \
						System.Data.DataSetExtensions.dll \
						System.Data.dll thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll \
						thirdparty/Tao/Tao.Platform.Windows.dll
gl_DEPS				:=	
gl_FLAGS			:=	

game_SRCS			:=	$(shell find OpenRa.Game/ -iname '*.cs')
game_TARGET			:= OpenRa.Game.exe
game_KIND			:= winexe
game_LIBS			:= $(COMMON_LIBS) System.Windows.Forms.dll $(fileformats_TARGET) $(gl_TARGET) \
						thirdparty/Tao/Tao.OpenAl.dll
game_RESOURCES		:= Resources
game_DEPS			:= $(fileformats_TARGET) $(gl_TARGET) $(game_RESOURCES)
game_FLAGS			:= -win32icon:OpenRa.Game/OpenRa.ico -platform:x86 \
						-res:OpenRa.Game/$(game_RESOURCES).resources,OpenRa.Resources.resources

$(game_RESOURCES): 
	$(RESC) OpenRa.Game/$(game_RESOURCES).resx

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
.PHONY: clean all default

clean: 
	@-rm *.exe *.dll *.mdb
