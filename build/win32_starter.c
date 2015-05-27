// this is an extremly ugly quick and dirty hack...
// maybe it could be refactored to do some error catching and
// other things, but right now it does what it should...
// (providing a single exe file in our main directory without
// polluting it with all these dlls located in the /bin folder)

#ifdef __MINGW32__
#define _WIN32_WINNT 0x0502
#endif

#include <windows.h>
#include <winbase.h>
#include <stdlib.h>
#include <stdio.h>

/* some typedef functions - these are not matching exactly the
original definitions, but their signature is supposed to be
compatible
Currently 10 different functions are needed - each with different signatures... */
typedef void *voidfunc ();
typedef int *varfunc (void *L,...); // quick and dirty using the varargs
typedef void varfuncvoid (void *L,...);

// from lua.h
#define LUA_GLOBALSINDEX	(-10002)

static voidfunc *luaL_newstate;
static varfunc *luaL_loadbuffer;
static varfunc *luaL_openlibs;
static varfunc *lua_pcall;
static varfunc *lua_pushcclosure;
static varfunc *lua_setfield;
static varfunc *lua_tolstring;
static varfuncvoid *lua_createtable;
static varfuncvoid *lua_pushstring;
static varfuncvoid *lua_rawseti;

static int luafunc_mbox (void *L)
{
  const char *title = (const char*)lua_tolstring(L,1,NULL);
  const char *msg = (const char*)lua_tolstring(L,2,NULL);
  MessageBox(NULL,msg,title,MB_OK|MB_ICONERROR);
  return 0;
}

static const char *luacode =
"local msg = _ERRMSG; _ERRMSG = nil "
"local arg = _ARG or {}; _ARG = nil "
"xpcall("
"function() "
"(loadfile 'src/main.lua')(unpack(arg)) end,"
"function(err) msg('Uncaught lua script exception',debug.traceback(err)) end)"
;

#if defined(_WIN32) && defined (_MSC_VER)

PCHAR*	CommandLineToArgvA(PCHAR CmdLine,int* _argc)
{
  PCHAR* argv;
  PCHAR  _argv;
  size_t  len;
  ULONG   argc;
  CHAR   a;
  size_t   i, j;

  BOOLEAN  in_QM;
  BOOLEAN  in_TEXT;
  BOOLEAN  in_SPACE;

  len = strlen(CmdLine);
  i = ((len+2)/2)*sizeof(PVOID) + sizeof(PVOID);

  argv = (PCHAR*)GlobalAlloc(GMEM_FIXED,
    i + (len+2)*sizeof(CHAR));

  _argv = (PCHAR)(((PUCHAR)argv)+i);

  argc = 0;
  argv[argc] = _argv;
  in_QM = 0;
  in_TEXT = 0;
  in_SPACE = 1;
  i = 0;
  j = 0;

  while( a = CmdLine[i] ) {
    if(in_QM) {
      if(a == '\"') {
        in_QM = 0;
      } else {
        _argv[j] = a;
        j++;
      }
    } else {
      switch(a) {
        case '\"':
          in_QM = 1;
          in_TEXT = 1;
          if(in_SPACE) {
            argv[argc] = _argv+j;
            argc++;
          }
          in_SPACE = 0;
          break;
        case ' ':
        case '\t':
        case '\n':
        case '\r':
          if(in_TEXT) {
            _argv[j] = '\0';
            j++;
          }
          in_TEXT = 0;
          in_SPACE = 1;
          break;
        default:
          in_TEXT = 1;
          if(in_SPACE) {
            argv[argc] = _argv+j;
            argc++;
          }
          _argv[j] = a;
          j++;
          in_SPACE = 0;
          break;
      }
    }
    i++;
  }
  _argv[j] = '\0';
  argv[argc] = NULL;

  (*_argc) = argc;
  return argv;
}

int WINAPI WinMain(HINSTANCE hInstance,  HINSTANCE hPrevInstance,  LPSTR lpCmdLine, int nCmdShow)
{
  int argc;
  char ** argv = CommandLineToArgvA(GetCommandLineA(),&argc);

#else
int main (int argc, char *argv[])
{
#endif
  HINSTANCE hinstLib;

  char buffer[MAX_PATH],*file;

  if (!GetFullPathName(argv[0],MAX_PATH,buffer,&file)) {
    MessageBox(NULL,
      TEXT("Couldn't find the correct working directory"),
      TEXT("Failed to start editor"),
      MB_OK|MB_ICONERROR);
    return 0;
  }
  if (file!=NULL) *file = 0; // finish the string, don't need the appname

  SetCurrentDirectory(buffer);

  // set the application as DPI aware
  typedef enum _Process_DPI_Awareness {
    Process_DPI_Unaware            = 0,
    Process_System_DPI_Aware       = 1,
    Process_Per_Monitor_DPI_Aware  = 2
  } Process_DPI_Awareness;
  typedef BOOL (WINAPI *SetProcessDPIAwareness_t)(Process_DPI_Awareness);
  SetProcessDPIAwareness_t pfnSetProcessDPIAwareness = (SetProcessDPIAwareness_t)
    GetProcAddress(GetModuleHandle(TEXT("user32.dll")), "SetProcessDPIAware");
  if (NULL != pfnSetProcessDPIAwareness) pfnSetProcessDPIAwareness(Process_System_DPI_Aware);

  SetDllDirectory(".\\bin\\");
  hinstLib = LoadLibrary("lua51.dll");
  if (hinstLib != NULL)
  {
    luaL_newstate = (voidfunc*) GetProcAddress(hinstLib, "luaL_newstate");
    luaL_loadbuffer = (varfunc*) GetProcAddress(hinstLib, "luaL_loadbuffer");
    luaL_openlibs = (varfunc*) GetProcAddress(hinstLib, "luaL_openlibs");
    lua_pcall = (varfunc*)GetProcAddress(hinstLib, "lua_pcall");
    lua_tolstring = (varfunc*)GetProcAddress(hinstLib, "lua_tolstring");
    lua_setfield = (varfunc*)GetProcAddress(hinstLib, "lua_setfield");
    lua_pushcclosure = (varfunc*)GetProcAddress(hinstLib, "lua_pushcclosure");
    lua_createtable = (varfuncvoid*)GetProcAddress(hinstLib, "lua_createtable");
    lua_pushstring = (varfuncvoid*)GetProcAddress(hinstLib, "lua_pushstring");
    lua_rawseti = (varfuncvoid*)GetProcAddress(hinstLib, "lua_rawseti");
    // If the function address is valid, call the function.

    if (luaL_newstate && luaL_loadbuffer && luaL_openlibs && lua_pcall &&
      lua_pushcclosure && lua_setfield && lua_tolstring &&
      lua_createtable && lua_pushstring && lua_rawseti)
    {
      // OK, I don't do any error checking here, which COULD
      // lead to bugs that are hard to find, but considered the simplicity
      // of the whole process, it SHOULD be pretty unlikely to fail here
      // but don't come back on me if it does...
      void *L = luaL_newstate();
      int i;

      if (L!=NULL) {
        lua_createtable(L,argc,0);
        for (i=0;i<argc;i++) {
          lua_pushstring(L,argv[i]);
          lua_rawseti(L,-2,i+1);
        }
        lua_setfield(L,LUA_GLOBALSINDEX,"_ARG");
        luaL_openlibs(L);
        lua_pushcclosure(L,luafunc_mbox,0);
        lua_setfield(L,LUA_GLOBALSINDEX,"_ERRMSG");
        if (luaL_loadbuffer(L,luacode,strlen(luacode),"Initializer") == 0)
          lua_pcall(L,0,0,0);
        else
          MessageBox(NULL,
          TEXT("An unexpected error occured while loading the lua chunk."),
          TEXT("Failed to start editor"),
          MB_OK|MB_ICONERROR);
      } else
        MessageBox(NULL,
        TEXT("Couldn't initialize a luastate"),
        TEXT("Failed to start editor"),
        MB_OK|MB_ICONERROR);
    } else {
      MessageBox(NULL,
        TEXT("Could not load all functions that are supposed to be located in the lua51.dll\n"
        "This is not supposed to be happening..."),
        TEXT("Failed to start editor"),
        MB_OK|MB_ICONERROR);
    }

    // Free the DLL module.
    FreeLibrary(hinstLib);
  } else {
    MessageBox(NULL,
      TEXT("The lua51.dll could not be found or loaded, please check the working directory of the application.\n"),
      TEXT("Failed to initialize editor"),
      MB_OK|MB_ICONERROR);
  }

  return 0;
}
