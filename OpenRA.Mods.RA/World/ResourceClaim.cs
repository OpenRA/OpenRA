namespace OpenRA.Mods.RA
{
	public sealed class ResourceClaim
	{
		public readonly Actor Claimer;
		public CPos Cell;

		public ResourceClaim(Actor claimer, CPos cell) { Claimer = claimer; Cell = cell; }
	}
}
