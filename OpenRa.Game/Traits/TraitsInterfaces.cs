using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using System.Drawing;

namespace OpenRa.Game.Traits
{
	enum DamageState { Normal, Half, Dead };
	
	// depends on the order of pips in WorldRenderer.cs!
	enum PipType { Transparent, Green, Yellow, Red, Gray };
	enum TagType { None, Fake, Primary };
	
	interface ITick { void Tick(Actor self); }
	interface IRender { IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self); }
	interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
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
	interface IPips { IEnumerable<PipType> GetPips(); }
	interface ITags { IEnumerable<TagType> GetTags(); }
	interface IMovement
	{
		UnitMovementType GetMovementType();
		bool CanEnterCell(int2 location);
	}
}
