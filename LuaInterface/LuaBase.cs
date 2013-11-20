using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInterface
{
    /// <summary>
    /// Base class to provide consistent disposal flow across lua objects. Uses code provided by Yves Duhoux and suggestions by Hans Schmeidenbacher and Qingrui Li
    /// </summary>
    public abstract class LuaBase : IDisposable
    {
        private bool _Disposed;
        protected int _Reference;
        protected Lua _Interpreter;

        ~LuaBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposeManagedResources)
        {
            if (!_Disposed)
            {
                if (disposeManagedResources)
                {
                    if (_Reference != 0)
                        _Interpreter.dispose(_Reference);
                }
                _Interpreter = null;
                _Disposed = true;
            }
        }

        public override bool Equals(object o)
        {
            if (o is LuaBase)
            {
                LuaBase l = (LuaBase)o;
                return _Interpreter.compareRef(l._Reference, _Reference);
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return _Reference;
        }
    }
}
