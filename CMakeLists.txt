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
    execute_process(COMMAND "${LUA_EXECUTABLE}" -e "require(\"${MODULE}\")" RESULT_VARIABLE EXIT_CODE)
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
set(SOURCES "estrela/unix_starter.cpp")

# pass the data directory to the starter application
add_definitions("-DDATADIR=\"${CMAKE_SOURCE_DIR}\"")

# enable the compiler warnings
if(MSVC)
  add_definitions("/W3")
elseif(CMAKE_COMPILER_IS_GNUCXX)
  add_definitions("-Wall")
endif()

# add the executable file to our project
add_executable(${IDE_FRONTEND} WIN32 ${SOURCES})
target_link_libraries(${IDE_FRONTEND} ${LUA_LIBRARIES} ${wxWidgets_LIBRARIES})
