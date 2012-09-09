# setup the minimum allowed CMake version
cmake_minimum_required(VERSION 2.8)

# set the default build type (this needs to be done *before* project command)
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE "RelWithDebInfo" CACHE STRING
    "Choose the type of build, options are: None Debug Release RelWithDebInfo MinSizeRel." FORCE)
endif()

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

# search for Lua 5.1
find_package(Lua51 REQUIRED)
include_directories("${LUA_INCLUDE_DIR}")

# search for wxWidgets
find_package(wxWidgets REQUIRED)
include("${wxWidgets_USE_FILE}")

# search for Lua modules
check_lua_module(wx TRUE)
check_lua_module(socket TRUE)
check_lua_module(copas ${USE_SYSTEM_COPAS})
check_lua_module(luainspect ${USE_SYSTEM_LUAINSPECT})
check_lua_module(mobdebug ${USE_SYSTEM_MOBDEBUG})

# adds an user option to enable/disable a particular system-wide Lua module
function(add_system_lua_module_option MODULE)
  string(TOUPPER ${MODULE} UPPERCASE_MODULE)
  if(NOT DEFINED USE_SYSTEM_${UPPERCASE_MODULE})
    option(USE_SYSTEM_${UPPERCASE_MODULE}
      "Use a system-wide \"${MODULE}\" Lua module instead of the bundled one." ${${MODULE}_FOUND})
  endif()
endfunction()

# add an user option to select the IDE frontend
set(IDE_FRONTEND "zbstudio" CACHE STRING "Choose the IDE frontend to build, options are: estrela zbstudio.")
if((NOT IDE_FRONTEND STREQUAL "estrela") AND (NOT IDE_FRONTEND STREQUAL "zbstudio"))
  message(FATAL_ERROR "Invalid IDE frontend specified: ${IDE_FRONTEND}")
endif()

# add user options to select between system-wide and bundled Lua modules
add_system_lua_module_option(copas)
add_system_lua_module_option(luainspect)
add_system_lua_module_option(mobdebug)

# list of C\C++ sources for building IDE executable file
set(SOURCES estrela/unix_starter.cpp)

# add the executable file to our project
add_executable(${IDE_FRONTEND} WIN32 ${SOURCES})
target_link_libraries(${IDE_FRONTEND} ${LUA_LIBRARIES} ${wxWidgets_LIBRARIES})

# determine the install directories
set(BINDIR "bin")
set(DATADIR "share/${IDE_FRONTEND}")
set(DOCDIR "share/doc/${IDE_FRONTEND}")

# pass the data directory to the starter application
add_definitions("-DDATADIR=\"${CMAKE_INSTALL_PREFIX}/${DATADIR}\"")

# install IDE executable
install(TARGETS ${IDE_FRONTEND} DESTINATION ${BINDIR})

# install source files
install(DIRECTORY src DESTINATION ${DATADIR})

# install Lua modules
if(NOT ${USE_SYSTEM_COPAS})
  install(DIRECTORY lualibs/copas lualibs/coxpcall DESTINATION ${DATADIR}/lualibs)
endif()
if(NOT ${USE_SYSTEM_LUAINSPECT})
  install(DIRECTORY lualibs/luainspect lualibs/metalua DESTINATION ${DATADIR}/lualibs)
endif()
if(NOT ${USE_SYSTEM_MOBDEBUG})
  install(DIRECTORY lualibs/mobdebug DESTINATION ${DATADIR}/lualibs)
endif()

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
  install(FILES api/lua/baselib.lua api/lua/love2d.lua DESTINATION ${DATADIR}/api/lua)
  install(FILES api/readme.txt DESTINATION ${DATADIR}/api)
  install(FILES cfg/user-sample.lua DESTINATION ${DATADIR}/cfg)
  install(FILES interpreters/love2d.lua interpreters/luadeb.lua interpreters/moai.lua
    DESTINATION ${DATADIR}/interpreters)
  install(FILES spec/lua.lua DESTINATION ${DATADIR}/spec)
  install(DIRECTORY zbstudio/res DESTINATION ${DATADIR}/zbstudio
    PATTERN zbstudio.desktop EXCLUDE PATTERN zbstudio.png EXCLUDE)
  install(FILES zbstudio/res/zbstudio.desktop DESTINATION share/applications)
  install(FILES zbstudio/res/zbstudio.png DESTINATION share/icons/hicolor/256x256/apps)
  file(GLOB FILELIST "${CMAKE_SOURCE_DIR}/zbstudio/*.lua")
  install(FILES ${FILELIST} DESTINATION ${DATADIR}/zbstudio)
endif()
