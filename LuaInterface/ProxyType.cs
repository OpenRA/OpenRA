using System;
using System.Globalization;
using System.Reflection;

namespace LuaInterface
{
    /// <summary>
    /// Summary description for ProxyType.
    /// </summary>
    public class ProxyType : IReflect
    {

        Type proxy;

        public ProxyType(Type proxy)
        {
            this.proxy = proxy;
        }

        /// <summary>
        /// Provide human readable short hand for this proxy object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ProxyType(" + UnderlyingSystemType + ")";
        }


        public Type UnderlyingSystemType
        {
            get
            {
                return proxy;
            }
        }

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return proxy.GetField(name, bindingAttr);
        }

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return proxy.GetFields(bindingAttr);
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            return proxy.GetMember(name, bindingAttr);
        }

        public MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return proxy.GetMembers(bindingAttr);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
        {
            return proxy.GetMethod(name, bindingAttr);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            return proxy.GetMethod(name, bindingAttr, binder, types, modifiers);
        }

        public MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return proxy.GetMethods(bindingAttr);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
        {
            return proxy.GetProperty(name, bindingAttr);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return proxy.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
        }

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return proxy.GetProperties(bindingAttr);
        }

        public object InvokeMember(string name,	BindingFlags invokeAttr, Binder binder,	object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            return proxy.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

    }
}
