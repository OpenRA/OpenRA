﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public sealed class ResourceClaimLayerInfo : TraitInfo<ResourceClaimLayer>
	{
	}

	public sealed class ResourceClaimLayer : IWorldLoaded
	{
		Dictionary<CPos, ResourceClaim> claimByCell;
		Dictionary<Actor, ResourceClaim> claimByActor;

		private void MakeClaim(Actor claimer, CPos cell)
		{
			UnclaimByActor(claimer);
			UnclaimByCell(cell, claimer);
			claimByActor[claimer] = claimByCell[cell] = new ResourceClaim(claimer, cell);
		}

		private void Unclaim(ResourceClaim claim, Actor claimer)
		{
			if (claimByActor.Remove(claim.Claimer) & claimByCell.Remove(claim.Cell))
			{
				if (claim.Claimer.Destroyed) return;
				if (!claim.Claimer.IsInWorld) return;
				if (claim.Claimer.IsDead()) return;

				claim.Claimer.Trait<INotifyResourceClaimLost>().OnNotifyResourceClaimLost(claim.Claimer, claim, claimer);
			}
		}

		public void WorldLoaded(OpenRA.World w, WorldRenderer wr)
		{
			// NOTE(jsd): 32 seems a sane default initial capacity for the total # of harvesters in a game. Purely a guesstimate.
			claimByCell = new Dictionary<CPos, ResourceClaim>(32);
			claimByActor = new Dictionary<Actor, ResourceClaim>(32);
		}

		/// <summary>
		/// Attempt to claim the resource at the cell for the given actor.
		/// </summary>
		/// <param name="claimer"></param>
		/// <param name="cell"></param>
		/// <returns></returns>
		public bool ClaimResource(Actor claimer, CPos cell)
		{
			// Has anyone else claimed this point?
			ResourceClaim claim;
			if (claimByCell.TryGetValue(cell, out claim))
			{
				// Same claimer:
				if (claim.Claimer == claimer) return true;

				// This is to prevent in-fighting amongst friendly harvesters:
				if (claimer.Owner == claim.Claimer.Owner) return false;
				if (claimer.Owner.Stances[claim.Claimer.Owner] == Stance.Ally) return false;

				// If an enemy/neutral claimed this, don't respect that claim:
			}

			// Either nobody else claims this point or an enemy/neutral claims it:
			MakeClaim(claimer, cell);
			return true;
		}

		/// <summary>
		/// Release the last resource claim made on this cell.
		/// </summary>
		/// <param name="cell"></param>
		public void UnclaimByCell(CPos cell, Actor claimer)
		{
			ResourceClaim claim;
			if (claimByCell.TryGetValue(cell, out claim))
				Unclaim(claim, claimer);
		}

		/// <summary>
		/// Release the last resource claim made by this actor.
		/// </summary>
		/// <param name="claimer"></param>
		public void UnclaimByActor(Actor claimer)
		{
			ResourceClaim claim;
			if (claimByActor.TryGetValue(claimer, out claim))
				Unclaim(claim, claimer);
		}

		/// <summary>
		/// Is the cell location <paramref name="cell"/> claimed for harvesting by any other actor?
		/// </summary>
		/// <param name="self"></param>
		/// <param name="cell"></param>
		/// <returns>true if already claimed by an ally that isn't <paramref name="self"/>; false otherwise.</returns>
		public bool IsClaimedByAnyoneElse(Actor self, CPos cell, out ResourceClaim claim)
		{
			if (claimByCell.TryGetValue(cell, out claim))
			{
				// Same claimer:
				if (claim.Claimer == self) return false;

				// This is to prevent in-fighting amongst friendly harvesters:
				if (self.Owner == claim.Claimer.Owner) return true;
				if (self.Owner.Stances[claim.Claimer.Owner] == Stance.Ally) return true;

				// If an enemy/neutral claimed this, don't respect that claim and fall through:
			}
			else
			{
				// No claim.
				claim = null;
			}

			return false;
		}
	}
}
