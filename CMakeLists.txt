# setup the minimum allowed CMake version
cmake_minimum_required(VERSION 2.8)

# set the default build type (this needs to be done *before* project command)
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE "Release" CACHE STRING "Choose the type of build." FORCE)
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "RelWithDebInfo" "MinSizeRel")
endif()

# restrict configuration types to the selected build type
set(CMAKE_CONFIGURATION_TYPES ${CMAKE_BUILD_TYPE} CACHE INTERNAL "" FORCE)

# set the project name (the actual executable name is controlled by IDE_FRONTEND option)
project("ide")

# checks if the Lua module is available
function(check_lua_module MODULE)
  if(NOT ${MODULE}_FOUND)
    # find Lua executable at first
    find_program(LUA_EXECUTABLE "lua")
    if(NOT LUA_EXECUTABLE)
      message(FATAL_ERROR "Lua executable is not found")
    endif()

    # run a short script with "require" statement to determine if the Lua module is available
    execute_process(COMMAND "${LUA_EXECUTABLE}" -e "require(\"${MODULE}\")"
      RESULT_VARIABLE EXIT_CODE OUTPUT_QUIET ERROR_QUIET)
    if(EXIT_CODE EQUAL 0)
      set(${MODULE}_FOUND TRUE CACHE INTERNAL "Lua module status")
      message(STATUS "Found Lua module: ${MODULE}")
    elseif(ARGV1)
      message(FATAL_ERROR "Lua module \"${MODULE}\" is not found")
    endif()
  endif()
endfunction()

# check for Lua modules
if(UNIX AND NOT APPLE)
  check_lua_module(wx TRUE)
  check_lua_module(socket TRUE)
  check_lua_module(copas ${USE_SYSTEM_COPAS})
  check_lua_module(luainspect ${USE_SYSTEM_LUAINSPECT})
  check_lua_module(mobdebug ${USE_SYSTEM_MOBDEBUG})
endif()

# add an user option to select the IDE frontend
set(IDE_FRONTEND "zbstudio" CACHE STRING "Choose the IDE frontend to build.")
set_property(CACHE IDE_FRONTEND PROPERTY STRINGS "estrela" "zbstudio")

# adds an user option to enable/disable a particular system-wide Lua module
function(add_system_lua_module_option MODULE)
  string(TOUPPER ${MODULE} UPPERCASE_MODULE)
  if(NOT DEFINED USE_SYSTEM_${UPPERCASE_MODULE})
    option(USE_SYSTEM_${UPPERCASE_MODULE}
      "Use a system-wide \"${MODULE}\" Lua module instead of the bundled one." ${${MODULE}_FOUND})
  endif()
endfunction()

# add user options to select between system-wide and bundled Lua modules
if(UNIX AND NOT APPLE)
  add_system_lua_module_option(copas)
  add_system_lua_module_option(luainspect)
  add_system_lua_module_option(mobdebug)
endif()

# add the executable file to our project
if(WIN32)
  add_executable(${IDE_FRONTEND} WIN32 estrela/win32_starter.c ${IDE_FRONTEND}/res/${IDE_FRONTEND}.rc)
endif()

# link to the static multi-threaded CRT under MSVC
if(MSVC)
  string(TOUPPER ${CMAKE_BUILD_TYPE} BUILD_TYPE)
  foreach(FLAGS_VAR CMAKE_C_FLAGS CMAKE_CXX_FLAGS CMAKE_C_FLAGS_${BUILD_TYPE} CMAKE_CXX_FLAGS_${BUILD_TYPE})
    string(REGEX REPLACE "/MD" "/MT" ${FLAGS_VAR} ${${FLAGS_VAR}})
    string(REGEX REPLACE "/MDd" "/MTd" ${FLAGS_VAR} ${${FLAGS_VAR}})
  endforeach()
endif()

# determine the install directories
if(WIN32)
  set(BINDIR ".")
  set(DATADIR ".")
  set(DOCDIR ".")
elseif(APPLE)
  set(ROOTDIR "ZeroBraneStudio.app/Contents")
  set(BINDIR "${ROOTDIR}/MacOS")
  set(DATADIR "${ROOTDIR}/ZeroBraneStudio")
  set(DOCDIR "${DATADIR}")
else()
  set(BINDIR "bin")
  set(DATADIR "share/${IDE_FRONTEND}")
  set(DOCDIR "share/doc/${IDE_FRONTEND}")
endif()

# install IDE executable
if(WIN32)
  install(TARGETS ${IDE_FRONTEND} DESTINATION ${BINDIR})
elseif(APPLE)
  install(PROGRAMS zbstudio/ZeroBraneStudio.app/Contents/MacOS/ZeroBraneStudio DESTINATION ${BINDIR})
else()
  set(IDE_DATADIR "${CMAKE_INSTALL_PREFIX}/${DATADIR}")
  configure_file(zbstudio/zbstudio.in "${CMAKE_BINARY_DIR}/${IDE_FRONTEND}")
  install(PROGRAMS "${CMAKE_BINARY_DIR}/${IDE_FRONTEND}" DESTINATION ${BINDIR})
endif()

# install precompiled dependencies
if(WIN32)
  install(FILES bin/clibs/mime/core.dll DESTINATION bin/clibs/mime)
  install(FILES bin/clibs/socket/core.dll DESTINATION bin/clibs/socket)
  install(FILES bin/lua.exe bin/lua5.1.dll bin/lua51.dll bin/winapi.dll bin/wx.dll DESTINATION bin)
elseif(APPLE)
  install(PROGRAMS bin/clibs/mime/core.dylib DESTINATION ${DATADIR}/bin/clibs/mime)
  install(PROGRAMS bin/clibs/socket/core.dylib DESTINATION ${DATADIR}/bin/clibs/socket)
  install(PROGRAMS bin/lua.app/Contents/MacOS/lua DESTINATION ${DATADIR}/bin/lua.app/Contents/MacOS)
  install(FILES bin/lua.app/Contents/Info.plist DESTINATION ${DATADIR}/bin/lua.app/Contents)
  file(GLOB FILELIST "${CMAKE_SOURCE_DIR}/bin/*.dylib")
  install(PROGRAMS ${FILELIST} bin/lua DESTINATION ${DATADIR}/bin)
endif()

# install Lua modules
if(NOT USE_SYSTEM_COPAS)
  install(DIRECTORY lualibs/copas lualibs/coxpcall DESTINATION ${DATADIR}/lualibs)
endif()
if(NOT USE_SYSTEM_LUAINSPECT)
  install(DIRECTORY lualibs/luainspect lualibs/metalua DESTINATION ${DATADIR}/lualibs)
endif()
if(NOT USE_SYSTEM_MOBDEBUG)
  install(DIRECTORY lualibs/mobdebug DESTINATION ${DATADIR}/lualibs)
endif()
if(WIN32 OR APPLE)
  install(DIRECTORY lualibs/socket DESTINATION ${DATADIR}/lualibs)
  install(FILES lualibs/ltn12.lua lualibs/mime.lua lualibs/socket.lua DESTINATION ${DATADIR}/lualibs)
endif()

# install source files
install(DIRECTORY src DESTINATION ${DATADIR})

# install miscellaneous documentation files
install(FILES CHANGELOG.md LICENSE README.md DESTINATION ${DOCDIR})

# install IDE frontend-specific files
if(IDE_FRONTEND STREQUAL "estrela")
  install(DIRECTORY api cfg interpreters spec tools DESTINATION ${DATADIR})
  install(FILES estrela/res/estrela.ico estrela/res/estrela.png estrela/res/wxlualogo2.png
    DESTINATION ${DATADIR}/estrela/res)
  file(GLOB FILELIST "${CMAKE_SOURCE_DIR}/estrela/*.lua")
  install(FILES ${FILELIST} estrela/readme.txt DESTINATION ${DATADIR}/estrela)
else()
  install(FILES api/lua/baselib.lua api/lua/love2d.lua api/lua/moai.lua DESTINATION ${DATADIR}/api/lua)
  install(FILES api/readme.txt DESTINATION ${DATADIR}/api)
  install(FILES cfg/user-sample.lua DESTINATION ${DATADIR}/cfg)
  install(FILES interpreters/love2d.lua interpreters/luadeb.lua interpreters/moai.lua
    DESTINATION ${DATADIR}/interpreters)
  install(FILES spec/lua.lua DESTINATION ${DATADIR}/spec)
  install(DIRECTORY zbstudio/res/16 DESTINATION ${DATADIR}/zbstudio/res)
  install(FILES zbstudio/res/16.ico zbstudio/res/32.ico zbstudio/res/zerobrane.png
    DESTINATION ${DATADIR}/zbstudio/res)
  file(GLOB FILELIST "${CMAKE_SOURCE_DIR}/zbstudio/*.lua")
  install(FILES ${FILELIST} DESTINATION ${DATADIR}/zbstudio)

  # install icon pack and .plist file on OS X
  if(APPLE)
    install(DIRECTORY zbstudio/ZeroBraneStudio.app/Contents/Resources DESTINATION ${ROOTDIR})
    install(FILES zbstudio/ZeroBraneStudio.app/Contents/Info.plist DESTINATION ${ROOTDIR})
  endif()

  # install .desktop file and hicolor icon theme on Linux
  if(UNIX AND NOT APPLE)
    install(DIRECTORY zbstudio/res/icons/ DESTINATION share/icons/hicolor)
    install(FILES zbstudio/res/zbstudio.desktop DESTINATION share/applications)
  endif()
endif()
