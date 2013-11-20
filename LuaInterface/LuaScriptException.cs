using System;

namespace LuaInterface
{
    /// <summary>
    /// Exceptions thrown by the Lua runtime because of errors in the script
    /// </summary>
    public class LuaScriptException : LuaException
    {
        /// <summary>
        /// Returns true if the exception has occured as the result of a .NET exception in user code
        /// </summary>
        public bool IsNetException { get; private set; }

        private readonly string source;

        /// <summary>
        /// The position in the script where the exception was triggered.
        /// </summary>
        public override string Source { get { return source; } }

        /// <summary>
        /// Creates a new Lua-only exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="source">The position in the script where the exception was triggered.</param>
        public LuaScriptException(string message, string source) : base(message)
        {
            this.source = source;
        }

        /// <summary>
        /// Creates a new .NET wrapping exception.
        /// </summary>
        /// <param name="innerException">The .NET exception triggered by user-code.</param>
        /// <param name="source">The position in the script where the exception was triggered.</param>
        public LuaScriptException(Exception innerException, string source)
            : base(innerException.Message, innerException)
        {
            this.source = source;
            this.IsNetException = true;
        }

        public override string ToString()
        {
            // Prepend the error source		
            return GetType().FullName + ": " + source + Message;
        }
    }
}
