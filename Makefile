CSC        = gmcs
RESC       = resgen2
CSFLAGS	   = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINES    = DEBUG;TRACE
OUTDIR     = bin



FF_defines = $(DEFINES)
FF_prefix  = OpenRa.FileFormats
FF_flags   = $(CSFLAGS)
FF_program = OpenRa.FileFormats.dll
FF_libs    = System.dll \
             System.Core.dll \
             System.Drawing.dll \
             System.Xml.dll
FF_type    = library
FF_dirs    = . \
             Support \
             Collections \
             Properties
FF_outpath = $(OUTDIR)/$(FF_program)
FF_refs := $(patsubst %,-r:%, $(FF_libs))
FF_files := $(foreach dir, $(FF_dirs),$(wildcard $(FF_prefix)/$(dir)/*.cs))



GL_defines = $(DEFINES)
GL_prefix  = OpenRa.Gl
GL_flags   = $(CSFLAGS)
GL_program = OpenRa.Gl.dll
GL_libs    = System.dll \
             System.Core.dll \
             System.Drawing.dll \
             System.Windows.Forms.dll \
             System.Xml.Linq.dll \
             System.Data.DataSetExtensions.dll \
             System.Data.dll \
             System.Xml.dll \
             thirdparty/Tao/Tao.Cg.dll \
             thirdparty/Tao/Tao.OpenGl.dll \
             thirdparty/Tao/Tao.Platform.Windows.dll
GL_type    = library
GL_dirs    = . \
             Properties
GL_outpath = $(OUTDIR)/$(GL_program)
GL_refs := $(patsubst %,-r:%, $(GL_libs))
GL_files := $(foreach dir, $(GL_dirs),$(wildcard $(GL_prefix)/$(dir)/*.cs))



RA_defines = $(DEFINES);SANITY_CHECKS
RA_prefix  = OpenRa.Game
RA_flags   = $(CSFLAGS) -win32icon:$(RA_prefix)/OpenRa.ico -platform:x86
RA_program = OpenRa.Game.exe
RA_libs    = System.dll \
             System.Core.dll \
             System.Drawing.dll \
             System.Data.dll \
             System.Windows.Forms.dll \
             System.Xml.dll \
             bin/OpenRa.FileFormats.dll \
             bin/OpenRa.Gl.dll \
             thirdparty/Tao/Tao.OpenAl.dll
RA_type    = winexe
RA_dirs    = . \
             Effects \
             GameRules \
             Graphics \
             Network \
             Orders \
             Properties \
             Support \
             Traits \
             Traits/Activities \
             Traits/AI \
             Traits/Attack \
             Traits/Modifiers \
             Traits/Player \
             Traits/Render \
             Traits/SupportPowers \
             Traits/World
RA_outpath = $(OUTDIR)/$(RA_program)
RA_refs := $(patsubst %,-r:%, $(RA_libs))
RA_files := $(foreach dir, $(RA_dirs),$(wildcard $(RA_prefix)/$(dir)/*.cs))
RA_resources = Resources
RA_resources_path = $(RA_prefix)/$(RA_resources)




all: $(FF_program) $(GL_program) $(RA_program)

$(FF_program) : $(FF_files)
	$(CSC) $(FF_refs) "-out:$(FF_outpath)" $(FF_flags) "-define:$(FF_defines)" -t:$(FF_type) $(FF_files)

$(GL_program) : $(GL_files)
	$(CSC) $(GL_refs) "-out:$(GL_outpath)" $(GL_flags) "-define:$(GL_defines)" -t:$(GL_type) $(GL_files)
	cp thirdparty/Tao/Tao.OpenGl.dll bin/
	cp thirdparty/Tao/Tao.Cg.dll bin/
	cp thirdparty/Tao/Tao.Platform.Windows.dll bin/

$(RA_resources) : 
	$(RESC) $(RA_resources_path).resx

$(RA_program) : $(RA_files) $(RA_resources) $(FF_program) $(GL_program)
	$(CSC) $(RA_refs) "-out:$(RA_outpath)" $(RA_flags) "-define:$(RA_defines)" -t:$(RA_type) -res:$(RA_resources_path).resources,OpenRa.Resources.resources $(RA_files)
	cp thirdparty/Tao/Tao.OpenAl.dll bin/
