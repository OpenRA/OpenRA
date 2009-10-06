using OpenRa.Game.Graphics;
using OpenRa.TechTree;

namespace OpenRa.Game
{
	class SidebarItem
	{
		public readonly Item techTreeItem;
		public readonly float2 location;
		readonly Sprite sprite;

		public bool isBuilding = false;

		public SidebarItem(Sprite s, Item item, int y)
		{
			this.techTreeItem = item;
			this.sprite = s;
			location = new float2(item.IsStructure ? 0 : 64, y);
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
