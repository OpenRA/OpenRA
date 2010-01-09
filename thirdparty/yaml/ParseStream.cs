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
using System.Collections;

using System.IO;

namespace Yaml
{
	/// <summary>
	///   The Preprocessor class
	///   Given a character stream, this class will
	///   walk through that stream.
	///   NOTE: Comments are not longer skipped at this level,
	///         but now in the last level instead. (because of
	///         problems with comments within the buffer)
	///   NOTE: Null characters are skipped, read nulls should
	///         be escaped. \0
	/// </summary>
	public class Preprocessor
	{
		private TextReader stream;
		private int  currentline = 1; // Line numbers start with one
		private bool literal = false; // Parse literal/verbatim

		/// <summary> Constuctor </summary>
		public Preprocessor (TextReader stream)
		{
			this.stream = stream;
		}

		/// <summary> Jump to the next character </summary>
		public void Next ()
		{
			// Transition to the next line?
			if (Char == '\n')
				currentline ++;

			// Not yet passed the end of file
			if (! EOF)
			{
				// Next
				stream.Read ();

				// Skip null chars
				while (stream.Peek () == '\0')
					stream.Read ();
			}
		}

		/// <summary> Start parsing literal </summary>
		public void StartLiteral ()
		{
			literal = true;
		}

		/// <summary> Stop parsing literal </summary>
		public void StopLiteral ()
		{
			if (literal)
				literal = false;
			else
				throw new Exception ("Called StopLiteral without " +
						"calling StartLiteral before");
		}

		/// <summary> Literal parsing </summary>
		public bool Literal
		{
			get { return literal; }
			// No set method, setting must by using the {Start,Stop}Literal
			// methods. They provide mory symmetry in the parser.
		}

		/// <summary> The current character </summary>
		public char Char
		{
			get
			{
				if (EOF)
					return '\0';
				else
					return (char) stream.Peek ();
			}
		}

		/// <summary> End of file/stream </summary>
		public bool EOF
		{
			get { return stream.Peek () == -1; }
		}

		/// <summary> Returns the current line number </summary>
		public int CurrentLine
		{
			get { return currentline; }
		}
	}

	/// <summary>
	///   The indentation processor,
	///   This class divides the stream from the preprocessor
	///   in substreams, according to the current level
	///   of indentation.
	/// </summary>
	public class IndentationProcessor : Preprocessor
	{
		// While trying to readahead over whitespaces,
		// This is how many whitespaces were skipped that weren't yet read
		private int   whitespaces         = 0;
		private int   whitespacesSkipped  = 0;
		
		// Reached the end
		private bool  endofstream         = false;

		// Current level of indentation
		private int   indentationLevel    = 0;
		private bool  indentationRequest  = false;
		private Stack indentationStack    = new Stack ();

		/// <summary> Constructor </summary>
		public IndentationProcessor (TextReader stream) : base (stream) { }

		/// <summary>
		///   Request an indentation. When we meet a \n and the following
		///   line is more indented then the current indentationlever, then
		///   save this request
		/// </summary>
		public void Indent ()
		{
			if (Literal)
				throw new Exception ("Cannot (un)indent while literal parsing " +
						"has been enabled");
			else
			{
				// Handle double requests
				if (indentationRequest)
					indentationStack.Push ((object) indentationLevel);

				// Remember
				indentationRequest = true;
			}
		}

		/// <summary> Cancel the last indentation </summary>
		public void UnIndent ()
		{
			if (Literal)
				throw new Exception ("Cannot (un)indent while literal parsing " +
						"has been enabled");
			else
			{
				// Cancel the indentation request
				if (indentationRequest)
				{
					indentationRequest = false;
					return;
				}

				// Unpop the last indentation
				if (indentationStack.Count > 0)
					indentationLevel = (int) indentationStack.Pop ();

				// When not indented
				else
					throw new Exception ("Unable to unindent a not indented parse stream");

				// Parent stream not yet finished
				// Skipped whitespaces in the childstream (at that time assumed to be
				// indentation) can become content.
				if (endofstream && indentationLevel <= whitespaces)
				{
					endofstream = false;
					if (whitespaces == this.indentationLevel)
						whitespaces = 0;
				}
			}
		}

		/// <summary> Go to the next parsable char in the stream </summary>
		public new void Next ()
		{
			if (endofstream)
				return;

			// Are there still whitespaces to skip
			if (whitespaces > 0)
			{
				// All whitespaces were skipped
				if (whitespaces == whitespacesSkipped + this.indentationLevel)
					whitespaces = 0;

				// Else, skip one
				else
				{
					whitespacesSkipped ++;
					return;
				}
			}

			// All whitespaces have been skipped
			if (whitespaces == 0  && ! base.EOF)
			{
				// When a char is positioned at a newline '\n', 
				// then skip 'indentation' chars and continue.
				// When there are less spaces available, then we are
				// at the end of the (sub)stream
				if (! base.EOF && base.Char == '\n' && ! Literal)
				{
					// Skip over newline
					base.Next ();

					// Skip indentation (and count the spaces)
					int i = 0;
					while (! base.EOF && base.Char == ' ' && i < this.indentationLevel)
					{
						i ++;
						base.Next ();
					}

					// Not enough indented?
					if (i < this.indentationLevel)
					{
						// Remember the number of whitespaces, and
						// continue at the moment that the indentationlevel
						// drops below this number of whitespaces
						whitespaces = i;
						whitespacesSkipped = 0;
						endofstream = true;
						return;
					}
					// Indentation request
					else if (indentationRequest)
					{
						while (! base.EOF && base.Char == ' ')
						{
							i ++;
							base.Next ();
						}

						// Remember current indentation
						indentationStack.Push ((object) indentationLevel);
						indentationRequest = false;

						// Number of spaces before this line is equal to the
						// current level of indentation, so the
						// indentation request cannot be fulfilled
						if (indentationLevel == i)
						{
							whitespaces = i;
							whitespacesSkipped = 0;
							endofstream = true;
							return;
						}
						else // i > indentationLevel
							indentationLevel = i;
					}
				}
				else
					// Next char
					base.Next ();
			}
			else
				endofstream = true;
		}

		/// <summary> Reads the current char from the stream </summary>
		public new char Char
		{
			get
			{
				// In case of spaces
				if (whitespaces > 0)
					return ' ';

				// \0 at the end of the stream
				else if (base.EOF || endofstream)
					return '\0';

				// Return the char 
				else
					return base.Char;
			}
		}

		/// <summary> End of File/Stream </summary>
		public new bool EOF
		{
			get { return endofstream || base.EOF; }
		}
	}

	/// <summary>
	///   Third stream processor, this class adds a buffer with a maximum
	///   size of 1024 chars. The buffer cannot encapsulate multiple lines
	///   because that could do strange things while rewinding/indenting
	/// </summary>

	public class BufferStream : IndentationProcessor
	{
		LookaheadBuffer buffer = new LookaheadBuffer ();

		// When the buffer is used, this is true
		private bool    useLookaheadBuffer      = false;

		// In use, but requested to destroy. The buffer will keep to exists
		// (only in this layer) and shall be destroyed when we move out of
		// the buffer
		private bool    destroyRequest          = false;

		/// <summary> Constructor </summary>
		public BufferStream (TextReader stream) : base (stream) { }

		/// <summary> Build lookahead buffer </summary>
		public void BuildLookaheadBuffer ()
		{
			if (Literal)
				throw new Exception ("Cannot build a buffer while " +
						"literal parsing is enabled");
			else
			{
				// When the buffer is already in use
				if (useLookaheadBuffer && ! destroyRequest)
					throw new Exception ("Buffer already exist, cannot rebuild " +
							"the buffer at this level");

				// Cancel the destroy request
				if (destroyRequest)
					destroyRequest = false;

				// Or start a new buffer
				else
				{
					buffer.Clear ();
					buffer.Append (Char);
				}

				useLookaheadBuffer = true;
			}
		}

		/// <summary> Move to the next character in the parse stream. </summary>
		public new void Next ()
		{
			// End of file (This check is not really necessary because base.next
			// would skip this anyway)
			if (EOF) return;

			// When it's not allowed to leave the buffer
			if (useLookaheadBuffer && ! destroyRequest && ! NextInBuffer () )
				return;

			// When using the lookahead buffer
			if (useLookaheadBuffer)
			{
				// Requested to destroy
				if (destroyRequest)
				{
					// But not yet reached the end of the buffer
					if (buffer.Position < buffer.LastPosition)
					{
						buffer.Position ++;
						buffer.ForgetThePast ();
					}
					// Reached the end
					else
					{
						buffer.Clear ();
						useLookaheadBuffer = false;
						destroyRequest = false;

						base.Next ();
					}
				}
				// Continue in the buffer
				else
				{
					// We've been here before
					if (buffer.Position < buffer.LastPosition)
						buffer.Position ++;

					// This is new to the buffer, but there is place
					// to remember new chars
					else if (
						buffer.Position == buffer.LastPosition &&
						! buffer.Full)
					{
						// Save the next  char in the buffer
						base.Next();
						buffer.Append (base.Char);
					}
					// Otherwise, the buffer is full
					else 
						throw new Exception ("buffer overflow");
				}
			}
			// Not using the buffer
			else
				base.Next();
		}

		/// <summary> Returns true when using a buffer  </summary>
		public bool UsingBuffer ()
		{
			return useLookaheadBuffer && ! destroyRequest;
		}

		/// <summary>
		///   Returns true when the next char will still be in the buffer
		///   (after calling next)
		/// </summary>
		private bool NextInBuffer ()
		{
			return 
				// Using the buffer
				useLookaheadBuffer && 
				
				// Next char has been read before
				(buffer.Position < buffer.LastPosition || 
				
				// Or the next char will also be in the buffer
				(Char != '\n' && ! base.EOF && 

				// There is still unused space
				! buffer.Full));
		}

		/// <summary> Destroys the current lookaheadbuffer, if there is one </summary>
		public void DestroyLookaheadBuffer ()
		{
			if (useLookaheadBuffer && ! destroyRequest)
			{
				buffer.ForgetThePast ();
				destroyRequest = true;
			}
			else
				throw new Exception ("Called destroy buffer before building the buffer");
		}

		/// <summary> Rewind the buffer </summary>
		public void RewindLookaheadBuffer ()
		{
			if (! useLookaheadBuffer || destroyRequest)
				throw new Exception ("Cannot rewind the buffer. No buffer in use");

			else
				buffer.Rewind ();
		}

		/// <summary> The current character </summary>
		public new char Char
		{
			get
			{
				// When using a buffer
				if (useLookaheadBuffer)
					return buffer.Char;

				else
					return base.Char;
			}
		}

		/// <summary> End of stream/file </summary>
		public new bool EOF
		{
			get 
			{
				return 
					// When it's not allowed to run out of the buffer
					(useLookaheadBuffer && ! destroyRequest && ! NextInBuffer () ) ||

					// Not using the buffer, but the end of stream has been reached
					(! useLookaheadBuffer && base.EOF);
			}
		}

		/// <summary> Current position in the lookahead buffer </summary>
		protected int LookaheadPosition
		{
			get
			{
				if (useLookaheadBuffer)
					return buffer.Position;

				else
					throw new Exception ("Not using a lookahead buffer");
			}
			set
			{
				if (useLookaheadBuffer)
				{
					if (value >= 0 && value <= buffer.LastPosition)
						buffer.Position = value;

					else
						throw new Exception ("Lookahead position not between 0 " +
								"and the buffer size");
				}
				else
					throw new Exception ("Not using a lookahead buffer");
			}
		}
	}

	/// <summary> Parsestream with multilever buffer </summary>
	public class MultiBufferStream : BufferStream
	{
		private Stack bufferStack = new Stack (); // Top is current buffer start

		/// <summary> Constructor </summary>
		public MultiBufferStream (TextReader stream) : base (stream) { }

		/// <summary> Destroy the current buffer </summary>
		public new void BuildLookaheadBuffer ()
		{
			if (Literal)
				throw new Exception ("Cannot build a buffer while " +
						"literal parsing is enabled");
			else
			{
				// Already using a buffer
				if (base.UsingBuffer ())
					// Remember the current position
					bufferStack.Push ((object) base.LookaheadPosition);

				// Otherwise, create a new buffer
				else
				{
					// Remember the current position (= 0)
					bufferStack  .Push ((object) 0);

					base.BuildLookaheadBuffer ();
				}
			}
		}

		/// <summary> Destroy the current buffer </summary>
		public new void DestroyLookaheadBuffer ()
		{
			// Clear the buffer info when we runned out of the buffer,
			if ( ! base.UsingBuffer () )
				bufferStack.Clear ();

			else
			{
				// Unpop the buffers start index
				bufferStack.Pop ();

				// Destroy it when the last buffer is gone
				if (bufferStack.Count == 0)
					base.DestroyLookaheadBuffer ();
			}
		}

		/// <summary> Rewind the current buffer </summary>
		public new void RewindLookaheadBuffer ()
		{
			if (base.UsingBuffer () )
				base.LookaheadPosition = (int) bufferStack.Peek ();
			else
				throw new Exception ("Rewinding not possible. Not using a " +
						"lookahead buffer.");
		}
	}

	/// <summary>
	///   Drop the comments
	///   (This is disabled when literal parsing is enabled)
	/// </summary>
	public class DropComments : MultiBufferStream
	{
		/// <summary> Constructor </summary>
		public DropComments (TextReader stream) : base (stream) { }

		/// <summary> Move to the next character in the parse stream. </summary>
		public new void Next ()
		{
			base.Next ();

			// Skip comments
			if (base.Char == '#' && ! Literal)
				while (! base.EOF && base.Char != '\n')
					base.Next ();
		}
	}

	/// <summary>
	///  This layer removes the trailing newline at the end of each (sub)stream
	/// </summary>
	public class DropTrailingNewline : DropComments
	{
		// One char buffer
		private bool newline = false;

		/// <summary> Constructor </summary>
		public DropTrailingNewline (TextReader stream) : base (stream) { }

		/// <summary> The current character </summary>
		public new char Char
		{
			get
			{
				if (EOF)
					return '\0';
				else if (newline)
					return '\n';
				else
					return base.Char;
			}
		}

		/// <summary> End of File/Stream </summary>
		public new bool EOF
		{
			get { return ! newline && base.EOF; }
		}

		/// <summary> Skip space characters </summary>
		public int SkipSpaces ()
		{
			int count = 0;
			while (Char == ' ')
			{
				Next ();
				count ++;
			}
			return count;
		}

		/// <summary> Move to the next character in the parse stream. </summary>
		public new void Next ()
		{
			Next (false);
		}

		/// <summary> Move to the next character in the parse stream. </summary>
		/// <param name="dropLastNewLine"> Forget the last newline </param>
		public void Next (bool dropLastNewLine)
		{
			if (newline)
				newline = false;
			else
			{
				base.Next ();

				if (dropLastNewLine && ! base.EOF && Char == '\n')
				{
					base.Next ();

					if (base.EOF)
						newline = false;
					else
						newline = true;
				}
			}
		}
	}


	/// <summary>
	///   Stops parsing at specific characters, useful for parsing inline
	///   structures like (for instance):
	///
	///   [aaa, bbb, ccc, {ddd: eee, "fff": ggg}]
	/// </summary>
	public class ParseStream : DropTrailingNewline
	{
		private Stack stopstack = new Stack ();

		/// <summary> Constructor </summary>
		public ParseStream (TextReader stream) : base (stream) { }

		/// <summary> Set the characters where we should stop. </summary>
		public void StopAt (char [] characters)
		{
			stopstack.Push (characters);
		}

		/// <summary> Unset the characters where we should stop. </summary>
		public void DontStop ()
		{
			if (stopstack.Count > 0)
				stopstack.Pop ();
			else
				throw new Exception ("Called DontStop without " +
						"calling StopAt before");
		}

		/// <summary> True when we have to stop here </summary>
		private bool StopNow
		{
			get {
				if (stopstack.Count > 0)
					foreach (char c in (char []) stopstack.Peek ())
						if (c == base.Char)
							return true;

				return false;
			}
		}

		/// <summary> Start parsing literal </summary>
		public new void StartLiteral ()
		{
			base.StartLiteral ();

			// Parsing literal disables stopping
			StopAt (new Char [] { });
		}

		/// <summary> Stop parsing literal </summary>
		public new void StopLiteral ()
		{
			base.StopLiteral ();

			DontStop ();
		}
		/// <summary> Move to the next character in the parse stream. </summary>
		public new void Next ()
		{
			Next (false);
		}

		/// <summary> Move to the next character in the parse stream. </summary>
		public new void Next (bool dropLastNewLine)
		{
			if ( ! StopNow )
				base.Next (dropLastNewLine);
		}

		/// <summary> The current character </summary>
		public new char Char
		{
			get
			{
				if (StopNow)
					return '\0';

				else
					return base.Char;
			}
		}

		/// <summary> End of stream/file </summary>
		public new bool EOF
		{
			get { return StopNow || base.EOF; }
		}
	}

	/// <summary>
	///   The lookahead buffer, used by the buffer layer in the parser
	/// </summary>
	class LookaheadBuffer
	{
		// The buffer array
		private char [] buffer = new char [1024];

		private	int size     =  0; // 0 = Nothing in the buffer
		private int position = -1; // Current position
		private int rotation =  0; // Start of circular buffer

		/// <summary> Character at the current position </summary>
		public char Char
		{
			get
			{
				if (size > 0)
					return buffer [(position + rotation) % buffer.Length];

				else
					throw new Exception ("Trying to read from an emty buffer");
			}
		}

		/// <summary> The current position </summary>
		public int Position
		{
			get { return position; }

			set
			{
				if (value >= 0 && value < size)
					position = value;
				else
					throw new Exception ("Buffer position should be " +
							"between zero and 'size' ");
			}
		}

		/// <summary> The last possible postition which could be set </summary>
		public int LastPosition
		{
			get { return size - 1; }
		}

		/// <summary>
		///   The last possible position which could be set if
		///   the buffer where full
		/// </summary>
		public int MaxPosition
		{
			get { return buffer.Length - 1; }
		}

		/// <summary> True when the buffer is full </summary>
		public bool Full
		{
			get { return size == buffer.Length; }
		}

		/// <summary> Current buffer size </summary>
		public int Size
		{
			get { return size; }
		}

		/// <summary> Append a character to the buffer </summary>
		public void Append (char c)
		{
			// Appending is only possible when the current position is the
			// last in the buffer
			if (position < LastPosition)
				throw new Exception ("Appending to buffer only possible " +
						"when the position is the last");

			// Buffer overflow
			if (size == buffer.Length)
				throw new Exception ("Buffer full");

			// Append
			position ++;
			size     ++;
			buffer [(position + rotation) % buffer.Length] = c;
		}

		/// <summary> Rewind the buffer </summary>
		public void Rewind ()
		{
			position = 0;
		}

		/// <summary> Reset (clear) the buffer </summary>
		public void Clear ()
		{
			position = -1;
			size     =  0;
		}

		/// <summary> Move to the next character </summary>
		public void Next ()
		{
			if (Position < Size)
				Position ++;

			else throw new Exception ("Cannot move past the buffer");
		}

		/// <summary>
		///   Remove characters from the buffer before the current character
		/// </summary>
		public void ForgetThePast ()
		{
			// Size becomes smaller, characters before the position should be dropped
			size -= position;

			// The current position becomes the new startposition
			rotation = (rotation + position + buffer.Length) % buffer.Length;

			// The current position in the new buffer becomes zero
			position =  0;
		}
	}
}
