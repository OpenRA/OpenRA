namespace LuaInterface
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;

    /*
     * Passes objects from the CLR to Lua and vice-versa
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    public class ObjectTranslator
    {
        internal CheckType typeChecker;

        // object # to object (FIXME - it should be possible to get object address as an object #)
        public readonly Dictionary<int, object> objects = new Dictionary<int, object>();
        // object to object #
        public readonly Dictionary<object, int> objectsBackMap = new Dictionary<object, int>();
        internal Lua interpreter;
        private MetaFunctions metaFunctions;
        private List<Assembly> assemblies;
        private LuaCSFunction getMethodSigFunction, getConstructorSigFunction, ctypeFunction, enumFromIntFunction;

        internal EventHandlerContainer pendingEvents = new EventHandlerContainer();

        public ObjectTranslator(Lua interpreter,IntPtr luaState)
        {
            this.interpreter = interpreter;
            typeChecker = new CheckType(this);
            metaFunctions = new MetaFunctions(this);
            assemblies = new List<Assembly>();

            getMethodSigFunction= getMethodSignature;
            getConstructorSigFunction= getConstructorSignature;

            ctypeFunction = ctype;
            enumFromIntFunction = enumFromInt;

            createLuaObjectList(luaState);
            createIndexingMetaFunction(luaState);
            createBaseClassMetatable(luaState);
            createClassMetatable(luaState);
            createFunctionMetatable(luaState);
            setGlobalFunctions(luaState);
        }

        /*
         * Sets up the list of objects in the Lua side
         */
        private void createLuaObjectList(IntPtr luaState)
        {
            LuaDLL.lua_pushstring(luaState,"luaNet_objects");
            LuaDLL.lua_newtable(luaState);
            LuaDLL.lua_newtable(luaState);
            LuaDLL.lua_pushstring(luaState,"__mode");
            LuaDLL.lua_pushstring(luaState,"v");
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_setmetatable(luaState,-2);
            LuaDLL.lua_settable(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
        }
        /*
         * Registers the indexing function of CLR objects
         * passed to Lua
         */
        private void createIndexingMetaFunction(IntPtr luaState)
        {
            LuaDLL.lua_pushstring(luaState,"luaNet_indexfunction");
            LuaDLL.luaL_dostring(luaState,MetaFunctions.luaIndexFunction);	// steffenj: lua_dostring renamed to luaL_dostring
            //LuaDLL.lua_pushstdcallcfunction(luaState,indexFunction);
            LuaDLL.lua_rawset(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
        }
        /*
         * Creates the metatable for superclasses (the base
         * field of registered tables)
         */
        private void createBaseClassMetatable(IntPtr luaState)
        {
            LuaDLL.luaL_newmetatable(luaState,"luaNet_searchbase");
            LuaDLL.lua_pushstring(luaState,"__gc");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__tostring");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__index");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.baseIndexFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__newindex");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_settop(luaState,-2);
        }
        /*
         * Creates the metatable for type references
         */
        private void createClassMetatable(IntPtr luaState)
        {
            LuaDLL.luaL_newmetatable(luaState,"luaNet_class");
            LuaDLL.lua_pushstring(luaState,"__gc");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__tostring");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__index");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.classIndexFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__newindex");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.classNewindexFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__call");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.callConstructorFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_settop(luaState,-2);
        }
        /*
         * Registers the global functions used by LuaInterface
         */
        private void setGlobalFunctions(IntPtr luaState)
        {
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.indexFunction);
            LuaDLL.lua_setglobal(luaState,"get_object_member");
            /*LuaDLL.lua_pushstdcallcfunction(luaState,importTypeFunction);
            LuaDLL.lua_setglobal(luaState,"import_type");
            LuaDLL.lua_pushstdcallcfunction(luaState,loadAssemblyFunction);
            LuaDLL.lua_setglobal(luaState,"load_assembly");
            LuaDLL.lua_pushstdcallcfunction(luaState,registerTableFunction);
            LuaDLL.lua_setglobal(luaState,"make_object");
            LuaDLL.lua_pushstdcallcfunction(luaState,unregisterTableFunction);
            LuaDLL.lua_setglobal(luaState,"free_object");*/
            LuaDLL.lua_pushstdcallcfunction(luaState,getMethodSigFunction);
            LuaDLL.lua_setglobal(luaState,"get_method_bysig");
            LuaDLL.lua_pushstdcallcfunction(luaState,getConstructorSigFunction);
            LuaDLL.lua_setglobal(luaState,"get_constructor_bysig");
            LuaDLL.lua_pushstdcallcfunction(luaState,ctypeFunction);
            LuaDLL.lua_setglobal(luaState,"ctype");
            LuaDLL.lua_pushstdcallcfunction(luaState,enumFromIntFunction);
            LuaDLL.lua_setglobal(luaState,"enum");

        }

        /*
         * Creates the metatable for delegates
         */
        private void createFunctionMetatable(IntPtr luaState)
        {
            LuaDLL.luaL_newmetatable(luaState,"luaNet_function");
            LuaDLL.lua_pushstring(luaState,"__gc");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_pushstring(luaState,"__call");
            LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.execDelegateFunction);
            LuaDLL.lua_settable(luaState,-3);
            LuaDLL.lua_settop(luaState,-2);
        }
        /*
         * Passes errors (argument e) to the Lua interpreter
         */
        internal void throwError(IntPtr luaState, object e)
        {
            // We use this to remove anything pushed by luaL_where
            int oldTop = LuaDLL.lua_gettop(luaState);

            // Stack frame #1 is our C# wrapper, so not very interesting to the user
            // Stack frame #2 must be the lua code that called us, so that's what we want to use
            LuaDLL.luaL_where(luaState, 1);
            object[] curlev = popValues(luaState, oldTop);

            // Determine the position in the script where the exception was triggered
            string errLocation = "";
            if (curlev.Length > 0)
                errLocation = curlev[0].ToString();

            string message = e as string;
            if (message != null)
            {
                // Wrap Lua error (just a string) and store the error location
                e = new LuaScriptException(message, errLocation);
            }
            else
            {
                Exception ex = e as Exception;
                if (ex != null)
                {
                    // Wrap generic .NET exception as an InnerException and store the error location
                    e = new LuaScriptException(ex, errLocation);
                }
            }

            push(luaState, e);
            LuaDLL.lua_error(luaState);
        }
        /*
         * Implementation of load_assembly. Throws an error
         * if the assembly is not found.
         */
        private int loadAssembly(IntPtr luaState)
        {
            try
            {
                string assemblyName = LuaDLL.lua_tostring(luaState,1);

                Assembly assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));

                if (assembly != null && !assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
            }
            catch(Exception e)
            {
                throwError(luaState,e);
            }

            return 0;
        }

        internal Type FindType(string className)
        {
            foreach(Assembly assembly in assemblies)
            {
                Type klass=assembly.GetType(className);
                if(klass!=null)
                {
                    return klass;
                }
            }
            return null;
        }

        /*
         * Implementation of import_type. Returns nil if the
         * type is not found.
         */
        private int importType(IntPtr luaState)
        {
            string className=LuaDLL.lua_tostring(luaState,1);
            Type klass=FindType(className);
            if(klass!=null)
                pushType(luaState,klass);
            else
                LuaDLL.lua_pushnil(luaState);
            return 1;
        }
        /*
         * Implementation of make_object. Registers a table (first
         * argument in the stack) as an object subclassing the
         * type passed as second argument in the stack.
         */
        private int registerTable(IntPtr luaState)
        {
            if(LuaDLL.lua_type(luaState,1)==LuaTypes.LUA_TTABLE)
            {
                LuaTable luaTable=getTable(luaState,1);
                string superclassName = LuaDLL.lua_tostring(luaState, 2);
                if (superclassName != null)
                {
                    Type klass = FindType(superclassName);
                    if (klass != null)
                    {
                        // Creates and pushes the object in the stack, setting
                        // it as the  metatable of the first argument
                        object obj = CodeGeneration.Instance.GetClassInstance(klass, luaTable);
                        pushObject(luaState, obj, "luaNet_metatable");
                        LuaDLL.lua_newtable(luaState);
                        LuaDLL.lua_pushstring(luaState, "__index");
                        LuaDLL.lua_pushvalue(luaState, -3);
                        LuaDLL.lua_settable(luaState, -3);
                        LuaDLL.lua_pushstring(luaState, "__newindex");
                        LuaDLL.lua_pushvalue(luaState, -3);
                        LuaDLL.lua_settable(luaState, -3);
                        LuaDLL.lua_setmetatable(luaState, 1);
                        // Pushes the object again, this time as the base field
                        // of the table and with the luaNet_searchbase metatable
                        LuaDLL.lua_pushstring(luaState, "base");
                        int index = addObject(obj);
                        pushNewObject(luaState, obj, index, "luaNet_searchbase");
                        LuaDLL.lua_rawset(luaState, 1);
                    }
                    else
                        throwError(luaState, "register_table: can not find superclass '" + superclassName + "'");
                }
                else
                    throwError(luaState, "register_table: superclass name can not be null");
            }
            else throwError(luaState,"register_table: first arg is not a table");
            return 0;
        }
        /*
         * Implementation of free_object. Clears the metatable and the
         * base field, freeing the created object for garbage-collection
         */
        private int unregisterTable(IntPtr luaState)
        {
            try
            {
                if(LuaDLL.lua_getmetatable(luaState,1)!=0)
                {
                    LuaDLL.lua_pushstring(luaState,"__index");
                    LuaDLL.lua_gettable(luaState,-2);
                    object obj=getRawNetObject(luaState,-1);
                    if(obj==null) throwError(luaState,"unregister_table: arg is not valid table");
                    FieldInfo luaTableField=obj.GetType().GetField("__luaInterface_luaTable");
                    if(luaTableField==null) throwError(luaState,"unregister_table: arg is not valid table");
                    luaTableField.SetValue(obj,null);
                    LuaDLL.lua_pushnil(luaState);
                    LuaDLL.lua_setmetatable(luaState,1);
                    LuaDLL.lua_pushstring(luaState,"base");
                    LuaDLL.lua_pushnil(luaState);
                    LuaDLL.lua_settable(luaState,1);
                }
                else throwError(luaState,"unregister_table: arg is not valid table");
            }
            catch(Exception e)
            {
                throwError(luaState,e.Message);
            }
            return 0;
        }
        /*
         * Implementation of get_method_bysig. Returns nil
         * if no matching method is not found.
         */
        private int getMethodSignature(IntPtr luaState)
        {
            IReflect klass; object target;
            int udata=LuaDLL.luanet_checkudata(luaState,1,"luaNet_class");
            if(udata!=-1)
            {
                klass=(IReflect)objects[udata];
                target=null;
            }
            else
            {
                target=getRawNetObject(luaState,1);
                if(target==null)
                {
                    throwError(luaState,"get_method_bysig: first arg is not type or object reference");
                    LuaDLL.lua_pushnil(luaState);
                    return 1;
                }
                klass=target.GetType();
            }
            string methodName=LuaDLL.lua_tostring(luaState,2);
            Type[] signature=new Type[LuaDLL.lua_gettop(luaState)-2];
            for(int i=0;i<signature.Length;i++)
                signature[i]=FindType(LuaDLL.lua_tostring(luaState,i+3));
            try
            {
                //CP: Added ignore case
                MethodInfo method=klass.GetMethod(methodName,BindingFlags.Public | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase, null, signature, null);
                pushFunction(luaState,new LuaCSFunction((new LuaMethodWrapper(this,target,klass,method)).call));
            }
            catch(Exception e)
            {
                throwError(luaState,e);
                LuaDLL.lua_pushnil(luaState);
            }
            return 1;
        }
        /*
         * Implementation of get_constructor_bysig. Returns nil
         * if no matching constructor is found.
         */
        private int getConstructorSignature(IntPtr luaState)
        {
            IReflect klass=null;
            int udata=LuaDLL.luanet_checkudata(luaState,1,"luaNet_class");
            if(udata!=-1)
            {
                klass=(IReflect)objects[udata];
            }
            if(klass==null)
            {
                throwError(luaState,"get_constructor_bysig: first arg is invalid type reference");
            }
            Type[] signature=new Type[LuaDLL.lua_gettop(luaState)-1];
            for(int i=0;i<signature.Length;i++)
                signature[i]=FindType(LuaDLL.lua_tostring(luaState,i+2));
            try
            {
                ConstructorInfo constructor=klass.UnderlyingSystemType.GetConstructor(signature);
                pushFunction(luaState,new LuaCSFunction((new LuaMethodWrapper(this,null,klass,constructor)).call));
            }
            catch(Exception e)
            {
                throwError(luaState,e);
                LuaDLL.lua_pushnil(luaState);
            }
            return 1;
        }

        private Type typeOf(IntPtr luaState, int idx)
        {
            int udata=LuaDLL.luanet_checkudata(luaState,1,"luaNet_class");
            if (udata == -1) {
                return null;
            } else {
                ProxyType pt = (ProxyType)objects[udata];
                return pt.UnderlyingSystemType;
            }
        }

        public int pushError(IntPtr luaState, string msg)
        {
            LuaDLL.lua_pushnil(luaState);
            LuaDLL.lua_pushstring(luaState,msg);
            return 2;
        }

        private int ctype(IntPtr luaState)
        {
            Type t = typeOf(luaState,1);
            if (t == null) {
                return pushError(luaState,"not a CLR class");
            }
            pushObject(luaState,t,"luaNet_metatable");
            return 1;
        }

        private int enumFromInt(IntPtr luaState)
        {
            Type t = typeOf(luaState,1);
            if (t == null || ! t.IsEnum) {
                return pushError(luaState,"not an enum");
            }
            object res = null;
            LuaTypes lt = LuaDLL.lua_type(luaState,2);
            if (lt == LuaTypes.LUA_TNUMBER) {
                int ival = (int)LuaDLL.lua_tonumber(luaState,2);
                res = Enum.ToObject(t,ival);
            } else
            if (lt == LuaTypes.LUA_TSTRING) {
                string sflags = LuaDLL.lua_tostring(luaState,2);
                string err = null;
                try {
                    res = Enum.Parse(t,sflags);
                } catch (ArgumentException e) {
                    err = e.Message;
                }
                if (err != null) {
                    return pushError(luaState,err);
                }
            } else {
                return pushError(luaState,"second argument must be a integer or a string");
            }
            pushObject(luaState,res,"luaNet_metatable");
            return 1;
        }

        /*
         * Pushes a type reference into the stack
         */
        internal void pushType(IntPtr luaState, Type t)
        {
            pushObject(luaState,new ProxyType(t),"luaNet_class");
        }
        /*
         * Pushes a delegate into the stack
         */
        internal void pushFunction(IntPtr luaState, LuaCSFunction func)
        {
            pushObject(luaState,func,"luaNet_function");
        }
        /*
         * Pushes a CLR object into the Lua stack as an userdata
         * with the provided metatable
         */
        internal void pushObject(IntPtr luaState, object o, string metatable)
        {
            int index = -1;
            // Pushes nil
            if(o==null)
            {
                LuaDLL.lua_pushnil(luaState);
                return;
            }

            // Object already in the list of Lua objects? Push the stored reference.
            bool found = objectsBackMap.TryGetValue(o, out index);
            if(found)
            {
                LuaDLL.luaL_getmetatable(luaState,"luaNet_objects");
                LuaDLL.lua_rawgeti(luaState,-1,index);

                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
                // object here
                // did we find a non nil object in our table? if not, we need to call collect object
                LuaTypes type = LuaDLL.lua_type(luaState, -1);
                if (type != LuaTypes.LUA_TNIL)
                {
                    LuaDLL.lua_remove(luaState, -2);     // drop the metatable - we're going to leave our object on the stack

                    return;
                }

                // MetaFunctions.dumpStack(this, luaState);
                LuaDLL.lua_remove(luaState, -1);    // remove the nil object value
                LuaDLL.lua_remove(luaState, -1);    // remove the metatable

                collectObject(o, index);            // Remove from both our tables and fall out to get a new ID
            }
            index = addObject(o);

            pushNewObject(luaState,o,index,metatable);
        }


        /*
         * Pushes a new object into the Lua stack with the provided
         * metatable
         */
        private void pushNewObject(IntPtr luaState,object o,int index,string metatable)
        {
            if(metatable=="luaNet_metatable")
            {
                // Gets or creates the metatable for the object's type
                LuaDLL.luaL_getmetatable(luaState,o.GetType().AssemblyQualifiedName);

                if(LuaDLL.lua_isnil(luaState,-1))
                {
                    LuaDLL.lua_settop(luaState,-2);
                    LuaDLL.luaL_newmetatable(luaState,o.GetType().AssemblyQualifiedName);
                    LuaDLL.lua_pushstring(luaState,"cache");
                    LuaDLL.lua_newtable(luaState);
                    LuaDLL.lua_rawset(luaState,-3);
                    LuaDLL.lua_pushlightuserdata(luaState,LuaDLL.luanet_gettag());
                    LuaDLL.lua_pushnumber(luaState,1);
                    LuaDLL.lua_rawset(luaState,-3);
                    LuaDLL.lua_pushstring(luaState,"__index");
                    LuaDLL.lua_pushstring(luaState,"luaNet_indexfunction");
                    LuaDLL.lua_rawget(luaState, (int) LuaIndexes.LUA_REGISTRYINDEX);
                    LuaDLL.lua_rawset(luaState,-3);
                    LuaDLL.lua_pushstring(luaState,"__gc");
                    LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
                    LuaDLL.lua_rawset(luaState,-3);
                    LuaDLL.lua_pushstring(luaState,"__tostring");
                    LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
                    LuaDLL.lua_rawset(luaState,-3);
                    LuaDLL.lua_pushstring(luaState,"__newindex");
                    LuaDLL.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
                    LuaDLL.lua_rawset(luaState,-3);
                }
            }
            else
            {
                LuaDLL.luaL_getmetatable(luaState,metatable);
            }

            // Stores the object index in the Lua list and pushes the
            // index into the Lua stack
            LuaDLL.luaL_getmetatable(luaState,"luaNet_objects");
            LuaDLL.luanet_newudata(luaState,index);
            LuaDLL.lua_pushvalue(luaState,-3);
            LuaDLL.lua_remove(luaState,-4);
            LuaDLL.lua_setmetatable(luaState,-2);
            LuaDLL.lua_pushvalue(luaState,-1);
            LuaDLL.lua_rawseti(luaState,-3,index);
            LuaDLL.lua_remove(luaState,-2);
        }
        /*
         * Gets an object from the Lua stack with the desired type, if it matches, otherwise
         * returns null.
         */
        internal object getAsType(IntPtr luaState,int stackPos,Type paramType)
        {
            ExtractValue extractor=typeChecker.checkType(luaState,stackPos,paramType);
            if(extractor!=null) return extractor(luaState,stackPos);
            return null;
        }


        /// <summary>
        /// Given the Lua int ID for an object remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
        internal void collectObject(int udata)
        {
            object o;
            bool found = objects.TryGetValue(udata, out o);

            // The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
            if (found)
            {
                // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

                objects.Remove(udata);
                objectsBackMap.Remove(o);
            }
        }


        /// <summary>
        /// Given an object reference, remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
        void collectObject(object o, int udata)
        {
            // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

            objects.Remove(udata);
            objectsBackMap.Remove(o);
        }


        /// <summary>
        /// We want to ensure that objects always have a unique ID
        /// </summary>
        int nextObj = 0;

        int addObject(object obj)
        {
            // New object: inserts it in the list
            int index = nextObj++;

            // Debug.WriteLine("Adding " + obj.ToString() + " @ " + index);

            objects[index] = obj;
            objectsBackMap[obj] = index;

            return index;
        }



        /*
         * Gets an object from the Lua stack according to its Lua type.
         */
        internal object getObject(IntPtr luaState,int index)
        {
            LuaTypes type=LuaDLL.lua_type(luaState,index);
            switch(type)
            {
                case LuaTypes.LUA_TNUMBER:
                {
                    return LuaDLL.lua_tonumber(luaState,index);
                }
                case LuaTypes.LUA_TSTRING:
                {
                    return LuaDLL.lua_tostring(luaState,index);
                }
                case LuaTypes.LUA_TBOOLEAN:
                {
                    return LuaDLL.lua_toboolean(luaState,index);
                }
                case LuaTypes.LUA_TTABLE:
                {
                    return getTable(luaState,index);
                }
                case LuaTypes.LUA_TFUNCTION:
                {
                    return getFunction(luaState,index);
                }
                case LuaTypes.LUA_TUSERDATA:
                {
                    int udata=LuaDLL.luanet_tonetobject(luaState,index);
                    if(udata!=-1)
                        return objects[udata];
                    else
                        return getUserData(luaState,index);
                }
                default:
                    return null;
            }
        }
        /*
         * Gets the table in the index positon of the Lua stack.
         */
        internal LuaTable getTable(IntPtr luaState,int index)
        {
            LuaDLL.lua_pushvalue(luaState,index);
            return new LuaTable(LuaDLL.lua_ref(luaState,1),interpreter);
        }
        /*
         * Gets the userdata in the index positon of the Lua stack.
         */
        internal LuaUserData getUserData(IntPtr luaState,int index)
        {
            LuaDLL.lua_pushvalue(luaState,index);
            return new LuaUserData(LuaDLL.lua_ref(luaState,1),interpreter);
        }
        /*
         * Gets the function in the index positon of the Lua stack.
         */
        internal LuaFunction getFunction(IntPtr luaState,int index)
        {
            LuaDLL.lua_pushvalue(luaState,index);
            return new LuaFunction(LuaDLL.lua_ref(luaState,1),interpreter);
        }
        /*
         * Gets the CLR object in the index positon of the Lua stack. Returns
         * delegates as Lua functions.
         */
        internal object getNetObject(IntPtr luaState,int index)
        {
            int idx=LuaDLL.luanet_tonetobject(luaState,index);
            if(idx!=-1)
                return objects[idx];
            else
                return null;
        }
        /*
         * Gets the CLR object in the index positon of the Lua stack. Returns
         * delegates as is.
         */
        internal object getRawNetObject(IntPtr luaState,int index)
        {
            int udata=LuaDLL.luanet_rawnetobj(luaState,index);
            if(udata!=-1)
            {
                return objects[udata];
            }
            return null;
        }
        /*
         * Pushes the entire array into the Lua stack and returns the number
         * of elements pushed.
         */
        internal int returnValues(IntPtr luaState, object[] returnValues)
        {
            if(LuaDLL.lua_checkstack(luaState,returnValues.Length+5))
            {
                for(int i=0;i<returnValues.Length;i++)
                {
                    push(luaState,returnValues[i]);
                }
                return returnValues.Length;
            } else
                return 0;
        }
        /*
         * Gets the values from the provided index to
         * the top of the stack and returns them in an array.
         */
        internal object[] popValues(IntPtr luaState,int oldTop)
        {
            int newTop=LuaDLL.lua_gettop(luaState);
            if(oldTop==newTop)
            {
                return null;
            }
            else
            {
                ArrayList returnValues=new ArrayList();
                for(int i=oldTop+1;i<=newTop;i++)
                {
                    returnValues.Add(getObject(luaState,i));
                }
                LuaDLL.lua_settop(luaState,oldTop);
                return returnValues.ToArray();
            }
        }
        /*
         * Gets the values from the provided index to
         * the top of the stack and returns them in an array, casting
         * them to the provided types.
         */
        internal object[] popValues(IntPtr luaState,int oldTop,Type[] popTypes)
        {
            int newTop=LuaDLL.lua_gettop(luaState);
            if(oldTop==newTop)
            {
                return null;
            }
            else
            {
                int iTypes;
                ArrayList returnValues=new ArrayList();
                if(popTypes[0] == typeof(void))
                    iTypes=1;
                else
                    iTypes=0;
                for(int i=oldTop+1;i<=newTop;i++)
                {
                    returnValues.Add(getAsType(luaState,i,popTypes[iTypes]));
                    iTypes++;
                }
                LuaDLL.lua_settop(luaState,oldTop);
                return returnValues.ToArray();
            }
        }

        // kevinh - the following line doesn't work for remoting proxies - they always return a match for 'is'
        // else if(o is ILuaGeneratedType)
        static bool IsILua(object o)
        {
            if (o is ILuaGeneratedType)
            {
                // Make sure we are _really_ ILuaGenerated
                Type type = o.GetType();

                return type.GetInterface("ILuaGeneratedType") != null;
            }
            return false;
        }

        /*
         * Pushes the object into the Lua stack according to its type.
         */
        internal void push(IntPtr luaState, object o)
        {
            if(o==null)
            {
                LuaDLL.lua_pushnil(luaState);
            }
            else if(o is sbyte || o is byte || o is short || o is ushort ||
                o is int || o is uint || o is long || o is float ||
                o is ulong || o is decimal || o is double)
            {
                double d=Convert.ToDouble(o);
                LuaDLL.lua_pushnumber(luaState,d);
            }
            else if(o is char)
            {
                double d = (char)o;
                LuaDLL.lua_pushnumber(luaState,d);
            }
            else if(o is string)
            {
                string str=(string)o;
                LuaDLL.lua_pushstring(luaState,str);
            }
            else if(o is bool)
            {
                bool b=(bool)o;
                LuaDLL.lua_pushboolean(luaState,b);
            }
            else if(IsILua(o))
            {
                (((ILuaGeneratedType)o).__luaInterface_getLuaTable()).push(luaState);
            }
            else if(o is LuaTable)
            {
                ((LuaTable)o).push(luaState);
            }
            else if(o is LuaCSFunction)
            {
                pushFunction(luaState,(LuaCSFunction)o);
            }
            else if(o is LuaFunction)
            {
                ((LuaFunction)o).push(luaState);
            }
            else
            {
                pushObject(luaState,o,"luaNet_metatable");
            }
        }
        /*
         * Checks if the method matches the arguments in the Lua stack, getting
         * the arguments if it does.
         */
        internal bool matchParameters(IntPtr luaState,MethodBase method,ref MethodCache methodCache)
        {
            return metaFunctions.matchParameters(luaState,method,ref methodCache);
        }

		internal Array tableToArray(object luaParamValue, Type paramArrayType) {
			return metaFunctions.TableToArray(luaParamValue,paramArrayType);
		}
    }
}
