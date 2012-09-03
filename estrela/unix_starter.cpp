#include <iostream>
#include <string>
#include <unistd.h>
#include <lua.hpp>
#include <wx/wx.h>

// Lua bootstrap chunk
static const char *LUA_CODE =
  "local arg = _arg; _arg = nil\n"
  "local errorHandler = _errorHandler; _errorHandler = nil\n"
  "xpcall(\n"
  "  function() local func, error = loadfile('src/main.lua')\n"
  "    if func then func(unpack(arg)) else errorHandler('Failed to load Lua script:\\n' .. error) end end,\n"
  "  function(error) errorHandler('Uncaught Lua script exception:\\n' .. debug.traceback(error)) end)\n";

// global copy of main() function arguments
static int argc;
static char **argv;

static void showErrorDialog(const char *message, const char *caption)
{
  if (wxApp::GetInstance() != NULL)
  {
    wxMessageBox(wxString::FromAscii(message), wxString::FromAscii(caption), wxOK | wxICON_ERROR);
  }
  else
  {
    wxApp::SetInstance(new wxApp());
    if (wxEntryStart(argc, argv))
    {
      wxMessageBox(wxString::FromAscii(message), wxString::FromAscii(caption), wxOK | wxICON_ERROR);
      wxEntryCleanup();
    }
    else
    {
      std::cout << message << std::endl;
    }
  }
}

static int errorHandler(lua_State *L)
{
  showErrorDialog(lua_tostring(L, -1), "Error");
  return 0;
}

int main(int argc, char **argv)
{
  // save argc/argv parameters
  ::argc = argc;
  ::argv = argv;

  // on Linux change to the predefined data directory selected at CMake configuration step
  chdir(DATADIR);

  // create and initialize a new Lua state
  lua_State *L = luaL_newstate();
  if (L == NULL)
  {
    showErrorDialog("Failed to create a new Lua state.", "Error");
    return -1;
  }
  luaL_openlibs(L);

  // store all command-line arguments in "_arg" global variable
  lua_createtable(L, argc, 0);
  for (int i = 0; i < argc; ++i)
  {
    lua_pushstring(L, argv[i]);
    lua_rawseti(L, -2, i + 1);
  }
  lua_setglobal(L, "_arg");

  // store the error handler function in "_errorHandler" global variable
  lua_pushcfunction(L, errorHandler);
  lua_setglobal(L, "_errorHandler");

  // load and run Lua bootstrap chunk
  if (luaL_loadstring(L, LUA_CODE) != 0 || lua_pcall(L, 0, 0, 0) != 0)
  {
    showErrorDialog((std::string("Unexpected error in Lua bootstrap chunk:\n") + lua_tostring(L, -1)).c_str(), "Error");
    lua_close(L);
    return -1;
  }

  // close Lua state upon exit
  lua_close(L);
  return 0;
}
