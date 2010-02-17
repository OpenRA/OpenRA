CSC     = gmcs
CFLAGS  = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE  = DEBUG;TRACE
PROGRAM = OpenRa.FileFormats.dll
LIBS    = System.dll \
          System.Core.dll \
          System.Drawing.dll \
          System.Xml.dll
TYPE    = library
DIRS	= . \
          Support \
          Collections \
          Properties

refs := $(foreach ref, $(LIBS),-r:$(ref))
files := $(foreach dir, $(DIRS),$(wildcard $(dir)/*.cs))

$(PROGRAM) :
	$(CSC) $(refs) "-out:$(PROGRAM)" $(CFLAGS) "-define:$(DEFINE)" -t:$(TYPE) $(files)
