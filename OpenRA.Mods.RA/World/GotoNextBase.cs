using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class GotoNextBaseInfo : TraitInfo<GotoNextBase>
	{

	}

	public class GotoNextBase : INotifyKeyPress
	{
		public bool KeyPressed(Actor self, KeyInput e)
		{
			if (self.World.LocalPlayer == null) return false;

			if (e.KeyChar == '\b' || e.KeyChar == (char)127)
			{
				CycleBases(self.World);
				return true;
			}

			return false;
		}

		public static void CycleBases(World world)
		{
			var bases = world.Queries.OwnedBy[world.LocalPlayer].WithTrait<BaseBuilding>().ToArray();
			if (!bases.Any()) return;

			var next = bases
				.Select(b => b.Actor)
				.SkipWhile(b => !world.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.Select(b => b.Actor).First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);
			Game.viewport.Center(world.Selection.Actors);
		}
	}
}
