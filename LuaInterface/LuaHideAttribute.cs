using System;

namespace LuaInterface
{
    /// <summary>
    /// Marks a method, field or property to be hidden from Lua auto-completion
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class LuaHideAttribute : Attribute
    {}
}
