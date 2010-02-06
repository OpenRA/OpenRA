using OpenRa.Graphics;

namespace OpenRa
{
	public class Cursor
	{
		CursorSequence sequence;
		public Cursor(string cursor)
		{
			sequence = SequenceProvider.GetCursorSequence(cursor);
		}

		public Sprite GetSprite(int frame) { return sequence.GetSprite(frame); }
		public int2 GetHotspot() { return sequence.Hotspot; }
	}
}
