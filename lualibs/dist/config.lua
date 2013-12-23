-- Luadist configuration

module ("dist.config", package.seeall)

local sys = require "dist.sys"
local utils = require "dist.utils"
local win = (os.getenv('WINDIR') or (os.getenv('OS') or ''):match('[Ww]indows'))
  and not (os.getenv('OSTYPE') or ''):match('cygwin') -- exclude cygwin

-- System information ------------------------------------------------
version       = "0.2.7"   -- Current LuaDist version
-- set initial architecture as it's important for path separators
arch          = win and "Windows" or "Linux" -- Host architecture
type          = "x86"      -- Host type

-- Directories -------------------------------------------------------
root_dir      = os.getenv("DIST_ROOT") or utils.get_luadist_location() or sys.path_separator()
temp_dir      = "tmp"
cache_dir     = sys.make_path(temp_dir, "cache")
distinfos_dir = sys.make_path("share", "luadist-git", "dists")
test_dir      = sys.make_path("share", "luadist-git", "test")

-- Files -------------------------------------------------------------
manifest_file  = sys.make_path(cache_dir, ".gitmodules")
dep_cache_file = sys.make_path(cache_dir, ".depcache")
log_file       = sys.make_path(temp_dir, "luadist.log")
cache_file     = ""

-- Repositories ------------------------------------------------------
repos = {
    "git://github.com/LuaDist/Repository.git",
}

upload_url = "git@github.com:LuaDist" -- must not contain trailing '/'

-- Settings ----------------------------------------------------------
debug         = false         -- Use debug mode.
verbose       = false         -- Print verbose output.
simulate      = false         -- Only simulate installation of packages.
binary        = true          -- Use binary version of modules.
source        = true          -- Use source version of modules.
test          = false         -- Run CTest before install.

cache         = true          -- Use cache.
cache_timeout = 3 * 60 * 60   -- Cache timeout in seconds.

dep_cache     = true          -- Use cache for dependency information (tree functionality).

-- Components (of modules) that will be installed.
components    = {
  "Runtime", "Library", "Header", "Data", "Documentation", "Example", "Test", "Other", "Unspecified"
}

-- Available log levels are: DEBUG, INFO, WARN, ERROR, FATAL (see dist.logger for more information).
print_log_level = "WARN"      -- Minimum level for log messages to be printed (nil to disable).
write_log_level = "INFO"      -- Minimum level for log messages to be logged (nil to disable).


-- CMake variables ---------------------------------------------------
variables = {
  --- Install defaults
  INSTALL_BIN                        = "bin",
  INSTALL_LIB                        = "lib",
  INSTALL_INC                        = "include",
  INSTALL_ETC                        = "etc",
  INSTALL_LMOD                       = "lib/lua",
  INSTALL_CMOD                       = "lib/lua",

  --- LuaDist specific variables
  DIST_VERSION                       = version,
  DIST_ARCH                          = arch,
  DIST_TYPE                          = type,

  -- CMake specific setup
  CMAKE_GENERATOR                    = win and "MinGW Makefiles" or "Unix Makefiles",
  CMAKE_BUILD_TYPE                   = "MinSizeRel",

  -- RPath functionality
  CMAKE_SKIP_BUILD_RPATH             = "FALSE",
  CMAKE_BUILD_WITH_INSTALL_RPATH     = "FALSE",
  CMAKE_INSTALL_RPATH                = "$ORIGIN/../lib",
  CMAKE_INSTALL_RPATH_USE_LINK_PATH  = "TRUE",
  CMAKE_INSTALL_NAME_DIR             = "@executable_path/../lib",

  -- OSX specific
  CMAKE_OSX_ARCHITECTURES            = "",
}

-- Building ----------------------------------------------------------
cmake         = "cmake"
ctest         = "ctest"

cache_command = cmake .. " -C cache.cmake"
build_command = cmake .. " --build . --clean-first"

install_component_command = " -DCOMPONENT=#COMPONENT# -P cmake_install.cmake"

test_command = ctest .. " -V ."

strip_option = " -DCMAKE_INSTALL_DO_STRIP=true"
cache_debug_options = "-DCMAKE_VERBOSE_MAKEFILE=true -DCMAKE_BUILD_TYPE=Debug"
build_debug_options = ""

-- Add -j option to make in case of unix makefiles to speed up builds
if (variables.CMAKE_GENERATOR == "Unix Makefiles") then
        build_command = build_command .. " -- -j6"
end

-- Add -j option to make in case of MinGW makefiles to speed up builds
if (variables.CMAKE_GENERATOR == "MinGW Makefiles") then
        build_command = "set SHELL=cmd.exe && " .. build_command .. " -- -j"
end
