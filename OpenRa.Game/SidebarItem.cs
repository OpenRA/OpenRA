using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class SidebarItem
	{
		public readonly float2 location;
		public readonly string Tag;
		public readonly bool IsStructure;
		readonly Sprite sprite;

		public SidebarItem(Sprite s, string tag, bool isStructure, int y)
		{
			this.sprite = s;
			this.Tag = tag;
			this.IsStructure = isStructure;
			location = new float2(isStructure ? 0 : 64, y);
		}

		public bool Clicked(float2 p)
		{
			if (p.X < location.X || p.Y < location.Y)
				return false;

			if (p.X > location.X + 64 || p.Y > location.Y + 48)
				return false;

			return true;
		}

		public void Paint(SpriteRenderer renderer, float2 offset)
		{
			renderer.DrawSprite(sprite, location + offset, 0);
		}
	}
}
