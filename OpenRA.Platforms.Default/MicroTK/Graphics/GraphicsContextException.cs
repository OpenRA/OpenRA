using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK.Graphics
{
	/// <summary>
	/// Represents errors related to a GraphicsContext.
	/// </summary>
	public class GraphicsContextException : Exception
	{
		/// <summary>
		/// Constructs a new GraphicsContextException.
		/// </summary>
		public GraphicsContextException() : base() { }
		/// <summary>
		/// Constructs a new GraphicsContextException with the given error message.
		/// </summary>
		public GraphicsContextException(string message) : base(message) { }
	}
}
