using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	enum DamageState { Normal, Half, Dead };

	interface ITick { void Tick(Actor self); }
	interface IRender { IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self); }
	interface INotifyDamage { void Damaged(Actor self, DamageState ds); }
	interface INotifyDamageEx : INotifyDamage { void Damaged(Actor self, int damage); }
	interface INotifyBuildComplete { void BuildingComplete (Actor self); }
	interface IOrder
	{
		Order IssueOrder( Actor self, int2 xy, bool lmb, Actor underCursor );
		void ResolveOrder( Actor self, Order order );
	}
}
