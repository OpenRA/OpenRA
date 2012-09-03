# setup the minimum allowed CMake version
cmake_minimum_required(VERSION 2.8)

# set the default build type (this needs to be done *before* project command)
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE "RelWithDebInfo" CACHE STRING
    "Choose the type of build, options are: None Debug Release RelWithDebInfo MinSizeRel." FORCE)
endif()

# set the project name (the actual executable name is controlled by IDE_FRONTEND option below)
project("ide")

# add an user option to select the IDE frontend
set(IDE_FRONTEND "zbstudio" CACHE STRING "Choose the IDE frontend to build, options are: estrela zbstudio.")
if((NOT IDE_FRONTEND STREQUAL "estrela") AND (NOT IDE_FRONTEND STREQUAL "zbstudio"))
  message(FATAL_ERROR "Invalid IDE frontend specified: ${IDE_FRONTEND}")
endif()

# list of C\C++ sources for building IDE executable file
set(SOURCES "estrela/unix_starter.cpp")

# set the path for our additional CMake modules like FindwxLua.cmake
list(APPEND CMAKE_MODULE_PATH "${CMAKE_SOURCE_DIR}/cmake")

# search for Lua 5.1
find_package(Lua51 REQUIRED)
include_directories("${LUA_INCLUDE_DIR}")

# search for wxWidgets
find_package(wxWidgets REQUIRED)
include("${wxWidgets_USE_FILE}")

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
