--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NForce = { "e1", "e1", "e1", "e3", "cyborg", "cyborg" }
NForcePath = { NodW.Location }
NForceInterval = 5

VNForce = { "bike", "bike", "bggy", "bggy", "e1", "e1", "e3" }
VNForcePath = { South.Location }
VNForceInterval = 15

GForce = { "e1", "e1", "e1", "e1", "e2", "e1", "e2" }
GForcePath = { GDIW.Location }
GForceInterval = 5

VGForce = { "e2", "smech", "smech", "e1", "e1", "apc" }
VGForcePath = { North.Location }
VGForceInterval = 15

ProducedUnitTypes =
{
	{ nodhand1, { "e1", "e3" }, GDIBase.Location },
	{ gdibar1, { "e1", "e2" }, NodBase.Location }
}

ProduceUnits = function(t)
	local factory = t[1]
	if not factory.IsDead then
		local unitType = t[2][Utils.RandomInteger(1, #t[2] + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(pair)
		Trigger.OnProduction(pair[1], function(_, a) BindActorTriggers(a, pair[3]) end)
	end)
end

SetupInvulnerability = function()
   Utils.Do(Map.NamedActors, function(actor)
		if actor.HasProperty("AcceptsCondition") and actor.AcceptsCondition("unkillable") then
			actor.GrantCondition("unkillable")
		end
   end)
end

SendNodInfantry = function()
	local units = Reinforcements.Reinforce(nod, NForce, NForcePath, NForceInterval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit, GDIBase.Location)
	end)
	Trigger.AfterDelay(DateTime.Seconds(60), SendNodInfantry)
end

SendNodVehicles = function()
	local units = Reinforcements.Reinforce(nod, VNForce, VNForcePath, VNForceInterval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit, GDIBase.Location)
	end)
	Trigger.AfterDelay(DateTime.Seconds(110), SendNodVehicles)
end

SendGDIInfantry = function()
	local units = Reinforcements.Reinforce(gdi, GForce, GForcePath, GForceInterval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit, NodBase.Location)
	end)
	Trigger.AfterDelay(DateTime.Seconds(60), SendGDIInfantry)
end

SendGDIVehicles = function()
	local units = Reinforcements.Reinforce(gdi, VGForce, VGForcePath, VGForceInterval)
	Utils.Do(units, function(unit)
		BindActorTriggers(unit, NodBase.Location)
	end)
	Trigger.AfterDelay(DateTime.Seconds(110), SendGDIVehicles)
end

BindActorTriggers = function(a, loc)
	if a.HasProperty("AttackMove") then
		a.AttackMove(loc)
	else
		a.Move(loc)
	end

	if a.HasProperty("Hunt") then
		Trigger.OnIdle(a, a.Hunt)
	else
		Trigger.OnIdle(a, function()
			a.Move(loc)
		end)
	end

	if a.HasProperty("HasPassengers") then
		Trigger.OnDamaged(a, function()
			if a.HasPassengers then
				a.Stop()
				a.UnloadPassengers()
			end
		end)

		Trigger.OnPassengerExited(a, function(_, p)
			BindActorTriggers(p, loc)
		end)
	end
end

WorldLoaded = function()
	nod = Player.GetPlayer("Nod")
	gdi = Player.GetPlayer("GDI")

	SetupFactories()
	SetupInvulnerability()

	Utils.Do(ProducedUnitTypes, ProduceUnits)
	SendNodInfantry()
	Trigger.AfterDelay(DateTime.Seconds(50), SendNodVehicles)
	SendGDIInfantry()
	Trigger.AfterDelay(DateTime.Seconds(70), SendGDIVehicles)
end
