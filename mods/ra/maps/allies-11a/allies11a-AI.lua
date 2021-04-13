--[[
   Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Blk1Units = 
{
	hard = { "3tnk", "3tnk", "3tnk" },
	normal = { "3tnk", "3tnk" },
	easy = { "3tnk" }
}

Blk2Units = 
{
	hard = { "4tnk", "4tnk", "v2rl", "v2rl" },
	normal = { "4tnk", "v2rl", "v2rl" },
	easy = { "4tnk", "v2rl" }
}

Blk3Units = 
{
	hard = { "4tnk", "4tnk", "v2rl", "v2rl" },
	normal = { "4tnk", "v2rl", "v2rl" },
	easy = { "4tnk", "v2rl" }
}

Blk4Units = 
{
	hard = { "4tnk", "4tnk", "v2rl", "v2rl" },
	normal = { "4tnk", "v2rl", "v2rl" },
	easy = { "4tnk", "v2rl" }
}

Blk5Units = 
{
	hard = { "e4", "e4", "e4", "e4", "e4", "e4" },
	normal = { "e4", "e4", "e4", "e4", "e4" },
	easy = { "e4", "e4", "e4", "e4" }
}

Periodic1Units = 
{
	hard = { "4tnk", "4tnk" },
	normal = { "4tnk" },
	easy = { "3tnk" }
}

Periodic2Units = 
{
	hard = { "e4", "e4", "e4", "e4", "e1", "e1", "e1", "e2", "e2"  },
	normal = { "e4", "e4", "e4", "e1", "e1", "e2", "e2" },
	easy = { "e4", "e4", "e1", "e1", "e2" }
}

Periodic3Units = 
{
	hard = { "4tnk", "4tnk", "v2rl", "v2rl" },
	normal = { "4tnk", "v2rl", "v2rl" },
	easy = { "4tnk", "v2rl" }
}

Periodic4Units = 
{
	hard = { "v2rl", "v2rl", "v2rl" },
	normal = { "v2rl", "v2rl" },
	easy = { "v2rl" }
}

BadguyPeriodic1Units = 
{
	hard = { "e2", "e2", "e2", "e4", "e4", "e4" },
	normal = { "e2", "e2", "e2", "e4", "e4" },
	easy = { "e2", "e2", "e2", "e4" }
}

BadguyPeriodic2Units = 
{
	hard = { "e1", "e1", "e2", "e2", "e2", "e2" },
	normal = { "e1", "e1", "e2", "e2", "e2" },
	easy = { "e1", "e1", "e2", "e2" }
}

BadguyPeriodic3Units = 
{
	hard = { "3tnk","3tnk", "v2rl", "v2rl" },
	normal = { "3tnk", "v2rl", "v2rl" },
	easy = { "3tnk", "v2rl" }
}

BadguyPeriodic4Units = 
{
	hard = { "3tnk", "3tnk", "3tnk", "3tnk" },
	normal = { "3tnk", "3tnk", "3tnk" },
	easy = { "3tnk", "3tnk" }
}

ProductionInterval =
{
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(40),
	hard = DateTime.Seconds(20)
}

Blk1UnitsGroup = { }
Blk2UnitsGroup = { }
Blk3UnitsGroup = { }
Blk4UnitsGroup = { }
Blk5UnitsGroup = { }
Periodic1UnitsGroup = { }
Periodic2UnitsGroup = { }
Periodic3UnitsGroup = { }
Periodic4UnitsGroup = { }
BadguyPeriodic1UnitsGroup = { }
BadguyPeriodic2UnitsGroup = { }
BadguyPeriodic3UnitsGroup = { }
BadguyPeriodic4UnitsGroup = { }

AirGroup1 = { "yak" }
AirGroup2 = { "yak", "yak" }
AirGroup3 = { "mig", "mig" }
AirGroup4 = { "mig", "mig", "yak" }
AirGroup5 = { "mig", "mig", "mig", "yak", "yak", "yak", "yak" }

AirGroup1Route = { WP96.Location }
AirGroup2Route = { WP96.Location }
AirGroup3Route = { WP83.Location }
AirGroup4Route = { WP83.Location }
AirGroup5Route = { WP84.Location }

BaseBuildings =
{
	{ type = "afld", pos = CVec.New(-12, 7), cost = 500 },
	{ type = "apwr", pos = CVec.New(3, 1), cost = 500 },
	{ type = "barr", pos = CVec.New(-5, 3), cost = 500 },
	{ type = "apwr", pos = CVec.New(3, 5), cost = 500 },
	{ type = "afld", pos = CVec.New(-12, 9), cost = 500 },
	{ type = "ftur", pos = CVec.New(-4, 13), cost = 600 },
	{ type = "ftur", pos = CVec.New(0, 13), cost = 600 },
	{ type = "tsla", pos = CVec.New(-2, 11), cost = 1200 },
	{ type = "weap", pos = CVec.New(-8, 8), cost = 2000 },
	{ type = "proc", pos = CVec.New(0, 8), cost = 1400 },
	{ type = "sam", pos = CVec.New(-12, 12), cost = 1400 },
	{ type = "sam", pos = CVec.New(4, 12), cost = 700 },
	{ type = "sam", pos = CVec.New(3, 0), cost = 700 },
	{ type = "sam", pos = CVec.New(-12, 4), cost = 700 },
	{ type = "apwr", pos = CVec.New(3, 8), cost = 500 },
	{ type = "tsla", pos = CVec.New(-3, 3), cost = 1200 }
}

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

GetAirstrikeTarget = function()
	local list = Greece.GetGroundAttackers()

	if #list == 0 then
		return
	end
	
	local target = list[DateTime.GameTime % #list + 1].CenterPosition
	return target
end

SendAirstrike = function()
	if AF1.IsDead or AF1.Owner ~= USSR then
		return
	end
	local target = GetAirstrikeTarget()

	if target then
		AF1.TargetAirstrike(target, Angle.SouthWest + Angle.New(16))
		Trigger.AfterDelay(DateTime.Seconds(4), SendAirstrike)
	else
		Trigger.AfterDelay(DateTime.Seconds(4)/4, SendAirstrike)
	end
end

SendRenAirstrike = function(unit, route)
	if (AF1.IsDead or AF1.Owner ~= USSR) and (AF2.IsDead or AF2.Owner ~= USSR) and (AF3.IsDead or AF3.Owner ~= USSR) and (AF4.IsDead or AF4.Owner ~= USSR) then
		return
	end
	local attackers = Reinforcements.Reinforce(USSR, unit, route)
	for i = 1, #attackers do
		InitializeAttackAircraft(attackers[i], Greece)
	end
end

SendBlk1AttackGroup = function()
	if #Blk1UnitsGroup < #Blk1Units then
		return
	end
	for i = 1, #Blk1UnitsGroup do
		if not Blk1UnitsGroup[i].IsDead then
			Blk1UnitsGroup[i].AttackMove(WP2.Location)
		end
	end
end

SendBlk2AttackGroup = function()
	if #Blk2UnitsGroup < #Blk2Units then
		return
	end
	for i = 1, #Blk2UnitsGroup do
		if not Blk2UnitsGroup[i].IsDead then
			Blk2UnitsGroup[i].AttackMove(WP3.Location)
		end
	end
end

SendBlk3AttackGroup = function()
	if #Blk3UnitsGroup < #Blk3Units then
		return
	end
	for i = 1, #Blk3UnitsGroup do
		if not Blk3UnitsGroup[i].IsDead then
			Blk3UnitsGroup[i].AttackMove(WP4.Location)
		end
	end
end

SendBlk4AttackGroup = function()
	if #Blk4UnitsGroup < #Blk4Units then
		return
	end
	for i = 1, #Blk4UnitsGroup do
		if not Blk4UnitsGroup[i].IsDead then
			Blk4UnitsGroup[i].AttackMove(WP5.Location)
		end
	end
end

SendBlk5AttackGroup = function()
	if #Blk5UnitsGroup < #Blk5Units then
		return
	end
	Utils.Do(Blk5UnitsGroup, IdleHunt)
end

SendPeriodic1UnitsAttackGroup = function()
	if #Periodic1UnitsGroup < #Periodic1Units then
		return
	end
	Utils.Do(Periodic1UnitsGroup, IdleHunt)
	Periodic1UnitsGroup = { }
end

SendPeriodic2UnitsAttackGroup = function()
	if #Periodic2UnitsGroup < #Periodic2Units then
		return
	end
	Utils.Do(Periodic2UnitsGroup, IdleHunt)
	Periodic2UnitsGroup = { }
end

SendPeriodic3UnitsAttackGroup = function()
	if #Periodic3UnitsGroup < #Periodic3Units then
		return
	end
	Utils.Do(Periodic3UnitsGroup, IdleHunt)
	Periodic3UnitsGroup = { }
end

SendPeriodic4UnitsAttackGroup = function()
	if #Periodic4UnitsGroup < #Periodic4Units then
		return
	end
	Utils.Do(Periodic4UnitsGroup, IdleHunt)
	Periodic4UnitsGroup = { }
end

SendBadGuyPeriodic1UnitsAttackGroup = function()
	if #BadguyPeriodic1UnitsGroup < #BadguyPeriodic1Units then
		return
	end
	Utils.Do(BadguyPeriodic1UnitsGroup, IdleHunt)
	BadguyPeriodic1UnitsGroup = { }
end

SendBadGuyPeriodic2UnitsAttackGroup = function()
	if #BadguyPeriodic2UnitsGroup < #BadguyPeriodic2Units then
		return
	end
	Utils.Do(BadguyPeriodic2UnitsGroup, IdleHunt)
	BadguyPeriodic2UnitsGroup = { }
end

SendBadGuyPeriodic3UnitsAttackGroup = function()
	if #BadguyPeriodic3UnitsGroup < #BadguyPeriodic3Units then
		return
	end
	Utils.Do(BadguyPeriodic3UnitsGroup, IdleHunt)
	BadguyPeriodic3UnitsGroup = { }
end

SendBadGuyPeriodic4UnitsAttackGroup = function()
	if #BadguyPeriodic4UnitsGroup < #BadguyPeriodic4Units then
		return
	end
	Utils.Do(BadguyPeriodic4UnitsGroup, IdleHunt)
	BadguyPeriodic4UnitsGroup = { }
end

ProduceSovietBlk1Vehicle = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		return
	end
	USSR.Build({ Blk1Units[#Blk1UnitsGroup+1] }, function(units)
		table.insert(Blk1UnitsGroup, units[1])
		SendBlk1AttackGroup()
		if #Blk1UnitsGroup < #Blk1Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk1Vehicle)
		else
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk2Vehicle)
		end
	end)
end

ProduceSovietBlk2Vehicle = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		return
	end
	USSR.Build({ Blk2Units[#Blk2UnitsGroup+1] }, function(units)
		table.insert(Blk2UnitsGroup, units[1])
		SendBlk2AttackGroup()
		if #Blk2UnitsGroup < #Blk2Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk2Vehicle)
		else
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk3Vehicle)
		end
	end)
end

ProduceSovietBlk3Vehicle = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		return
	end
	USSR.Build({ Blk3Units[#Blk3UnitsGroup+1] }, function(units)
		table.insert(Blk3UnitsGroup, units[1])
		SendBlk3AttackGroup()
		if #Blk3UnitsGroup < #Blk3Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk3Vehicle)
		else
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk4Vehicle)
		end
	end)
end

ProduceSovietBlk4Vehicle = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		return
	end
	USSR.Build({ Blk2Units[#Blk4UnitsGroup+1] }, function(units)
		table.insert(Blk4UnitsGroup, units[1])
		SendBlk4AttackGroup()
		if #Blk4UnitsGroup < #Blk2Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk4Vehicle)
		else
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk5Infantry)
		end
	end)
end

ProduceSovietBlk5Infantry = function()
	if USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		return
	end
	USSR.Build({ Blk5Units[#Blk5UnitsGroup+1] }, function(units)
		table.insert(Blk5UnitsGroup, units[1])
		SendBlk5AttackGroup()
		if #Blk5UnitsGroup < #Blk5Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietBlk5Infantry)
		else
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic1Units)
		end
	end)
end

ProduceSovietPeriodic1Units = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		ProduceSovietPeriodic2Units()
	elseif USSRWar.IsDead or USSRWar.Owner ~= USSR and USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		return
	end
	USSR.Build({ Periodic1Units[#Periodic1UnitsGroup+1] }, function(units)
		table.insert(Periodic1UnitsGroup, units[1])
		if #Periodic1UnitsGroup < #Periodic1Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic1Units)
		else
			SendPeriodic1UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic2Units)
		end
	end)
end

ProduceSovietPeriodic2Units = function()
	if USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		ProduceSovietPeriodic3Units()
	elseif USSRWar.IsDead or USSRWar.Owner ~= USSR and USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		return
	end
	USSR.Build({ Periodic2Units[#Periodic2UnitsGroup+1] }, function(units)
		table.insert(Periodic2UnitsGroup, units[1])
		if #Periodic2UnitsGroup < #Periodic2Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic2Units)
		else
			SendPeriodic2UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic3Units)
		end
	end)
end

ProduceSovietPeriodic3Units = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		ProduceSovietPeriodic2Units()
	elseif USSRWar.IsDead or USSRWar.Owner ~= USSR and USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		return
	end
	USSR.Build({ Periodic3Units[#Periodic3UnitsGroup+1] }, function(units)
		table.insert(Periodic3UnitsGroup, units[1])
		if #Periodic3UnitsGroup < #Periodic3Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic3Units)
		else
			SendPeriodic3UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic4Units)
		end
	end)
end

ProduceSovietPeriodic4Units = function()
	if USSRWar.IsDead or USSRWar.Owner ~= USSR then
		ProduceSovietPeriodic2Units()
	elseif USSRWar.IsDead or USSRWar.Owner ~= USSR and USSRBarr.IsDead or USSRBarr.Owner ~= USSR then
		return
	end
	USSR.Build({ Periodic4Units[#Periodic4UnitsGroup+1] }, function(units)
		table.insert(Periodic4UnitsGroup, units[1])
		if #Periodic4UnitsGroup < #Periodic4Units then
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic4Units)
		else
			SendPeriodic4UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceSovietPeriodic1Units)
		end
	end)
end

ProduceBadGuyPeriodic1Units = function()
	BadGuy.Build({ BadguyPeriodic1Units[#BadguyPeriodic1UnitsGroup+1] }, function(units)
		table.insert(BadguyPeriodic1UnitsGroup, units[1])
		if #BadguyPeriodic1UnitsGroup < #BadguyPeriodic1Units then
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic1Units)
		else
			SendBadGuyPeriodic1UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic2Units)
		end
	end)
end

ProduceBadGuyPeriodic2Units = function()
	BadGuy.Build({ BadguyPeriodic2Units[#BadguyPeriodic2UnitsGroup+1] }, function(units)
		table.insert(BadguyPeriodic2UnitsGroup, units[1])
		if #BadguyPeriodic2UnitsGroup < #BadguyPeriodic2Units then
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic2Units)
		else
			SendBadGuyPeriodic2UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic1Units)
		end
	end)
end

ProduceBadGuyPeriodic3Units = function()
	BadGuy.Build({ BadguyPeriodic3Units[#BadguyPeriodic3UnitsGroup+1] }, function(units)
		table.insert(BadguyPeriodic3UnitsGroup, units[1])
		if #BadguyPeriodic3UnitsGroup < #BadguyPeriodic3Units then
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic3Units)
		else
			SendBadGuyPeriodic3UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic4Units)
		end
	end)
end

ProduceBadGuyPeriodic4Units = function()
	BadGuy.Build({ BadguyPeriodic4Units[#BadguyPeriodic4UnitsGroup+1] }, function(units)
		table.insert(BadguyPeriodic4UnitsGroup, units[1])
		if #BadguyPeriodic4UnitsGroup < #BadguyPeriodic4Units then
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic4Units)
		else
			SendBadGuyPeriodic4UnitsAttackGroup()
			Trigger.AfterDelay(ProductionInterval, ProduceBadGuyPeriodic3Units)
		end
	end)
end


BuildBase = function()
	if BadGuyCYard.IsDead or BadGuyCYard.Owner ~= BadGuy then
		return
	end
	for i,v in ipairs(BaseBuildings) do
		if not v.exists then
			BuildBuilding(v)
			return
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(5), BuildBase)
end

BuildBuilding = function(building)
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		if BadGuyCYard.IsDead or BadGuyCYard.Owner ~= BadGuy then
			return
		end
		local actor = Actor.Create(building.type, true, { Owner = BadGuy, Location = BadGuyCYard.Location + building.pos })
		BadGuy.Cash = BadGuy.Cash - building.cost

		building.exists = true
		if building.type == "barr" then
			Trigger.AfterDelay(DateTime.Seconds(40), ProduceBadGuyPeriodic1Units)
		elseif building.type == "weap" then
			Trigger.AfterDelay(DateTime.Seconds(120), ProduceBadGuyPeriodic3Units)
		end
		Trigger.OnKilled(actor, function() 
			building.exists = false 			
		end)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == BadGuy and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
		Trigger.AfterDelay(DateTime.Seconds(5), BuildBase)
	end)
end

ActivateAI = function()
	local difficulty = Map.LobbyOption("difficulty")
	ProductionInterval = ProductionInterval[difficulty]
	Blk1Units = Blk1Units[difficulty]
	Blk2Units = Blk2Units[difficulty]
	Blk3Units = Blk3Units[difficulty]
	Blk4Units = Blk4Units[difficulty]
	Blk5Units = Blk5Units[difficulty]
	Periodic1Units = Periodic1Units[difficulty]
	Periodic2Units = Periodic2Units[difficulty]
	Periodic3Units = Periodic3Units[difficulty]
	Periodic4Units = Periodic4Units[difficulty]
	BadguyPeriodic1Units = BadguyPeriodic1Units[difficulty]
	BadguyPeriodic2Units = BadguyPeriodic2Units[difficulty]
	BadguyPeriodic3Units = BadguyPeriodic3Units[difficulty]
	BadguyPeriodic4Units = BadguyPeriodic4Units[difficulty]
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(5), ProduceSovietBlk1Vehicle)
	Trigger.AfterDelay(DateTime.Seconds(126), function()
		SendRenAirstrike(AirGroup1, AirGroup1Route)
	end)
	Trigger.AfterDelay(DateTime.Seconds(172), function()
		SendRenAirstrike(AirGroup2, AirGroup2Route)
	end)
	Trigger.AfterDelay(DateTime.Seconds(260), function()
		SendRenAirstrike(AirGroup3, AirGroup3Route)
	end)
	Trigger.AfterDelay(DateTime.Seconds(316), function()
		SendRenAirstrike(AirGroup4, AirGroup4Route)
	end)
	Trigger.AfterDelay(DateTime.Seconds(432), function()
		SendRenAirstrike(AirGroup5, AirGroup5Route)
	end)
	Trigger.AfterDelay(DateTime.Seconds(432), SendAirstrike)
	Trigger.AfterDelay(DateTime.Minutes(27), BuildBase)
end
