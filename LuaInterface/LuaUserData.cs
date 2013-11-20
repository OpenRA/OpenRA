using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInterface
{
    public class LuaUserData : LuaBase
    {
        //internal int _Reference;
        //private Lua _Interpreter;
        public LuaUserData(int reference, Lua interpreter)
        {
            _Reference = reference;
            _Interpreter = interpreter;
        }
        //~LuaUserData()
        //{
        //    if (_Reference != 0)
        //        _Interpreter.dispose(_Reference);
        //}
        /*
         * Indexer for string fields of the userdata
         */
        public object this[string field]
        {
            get
            {
                return _Interpreter.getObject(_Reference, field);
            }
            set
            {
                _Interpreter.setObject(_Reference, field, value);
            }
        }
        /*
         * Indexer for numeric fields of the userdata
         */
        public object this[object field]
        {
            get
            {
                return _Interpreter.getObject(_Reference, field);
            }
            set
            {
                _Interpreter.setObject(_Reference, field, value);
            }
        }
        /*
         * Calls the userdata and returns its return values inside
         * an array
         */
        public object[] Call(params object[] args)
        {
            return _Interpreter.callFunction(this, args);
        }
        /*
         * Pushes the userdata into the Lua stack
         */
        internal void push(IntPtr luaState)
        {
            LuaDLL.lua_getref(luaState, _Reference);
        }
        public override string ToString()
        {
            return "userdata";
        }
        //public override bool Equals(object o)
        //{
        //    if (o is LuaUserData)
        //    {
        //        LuaUserData l = (LuaUserData)o;
        //        return _Interpreter.compareRef(l._Reference, _Reference);
        //    }
        //    else return false;
        //}
        //public override int GetHashCode()
        //{
        //    return _Reference;
        //}
    }
}
