using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace LuaInterface
{
    /*
     * Structure to store a type and the return types of
     * its methods (the type of the returned value and out/ref
     * parameters).
     */
    struct LuaClassType
    {
        public Type klass;
        public Type[][] returnTypes;
    }

    /*
     * Common interface for types generated from tables. The method
     * returns the table that overrides some or all of the type's methods.
     */
    public interface ILuaGeneratedType
    {
        LuaTable __luaInterface_getLuaTable();
    }

    /*
     * Class used for generating delegates that get a function from the Lua
     * stack as a delegate of a specific type.
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    class DelegateGenerator
    {
        private ObjectTranslator translator;
        private Type delegateType;

        public DelegateGenerator(ObjectTranslator translator,Type delegateType)
        {
            this.translator=translator;
            this.delegateType=delegateType;
        }
        public object extractGenerated(IntPtr luaState,int stackPos)
        {
            return CodeGeneration.Instance.GetDelegate(delegateType,translator.getFunction(luaState,stackPos));
        }
    }

    /*
     * Class used for generating delegates that get a table from the Lua
     * stack as a an object of a specific type.
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    class ClassGenerator
    {
        private ObjectTranslator translator;
        private Type klass;

        public ClassGenerator(ObjectTranslator translator,Type klass)
        {
            this.translator=translator;
            this.klass=klass;
        }
        public object extractGenerated(IntPtr luaState,int stackPos)
        {
            return CodeGeneration.Instance.GetClassInstance(klass,translator.getTable(luaState,stackPos));
        }
    }

    /*
     * Dynamically generates new types from existing types and
     * Lua function and table values. Generated types are event handlers,
     * delegates, interface implementations and subclasses.
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    class CodeGeneration
    {
        private Type eventHandlerParent=typeof(LuaEventHandler);
        private Dictionary<Type, Type> eventHandlerCollection=new Dictionary<Type, Type>();

        private Type delegateParent=typeof(LuaDelegate);
        private Dictionary<Type, Type> delegateCollection=new Dictionary<Type, Type>();

        private Type classHelper=typeof(LuaClassHelper);
        private Dictionary<Type, LuaClassType> classCollection=new Dictionary<Type, LuaClassType>();

        private AssemblyName assemblyName;
        private AssemblyBuilder newAssembly;
        private ModuleBuilder newModule;
        private int luaClassNumber=1;
        private static readonly  CodeGeneration instance = new CodeGeneration();

        static CodeGeneration()
        {
        }

        private CodeGeneration()
        {
            // Create an assembly name
            assemblyName=new AssemblyName( );
            assemblyName.Name="LuaInterface_generatedcode";
            // Create a new assembly with one module.
            newAssembly=Thread.GetDomain().DefineDynamicAssembly(
                assemblyName, AssemblyBuilderAccess.Run);
            newModule=newAssembly.DefineDynamicModule("LuaInterface_generatedcode");
        }

        /*
         * Singleton instance of the class
         */
        public static CodeGeneration Instance
        {
            get
            {
                return instance;
            }
        }

        /*
         *  Generates an event handler that calls a Lua function
         */
        private Type GenerateEvent(Type eventHandlerType)
        {
            string typeName;
            lock(this)
            {
                typeName = "LuaGeneratedClass" + luaClassNumber;
                luaClassNumber++;
            }
            // Define a public class in the assembly, called typeName
            TypeBuilder myType=newModule.DefineType(typeName,TypeAttributes.Public,eventHandlerParent);

            // Defines the handler method. Its signature is void(object,<subclassofEventArgs>)
            Type[] paramTypes = new Type[2];
            paramTypes[0]=typeof(object);
            paramTypes[1]=eventHandlerType;
            Type returnType=typeof(void);
            MethodBuilder handleMethod=myType.DefineMethod("HandleEvent",
                MethodAttributes.Public|MethodAttributes.HideBySig,
                returnType,paramTypes);

            // Emits the IL for the method. It loads the arguments
            // and calls the handleEvent method of the base class
            ILGenerator generator=handleMethod.GetILGenerator( );
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            MethodInfo miGenericEventHandler;
            miGenericEventHandler=eventHandlerParent.GetMethod("handleEvent");
            generator.Emit(OpCodes.Call,miGenericEventHandler);
            // returns
            generator.Emit(OpCodes.Ret);

            // creates the new type
            return myType.CreateType();
        }

        /*
         * Generates a type that can be used for instantiating a delegate
         * of the provided type, given a Lua function.
         */
        private Type GenerateDelegate(Type delegateType)
        {
            string typeName;
            lock(this)
            {
                typeName = "LuaGeneratedClass" + luaClassNumber;
                luaClassNumber++;
            }
            // Define a public class in the assembly, called typeName
            TypeBuilder myType=newModule.DefineType(typeName,TypeAttributes.Public,delegateParent);

            // Defines the delegate method with the same signature as the
            // Invoke method of delegateType
            MethodInfo invokeMethod=delegateType.GetMethod("Invoke");
            ParameterInfo[] paramInfo=invokeMethod.GetParameters();
            Type[] paramTypes=new Type[paramInfo.Length];
            Type returnType=invokeMethod.ReturnType;

            // Counts out and ref params, for use later
            int nOutParams=0; int nOutAndRefParams=0;
            for(int i=0;i<paramTypes.Length;i++)
            {
                paramTypes[i]=paramInfo[i].ParameterType;
                if((!paramInfo[i].IsIn) && paramInfo[i].IsOut)
                    nOutParams++;
                if(paramTypes[i].IsByRef)
                    nOutAndRefParams++;
            }
            int[] refArgs=new int[nOutAndRefParams];

            MethodBuilder delegateMethod=myType.DefineMethod("CallFunction",
                invokeMethod.Attributes,
                returnType,paramTypes);

            // Generates the IL for the method
            ILGenerator generator=delegateMethod.GetILGenerator( );

            generator.DeclareLocal(typeof(object[])); // original arguments
            generator.DeclareLocal(typeof(object[])); // with out-only arguments removed
            generator.DeclareLocal(typeof(int[])); // indexes of out and ref arguments
            if(!(returnType == typeof(void)))  // return value
                generator.DeclareLocal(returnType);
            else
                generator.DeclareLocal(typeof(object));
            // Initializes local variables
            generator.Emit(OpCodes.Ldc_I4,paramTypes.Length);
            generator.Emit(OpCodes.Newarr,typeof(object));
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldc_I4,paramTypes.Length-nOutParams);
            generator.Emit(OpCodes.Newarr,typeof(object));
            generator.Emit(OpCodes.Stloc_1);
            generator.Emit(OpCodes.Ldc_I4,nOutAndRefParams);
            generator.Emit(OpCodes.Newarr,typeof(int));
            generator.Emit(OpCodes.Stloc_2);
            // Stores the arguments in the local variables
            for(int iArgs=0,iInArgs=0,iOutArgs=0;iArgs<paramTypes.Length;iArgs++)
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4,iArgs);
                generator.Emit(OpCodes.Ldarg,iArgs+1);
                if(paramTypes[iArgs].IsByRef)
                {
                    if(paramTypes[iArgs].GetElementType().IsValueType)
                    {
                        generator.Emit(OpCodes.Ldobj,paramTypes[iArgs].GetElementType());
                        generator.Emit(OpCodes.Box,paramTypes[iArgs].GetElementType());
                    } else generator.Emit(OpCodes.Ldind_Ref);
                }
                else
                {
                    if(paramTypes[iArgs].IsValueType)
                        generator.Emit(OpCodes.Box,paramTypes[iArgs]);
                }
                generator.Emit(OpCodes.Stelem_Ref);
                if(paramTypes[iArgs].IsByRef)
                {
                    generator.Emit(OpCodes.Ldloc_2);
                    generator.Emit(OpCodes.Ldc_I4,iOutArgs);
                    generator.Emit(OpCodes.Ldc_I4,iArgs);
                    generator.Emit(OpCodes.Stelem_I4);
                    refArgs[iOutArgs]=iArgs;
                    iOutArgs++;
                }
                if(paramInfo[iArgs].IsIn || (!paramInfo[iArgs].IsOut))
                {
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Ldc_I4,iInArgs);
                    generator.Emit(OpCodes.Ldarg,iArgs+1);
                    if(paramTypes[iArgs].IsByRef)
                    {
                        if(paramTypes[iArgs].GetElementType().IsValueType)
                        {
                            generator.Emit(OpCodes.Ldobj,paramTypes[iArgs].GetElementType());
                            generator.Emit(OpCodes.Box,paramTypes[iArgs].GetElementType());
                        }
                        else generator.Emit(OpCodes.Ldind_Ref);
                    }
                    else
                    {
                        if(paramTypes[iArgs].IsValueType)
                            generator.Emit(OpCodes.Box,paramTypes[iArgs]);
                    }
                    generator.Emit(OpCodes.Stelem_Ref);
                    iInArgs++;
                }
            }
            // Calls the callFunction method of the base class
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ldloc_2);
            MethodInfo miGenericEventHandler;
            miGenericEventHandler=delegateParent.GetMethod("callFunction");
            generator.Emit(OpCodes.Call,miGenericEventHandler);
            // Stores return value
            if(returnType == typeof(void))
            {
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Ldnull);
            }
            else if(returnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox,returnType);
                generator.Emit(OpCodes.Ldobj,returnType);
            } else generator.Emit(OpCodes.Castclass,returnType);
            generator.Emit(OpCodes.Stloc_3);
            // Stores new value of out and ref params
            for(int i=0;i<refArgs.Length;i++)
            {
                generator.Emit(OpCodes.Ldarg,refArgs[i]+1);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4,refArgs[i]);
                generator.Emit(OpCodes.Ldelem_Ref);
                if(paramTypes[refArgs[i]].GetElementType().IsValueType)
                {
                    generator.Emit(OpCodes.Unbox,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Ldobj,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Stobj,paramTypes[refArgs[i]].GetElementType());
                }
                else
                {
                    generator.Emit(OpCodes.Castclass,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Stind_Ref);
                }
            }
            // Returns
            if(!(returnType == typeof(void)))
                generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Ret);

            // creates the new type
            return myType.CreateType();
        }

        /*
         * Generates an implementation of klass, if it is an interface, or
         * a subclass of klass that delegates its virtual methods to a Lua table.
         */
        public void GenerateClass(Type klass,out Type newType,out Type[][] returnTypes, LuaTable luaTable)
        {
            string typeName;
            lock(this)
            {
                typeName = "LuaGeneratedClass" + luaClassNumber;
                luaClassNumber++;
            }
            TypeBuilder myType;
            // Define a public class in the assembly, called typeName
            if(klass.IsInterface)
                myType=newModule.DefineType(typeName,TypeAttributes.Public,typeof(object),new Type[] { klass,typeof(ILuaGeneratedType) });
            else
                myType=newModule.DefineType(typeName,TypeAttributes.Public,klass,new Type[] { typeof(ILuaGeneratedType) });
            // Field that stores the Lua table
            FieldBuilder luaTableField=myType.DefineField("__luaInterface_luaTable",typeof(LuaTable),FieldAttributes.Public);
            // Field that stores the return types array
            FieldBuilder returnTypesField=myType.DefineField("__luaInterface_returnTypes",typeof(Type[][]),FieldAttributes.Public);
            // Generates the constructor for the new type, it takes a Lua table and an array
            // of return types and stores them in the respective fields
            ConstructorBuilder constructor=
                myType.DefineConstructor(MethodAttributes.Public,CallingConventions.Standard,new Type[] { typeof(LuaTable),typeof(Type[][]) });
            ILGenerator generator=constructor.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            if(klass.IsInterface)
                generator.Emit(OpCodes.Call,typeof(object).GetConstructor(Type.EmptyTypes));
            else
                generator.Emit(OpCodes.Call,klass.GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld,luaTableField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Stfld,returnTypesField);
            generator.Emit(OpCodes.Ret);
            // Generates overriden versions of the klass' public and protected virtual methods that have been explicitly specfied
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo[] classMethods=klass.GetMethods(flags);
            returnTypes=new Type[classMethods.Length][];
            int i=0;
            foreach(MethodInfo method in classMethods)
            {
                if(klass.IsInterface)
                {
                    GenerateMethod(myType,method,
                        MethodAttributes.HideBySig|MethodAttributes.Virtual|MethodAttributes.NewSlot,
                        i,luaTableField,returnTypesField,false,out returnTypes[i]);
                    i++;
                }
                else
                {
                    if(!method.IsPrivate && !method.IsFinal && method.IsVirtual)
                    {
                        if (luaTable[method.Name] != null) {
                            GenerateMethod(myType,method,(method.Attributes|MethodAttributes.NewSlot)^MethodAttributes.NewSlot,i,
                                luaTableField,returnTypesField,true,out returnTypes[i]);
                            i++;
                        }
                    }
                }
            }
            // Generates an implementation of the __luaInterface_getLuaTable method
            MethodBuilder returnTableMethod=myType.DefineMethod("__luaInterface_getLuaTable",
                MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.Virtual,
                typeof(LuaTable),new Type[0]);
            myType.DefineMethodOverride(returnTableMethod,typeof(ILuaGeneratedType).GetMethod("__luaInterface_getLuaTable"));
            generator=returnTableMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld,luaTableField);
            generator.Emit(OpCodes.Ret);
            // Creates the type
            newType=myType.CreateType();
        }

        /*
         * Generates an overriden implementation of method inside myType that delegates
         * to a function in a Lua table with the same name, if the function exists. If it
         * doesn't the method calls the base method (or does nothing, in case of interface
         * implementations).
         */
        private void GenerateMethod(TypeBuilder myType,MethodInfo method,MethodAttributes attributes,
            int methodIndex,FieldInfo luaTableField,FieldInfo returnTypesField,bool generateBase,out Type[] returnTypes)
        {
            ParameterInfo[] paramInfo=method.GetParameters();
            Type[] paramTypes=new Type[paramInfo.Length];
            List<Type> returnTypesList=new List<Type>();

            // Counts out and ref parameters, for later use,
            // and creates the list of return types
            int nOutParams=0; int nOutAndRefParams=0;
            Type returnType=method.ReturnType;
            returnTypesList.Add(returnType);
            for(int i=0;i<paramTypes.Length;i++)
            {
                paramTypes[i]=paramInfo[i].ParameterType;
                if((!paramInfo[i].IsIn) && paramInfo[i].IsOut)
                    nOutParams++;
                if(paramTypes[i].IsByRef)
                {
                    returnTypesList.Add(paramTypes[i].GetElementType());
                    nOutAndRefParams++;
                }
            }
            int[] refArgs=new int[nOutAndRefParams];
            returnTypes=returnTypesList.ToArray();

            // Generates a version of the method that calls the base implementation
            // directly, for use by the base field of the table
            if(generateBase)
            {
                String baseName = "__luaInterface_base_"+method.Name;
                MethodBuilder baseMethod=myType.DefineMethod(baseName,
                    MethodAttributes.Public|MethodAttributes.NewSlot|MethodAttributes.HideBySig,
                    returnType,paramTypes);
                ILGenerator generatorBase=baseMethod.GetILGenerator();
                generatorBase.Emit(OpCodes.Ldarg_0);
                for(int i=0;i<paramTypes.Length;i++)
                    generatorBase.Emit(OpCodes.Ldarg,i+1);
                generatorBase.Emit(OpCodes.Call,method);
                //if (returnType == typeof(void))
                 //   generatorBase.Emit(OpCodes.Pop);
                generatorBase.Emit(OpCodes.Ret);
            }

            // Defines the method
            MethodBuilder methodImpl=myType.DefineMethod(method.Name,attributes,
                returnType,paramTypes);
            // If it's an implementation of an interface tells what method it
            // is overriding
            if(myType.BaseType.Equals(typeof(object)))
                myType.DefineMethodOverride(methodImpl,method);

            ILGenerator generator=methodImpl.GetILGenerator( );

            generator.DeclareLocal(typeof(object[])); // original arguments
            generator.DeclareLocal(typeof(object[])); // with out-only arguments removed
            generator.DeclareLocal(typeof(int[])); // indexes of out and ref arguments
            if(!(returnType == typeof(void))) // return value
                generator.DeclareLocal(returnType);
            else
                generator.DeclareLocal(typeof(object));
            // Initializes local variables
            generator.Emit(OpCodes.Ldc_I4,paramTypes.Length);
            generator.Emit(OpCodes.Newarr,typeof(object));
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldc_I4,paramTypes.Length-nOutParams+1);
            generator.Emit(OpCodes.Newarr,typeof(object));
            generator.Emit(OpCodes.Stloc_1);
            generator.Emit(OpCodes.Ldc_I4,nOutAndRefParams);
            generator.Emit(OpCodes.Newarr,typeof(int));
            generator.Emit(OpCodes.Stloc_2);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld,luaTableField);
            generator.Emit(OpCodes.Stelem_Ref);
            // Stores the arguments into the local variables, as needed
            for(int iArgs=0,iInArgs=1,iOutArgs=0;iArgs<paramTypes.Length;iArgs++)
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4,iArgs);
                generator.Emit(OpCodes.Ldarg,iArgs+1);
                if(paramTypes[iArgs].IsByRef)
                {
                    if(paramTypes[iArgs].GetElementType().IsValueType)
                    {
                        generator.Emit(OpCodes.Ldobj,paramTypes[iArgs].GetElementType());
                        generator.Emit(OpCodes.Box,paramTypes[iArgs].GetElementType());
                    }
                    else generator.Emit(OpCodes.Ldind_Ref);
                }
                else
                {
                    if(paramTypes[iArgs].IsValueType)
                        generator.Emit(OpCodes.Box,paramTypes[iArgs]);
                }
                generator.Emit(OpCodes.Stelem_Ref);
                if(paramTypes[iArgs].IsByRef)
                {
                    generator.Emit(OpCodes.Ldloc_2);
                    generator.Emit(OpCodes.Ldc_I4,iOutArgs);
                    generator.Emit(OpCodes.Ldc_I4,iArgs);
                    generator.Emit(OpCodes.Stelem_I4);
                    refArgs[iOutArgs]=iArgs;
                    iOutArgs++;
                }
                if(paramInfo[iArgs].IsIn || (!paramInfo[iArgs].IsOut))
                {
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Ldc_I4,iInArgs);
                    generator.Emit(OpCodes.Ldarg,iArgs+1);
                    if(paramTypes[iArgs].IsByRef)
                    {
                        if(paramTypes[iArgs].GetElementType().IsValueType)
                        {
                            generator.Emit(OpCodes.Ldobj,paramTypes[iArgs].GetElementType());
                            generator.Emit(OpCodes.Box,paramTypes[iArgs].GetElementType());
                        }
                        else generator.Emit(OpCodes.Ldind_Ref);
                    }
                    else
                    {
                        if(paramTypes[iArgs].IsValueType)
                            generator.Emit(OpCodes.Box,paramTypes[iArgs]);
                    }
                    generator.Emit(OpCodes.Stelem_Ref);
                    iInArgs++;
                }
            }
            // Gets the function the method will delegate to by calling
            // the getTableFunction method of class LuaClassHelper
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld,luaTableField);
            generator.Emit(OpCodes.Ldstr,method.Name);
            generator.Emit(OpCodes.Call,classHelper.GetMethod("getTableFunction"));
            Label lab1=generator.DefineLabel();
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Brtrue_S,lab1);
            // Function does not exist, call base method
            generator.Emit(OpCodes.Pop);
            if(!method.IsAbstract)
            {
                generator.Emit(OpCodes.Ldarg_0);
                for(int i=0;i<paramTypes.Length;i++)
                    generator.Emit(OpCodes.Ldarg,i+1);
                generator.Emit(OpCodes.Call,method);
                if(returnType == typeof(void))
                    generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Ret);
                generator.Emit(OpCodes.Ldnull);
            } else
                generator.Emit(OpCodes.Ldnull);
            Label lab2=generator.DefineLabel();
            generator.Emit(OpCodes.Br_S,lab2);
            generator.MarkLabel(lab1);
            // Function exists, call using method callFunction of LuaClassHelper
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld,returnTypesField);
            generator.Emit(OpCodes.Ldc_I4,methodIndex);
            generator.Emit(OpCodes.Ldelem_Ref);
            generator.Emit(OpCodes.Ldloc_1);
            generator.Emit(OpCodes.Ldloc_2);
            generator.Emit(OpCodes.Call,classHelper.GetMethod("callFunction"));
            generator.MarkLabel(lab2);
            // Stores the function return value
            if(returnType == typeof(void))
            {
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Ldnull);
            }
            else if(returnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox,returnType);
                generator.Emit(OpCodes.Ldobj,returnType);
            }
            else generator.Emit(OpCodes.Castclass,returnType);
            generator.Emit(OpCodes.Stloc_3);
            // Sets return values of out and ref parameters
            for(int i=0;i<refArgs.Length;i++)
            {
                generator.Emit(OpCodes.Ldarg,refArgs[i]+1);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4,refArgs[i]);
                generator.Emit(OpCodes.Ldelem_Ref);
                if(paramTypes[refArgs[i]].GetElementType().IsValueType)
                {
                    generator.Emit(OpCodes.Unbox,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Ldobj,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Stobj,paramTypes[refArgs[i]].GetElementType());
                }
                else
                {
                    generator.Emit(OpCodes.Castclass,paramTypes[refArgs[i]].GetElementType());
                    generator.Emit(OpCodes.Stind_Ref);
                }
            }

            // Returns
            if(!(returnType == typeof(void)))
                generator.Emit(OpCodes.Ldloc_3);
            generator.Emit(OpCodes.Ret);
        }

        /*
         * Gets an event handler for the event type that delegates to the eventHandler Lua function.
         * Caches the generated type.
         */
        public LuaEventHandler GetEvent(Type eventHandlerType, LuaFunction eventHandler)
        {
            Type eventConsumerType;
            if (eventHandlerCollection.ContainsKey(eventHandlerType))
            {
                eventConsumerType=eventHandlerCollection[eventHandlerType];
            }
            else
            {
                eventConsumerType=GenerateEvent(eventHandlerType);
                eventHandlerCollection[eventHandlerType] = eventConsumerType;
            }
            LuaEventHandler luaEventHandler=(LuaEventHandler)Activator.CreateInstance(eventConsumerType);
            luaEventHandler.handler=eventHandler;
            return luaEventHandler;
        }

        /*
         * Gets a delegate with delegateType that calls the luaFunc Lua function
         * Caches the generated type.
         */
        public Delegate GetDelegate(Type delegateType, LuaFunction luaFunc)
        {
            List<Type> returnTypes=new List<Type>();
            Type luaDelegateType;
            if (delegateCollection.ContainsKey(delegateType))
            {
                luaDelegateType=delegateCollection[delegateType];
            }
            else
            {
                luaDelegateType=GenerateDelegate(delegateType);
                delegateCollection[delegateType] = luaDelegateType;
            }
            MethodInfo methodInfo=delegateType.GetMethod("Invoke");
            returnTypes.Add(methodInfo.ReturnType);
            foreach(ParameterInfo paramInfo in methodInfo.GetParameters())
                if(paramInfo.ParameterType.IsByRef)
                    returnTypes.Add(paramInfo.ParameterType);
            LuaDelegate luaDelegate=(LuaDelegate)Activator.CreateInstance(luaDelegateType);
            luaDelegate.function=luaFunc;
            luaDelegate.returnTypes=returnTypes.ToArray();
            return Delegate.CreateDelegate(delegateType,luaDelegate,"CallFunction");
        }

        /*
         * Gets an instance of an implementation of the klass interface or
         * subclass of klass that delegates public virtual methods to the
         * luaTable table.
         * Caches the generated type.
         */
        public object GetClassInstance(Type klass, LuaTable luaTable)
        {
            LuaClassType luaClassType;
            if (classCollection.ContainsKey(klass))
            {
                luaClassType=classCollection[klass];
            }
            else
            {
                luaClassType=new LuaClassType();
                GenerateClass(klass,out luaClassType.klass,out luaClassType.returnTypes,luaTable);
                classCollection[klass] = luaClassType;
            }
            return Activator.CreateInstance(luaClassType.klass,new object[] {luaTable,luaClassType.returnTypes});
        }
    }
}
