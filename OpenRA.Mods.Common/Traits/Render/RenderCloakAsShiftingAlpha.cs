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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render the actor with alpha when cloaked.")]
	public class RenderCloakAsShiftingAlphaInfo : RenderCloakAsBaseInfo, IRulesetLoaded
	{
		[Desc("The minimum alpha level to use.")]
		public readonly float MinAlpha = 0.4f;

		[Desc("The maximum alpha level to use.")]
		public readonly float MaxAlpha = 0.7f;

		[FieldLoader.Require]
		[Desc("Time to to change from maximum alpha level to minimum alpha level.")]
		public readonly int ChangeInterval = 0;

		[Desc("Starting percentage from the minimum level to the maximum alpha level.",
			"0 means the minimum alpha level, 100 means the maximum alpha level, and empty means random.")]
		public readonly int? StartingPercent = null;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
			if (MinAlpha < 0 || MinAlpha > 1)
				throw new YamlException("RenderCloakAsShiftingAlpha.MinAlpha must be between 0 and 1.");
			if (MaxAlpha < 0 || MaxAlpha > 1)
				throw new YamlException("RenderCloakAsShiftingAlpha.MaxAlpha must be between 0 and 1.");
			if (ChangeInterval < 1)
				throw new YamlException("RenderCloakAsShiftingAlpha.AlphaChangeInterval must be greater than 0.");
			if (StartingPercent.HasValue && (StartingPercent.Value < 0 || StartingPercent.Value > 100))
				throw new YamlException("RenderCloakAsShiftingAlpha.StartingPercent must be between 0 and 100.");
		}

		public override object Create(ActorInitializer init) { return new RenderCloakAsShiftingAlpha(init, this); }
	}

	public class RenderCloakAsShiftingAlpha : RenderCloakAsBase<RenderCloakAsShiftingAlphaInfo>, ITick
	{
		float currentAlpha;
		float alphaChange;
		bool cloaked = false;
		public RenderCloakAsShiftingAlpha(ActorInitializer init, RenderCloakAsShiftingAlphaInfo info)
			: base(info)
		{
			alphaChange = (Info.MaxAlpha - Info.MinAlpha) / Info.ChangeInterval;
			currentAlpha = Info.MinAlpha + alphaChange * (Info.StartingPercent.HasValue
				? Info.ChangeInterval * Info.StartingPercent.Value / 100
				: Game.CosmeticRandom.Next(Info.ChangeInterval));
		}

		protected override IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return r.Select(a => !a.IsDecoration && a is IModifyableRenderable mr ? mr.WithAlpha(currentAlpha) : a);
		}

		protected override void OnCloaked(Actor self, CloakInfo cloakInfo, bool isInitial)
		{
			cloaked = true;
		}

		protected override void OnUncloaked(Actor self, CloakInfo cloakInfo, bool isInitial)
		{
			cloaked = false;
		}

		void ITick.Tick(Actor self)
		{
			if (!cloaked)
				return;

			currentAlpha += alphaChange;
			if (alphaChange > 0 && currentAlpha > Info.MaxAlpha)
			{
				alphaChange = -alphaChange;
				currentAlpha = Info.MaxAlpha;
			}
			else if (alphaChange < 0 && currentAlpha < Info.MinAlpha)
			{
				alphaChange = -alphaChange;
				currentAlpha = Info.MinAlpha;
			}
		}
	}
}
