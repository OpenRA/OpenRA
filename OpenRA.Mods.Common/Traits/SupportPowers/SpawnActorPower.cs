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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time.")]
	public class SpawnActorPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn.")]
		public readonly string Actor = null;

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		[Desc("Only allow this to be spawned on this terrain.")]
		public readonly string[] Terrain = null;

		public readonly bool AllowUnderShroud = true;

		public readonly string DeploySound = null;

		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage))]
		public readonly string EffectSequence = null;

		[PaletteReference]
		public readonly string EffectPalette = null;

		[Desc("Cursor to display when the location is unsuitable.")]
		public readonly string BlockedCursor = "move-blocked";

		public override object Create(ActorInitializer init) { return new SpawnActorPower(init.Self, this); }
	}

	public class SpawnActorPower : SupportPower
	{
		public SpawnActorPower(Actor self, SpawnActorPowerInfo info)
			: base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var info = Info as SpawnActorPowerInfo;
			var position = order.Target.CenterPosition;
			var cell = self.World.Map.CellContaining(position);

			if (!Validate(self.World, info, cell))
				return;

			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();
				Game.Sound.Play(SoundType.World, info.DeploySound, position);

				if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
					w.Add(new SpriteEffect(position, w, info.EffectImage, info.EffectSequence, info.EffectPalette));

				var actor = w.CreateActor(info.Actor, new TypeDictionary
				{
					new LocationInit(cell),
					new OwnerInit(self.Owner),
				});

				if (info.LifeTime > -1)
				{
					actor.QueueActivity(new Wait(info.LifeTime));
					actor.QueueActivity(new RemoveSelf());
				}
			});
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(Info.SelectTargetTextNotification, manager.Self.Owner);

			self.World.OrderGenerator = new SelectSpawnActorPowerTarget(order, manager, this, MouseButton.Left);
		}

		public bool Validate(World world, SpawnActorPowerInfo info, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!info.AllowUnderShroud && world.ShroudObscures(cell))
				return false;

			if (info.Terrain != null && !info.Terrain.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			return true;
		}
	}

	public class SelectSpawnActorPowerTarget : OrderGenerator
	{
		readonly SpawnActorPower power;
		readonly SpawnActorPowerInfo info;
		readonly SupportPowerManager manager;
		readonly string order;
		readonly MouseButton expectedButton;

		public string OrderKey { get { return order; } }

		public SelectSpawnActorPowerTarget(string order, SupportPowerManager manager, SpawnActorPower power, MouseButton button)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.manager = manager;
			this.power = power;
			this.order = order;
			expectedButton = button;

			info = (SpawnActorPowerInfo)power.Info;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();

			if (!power.Validate(world, info, cell))
				yield break;

			if (mi.Button == expectedButton)
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }
		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return power.Validate(world, info, cell) ? info.Cursor : info.BlockedCursor;
		}
	}
}
