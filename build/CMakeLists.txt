# setup the minimum allowed CMake version
cmake_minimum_required(VERSION 2.8)

# set the default build type (this needs to be done *before* project command)
if(NOT CMAKE_BUILD_TYPE)
  set(CMAKE_BUILD_TYPE "Release" CACHE STRING "Choose the type of build." FORCE)
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "RelWithDebInfo" "MinSizeRel")
endif()

# restrict configuration types to the selected build type
set(CMAKE_CONFIGURATION_TYPES ${CMAKE_BUILD_TYPE} CACHE INTERNAL "" FORCE)

# set the project name
project("zbstudio")

# set the top-level project directory relative to CMakeLists.txt location
set(TOPDIR ..)

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
      set(${MODULE}_FOUND TRUE CACHE INTERNAL "")
      message(STATUS "Found Lua module: ${MODULE}")
    elseif(ARGV1)
      message(FATAL_ERROR "Lua module \"${MODULE}\" is not found")
    endif()
  endif()
endfunction()

# adds an user option to select between system-wide and bundled Lua module
function(add_system_lua_module_option MODULE)
  string(TOUPPER ${MODULE} UPPERCASE_MODULE)
  if(NOT DEFINED USE_SYSTEM_${UPPERCASE_MODULE})
    option(USE_SYSTEM_${UPPERCASE_MODULE}
      "Use a system-wide \"${MODULE}\" Lua module instead of the bundled one." ${${MODULE}_FOUND})
  endif()
endfunction()

# installs all files listed in the manifest
function(install_from_manifest TYPE MANIFEST DESTDIR EXCLUDE_REGEX)
  file(STRINGS ${MANIFEST} PATTERN_LIST)
  foreach(PATTERN ${PATTERN_LIST})
    string(STRIP ${PATTERN} PATTERN)
    if(NOT PATTERN MATCHES ${EXCLUDE_REGEX})
      file(GLOB FILELIST RELATIVE "${CMAKE_SOURCE_DIR}/${TOPDIR}" "${CMAKE_SOURCE_DIR}/${TOPDIR}/${PATTERN}")
      foreach(FILENAME ${FILELIST})
        get_filename_component(FILEPATH ${FILENAME} PATH)
        install(${TYPE} ${TOPDIR}/${FILENAME} DESTINATION ${DESTDIR}/${FILEPATH})
      endforeach()
    endif()
  endforeach()
endfunction()

if(WIN32)
  # add the executable file to our project
  add_executable(zbstudio WIN32 ${TOPDIR}/build/win32_starter.c ${TOPDIR}/zbstudio/res/zbstudio.rc)

  # link to the static multi-threaded CRT under MSVC
  if(MSVC)
    string(TOUPPER ${CMAKE_BUILD_TYPE} BUILD_TYPE)
    foreach(FLAGS_VAR CMAKE_C_FLAGS CMAKE_CXX_FLAGS CMAKE_C_FLAGS_${BUILD_TYPE} CMAKE_CXX_FLAGS_${BUILD_TYPE})
      string(REGEX REPLACE "/MD" "/MT" ${FLAGS_VAR} ${${FLAGS_VAR}})
      string(REGEX REPLACE "/MDd" "/MTd" ${FLAGS_VAR} ${${FLAGS_VAR}})
    endforeach()
  endif()

  # setup the data directory
  set(DATADIR .)

  # install IDE executable
  install(TARGETS zbstudio DESTINATION ${DATADIR})

  # install files from manifest
  install_from_manifest(FILES ${TOPDIR}/zbstudio/MANIFEST ${DATADIR} "^$")
  install_from_manifest(FILES ${TOPDIR}/zbstudio/MANIFEST-bin-win32 ${DATADIR} "^zbstudio.exe$")
elseif(APPLE)
  set(OSX_INSTALL_PREFIX "/Applications" CACHE PATH "Default OSX prefix")
  set(CMAKE_INSTALL_PREFIX "${OSX_INSTALL_PREFIX}" CACHE INTERNAL "Prefix prepended to install directories")
  # setup the data directory
  set(ROOTDIR ZeroBraneStudio.app/Contents)
  set(DATADIR ${ROOTDIR}/ZeroBraneStudio)

  # install IDE shell script
  install(PROGRAMS ${TOPDIR}/zbstudio/ZeroBraneStudio.app/Contents/MacOS/ZeroBraneStudio DESTINATION ${ROOTDIR}/MacOS)

  # install icon pack and .plist file
  install(DIRECTORY ${TOPDIR}/zbstudio/ZeroBraneStudio.app/Contents/Resources DESTINATION ${ROOTDIR})
  install(FILES ${TOPDIR}/zbstudio/ZeroBraneStudio.app/Contents/Info.plist DESTINATION ${ROOTDIR})

  # install files from manifest
  install_from_manifest(FILES ${TOPDIR}/zbstudio/MANIFEST ${DATADIR} "^$")
  install_from_manifest(PROGRAMS ${TOPDIR}/zbstudio/MANIFEST-bin-macos ${DATADIR} "^$")
else()
  # check the available Lua modules
  check_lua_module(wx TRUE)
  check_lua_module(socket TRUE)
  check_lua_module(copas ${USE_SYSTEM_COPAS})
  check_lua_module(luainspect ${USE_SYSTEM_LUAINSPECT})
  #check_lua_module(mobdebug ${USE_SYSTEM_MOBDEBUG})

  # add user options to select between system-wide and bundled Lua modules
  add_system_lua_module_option(copas)
  add_system_lua_module_option(luainspect)
  #add_system_lua_module_option(mobdebug)

  # setup the data directory
  set(DATADIR share/zbstudio)

  # install IDE shell script
  set(IDE_DATADIR "${CMAKE_INSTALL_PREFIX}/${DATADIR}")
  configure_file(${TOPDIR}/zbstudio/zbstudio.in "${CMAKE_BINARY_DIR}/zbstudio")
  install(PROGRAMS "${CMAKE_BINARY_DIR}/zbstudio" DESTINATION bin)

  # install bundled Lua modules
  if(NOT USE_SYSTEM_COPAS)
    install(DIRECTORY ${TOPDIR}/lualibs/copas ${TOPDIR}/lualibs/coxpcall DESTINATION ${DATADIR}/lualibs)
  endif()
  if(NOT USE_SYSTEM_LUAINSPECT)
    install(DIRECTORY ${TOPDIR}/lualibs/luainspect ${TOPDIR}/lualibs/metalua DESTINATION ${DATADIR}/lualibs)
  endif()
  if(NOT USE_SYSTEM_MOBDEBUG)
    install(DIRECTORY ${TOPDIR}/lualibs/mobdebug DESTINATION ${DATADIR}/lualibs)
  endif()

  # install .desktop file and hicolor icon theme
  install(DIRECTORY ${TOPDIR}/zbstudio/res/icons/ DESTINATION share/icons/hicolor)
  install(FILES ${TOPDIR}/zbstudio/res/zbstudio.desktop DESTINATION share/applications)

  # install miscellaneous documentation files
  install(FILES ${TOPDIR}/CHANGELOG.md ${TOPDIR}/LICENSE ${TOPDIR}/README.md DESTINATION share/doc/zbstudio)

  # install files from manifest
  install_from_manifest(FILES ${TOPDIR}/zbstudio/MANIFEST ${DATADIR} "^(CHANGELOG.md|LICENSE|README.md)$|^lualibs/")
endif()
