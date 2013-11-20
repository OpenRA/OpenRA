namespace LuaInterface
{

	using System;
	using System.Runtime.InteropServices;
	using System.Reflection;
	using System.Collections;
	using System.Text;
    using System.Security;

	/*
	 * Lua types for the API, returned by lua_type function
	 */
	public enum LuaTypes
	{
		LUA_TNONE=-1,
		LUA_TNIL=0,
		LUA_TNUMBER=3,
		LUA_TSTRING=4,
		LUA_TBOOLEAN=1,
		LUA_TTABLE=5,
		LUA_TFUNCTION=6,
		LUA_TUSERDATA=7,
		LUA_TLIGHTUSERDATA=2
	}

	// steffenj: BEGIN lua garbage collector options
	/*
	 * Lua Garbage Collector options (param "what")
	 */
	public enum LuaGCOptions
	{
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7,
	}
		/*
	sealed class LuaGCOptions
	{
		public static int LUA_GCSTOP = 0;
		public static int LUA_GCRESTART = 1;
		public static int LUA_GCCOLLECT = 2;
		public static int LUA_GCCOUNT = 3;
		public static int LUA_GCCOUNTB = 4;
		public static int LUA_GCSTEP = 5;
		public static int LUA_GCSETPAUSE = 6;
		public static int LUA_GCSETSTEPMUL = 7;
	};
		 */
	// steffenj: END lua garbage collector options

	/*
	 * Special stack indexes
	 */
	sealed class LuaIndexes
	{
		public static int LUA_REGISTRYINDEX=-10000;
		public static int LUA_ENVIRONINDEX=-10001;	// steffenj: added environindex
		public static int LUA_GLOBALSINDEX=-10002;	// steffenj: globalsindex previously was -10001
	}

	/*
	 * Structure used by the chunk reader
	 */
	[ StructLayout( LayoutKind.Sequential )]
	public struct ReaderInfo
	{
		public String chunkData;
		public bool finished;
	}

	/*
	 * Delegate for functions passed to Lua as function pointers
	 */
	public delegate int LuaCSFunction(IntPtr luaState);

	/*
	 * Delegate for chunk readers used with lua_load
	 */
	public delegate string LuaChunkReader(IntPtr luaState,ref ReaderInfo data,ref uint size);


    /// <summary>
    /// Used to handle Lua panics
    /// </summary>
    /// <param name="luaState"></param>
    /// <returns></returns>
    public delegate int LuaFunctionCallback(IntPtr luaState);

	/*
	 * P/Invoke wrapper of the Lua API
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 *
	 * // steffenj: noteable changes in the LuaDLL API:
	 * - luaopen_* functions are gone
	 *		(however Lua class constructor already calls luaL_openlibs now, so just remove these calls)
	 * - deprecated functions: lua_open, lua_strlen, lua_dostring
	 *		(they still work but may be removed with next Lua version)
	 *
	 * list of functions of the Lua 5.1.1 C API that are not in LuaDLL
	 * i thought this may come in handy for the next Lua version upgrade and for anyone to see
	 * what the differences are in the APIs (C API vs LuaDLL API)
		lua_concat			(use System.String concatenation or similar)
		lua_cpcall			(no point in calling C functions)
		lua_dump			(would write to unmanaged memory via lua_Writer)
		lua_getallocf		(no C functions/pointers)
		lua_isthread		(no threads)
		lua_newstate		(use luaL_newstate)
		lua_newthread		(no threads)
		lua_pushcclosure	(no C functions/pointers)
		lua_pushcfunction	(no C functions/pointers)
		lua_pushfstring		(use lua_pushstring)
		lua_pushthread		(no threads)
		lua_pushvfstring	(use lua_pushstring)
		lua_register		(all libs already opened, use require in scripts for external libs)
		lua_resume			(no threads)
		lua_setallocf		(no C functions/pointers)
		lua_status			(no threads)
		lua_tointeger		(use System.Convert)
		lua_tolstring		(use lua_tostring)
		lua_topointer		(no C functions/pointers)
		lua_tothread		(no threads)
		lua_xmove			(no threads)
		lua_yield			(no threads)

		luaL_add*			(use System.String concatenation or similar)
		luaL_argcheck		(function argument checking unnecessary)
		luaL_argerror		(function argument checking unnecessary)
		luaL_buffinit		(use System.String concatenation or similar)
		luaL_checkany		(function argument checking unnecessary)
		luaL_checkint		(function argument checking unnecessary)
		luaL_checkinteger	(function argument checking unnecessary)
		luaL_checklong		(function argument checking unnecessary)
		luaL_checklstring	(function argument checking unnecessary)
		luaL_checknumber	(function argument checking unnecessary)
		luaL_checkoption	(function argument checking unnecessary)
		luaL_checkstring	(function argument checking unnecessary)
		luaL_checktype		(function argument checking unnecessary)
		luaL_prepbuffer		(use System.String concatenation or similar)
		luaL_pushresult		(use System.String concatenation or similar)
		luaL_register		(all libs already opened, use require in scripts for external libs)
		luaL_typerror		(function argument checking unnecessary)

		(complete lua_Debug interface omitted)
		lua_gethook***
		lua_getinfo
		lua_getlocal
		lua_getstack
		lua_getupvalue
		lua_sethook
		lua_setlocal
		lua_setupvalue
	 */
	public class LuaDLL
	{
        // for debugging
        // const string BASEPATH = @"C:\development\software\dotnet\tools\PulseRecognizer\PulseRecognizer\bin\Debug\";
        // const string BASEPATH = @"C:\development\software\ThirdParty\lua\Built\";
		const string LUADLL = "lua51.dll";
		const string LUALIBDLL = LUADLL;
        const string STUBDLL = LUADLL;

		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gc(IntPtr luaState, LuaGCOptions what, int data);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_typename(IntPtr luaState, LuaTypes type);
		public static string luaL_typename(IntPtr luaState, int stackPos)
		{
			return LuaDLL.lua_typename(luaState, LuaDLL.lua_type(luaState, stackPos));
		}

		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_error(IntPtr luaState, string message);
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern string luaL_gsub(IntPtr luaState, string str, string pattern, string replacement);

		// the functions below are still untested
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfenv(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_isfunction(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_islightuserdata(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_istable(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_isuserdata(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_lessthan(IntPtr luaState, int stackPos1, int stackPos2);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_rawequal(IntPtr luaState, int stackPos1, int stackPos2);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_setfenv(IntPtr luaState, int stackPos);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setfield(IntPtr luaState, int stackPos, string name);
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_callmeta(IntPtr luaState, int stackPos, string name);
		// steffenj: END additional Lua API functions new in Lua 5.1

		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_newstate();
		/// <summary>DEPRECATED - use luaL_newstate() instead!</summary>
		public static IntPtr lua_open()
		{
			return LuaDLL.luaL_newstate();
		}
		// steffenj: END Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_close(IntPtr luaState);
		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_openlibs(IntPtr luaState);
		/*
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaopen_base(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_io(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_table(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_string(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_math(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_debug(IntPtr luaState);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaopen_loadlib(IntPtr luaState);
		 */
		// steffenj: END Lua 5.1.1 API change (new function luaL_openlibs)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_objlen(IntPtr luaState, int stackPos);
		/// <summary>DEPRECATED - use lua_objlen(IntPtr luaState, int stackPos) instead!</summary>
		public static int lua_strlen(IntPtr luaState, int stackPos)
		{
			return lua_objlen(luaState, stackPos);
		}
		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadstring(IntPtr luaState, string chunk);
		public static int luaL_dostring(IntPtr luaState, string chunk)
		{
			int result = LuaDLL.luaL_loadstring(luaState, chunk);
			if (result != 0)
				return result;

			return LuaDLL.lua_pcall(luaState, 0, -1, 0);
		}
		/// <summary>DEPRECATED - use luaL_dostring(IntPtr luaState, string chunk) instead!</summary>
		public static int lua_dostring(IntPtr luaState, string chunk)
		{
			return LuaDLL.luaL_dostring(luaState, chunk);
		}
		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_createtable(IntPtr luaState, int narr, int nrec);
		public static void lua_newtable(IntPtr luaState)
		{
			LuaDLL.lua_createtable(luaState, 0, 0);
		}
		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
		//[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static int luaL_dofile(IntPtr luaState, string fileName)
		{
			int result = LuaDLL.luaL_loadfile(luaState, fileName);
			if (result != 0)
				return result;

			return LuaDLL.lua_pcall(luaState, 0, -1, 0);
		}
		// steffenj: END Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile)
		public static void lua_getglobal(IntPtr luaState, string name)
		{
			LuaDLL.lua_pushstring(luaState,name);
			LuaDLL.lua_gettable(luaState,LuaIndexes.LUA_GLOBALSINDEX);
		}
		public static void lua_setglobal(IntPtr luaState, string name)
		{
			LuaDLL.lua_pushstring(luaState,name);
			LuaDLL.lua_insert(luaState,-2);
			LuaDLL.lua_settable(luaState,LuaIndexes.LUA_GLOBALSINDEX);
		}
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_settop(IntPtr luaState, int newTop);
		// steffenj: BEGIN added lua_pop "macro"
		public static void lua_pop(IntPtr luaState, int amount)
		{
			LuaDLL.lua_settop(luaState, -(amount) - 1);
		}
		// steffenj: END added lua_pop "macro"
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_insert(IntPtr luaState, int newTop);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_remove(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_gettable(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_rawget(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_settable(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_rawset(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_setmetatable(IntPtr luaState, int objIndex);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_getmetatable(IntPtr luaState, int objIndex);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_equal(IntPtr luaState, int index1, int index2);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushvalue(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_replace(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_gettop(IntPtr luaState);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern LuaTypes lua_type(IntPtr luaState, int index);
		public static bool lua_isnil(IntPtr luaState, int index)
		{
			return (LuaDLL.lua_type(luaState,index)==LuaTypes.LUA_TNIL);
		}
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool lua_isnumber(IntPtr luaState, int index);
		public static bool lua_isboolean(IntPtr luaState, int index)
		{
			return LuaDLL.lua_type(luaState,index)==LuaTypes.LUA_TBOOLEAN;
		}
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luaL_ref(IntPtr luaState, int registryIndex);
		public static int lua_ref(IntPtr luaState, int lockRef)
		{
			if(lockRef!=0)
			{
				return LuaDLL.luaL_ref(luaState,LuaIndexes.LUA_REGISTRYINDEX);
			}
			else return 0;
		}
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_rawgeti(IntPtr luaState, int tableIndex, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_rawseti(IntPtr luaState, int tableIndex, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr lua_newuserdata(IntPtr luaState, int size);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr lua_touserdata(IntPtr luaState, int index);
		public static void lua_getref(IntPtr luaState, int reference)
		{
			LuaDLL.lua_rawgeti(luaState,LuaIndexes.LUA_REGISTRYINDEX,reference);
		}
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void luaL_unref(IntPtr luaState, int registryIndex, int reference);
		public static void lua_unref(IntPtr luaState, int reference)
		{
			LuaDLL.luaL_unref(luaState,LuaIndexes.LUA_REGISTRYINDEX,reference);
		}
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool lua_isstring(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool lua_iscfunction(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushnil(IntPtr luaState);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushstdcallcfunction(IntPtr luaState, [MarshalAs(UnmanagedType.FunctionPtr)]LuaCSFunction function);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_call(IntPtr luaState, int nArgs, int nResults);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_rawcall(IntPtr luaState, int nArgs, int nResults);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr lua_tocfunction(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern double lua_tonumber(IntPtr luaState, int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool lua_toboolean(IntPtr luaState, int index);

		[DllImport(LUADLL,CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tolstring(IntPtr luaState, int index, out int strLen);

		public static string lua_tostring(IntPtr luaState, int index)
		{
            int strlen;

            IntPtr str = lua_tolstring(luaState, index, out strlen);
            if (str != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(str, strlen);
            else
                return null;            // treat lua nulls to as C# nulls
		}

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_atpanic(IntPtr luaState, LuaFunctionCallback panicf);

		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushnumber(IntPtr luaState, double number);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushboolean(IntPtr luaState, bool value);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushlstring(IntPtr luaState, string str, int size);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushstring(IntPtr luaState, string str);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luaL_newmetatable(IntPtr luaState, string meta);
		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfield(IntPtr luaState, int stackPos, string meta);
		public static void luaL_getmetatable(IntPtr luaState, string meta)
		{
			LuaDLL.lua_getfield(luaState, LuaIndexes.LUA_REGISTRYINDEX, meta);
		}
		// steffenj: END Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		[DllImport(LUALIBDLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_checkudata(IntPtr luaState, int stackPos, string meta);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool luaL_getmetafield(IntPtr luaState, int stackPos, string field);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_load(IntPtr luaState, LuaChunkReader chunkReader, ref ReaderInfo data, string chunkName);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luaL_loadbuffer(IntPtr luaState, string buff, int size, string name);
		[DllImport(LUALIBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luaL_loadfile(IntPtr luaState, string filename);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern bool luaL_checkmetatable(IntPtr luaState,int obj);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luanet_tonetobject(IntPtr luaState,int obj);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luanet_newudata(IntPtr luaState,int val);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luanet_rawnetobj(IntPtr luaState,int obj);
		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int luanet_checkudata(IntPtr luaState,int obj,string meta);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_error(IntPtr luaState);
		[DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_checkstack(IntPtr luaState,int extra);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern int lua_next(IntPtr luaState,int index);
		[DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern void lua_pushlightuserdata(IntPtr luaState, IntPtr udata);
 		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr luanet_gettag();
        [DllImport(LUADLL,CallingConvention=CallingConvention.Cdecl)]
        public static extern void luaL_where (IntPtr luaState, int level);
	}
}
