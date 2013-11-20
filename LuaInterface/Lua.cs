
namespace LuaInterface
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Threading;
	using OpenRA;
	using System.Text;

    /*
     * Main class of LuaInterface
     * Object-oriented wrapper to Lua API
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     *
     * // steffenj: important changes in Lua class:
     * - removed all Open*Lib() functions
     * - all libs automatically open in the Lua class constructor (just assign nil to unwanted libs)
     * */
    public class Lua : IDisposable
    {
        /*readonly */ IntPtr luaState;
        ObjectTranslator translator;

        LuaFunctionCallback panicCallback;
        LuaCSFunction tracebackFunction;
        // lockCallback, unlockCallback; used by debug code commented out for now

        public Lua()
        {
            luaState = LuaDLL.luaL_newstate();	// steffenj: Lua 5.1.1 API change (lua_open is gone)

            // Load libraries
            LuaDLL.luaL_openlibs(luaState);

            // Add LuaInterface marker
            LuaDLL.lua_pushstring(luaState, "LUAINTERFACE LOADED");
            LuaDLL.lua_pushboolean(luaState, true);
            LuaDLL.lua_settable(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);

            translator=new ObjectTranslator(this,luaState);

            tracebackFunction = new LuaCSFunction(traceback);

            // We need to keep this in a managed reference so the delegate doesn't get garbage collected
            panicCallback = new LuaFunctionCallback(PanicCallback);
            LuaDLL.lua_atpanic(luaState, panicCallback);
        }

        private bool _StatePassed;

        /*
         * CAUTION: LuaInterface.Lua instances can't share the same lua state!
         */
        public Lua( Int64 luaState )
        {
             // Check for existing LuaInterface marker
            IntPtr lState = new IntPtr(luaState);
            LuaDLL.lua_pushstring(lState, "LUAINTERFACE LOADED");
            LuaDLL.lua_gettable(lState, (int)LuaIndexes.LUA_REGISTRYINDEX);

            if(LuaDLL.lua_toboolean(lState,-1))
            {
                LuaDLL.lua_settop(lState,-2);
                throw new LuaException("There is already a LuaInterface.Lua instance associated with this Lua state");
            }
            else
            {
                // Add LuaInterface marker
                LuaDLL.lua_settop(lState,-2);
                LuaDLL.lua_pushstring(lState, "LUAINTERFACE LOADED");
                LuaDLL.lua_pushboolean(lState, true);
                LuaDLL.lua_settable(lState, (int)LuaIndexes.LUA_REGISTRYINDEX);

                this.luaState = lState;
                LuaDLL.lua_pushvalue(lState, (int)LuaIndexes.LUA_GLOBALSINDEX);

                translator = new ObjectTranslator(this, this.luaState);
            }

            _StatePassed = true;
        }

        public void Close()
        {
            if (_StatePassed)
                return;

            if (luaState != IntPtr.Zero)
                LuaDLL.lua_close(luaState);
            //luaState = IntPtr.Zero; <- suggested by Christopher Cebulski http://luaforge.net/forum/forum.php?thread_id=44593&forum_id=146
        }

        static int PanicCallback(IntPtr luaState)
        {
            // string desc = LuaDLL.lua_tostring(luaState, 1);
            string reason = String.Format("unprotected error in call to Lua API ({0})", LuaDLL.lua_tostring(luaState, -1));

           //        lua_tostring(L, -1);

            throw new LuaException(reason);
        }



        /// <summary>
        /// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
        /// </summary>
        /// <exception cref="LuaScriptException">Thrown if the script caused an exception</exception>
        void ThrowExceptionFromError(int oldTop)
        {
            object err = translator.getObject(luaState, -1);
            LuaDLL.lua_settop(luaState, oldTop);

            // A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
            LuaScriptException luaEx = err as LuaScriptException;
            if (luaEx != null) throw luaEx;

            // A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
            if (err == null) err = "Unknown Lua Error";
            throw new LuaScriptException(err.ToString(), "");
        }



        /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name="e">null for no pending exception</param>
        internal int SetPendingException(Exception e)
        {
            Exception caughtExcept = e;

            if (caughtExcept != null)
            {
                translator.throwError(luaState, caughtExcept);
                LuaDLL.lua_pushnil(luaState);

                return 1;
            }
            else
                return 0;
        }

        private bool executing;

        /// <summary>
        /// True while a script is being executed
        /// </summary>
        public bool IsExecuting { get { return executing; } }

        /// <summary>
        ///
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public LuaFunction LoadString(string chunk, string name)
        {
            int oldTop = LuaDLL.lua_gettop(luaState);

            executing = true;

			// Somehow, on OS X, we need to use the UTF-8 byte count rather than the string length
			// Adapted for OpenRA
			var chunkLength = Platform.CurrentPlatform != PlatformType.Windows
				? Encoding.UTF8.GetByteCount(chunk)
				: chunk.Length;

            try
            {
                if (LuaDLL.luaL_loadbuffer(luaState, chunk, chunkLength, name) != 0)
                    ThrowExceptionFromError(oldTop);
            }
            finally { executing = false; }

            LuaFunction result = translator.getFunction(luaState, -1);
            translator.popValues(luaState, oldTop);

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public LuaFunction LoadFile(string fileName)
        {
            int oldTop = LuaDLL.lua_gettop(luaState);
            if (LuaDLL.luaL_loadfile(luaState, fileName) != 0)
                ThrowExceptionFromError(oldTop);

            LuaFunction result = translator.getFunction(luaState, -1);
            translator.popValues(luaState, oldTop);

            return result;
        }


        /*
         * Excutes a Lua chunk and returns all the chunk's return
         * values in an array
         */
        public object[] DoString(string chunk)
        {
            return DoString(chunk,"chunk");
        }

        /// <summary>
        /// Executes a Lua chnk and returns all the chunk's return values in an array.
        /// </summary>
        /// <param name="chunk">Chunk to execute</param>
        /// <param name="chunkName">Name to associate with the chunk</param>
        /// <returns></returns>
        public object[] DoString(string chunk, string chunkName)
        {
            int oldTop = LuaDLL.lua_gettop(luaState);
            executing = true;

			// Somehow, on OS X, we need to use the UTF-8 byte count rather than the string length
			// Adapted for OpenRA
			var chunkLength = Platform.CurrentPlatform != PlatformType.Windows
				? Encoding.UTF8.GetByteCount(chunk)
				: chunk.Length;

            if (LuaDLL.luaL_loadbuffer(luaState, chunk, chunkLength, chunkName) == 0)
            {
                try
                {
                    if (LuaDLL.lua_pcall(luaState, 0, -1, 0) == 0)
                        return translator.popValues(luaState, oldTop);
                    else
                        ThrowExceptionFromError(oldTop);
                }
                finally { executing = false; }
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;            // Never reached - keeps compiler happy
        }

        private int traceback(IntPtr luaState)
        {
            LuaDLL.lua_getglobal(luaState,"debug");
            LuaDLL.lua_getfield(luaState,-1,"traceback");
            LuaDLL.lua_pushvalue(luaState,1);
            LuaDLL.lua_pushnumber(luaState,2);
            LuaDLL.lua_call (luaState,2,1);
            return 1;
        }

        /*
         * Excutes a Lua file and returns all the chunk's return
         * values in an array
         */
        public object[] DoFile(string fileName)
        {
            LuaDLL.lua_pushstdcallcfunction(luaState,tracebackFunction);
            int oldTop=LuaDLL.lua_gettop(luaState);
            if(LuaDLL.luaL_loadfile(luaState,fileName)==0)
            {
                executing = true;
                try
                {
                    if (LuaDLL.lua_pcall(luaState, 0, -1, -2) == 0)
                        return translator.popValues(luaState, oldTop);
                    else
                        ThrowExceptionFromError(oldTop);
                }
                finally { executing = false; }
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;            // Never reached - keeps compiler happy
        }


        public void CollectGarbage()
        {
            LuaDLL.lua_gc( luaState, LuaGCOptions.LUA_GCCOLLECT, 0 );
        }

        /*
         * Indexer for global variables from the LuaInterpreter
         * Supports navigation of tables by using . operator
         */
        public object this[string fullPath]
        {
            get
            {
                object returnValue=null;
                int oldTop=LuaDLL.lua_gettop(luaState);
                string[] path=fullPath.Split(new char[] { '.' });
                LuaDLL.lua_getglobal(luaState,path[0]);
                returnValue=translator.getObject(luaState,-1);
                if(path.Length>1)
                {
                    string[] remainingPath=new string[path.Length-1];
                    Array.Copy(path,1,remainingPath,0,path.Length-1);
                    returnValue=getObject(remainingPath);
                }
                LuaDLL.lua_settop(luaState,oldTop);
                return returnValue;
            }
            set
            {
                int oldTop=LuaDLL.lua_gettop(luaState);
                string[] path=fullPath.Split(new char[] { '.' });
                if(path.Length==1)
                {
                    translator.push(luaState,value);
                    LuaDLL.lua_setglobal(luaState,fullPath);
                }
                else
                {
                    LuaDLL.lua_getglobal(luaState,path[0]);
                    string[] remainingPath=new string[path.Length-1];
                    Array.Copy(path,1,remainingPath,0,path.Length-1);
                    setObject(remainingPath,value);
                }
                LuaDLL.lua_settop(luaState,oldTop);

                // Globals auto-complete
                if (value == null)
                {
                    // Remove now obsolete entries
                    globals.Remove(fullPath);
                }
                else
                {
                    // Add new entries
                    if (!globals.Contains(fullPath))
                        registerGlobal(fullPath, value.GetType(), 0);
                }
            }
        }

        #region Globals auto-complete
        private readonly List<string> globals = new List<string>();
        private bool globalsSorted;

        /// <summary>
        /// An alphabetically sorted list of all globals (objects, methods, etc.) externally added to this Lua instance
        /// </summary>
        /// <remarks>Members of globals are also listed. The formatting is optimized for text input auto-completion.</remarks>
        public IEnumerable<string> Globals
        {
            get
            {
                // Only sort list when necessary
                if (!globalsSorted)
                {
                    globals.Sort();
                    globalsSorted = true;
                }

                return globals;
            }
        }

        /// <summary>
        /// Adds an entry to <see cref="globals"/> (recursivley handles 2 levels of members)
        /// </summary>
        /// <param name="path">The index accessor path ot the entry</param>
        /// <param name="type">The type of the entry</param>
        /// <param name="recursionCounter">How deep have we gone with recursion?</param>
        private void registerGlobal(string path, Type type, int recursionCounter)
        {
            // If the type is a global method, list it directly
            if (type == typeof(LuaCSFunction))
            {
                // Format for easy method invocation
                globals.Add(path + "(");
            }
            // If the type is a class or an interface and recursion hasn't been running too long, list the members
            else if ((type.IsClass || type.IsInterface) && type != typeof(string) && recursionCounter < 2)
            {
                #region Methods
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (method.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
                        (method.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0) &&
                        // Exclude some generic .NET methods that wouldn't be very usefull in Lua
                        method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
                        method.Name != "ToString" && method.Name != "Clone" && method.Name != "Dispose" &&
                        method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
                        !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                        !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                        !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                        !method.Name.StartsWith("remove_", StringComparison.Ordinal))
                    {
                        // Format for easy method invocation
                        string command = path + ":" + method.Name + "(";
                        if (method.GetParameters().Length == 0) command += ")";
                        globals.Add(command);
                    }
                }
                #endregion

                #region Fields
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (field.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
                        (field.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0))
                    {
                        // Go into recursion for members
                        registerGlobal(path + "." + field.Name, field.FieldType, recursionCounter + 1);
                    }
                }
                #endregion

                #region Properties
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (property.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
                        (property.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0)
                        // Exclude some generic .NET properties that wouldn't be very usefull in Lua
                        && property.Name != "Item")
                    {
                        // Go into recursion for members
                        registerGlobal(path + "." + property.Name, property.PropertyType, recursionCounter + 1);
                    }
                }
                #endregion
            }
            // Otherwise simply add the element to the list
            else globals.Add(path);

            // List will need to be sorted on next access
            globalsSorted = false;
        }
        #endregion

        /*
         * Navigates a table in the top of the stack, returning
         * the value of the specified field
         */
        internal object getObject(string[] remainingPath)
        {
            object returnValue=null;
            for(int i=0;i<remainingPath.Length;i++)
            {
                LuaDLL.lua_pushstring(luaState,remainingPath[i]);
                LuaDLL.lua_gettable(luaState,-2);
                returnValue=translator.getObject(luaState,-1);
                if(returnValue==null) break;
            }
            return returnValue;
        }
        /*
         * Gets a numeric global variable
         */
        public double GetNumber(string fullPath)
        {
            return (double)this[fullPath];
        }
        /*
         * Gets a string global variable
         */
        public string GetString(string fullPath)
        {
            return (string)this[fullPath];
        }
        /*
         * Gets a table global variable
         */
        public LuaTable GetTable(string fullPath)
        {
            return (LuaTable)this[fullPath];
        }

        /*
         * Gets a table global variable as an object implementing
         * the interfaceType interface
         */
        public object GetTable(Type interfaceType, string fullPath)
        {
            return CodeGeneration.Instance.GetClassInstance(interfaceType, GetTable(fullPath));
        }
        /*
         * Gets a function global variable
         */
        public LuaFunction GetFunction(string fullPath)
        {
            object obj = this[fullPath];
            return (obj is LuaCSFunction ? new LuaFunction((LuaCSFunction)obj, this) : (LuaFunction)obj);
        }
        /*
         * Gets a function global variable as a delegate of
         * type delegateType
         */
        public Delegate GetFunction(Type delegateType,string fullPath)
        {
            return CodeGeneration.Instance.GetDelegate(delegateType, GetFunction(fullPath));
        }
        /*
         * Calls the object as a function with the provided arguments,
         * returning the function's returned values inside an array
         */
        internal object[] callFunction(object function,object[] args)
        {
            return callFunction(function, args, null);
        }


        /*
         * Calls the object as a function with the provided arguments and
         * casting returned values to the types in returnTypes before returning
         * them in an array
         */
        internal object[] callFunction(object function,object[] args,Type[] returnTypes)
        {
            int nArgs=0;
            int oldTop=LuaDLL.lua_gettop(luaState);
            if(!LuaDLL.lua_checkstack(luaState,args.Length+6))
                throw new LuaException("Lua stack overflow");
            translator.push(luaState,function);
            if(args!=null)
            {
                nArgs=args.Length;
                for(int i=0;i<args.Length;i++)
                {
                    translator.push(luaState,args[i]);
                }
            }
            executing = true;
            try
            {
                int error = LuaDLL.lua_pcall(luaState, nArgs, -1, 0);
                if (error != 0)
                    ThrowExceptionFromError(oldTop);
            }
            finally { executing = false; }

            if(returnTypes != null)
                return translator.popValues(luaState,oldTop,returnTypes);
            else
                return translator.popValues(luaState, oldTop);
        }
        /*
         * Navigates a table to set the value of one of its fields
         */
        internal void setObject(string[] remainingPath, object val)
        {
            for(int i=0; i<remainingPath.Length-1;i++)
            {
                LuaDLL.lua_pushstring(luaState,remainingPath[i]);
                LuaDLL.lua_gettable(luaState,-2);
            }
            LuaDLL.lua_pushstring(luaState,remainingPath[remainingPath.Length-1]);
            translator.push(luaState,val);
            LuaDLL.lua_settable(luaState,-3);
        }
        /*
         * Creates a new table as a global variable or as a field
         * inside an existing table
         */
        public void NewTable(string fullPath)
        {
            string[] path=fullPath.Split(new char[] { '.' });
            int oldTop=LuaDLL.lua_gettop(luaState);
            if(path.Length==1)
            {
                LuaDLL.lua_newtable(luaState);
                LuaDLL.lua_setglobal(luaState,fullPath);
            }
            else
            {
                LuaDLL.lua_getglobal(luaState,path[0]);
                for(int i=1; i<path.Length-1;i++)
                {
                    LuaDLL.lua_pushstring(luaState,path[i]);
                    LuaDLL.lua_gettable(luaState,-2);
                }
                LuaDLL.lua_pushstring(luaState,path[path.Length-1]);
                LuaDLL.lua_newtable(luaState);
                LuaDLL.lua_settable(luaState,-3);
            }
            LuaDLL.lua_settop(luaState,oldTop);
        }

		/*
		 * Creates a new unnamed table
		 */
		public LuaTable NewTable()
		{
			int oldTop = LuaDLL.lua_gettop( luaState );
			LuaDLL.lua_newtable( luaState );
			LuaTable table = translator.getTable( luaState,-1 );
			LuaDLL.lua_settop( luaState, oldTop );

			table.IsOrphaned = true;

			return table;
		}

        public ListDictionary GetTableDict(LuaTable table)
        {
            ListDictionary dict = new ListDictionary();

            int oldTop = LuaDLL.lua_gettop(luaState);
            translator.push(luaState, table);
            LuaDLL.lua_pushnil(luaState);
            while (LuaDLL.lua_next(luaState, -2) != 0)
            {
                dict[translator.getObject(luaState, -2)] = translator.getObject(luaState, -1);
                LuaDLL.lua_settop(luaState, -2);
            }
            LuaDLL.lua_settop(luaState, oldTop);

            return dict;
        }

        /*
         * Lets go of a previously allocated reference to a table, function
         * or userdata
         */

        internal void dispose(int reference)
        {
            if (luaState != IntPtr.Zero) //Fix submitted by Qingrui Li
                LuaDLL.lua_unref(luaState,reference);
        }
        /*
         * Gets a field of the table corresponding to the provided reference
         * using rawget (do not use metatables)
         */
        internal object rawGetObject(int reference,string field)
        {
            int oldTop=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,reference);
            LuaDLL.lua_pushstring(luaState,field);
            LuaDLL.lua_rawget(luaState,-2);
            object obj=translator.getObject(luaState,-1);
            LuaDLL.lua_settop(luaState,oldTop);
            return obj;
        }
        /*
         * Gets a field of the table or userdata corresponding to the provided reference
         */
        internal object getObject(int reference,string field)
        {
            int oldTop=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,reference);
            object returnValue=getObject(field.Split(new char[] {'.'}));
            LuaDLL.lua_settop(luaState,oldTop);
            return returnValue;
        }
        /*
         * Gets a numeric field of the table or userdata corresponding the the provided reference
         */
        internal object getObject(int reference,object field)
        {
            int oldTop=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,reference);
            translator.push(luaState,field);
            LuaDLL.lua_gettable(luaState,-2);
            object returnValue=translator.getObject(luaState,-1);
            LuaDLL.lua_settop(luaState,oldTop);
            return returnValue;
        }
        /*
         * Sets a field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void setObject(int reference, string field, object val)
        {
            int oldTop=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,reference);
            setObject(field.Split(new char[] {'.'}),val);
            LuaDLL.lua_settop(luaState,oldTop);
        }
        /*
         * Sets a numeric field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void setObject(int reference, object field, object val)
        {
            int oldTop=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,reference);
            translator.push(luaState,field);
            translator.push(luaState,val);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_settop(luaState,oldTop);
        }

        /*
         * Registers an object's method as a Lua function (global or table field)
         * The method may have any signature
         */
        public LuaFunction RegisterFunction(string path, object target, MethodBase function /*MethodInfo function*/)  //CP: Fix for struct constructor by Alexander Kappner (link: http://luaforge.net/forum/forum.php?thread_id=2859&forum_id=145)
        {
            // We leave nothing on the stack when we are done
            int oldTop = LuaDLL.lua_gettop(luaState);

            LuaMethodWrapper wrapper=new LuaMethodWrapper(translator,target,function.DeclaringType,function);
            translator.push(luaState,new LuaCSFunction(wrapper.call));

            this[path]=translator.getObject(luaState,-1);
            LuaFunction f = GetFunction(path);

            LuaDLL.lua_settop(luaState, oldTop);

            return f;
        }


        /*
         * Compares the two values referenced by ref1 and ref2 for equality
         */
        internal bool compareRef(int ref1, int ref2)
        {
            int top=LuaDLL.lua_gettop(luaState);
            LuaDLL.lua_getref(luaState,ref1);
            LuaDLL.lua_getref(luaState,ref2);
            int equal=LuaDLL.lua_equal(luaState,-1,-2);
            LuaDLL.lua_settop(luaState,top);
            return (equal!=0);
        }

        internal void pushCSFunction(LuaCSFunction function)
        {
            translator.pushFunction(luaState,function);
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (translator != null)
            {
                translator.pendingEvents.Dispose();
                translator = null;
            }

            this.Close();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        #endregion
   }



}
