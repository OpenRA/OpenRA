// this is an extremly ugly quick and dirty hack...
// maybe it could be refactored to do some error catching and
// other things, but right now it does what it should...
// (providing a single exe file in our main directory without
// polluting it with all these dlls located in the /bin folder)

#ifdef __MINGW__ /* not sure if this is the *official* define */
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
	"local msg = _ERRMSG _ERRMSG = nil "
	"xpcall("
	"function() "
	"package.cpath = package.cpath..';bin/?.dll;lualibs/?.dll;lualibs/?/?.dll;lualibs/?/?/?.dll' "
	"package.path = package.path..'lualibs/?.lua;lualibs/?/?.lua;lualibs/?/init.lua;lualibs/?/?/?.lua;lualibs/?/?/init.lua' "
	"dofile 'src/main.lua' end,"
	"function(err) msg('Uncaught lua script exception',debug.traceback(err)) end)"
	;

int main (int argc, char *argv[])
{
	HINSTANCE hinstLib;


	char buffer[MAX_PATH],*file;

	if (!GetFullPathName(argv[0],MAX_PATH,buffer,&file)) {
		MessageBox(NULL,
						TEXT("Couldn't find the correct working directory"),
						TEXT("Failed to start estrela"),
						MB_OK|MB_ICONERROR);
		return 0;
	}
	if (file!=NULL)
		*file = 0; // finish the string, I don't need the appname
	//int i;
	//for (i=0;i<argc; i++)
//		printf("%d %s\n",i,argv[i]);

	SetCurrentDirectory(buffer);


	SetDllDirectory(".\\bin\\");
	hinstLib = LoadLibrary("lua5.1.dll");
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
        		lua_createtable(L,argc-1,1);
        		for (i=0;i<argc;i++) {
        			lua_pushstring(L,argv[i]);
        			lua_rawseti(L,-2,i);
        		}
        		lua_setfield(L,LUA_GLOBALSINDEX,"arg");
				luaL_openlibs(L);
				lua_pushcclosure(L,luafunc_mbox,0);
				lua_setfield(L,LUA_GLOBALSINDEX,"_ERRMSG");
				if (luaL_loadbuffer(L,luacode,strlen(luacode),"Initializer") == 0)
					lua_pcall(L,0,0,0);
				else
					MessageBox(NULL,
						TEXT("An unexpected error occured while loading the lua chunk."),
						TEXT("Failed to start estrela"),
						MB_OK|MB_ICONERROR);
        	} else
				MessageBox(NULL,
						TEXT("Couldn't initialize a luastate"),
						TEXT("Failed to start estrela"),
						MB_OK|MB_ICONERROR);
        } else {
        	MessageBox(NULL,
				TEXT("Could not load all functions that are supposed to be located in the lua5.1.dll\n"
				"This is not supposed to be happening..."),
				TEXT("Failed to start estrela"),
				MB_OK|MB_ICONERROR);
        }

        // Free the DLL module.
        FreeLibrary(hinstLib);
    } else {
    	MessageBox(NULL,
				TEXT("The lua5.1.dll could not be found or loaded, please check the working directory of the application.\n"),
				TEXT("Failed to initialize estrela"),
				MB_OK|MB_ICONERROR);
    }


	return 0;
}
