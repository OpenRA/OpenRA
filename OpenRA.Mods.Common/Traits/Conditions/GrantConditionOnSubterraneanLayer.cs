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

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants Condition on subterranean layer. Also plays transition audio-visuals.")]
	public class GrantConditionOnSubterraneanLayerInfo : GrantConditionOnLayerInfo
	{
		[Desc("Dig animation image to play when transitioning.")]
		public readonly string SubterraneanTransitionImage = null;

		[SequenceReference(nameof(SubterraneanTransitionImage))]
		[Desc("Dig animation sequence to play when transitioning.")]
		public readonly string SubterraneanTransitionSequence = null;

		[PaletteReference]
		public readonly string SubterraneanTransitionPalette = "effect";

		[Desc("Dig sound to play when transitioning.")]
		public readonly string SubterraneanTransitionSound = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnSubterraneanLayer(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var mobileInfo = ai.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo == null || !(mobileInfo.LocomotorInfo is SubterraneanLocomotorInfo))
				throw new YamlException("GrantConditionOnSubterraneanLayer requires Mobile to be linked to a SubterraneanLocomotor!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class GrantConditionOnSubterraneanLayer : GrantConditionOnLayer<GrantConditionOnSubterraneanLayerInfo>, INotifyCenterPositionChanged
	{
		WDist transitionDepth;

		public GrantConditionOnSubterraneanLayer(GrantConditionOnSubterraneanLayerInfo info)
			: base(info, CustomMovementLayerType.Subterranean) { }

		protected override void Created(Actor self)
		{
			var mobileInfo = self.Info.TraitInfo<MobileInfo>();
			var li = (SubterraneanLocomotorInfo)mobileInfo.LocomotorInfo;
			transitionDepth = li.SubterraneanTransitionDepth;
			base.Created(self);
		}

		void PlayTransitionAudioVisuals(Actor self, CPos fromCell)
		{
			if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSequence))
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.World.Map.CenterOfCell(fromCell), self.World,
					Info.SubterraneanTransitionImage,
					Info.SubterraneanTransitionSequence, Info.SubterraneanTransitionPalette)));

			if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSound))
				Game.Sound.Play(SoundType.World, Info.SubterraneanTransitionSound);
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			var depth = self.World.Map.DistanceAboveTerrain(self.CenterPosition);

			// Grant condition when new layer is Subterranean and depth is lower than transition depth,
			// revoke condition when new layer is not Subterranean and depth is at or higher than transition depth.
			if (newLayer == ValidLayerType && depth < transitionDepth && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
			else if (newLayer != ValidLayerType && depth > transitionDepth && conditionToken != Actor.InvalidConditionToken)
			{
				conditionToken = self.RevokeCondition(conditionToken);
				PlayTransitionAudioVisuals(self, self.Location);
			}
		}

		protected override void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			// Special case, only audio-visuals are played at the time the Layer changes from normal to Subterranean
			if (newLayer == ValidLayerType && oldLayer != ValidLayerType)
				PlayTransitionAudioVisuals(self, self.Location);
		}
	}
}
