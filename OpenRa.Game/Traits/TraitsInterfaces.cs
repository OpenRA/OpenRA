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
	interface IRender { IEnumerable<Pair<Sprite, float2>> Render(Actor self); }
	interface IOrder { Order Order(Actor self, int2 xy, bool lmb, Actor underCursor); }
	interface INotifyDamage { void Damaged(Actor self, DamageState ds); }
	interface INotifyDamageEx : INotifyDamage { void Damaged(Actor self, int damage); }
	interface INotifyBuildComplete { void BuildingComplete (Actor self); }
}
