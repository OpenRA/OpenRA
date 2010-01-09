//     ====================================================================================================
//     YAML Parser for the .NET Framework
//     ====================================================================================================
//
//     Copyright (c) 2006
//         Christophe Lambrechts
//         Jonathan Slenders
//
//     ====================================================================================================
//     This file is part of the .NET YAML Parser.
// 
//     This .NET YAML parser is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as published by
//     the Free Software Foundation; either version 2.1 of the License, or
//     (at your option) any later version.
// 
//     The .NET YAML parser is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Lesser General Public License for more details.
// 
//     You should have received a copy of the GNU Lesser General Public License
//     along with Foobar; if not, write to the Free Software
//     Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USAusing System.Reflection;
//     ====================================================================================================

using System;

namespace Yaml
{
	/// <summary>
	///   ParseException, could be thrown while parsing a YAML stream
	/// </summary>
	public class ParseException : Exception
	{
		// Line of the Yaml stream/file where the fault occures
		private readonly int linenr;

		/// <summary> Constructor </summary>
		/// <param name="stream"> The parse stream (contains the line number where it went wrong) </param>
		/// <param name="message"> Info about the exception </param>
		public ParseException (ParseStream stream, string message) :
			base ("Parse error near line " + stream.CurrentLine + ": " + message)
		{
			this.linenr = stream.CurrentLine;
		}

		/// <summary> Constructor </summary>
		/// <param name="stream"> The parse stream (contains the line number where it went wrong) </param>
		/// <param name="child"> The exception that is for example throwed again </param>
		public ParseException (ParseStream stream, Exception child) :
			base ( "Parse error near line " + stream.CurrentLine, child )
		{
			this.linenr = stream.CurrentLine;
		}

		/// <summary> The line where the error occured </summary>
		public int LineNumber
		{
			get { return linenr; }
		}
	}
}
