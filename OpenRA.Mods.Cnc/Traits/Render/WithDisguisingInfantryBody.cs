#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	class WithDisguisingInfantryBodyInfo : WithInfantryBodyInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new WithDisguisingInfantryBody(init, this); }
	}

	class WithDisguisingInfantryBody : WithInfantryBody
	{
		readonly Disguise disguise;
		readonly RenderSprites rs;
		ActorInfo disguiseActor;
		Player disguisePlayer;
		WithInfantryBodyInfo disguiseInfantryBody;
		string disguiseImage;

		public WithDisguisingInfantryBody(ActorInitializer init, WithDisguisingInfantryBodyInfo info)
			: base(init, info)
		{
			rs = init.Self.Trait<RenderSprites>();
			disguise = init.Self.Trait<Disguise>();
		}

		protected override WithInfantryBodyInfo GetDisplayInfo()
		{
			return disguiseInfantryBody ?? Info;
		}

		protected override void Tick(Actor self)
		{
			if (disguise.AsActor != disguiseActor || disguise.AsPlayer != disguisePlayer)
			{
				// Force actor back to the stand state to avoid mismatched sequences
				PlayStandAnimation(self);

				disguiseActor = disguise.AsActor;
				disguisePlayer = disguise.AsPlayer;
				disguiseImage = null;
				disguiseInfantryBody = null;

				if (disguisePlayer != null)
				{
					var renderSprites = disguiseActor.TraitInfoOrDefault<RenderSpritesInfo>();
					var infantryBody = disguiseActor.TraitInfos<WithInfantryBodyInfo>()
						.FirstOrDefault(t => t.EnabledByDefault);
					if (renderSprites != null && infantryBody != null)
					{
						disguiseImage = renderSprites.GetImage(disguiseActor, disguisePlayer.Faction.InternalName);
						disguiseInfantryBody = infantryBody;
					}
				}

				var sequence = DefaultAnimation.GetRandomExistingSequence(GetDisplayInfo().StandSequences, Game.CosmeticRandom);
				if (sequence != null)
					DefaultAnimation.ChangeImage(disguiseImage ?? rs.GetImage(self), sequence);

				rs.UpdatePalette();
			}

			base.Tick(self);
		}
	}
}
