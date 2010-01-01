using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class Cursor
	{
		CursorSequence sequence;
		Cursor(string cursor)
		{
			sequence = SequenceProvider.GetCursorSequence(cursor);
		}

		public Sprite GetSprite(int frame) { return sequence.GetSprite(frame); }
		public int2 GetHotspot() { return sequence.Hotspot; }

		public static Cursor None { get { return null; } }
		public static Cursor Default { get { return new Cursor("default"); } }
		public static Cursor Move { get { return new Cursor("move"); } }
		public static Cursor Select { get { return new Cursor("select"); } }
		public static Cursor MoveBlocked { get { return new Cursor("move-blocked"); } }
		public static Cursor Attack { get { return new Cursor("attack"); } }
		public static Cursor Deploy { get { return new Cursor("deploy"); } }
		public static Cursor Enter { get { return new Cursor("enter"); } }
		public static Cursor DeployBlocked { get { return new Cursor("deploy-blocked"); } }
        public static Cursor Chronoshift { get { return new Cursor("chrono"); } }
		public static Cursor C4 { get { return new Cursor("c4"); } }
		public static Cursor Capture { get { return new Cursor("capture"); } }
		public static Cursor Heal { get { return new Cursor("heal"); } }
		public static Cursor Sell { get { return new Cursor("sell"); } }
	}
}
