using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using System.IO;

namespace OpenRa.Game
{
	public class Cursor
	{
		CursorSequence sequence;
		Cursor(string cursor)
		{
			sequence = SequenceProvider.GetCursorSequence(cursor);
			
		}

		public static Cursor Default
		{
			get { return new Cursor("default"); }
		}

		public static Cursor Move
		{
			get { return new Cursor("move"); }
		}
	}
}
