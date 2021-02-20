############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make [DEBUG=true]
#
# to compile using system libraries for native dependencies, run:
#   make [DEBUG=true] TARGETPLATFORM=unix-generic
#
# to check the official mods for erroneous yaml files, run:
#   make test
#
# to check the engine and official mod dlls for code style violations, run:
#   make check
#
# to compile and install Red Alert, Tiberian Dawn, and Dune 2000, run:
#   make [prefix=/foo] [bindir=/bar/bin] install
#
# to compile and install Red Alert, Tiberian Dawn, and Dune 2000
# using system libraries for native dependencies, run:
#   make [prefix=/foo] [bindir=/bar/bin] TARGETPLATFORM=unix-generic install
#
# to install Linux startup scripts, desktop files, icons, and MIME metadata
#   make install-linux-shortcuts
#
# to install Linux AppStream metadata
#   make install-linux-appdata
#
# for help, run:
#   make help
#

############################## TOOLCHAIN ###############################
#
# List of .NET assemblies that we can guarantee exist
WHITELISTED_OPENRA_ASSEMBLIES = OpenRA.exe OpenRA.Utility.exe OpenRA.Server.exe OpenRA.Platforms.Default.dll OpenRA.Game.dll OpenRA.Mods.Common.dll OpenRA.Mods.Cnc.dll OpenRA.Mods.D2k.dll

# These are explicitly shipped alongside our core files by the packaging script
WHITELISTED_THIRDPARTY_ASSEMBLIES = ICSharpCode.SharpZipLib.dll FuzzyLogicLibrary.dll Eluant.dll BeaconLib.dll Open.Nat.dll SDL2-CS.dll OpenAL-CS.Core.dll DiscordRPC.dll Newtonsoft.Json.dll

# These are shipped in our custom minimal mono runtime and also available in the full system-installed .NET/mono stack
# This list *must* be kept in sync with the files packaged by the AppImageSupport and OpenRALauncherOSX repositories
WHITELISTED_CORE_ASSEMBLIES = mscorlib.dll System.dll System.Configuration.dll System.Core.dll System.Numerics.dll System.Security.dll System.Xml.dll Mono.Security.dll netstandard.dll

######################### UTILITIES/SETTINGS ###########################
#
# Install locations for local installs and downstream packaging
prefix ?= /usr/local
datarootdir ?= $(prefix)/share
datadir ?= $(datarootdir)
mandir ?= $(datarootdir)/man/
bindir ?= $(prefix)/bin
libdir ?= $(prefix)/lib
gameinstalldir ?= $(libdir)/openra

# Toolchain
CWD = $(shell pwd)
MSBUILD = msbuild -verbosity:m -nologo
MONO = mono
RM = rm
RM_R = $(RM) -r
RM_F = $(RM) -f
RM_RF = $(RM) -rf

# Only for use in target version:
VERSION := $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || (c=$$(git rev-parse --short HEAD 2>/dev/null) && echo git-$$c))

# Detect target platform for dependencies if not given by the user
ifndef TARGETPLATFORM
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Darwin)
TARGETPLATFORM = osx-x64
else
ifeq ($(UNAME_M),x86_64)
TARGETPLATFORM = linux-x64
else
TARGETPLATFORM = unix-generic
endif
endif
endif

OPENRA_UTILITY = ENGINE_DIR=".." $(MONO) --debug bin/OpenRA.Utility.exe

##################### DEVELOPMENT BUILDS AND TESTS #####################
#
all:
	@command -v $(firstword $(MSBUILD)) >/dev/null || (echo "OpenRA requires the '$(MSBUILD)' tool provided by Mono >= 5.18."; exit 1)
	@$(MSBUILD) -t:Build -restore -p:Configuration=Release -p:TargetPlatform=$(TARGETPLATFORM)
ifeq ($(TARGETPLATFORM), unix-generic)
	@./configure-system-libraries.sh
endif
	@./fetch-geoip.sh

clean:
	@-$(RM_RF) ./bin ./*/bin ./*/obj
	@$(MSBUILD) -t:Clean
	@-$(RM_F) IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP

check:
	@echo
	@echo "Compiling in debug mode..."
	@$(MSBUILD) -t:build -restore -p:Configuration=Debug -p:TargetPlatform=$(TARGETPLATFORM)
ifeq ($(TARGETPLATFORM), unix-generic)
	@./configure-system-libraries.sh
endif
	@echo
	@echo "Checking runtime assemblies..."
	@$(OPENRA_UTILITY) all --check-runtime-assemblies $(WHITELISTED_OPENRA_ASSEMBLIES) $(WHITELISTED_THIRDPARTY_ASSEMBLIES) $(WHITELISTED_CORE_ASSEMBLIES)
	@echo
	@echo "Checking for explicit interface violations..."
	@$(OPENRA_UTILITY) all --check-explicit-interfaces
	@echo
	@echo "Checking for incorrect conditional trait interface overrides..."
	@$(OPENRA_UTILITY) all --check-conditional-trait-interface-overrides

check-scripts:
	@echo
	@echo "Checking for Lua syntax errors..."
	@luac -p $(shell find mods/*/maps/* -iname '*.lua')
	@luac -p $(shell find lua/* -iname '*.lua')
	@luac -p $(shell find mods/*/bits/scripts/* -iname '*.lua')

test: all
	@echo
	@echo "Testing Tiberian Sun mod MiniYAML..."
	@$(OPENRA_UTILITY) ts --check-yaml
	@echo
	@echo "Testing Dune 2000 mod MiniYAML..."
	@$(OPENRA_UTILITY) d2k --check-yaml
	@echo
	@echo "Testing Tiberian Dawn mod MiniYAML..."
	@$(OPENRA_UTILITY) cnc --check-yaml
	@echo
	@echo "Testing Red Alert mod MiniYAML..."
	@$(OPENRA_UTILITY) ra --check-yaml

############# LOCAL INSTALLATION AND DOWNSTREAM PACKAGING ##############
#
version: VERSION mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/ts/mod.yaml mods/modcontent/mod.yaml mods/all/mod.yaml
ifeq ($(VERSION),)
	$(error Unable to determine new version (requires git or override of variable VERSION))
endif
	@sh -c '. ./packaging/functions.sh; set_engine_version "$(VERSION)" .'
	@sh -c '. ./packaging/functions.sh; set_mod_version "$(VERSION)" mods/ra/mod.yaml mods/cnc/mod.yaml mods/d2k/mod.yaml mods/ts/mod.yaml mods/modcontent/mod.yaml mods/all/mod.yaml'

install:
	@sh -c '. ./packaging/functions.sh; install_assemblies_mono $(CWD) $(DESTDIR)$(gameinstalldir) $(TARGETPLATFORM) True True True'
	@sh -c '. ./packaging/functions.sh; install_data $(CWD) $(DESTDIR)$(gameinstalldir) cnc d2k ra'

install-linux-shortcuts:
	@sh -c '. ./packaging/functions.sh; install_linux_shortcuts $(CWD) "$(DESTDIR)" "$(gameinstalldir)" "$(bindir)" "$(datadir)" "$(shell head -n1 VERSION)" cnc d2k ra'

install-linux-appdata:
	@sh -c '. ./packaging/functions.sh; install_linux_appdata $(CWD) "$(DESTDIR)" "$(datadir)" cnc d2k ra'

help:
	@echo 'to compile, run:'
	@echo '  make [DEBUG=true]'
	@echo
	@echo 'to compile using system libraries for native dependencies, run:'
	@echo '  make [DEBUG=true] TARGETPLATFORM=unix-generic'
	@echo
	@echo 'to check the official mods for erroneous yaml files, run:'
	@echo '  make test'
	@echo
	@echo 'to check the engine and official mod dlls for code style violations, run:'
	@echo '  make test'
	@echo
	@echo 'to compile and install Red Alert, Tiberian Dawn, and Dune 2000 run:'
	@echo '  make [prefix=/foo] [TARGETPLATFORM=unix-generic] install'
	@echo
	@echo 'to compile and install Red Alert, Tiberian Dawn, and Dune 2000'
	@echo 'using system libraries for native dependencies, run:'
	@echo '   make [prefix=/foo] [bindir=/bar/bin] TARGETPLATFORM=unix-generic install'
	@echo
	@echo 'to install Linux startup scripts, desktop files, icons, and MIME metadata'
	@echo '  make install-linux-shortcuts'
	@echo
	@echo 'to install Linux AppStream metadata'
	@echo '  make install-linux-appdata'

########################### MAKEFILE SETTINGS ##########################
#
.DEFAULT_GOAL := all

.SUFFIXES:

.PHONY: all clean check check-scripts test version install install-linux-shortcuts install-linux-appdata help
