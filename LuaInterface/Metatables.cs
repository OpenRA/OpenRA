namespace LuaInterface
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /*
     * Functions used in the metatables of userdata representing
     * CLR objects
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    class MetaFunctions
    {
        /*
         * __index metafunction for CLR objects. Implemented in Lua.
         */
        internal static string luaIndexFunction =
    @"
        local function index(obj,name)
        local meta=getmetatable(obj)
        local cached=meta.cache[name]
        if cached then
           return cached
        else
           local value,isFunc = get_object_member(obj,name)
           if value==nil and type(isFunc)=='string' then error(isFunc,2) end
           if isFunc then
            meta.cache[name]=value
           end
           return value
         end
    end
    return index";

        private ObjectTranslator translator;
        private Hashtable memberCache = new Hashtable();
        internal LuaCSFunction gcFunction, indexFunction, newindexFunction,
            baseIndexFunction, classIndexFunction, classNewindexFunction,
            execDelegateFunction, callConstructorFunction, toStringFunction;

        public MetaFunctions(ObjectTranslator translator)
        {
            this.translator = translator;
            gcFunction = new LuaCSFunction(this.collectObject);
            toStringFunction = new LuaCSFunction(this.toString);
            indexFunction = new LuaCSFunction(this.getMethod);
            newindexFunction = new LuaCSFunction(this.setFieldOrProperty);
            baseIndexFunction = new LuaCSFunction(this.getBaseMethod);
            callConstructorFunction = new LuaCSFunction(this.callConstructor);
            classIndexFunction = new LuaCSFunction(this.getClassMethod);
            classNewindexFunction = new LuaCSFunction(this.setClassFieldOrProperty);
            execDelegateFunction = new LuaCSFunction(this.runFunctionDelegate);
        }

        /*
         * __call metafunction of CLR delegates, retrieves and calls the delegate.
         */
        private int runFunctionDelegate(IntPtr luaState)
        {
            LuaCSFunction func = (LuaCSFunction)translator.getRawNetObject(luaState, 1);
            LuaDLL.lua_remove(luaState, 1);
            return func(luaState);
        }
        /*
         * __gc metafunction of CLR objects.
         */
        private int collectObject(IntPtr luaState)
        {
            int udata = LuaDLL.luanet_rawnetobj(luaState, 1);
            if (udata != -1)
            {
                translator.collectObject(udata);
            }
            else
            {
                // Debug.WriteLine("not found: " + udata);
            }
            return 0;
        }
        /*
         * __tostring metafunction of CLR objects.
         */
        private int toString(IntPtr luaState)
        {
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj != null)
            {
                translator.push(luaState, obj.ToString() + ": " + obj.GetHashCode());
            }
            else LuaDLL.lua_pushnil(luaState);
            return 1;
        }


        /// <summary>
        /// Debug tool to dump the lua stack
        /// </summary>
        /// FIXME, move somewhere else
        public static void dumpStack(ObjectTranslator translator, IntPtr luaState)
        {
            int depth = LuaDLL.lua_gettop(luaState);

            Debug.WriteLine("lua stack depth: " + depth);
            for (int i = 1; i <= depth; i++)
            {
                LuaTypes type = LuaDLL.lua_type(luaState, i);
                // we dump stacks when deep in calls, calling typename while the stack is in flux can fail sometimes, so manually check for key types
                string typestr = (type == LuaTypes.LUA_TTABLE) ? "table" : LuaDLL.lua_typename(luaState, type);

                string strrep = LuaDLL.lua_tostring(luaState, i);
                if (type == LuaTypes.LUA_TUSERDATA)
                {
                    object obj = translator.getRawNetObject(luaState, i);
                    strrep = obj.ToString();
                }

                Debug.Print("{0}: ({1}) {2}", i, typestr, strrep);
            }
        }

        /*
         * Called by the __index metafunction of CLR objects in case the
         * method is not cached or it is a field/property/event.
         * Receives the object and the member name as arguments and returns
         * either the value of the member or a delegate to call it.
         * If the member does not exist returns nil.
         */
        private int getMethod(IntPtr luaState)
        {
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj == null)
            {
                translator.throwError(luaState, "trying to index an invalid object reference");
                LuaDLL.lua_pushnil(luaState);
                return 1;
            }

            object index = translator.getObject(luaState, 2);
 
            string methodName = index as string;        // will be null if not a string arg
            Type objType = obj.GetType();

            // Handle the most common case, looking up the method by name.

            // CP: This will fail when using indexers and attempting to get a value with the same name as a property of the object,
            // ie: xmlelement['item'] <- item is a property of xmlelement
            try
            {
                if (methodName != null && isMemberPresent(objType, methodName))
                    return getMember(luaState, objType, obj, methodName, BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            catch { }
            bool failed = true;

            // Try to access by array if the type is right and index is an int (lua numbers always come across as double)
            if (objType.IsArray && index is double)
            {
                int intIndex = (int)((double)index);
                Array aa = obj as Array;
                if (intIndex >= aa.Length) {
                    return translator.pushError(luaState,"array index out of bounds: "+intIndex + " " + aa.Length);
                }
                object val = aa.GetValue(intIndex);
                translator.push (luaState,val);
                failed = false;
            }
            else
            {
                // Try to use get_Item to index into this .net object
                //MethodInfo getter = objType.GetMethod("get_Item");
                // issue here is that there may be multiple indexers..
                MethodInfo[] methods = objType.GetMethods();

                foreach (MethodInfo mInfo in methods)
                {
                    if (mInfo.Name == "get_Item")
                    {
                        //check if the signature matches the input
                        if (mInfo.GetParameters().Length == 1)
                        {
                            MethodInfo getter = mInfo;
                            ParameterInfo[] actualParms = (getter != null) ? getter.GetParameters() : null;
                            if (actualParms == null || actualParms.Length != 1)
                            {
                                return translator.pushError(luaState, "method not found (or no indexer): " + index);
                            }
                            else
                            {
                                // Get the index in a form acceptable to the getter
                                index = translator.getAsType(luaState, 2, actualParms[0].ParameterType);
                                // Just call the indexer - if out of bounds an exception will happen
                                try
                                {
                                    object result = getter.Invoke(obj, new object[]{index});
                                    translator.push(luaState, result);
                                    failed = false;
                                }
                                catch (TargetInvocationException e)
                                {
                                    // Provide a more readable description for the common case of key not found
                                    if (e.InnerException is KeyNotFoundException)
                                       return translator.pushError(luaState, "key '" + index + "' not found ");
                                    else
                                       return translator.pushError(luaState, "exception indexing '" + index + "' " + e.Message);


                                }
                            }
                        }
                    }
                }


            }
            if (failed) {
                return translator.pushError(luaState,"cannot find " + index);
            }
            LuaDLL.lua_pushboolean(luaState, false);
            return 2;
        }


        /*
         * __index metafunction of base classes (the base field of Lua tables).
         * Adds a prefix to the method name to call the base version of the method.
         */
        private int getBaseMethod(IntPtr luaState)
        {
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj == null)
            {
                translator.throwError(luaState, "trying to index an invalid object reference");
                LuaDLL.lua_pushnil(luaState);
                LuaDLL.lua_pushboolean(luaState, false);
                return 2;
            }
            string methodName = LuaDLL.lua_tostring(luaState, 2);
            if (methodName == null)
            {
                LuaDLL.lua_pushnil(luaState);
                LuaDLL.lua_pushboolean(luaState, false);
                return 2;
            }
            getMember(luaState, obj.GetType(), obj, "__luaInterface_base_" + methodName, BindingFlags.Instance | BindingFlags.IgnoreCase);
            LuaDLL.lua_settop(luaState, -2);
            if (LuaDLL.lua_type(luaState, -1) == LuaTypes.LUA_TNIL)
            {
                LuaDLL.lua_settop(luaState, -2);
                return getMember(luaState, obj.GetType(), obj, methodName, BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            LuaDLL.lua_pushboolean(luaState, false);
            return 2;
        }


        /// <summary>
        /// Does this method exist as either an instance or static?
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        bool isMemberPresent(IReflect objType, string methodName)
        {
            object cachedMember = checkMemberCache(memberCache, objType, methodName);

            if (cachedMember != null)
                return true;

            //CP: Removed NonPublic binding search
            MemberInfo[] members = objType.GetMember(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase/* | BindingFlags.NonPublic*/);
            return (members.Length > 0);
        }

        /*
         * Pushes the value of a member or a delegate to call it, depending on the type of
         * the member. Works with static or instance members.
         * Uses reflection to find members, and stores the reflected MemberInfo object in
         * a cache (indexed by the type of the object and the name of the member).
         */
        private int getMember(IntPtr luaState, IReflect objType, object obj, string methodName, BindingFlags bindingType)
        {
            bool implicitStatic = false;
            MemberInfo member = null;
            object cachedMember = checkMemberCache(memberCache, objType, methodName);
            //object cachedMember=null;
            if (cachedMember is LuaCSFunction)
            {
                translator.pushFunction(luaState, (LuaCSFunction)cachedMember);
                translator.push(luaState, true);
                return 2;
            }
            else if (cachedMember != null)
            {
                member = (MemberInfo)cachedMember;
            }
            else
            {
                //CP: Removed NonPublic binding search
                MemberInfo[] members = objType.GetMember(methodName, bindingType | BindingFlags.Public | BindingFlags.IgnoreCase/*| BindingFlags.NonPublic*/);
                if (members.Length > 0)
                    member = members[0];
                else
                {
                    // If we can't find any suitable instance members, try to find them as statics - but we only want to allow implicit static
                    // lookups for fields/properties/events -kevinh
                    //CP: Removed NonPublic binding search and made case insensitive
                    members = objType.GetMember(methodName, bindingType | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase/*| BindingFlags.NonPublic*/);

                    if (members.Length > 0)
                    {
                        member = members[0];
                        implicitStatic = true;
                    }
                }
            }
            if (member != null)
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    FieldInfo field = (FieldInfo)member;
                    if (cachedMember == null) setMemberCache(memberCache, objType, methodName, member);
                    try
                    {
                        translator.push(luaState, field.GetValue(obj));
                    }
                    catch
                    {
                        LuaDLL.lua_pushnil(luaState);
                    }
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    PropertyInfo property = (PropertyInfo)member;
                    if (cachedMember == null) setMemberCache(memberCache, objType, methodName, member);
                    try
                    {
                        object val = property.GetValue(obj, null);

                        translator.push(luaState, val);
                    }
                    catch (ArgumentException)
                    {
                        // If we can't find the getter in our class, recurse up to the base class and see
                        // if they can help.

                        if (objType is Type && !(((Type)objType) == typeof(object)))
                            return getMember(luaState, ((Type)objType).BaseType, obj, methodName, bindingType);
                        else
                            LuaDLL.lua_pushnil(luaState);
                    }
                    catch (TargetInvocationException e)  // Convert this exception into a Lua error
                    {
                        ThrowError(luaState, e);
                        LuaDLL.lua_pushnil(luaState);
                    }
                }
                else if (member.MemberType == MemberTypes.Event)
                {
                    EventInfo eventInfo = (EventInfo)member;
                    if (cachedMember == null) setMemberCache(memberCache, objType, methodName, member);
                    translator.push(luaState, new RegisterEventHandler(translator.pendingEvents, obj, eventInfo));
                }
                else if (!implicitStatic)
                {
                    if (member.MemberType == MemberTypes.NestedType)
                    {
                        // kevinh - added support for finding nested types

                        // cache us
                        if (cachedMember == null) setMemberCache(memberCache, objType, methodName, member);

                        // Find the name of our class
                        string name = member.Name;
                        Type dectype = member.DeclaringType;

                        // Build a new long name and try to find the type by name
                        string longname = dectype.FullName + "+" + name;
                        Type nestedType = translator.FindType(longname);

                        translator.pushType(luaState, nestedType);
                    }
                    else
                    {
                        // Member type must be 'method'
                        LuaCSFunction wrapper = new LuaCSFunction((new LuaMethodWrapper(translator, objType, methodName, bindingType)).call);

                        if (cachedMember == null) setMemberCache(memberCache, objType, methodName, wrapper);
                        translator.pushFunction(luaState, wrapper);
                        translator.push(luaState, true);
                        return 2;
                    }
                }
                else
                {
                    // If we reach this point we found a static method, but can't use it in this context because the user passed in an instance
                    translator.throwError(luaState, "can't pass instance to static method " + methodName);

                    LuaDLL.lua_pushnil(luaState);
                }
            }
            else
            {
                // kevinh - we want to throw an exception because meerly returning 'nil' in this case
                // is not sufficient.  valid data members may return nil and therefore there must be some
                // way to know the member just doesn't exist.

                translator.throwError(luaState, "unknown member name " + methodName);

                LuaDLL.lua_pushnil(luaState);
            }

            // push false because we are NOT returning a function (see luaIndexFunction)
            translator.push(luaState, false);
            return 2;
        }
        /*
         * Checks if a MemberInfo object is cached, returning it or null.
         */
        private object checkMemberCache(Hashtable memberCache, IReflect objType, string memberName)
        {
            Hashtable members = (Hashtable)memberCache[objType];
            if (members != null)
                return members[memberName];
            else
                return null;
        }
        /*
         * Stores a MemberInfo object in the member cache.
         */
        private void setMemberCache(Hashtable memberCache, IReflect objType, string memberName, object member)
        {
            Hashtable members = (Hashtable)memberCache[objType];
            if (members == null)
            {
                members = new Hashtable();
                memberCache[objType] = members;
            }
            members[memberName] = member;
        }
        /*
         * __newindex metafunction of CLR objects. Receives the object,
         * the member name and the value to be stored as arguments. Throws
         * and error if the assignment is invalid.
         */
        private int setFieldOrProperty(IntPtr luaState)
        {
            object target = translator.getRawNetObject(luaState, 1);
            if (target == null)
            {
                translator.throwError(luaState, "trying to index and invalid object reference");
                return 0;
            }
            Type type = target.GetType();

            // First try to look up the parameter as a property name
            string detailMessage;
            bool didMember = trySetMember(luaState, type, target, BindingFlags.Instance | BindingFlags.IgnoreCase, out detailMessage);

            if (didMember)
                return 0;       // Must have found the property name

            // We didn't find a property name, now see if we can use a [] style this accessor to set array contents
            try
            {
                if (type.IsArray && LuaDLL.lua_isnumber(luaState, 2))
                {
                    int index = (int)LuaDLL.lua_tonumber(luaState, 2);

                    Array arr = (Array)target;
                    object val = translator.getAsType(luaState, 3, arr.GetType().GetElementType());
                    arr.SetValue(val, index);
                }
                else
                {
                    // Try to see if we have a this[] accessor
                    MethodInfo setter = type.GetMethod("set_Item");
                    if (setter != null)
                    {
                        ParameterInfo[] args = setter.GetParameters();
                        Type valueType = args[1].ParameterType;

                        // The new val ue the user specified
                        object val = translator.getAsType(luaState, 3, valueType);

                        Type indexType = args[0].ParameterType;
                        object index = translator.getAsType(luaState, 2, indexType);

                        object[] methodArgs = new object[2];

                        // Just call the indexer - if out of bounds an exception will happen
                        methodArgs[0] = index;
                        methodArgs[1] = val;

                        setter.Invoke(target, methodArgs);
                    }
                    else
                    {
                        translator.throwError(luaState, detailMessage); // Pass the original message from trySetMember because it is probably best
                    }
                }
            }
            catch (SEHException)
            {
                // If we are seeing a C++ exception - this must actually be for Lua's private use.  Let it handle it
                throw;
            }
            catch (Exception e)
            {
                ThrowError(luaState, e);
            }
            return 0;
        }

        /// <summary>
        /// Tries to set a named property or field
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="targetType"></param>
        /// <param name="target"></param>
        /// <param name="bindingType"></param>
        /// <returns>false if unable to find the named member, true for success</returns>
        private bool trySetMember(IntPtr luaState, IReflect targetType, object target, BindingFlags bindingType, out string detailMessage)
        {
            detailMessage = null;   // No error yet

            // If not already a string just return - we don't want to call tostring - which has the side effect of
            // changing the lua typecode to string
            // Note: We don't use isstring because the standard lua C isstring considers either strings or numbers to
            // be true for isstring.
            if (LuaDLL.lua_type(luaState, 2) != LuaTypes.LUA_TSTRING)
            {
                detailMessage = "property names must be strings";
                return false;
            }

            // We only look up property names by string
            string fieldName = LuaDLL.lua_tostring(luaState, 2);
            if (fieldName == null || fieldName.Length < 1 || !(char.IsLetter(fieldName[0]) || fieldName[0] == '_'))
            {
                detailMessage = "invalid property name";
                return false;
            }

            // Find our member via reflection or the cache
            MemberInfo member = (MemberInfo)checkMemberCache(memberCache, targetType, fieldName);
            if (member == null)
            {
                //CP: Removed NonPublic binding search and made case insensitive
                MemberInfo[] members = targetType.GetMember(fieldName, bindingType | BindingFlags.Public | BindingFlags.IgnoreCase/*| BindingFlags.NonPublic*/);
                if (members.Length > 0)
                {
                    member = members[0];
                    setMemberCache(memberCache, targetType, fieldName, member);
                }
                else
                {
                    detailMessage = "field or property '" + fieldName + "' does not exist";
                    return false;
                }
            }

            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo field = (FieldInfo)member;
                object val = translator.getAsType(luaState, 3, field.FieldType);
                try
                {
                    field.SetValue(target, val);
                }
                catch (Exception e)
                {
                    ThrowError(luaState, e);
                }
                // We did a call
                return true;
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo property = (PropertyInfo)member;
                object val = translator.getAsType(luaState, 3, property.PropertyType);
                try
                {
                    property.SetValue(target, val, null);
                }
                catch (Exception e)
                {
                    ThrowError(luaState, e);
                }
                // We did a call
                return true;
            }

            detailMessage = "'" + fieldName + "' is not a .net field or property";
            return false;
        }


        /*
         * Writes to fields or properties, either static or instance. Throws an error
         * if the operation is invalid.
         */
        private int setMember(IntPtr luaState, IReflect targetType, object target, BindingFlags bindingType)
        {
            string detail;
            bool success = trySetMember(luaState, targetType, target, bindingType, out detail);

            if (!success)
                translator.throwError(luaState, detail);

            return 0;
        }

        /// <summary>
        /// Convert a C# exception into a Lua error
        /// </summary>
        /// <param name="e"></param>
        /// We try to look into the exception to give the most meaningful description
        void ThrowError(IntPtr luaState, Exception e)
        {
            // If we got inside a reflection show what really happened
            TargetInvocationException te = e as TargetInvocationException;

            if (te != null)
                e = te.InnerException;

            translator.throwError(luaState, e);
        }

        /*
         * __index metafunction of type references, works on static members.
         */
        private int getClassMethod(IntPtr luaState)
        {
            IReflect klass;
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj == null || !(obj is IReflect))
            {
                translator.throwError(luaState, "trying to index an invalid type reference");
                LuaDLL.lua_pushnil(luaState);
                return 1;
            }
            else klass = (IReflect)obj;
            if (LuaDLL.lua_isnumber(luaState, 2))
            {
                int size = (int)LuaDLL.lua_tonumber(luaState, 2);
                translator.push(luaState, Array.CreateInstance(klass.UnderlyingSystemType, size));
                return 1;
            }
            else
            {
                string methodName = LuaDLL.lua_tostring(luaState, 2);
                if (methodName == null)
                {
                    LuaDLL.lua_pushnil(luaState);
                    return 1;
                } //CP: Ignore case
                else return getMember(luaState, klass, null, methodName, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.IgnoreCase);
            }
        }
        /*
         * __newindex function of type references, works on static members.
         */
        private int setClassFieldOrProperty(IntPtr luaState)
        {
            IReflect target;
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj == null || !(obj is IReflect))
            {
                translator.throwError(luaState, "trying to index an invalid type reference");
                return 0;
            }
            else target = (IReflect)obj;
            return setMember(luaState, target, null, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.IgnoreCase);
        }
        /*
         * __call metafunction of type references. Searches for and calls
         * a constructor for the type. Returns nil if the constructor is not
         * found or if the arguments are invalid. Throws an error if the constructor
         * generates an exception.
         */
        private int callConstructor(IntPtr luaState)
        {
            MethodCache validConstructor = new MethodCache();
            IReflect klass;
            object obj = translator.getRawNetObject(luaState, 1);
            if (obj == null || !(obj is IReflect))
            {
                translator.throwError(luaState, "trying to call constructor on an invalid type reference");
                LuaDLL.lua_pushnil(luaState);
                return 1;
            }
            else klass = (IReflect)obj;
            LuaDLL.lua_remove(luaState, 1);
            ConstructorInfo[] constructors = klass.UnderlyingSystemType.GetConstructors();
            foreach (ConstructorInfo constructor in constructors)
            {
                bool isConstructor = matchParameters(luaState, constructor, ref validConstructor);
                if (isConstructor)
                {
                    try
                    {
                        translator.push(luaState, constructor.Invoke(validConstructor.args));
                    }
                    catch (TargetInvocationException e)
                    {
                        ThrowError(luaState, e);
                        LuaDLL.lua_pushnil(luaState);
                    }
                    catch
                    {
                        LuaDLL.lua_pushnil(luaState);
                    }
                    return 1;
                }
            }

            string constructorName = (constructors.Length == 0) ? "unknown" : constructors[0].Name;

            translator.throwError(luaState, String.Format("{0} does not contain constructor({1}) argument match",
                klass.UnderlyingSystemType,
                constructorName));
            LuaDLL.lua_pushnil(luaState);
            return 1;
        }
        
        private static bool IsInteger(double x) {
            return Math.Ceiling(x) == x;	
        }			

        
        internal Array TableToArray(object luaParamValue, Type paramArrayType) {
            Array paramArray;

            if (luaParamValue is LuaTable)  {
                LuaTable table = (LuaTable)luaParamValue;
                IDictionaryEnumerator tableEnumerator = table.GetEnumerator();				
                tableEnumerator.Reset();
                paramArray = Array.CreateInstance(paramArrayType, table.Values.Count);                

                int paramArrayIndex = 0;
                
                while(tableEnumerator.MoveNext())  {
                    object o = tableEnumerator.Value;
                    if (paramArrayType == typeof(object)) { 
                        if (o != null && o.GetType() == typeof(double) && IsInteger((double)o))
                            o = Convert.ToInt32((double)o);
                    }													
                    paramArray.SetValue(Convert.ChangeType(o, paramArrayType), paramArrayIndex);
                    paramArrayIndex++;
                }
            } else {
                paramArray = Array.CreateInstance(paramArrayType, 1);
                paramArray.SetValue(luaParamValue, 0);
            }
    
            return paramArray;
            
        }
        
        /*
         * Matches a method against its arguments in the Lua stack. Returns
         * if the match was succesful. It it was also returns the information
         * necessary to invoke the method.
         */
        internal bool matchParameters(IntPtr luaState, MethodBase method, ref MethodCache methodCache)
        {
            ExtractValue extractValue;
            bool isMethod = true;
            ParameterInfo[] paramInfo = method.GetParameters();
            int currentLuaParam = 1;
            int nLuaParams = LuaDLL.lua_gettop(luaState);
            ArrayList paramList = new ArrayList();
            List<int> outList = new List<int>();
            List<MethodArgs> argTypes = new List<MethodArgs>();
            foreach (ParameterInfo currentNetParam in paramInfo)
            {
                if (!currentNetParam.IsIn && currentNetParam.IsOut)  // Skips out params
                {
                    outList.Add(paramList.Add(null));
                }
                else if (currentLuaParam > nLuaParams) // Adds optional parameters
                {
                    if (currentNetParam.IsOptional)
                    {
                        paramList.Add(currentNetParam.DefaultValue);
                    }
                    else
                    {
                        isMethod = false;
                        break;
                    }
                }
                else if (_IsTypeCorrect(luaState, currentLuaParam, currentNetParam, out extractValue))  // Type checking
                {
                    int index = paramList.Add(extractValue(luaState, currentLuaParam));

                    MethodArgs methodArg = new MethodArgs();
                    methodArg.index = index;
                    methodArg.extractValue = extractValue;
                    argTypes.Add(methodArg);

                    if (currentNetParam.ParameterType.IsByRef)
                        outList.Add(index);
                    currentLuaParam++;
                }  // Type does not match, ignore if the parameter is optional
                else if (_IsParamsArray(luaState, currentLuaParam, currentNetParam, out extractValue))
                {
                    object luaParamValue = extractValue(luaState, currentLuaParam);
                    Type paramArrayType = currentNetParam.ParameterType.GetElementType();
                    
                    Array paramArray = TableToArray(luaParamValue, paramArrayType);					
                    int index = paramList.Add(paramArray);

                    MethodArgs methodArg = new MethodArgs();
                    methodArg.index = index;
                    methodArg.extractValue = extractValue;
                    methodArg.isParamsArray = true;
                    methodArg.paramsArrayType = paramArrayType;
                    argTypes.Add(methodArg);

                    currentLuaParam++;
                }
                else if (currentNetParam.IsOptional)
                {
                    paramList.Add(currentNetParam.DefaultValue);
                }
                else  // No match
                {
                    isMethod = false;
                    break;
                }
            }
            if (currentLuaParam != nLuaParams + 1) // Number of parameters does not match
                isMethod = false;
            if (isMethod)
            {
                methodCache.args = paramList.ToArray();
                methodCache.cachedMethod = method;
                methodCache.outList = outList.ToArray();
                methodCache.argTypes = argTypes.ToArray();
            }
            return isMethod;
        }

        /// <summary>
        /// CP: Fix for operator overloading failure
        /// Returns true if the type is set and assigns the extract value
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="currentLuaParam"></param>
        /// <param name="currentNetParam"></param>
        /// <param name="extractValue"></param>
        /// <returns></returns>
        private bool _IsTypeCorrect(IntPtr luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
        {
            try
            {
                return (extractValue = translator.typeChecker.checkType(luaState, currentLuaParam, currentNetParam.ParameterType)) != null;
            }
            catch
            {
                extractValue = null;
                Debug.WriteLine("Type wasn't correct");
                return false;
            }
        }

        private bool _IsParamsArray(IntPtr luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
        {
            extractValue = null;

            if (currentNetParam.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
            {
                LuaTypes luaType;

                try
                {
                    luaType = LuaDLL.lua_type(luaState, currentLuaParam);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not retrieve lua type while attempting to determine params Array Status."+ ex.ToString());
                    Debug.WriteLine(ex.Message);
                    extractValue = null;
                    return false;
                }

                if (luaType == LuaTypes.LUA_TTABLE)
                {
                    try
                    {
                        extractValue = translator.typeChecker.getExtractor(typeof(LuaTable));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("An error occurred during an attempt to retrieve a LuaTable extractor while checking for params array status." + ex.ToString());
                    }

                    if (extractValue != null)
                    {
                        return true;
                    }
                }
                else
                {
                    Type paramElementType = currentNetParam.ParameterType.GetElementType();

                    try
                    {
                        extractValue = translator.typeChecker.checkType(luaState, currentLuaParam, paramElementType);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("An error occurred during an attempt to retrieve an extractor ({0}) while checking for params array status:{1}", paramElementType.FullName,ex.ToString()));
                    }

                    if (extractValue != null)
                    {
                        return true;
                    }
                }
            }

            Debug.WriteLine("Type wasn't Params object.");

            return false;
        }
    }
}
