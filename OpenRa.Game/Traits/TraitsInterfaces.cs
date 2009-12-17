using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	enum DamageState { Normal, Half, Dead };

	interface ITick { void Tick(Actor self); }
	interface IRender { IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self); }
	interface INotifyDamage { void Damaged(Actor self, DamageState ds); }
	interface INotifyDamageEx { void Damaged(Actor self, int damage, WarheadInfo warhead); }
	interface INotifyBuildComplete { void BuildingComplete (Actor self); }
	interface IOrder
	{
		Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor );
		void ResolveOrder( Actor self, Order order );
	}
	interface IProducer { bool Produce( Actor self, UnitInfo producee ); }
	interface IOccupySpace { IEnumerable<int2> OccupiedCells(); }
	interface INotifyAttack { void Attacking(Actor self); }
	interface IRenderModifier { IEnumerable<Tuple<Sprite, float2, int>> 
		ModifyRender( Actor self, IEnumerable<Tuple<Sprite, float2, int>> r ); }
	interface IDamageModifier { float GetDamageModifier(); }
	interface ISpeedModifier { float GetSpeedModifier(); }
}
