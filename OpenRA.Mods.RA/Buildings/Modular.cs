#region Copyright & License Information
/*
  * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
  * This file is part of OpenRA, which is free software. It is made
  * available to you under the terms of the GNU General Public License
  * as published by the Free Software Foundation. For more information,
  * see COPYING.
  */
#endregion

using System;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Buildings
{
	public class ModularInfo : ITraitInfo
	{
		public readonly CVec CellOffset = CVec.Zero;
		public readonly string[] Types = { };

		public object Create(ActorInitializer init) { return new Modular(this); }
	}

	public class Modular : INotifySold, INotifyKilled, INotifyCapture
	{
		[Sync] public Actor UpgradeActor;
		public ModularInfo Info;
		public bool IsUpgraded { get { return UpgradeActor != null; } }

		public Modular(ModularInfo info) { Info = info; }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (!IsUpgraded)
				return;

			UpgradeActor.ChangeOwner(newOwner);
		}

		public void Selling(Actor self) { }
		public void Sold(Actor self) { RemoveModule(); }
		public void Killed(Actor self, AttackInfo e) { RemoveModule(); }

		void RemoveModule()
		{
			if (!IsUpgraded)
				return;

			UpgradeActor.Destroy();
			UpgradeActor = null;
		}
	}
}
