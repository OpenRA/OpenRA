--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EA times are in six-second units. The existing port's difficulty
-- scaling of the ground teams, late chin4 and A10 is retained. Chin3/5/4 are
-- the only free armed reinforcements and each is scheduled exactly once.
-- Unlike the INI, chin3/5 unload; all three land near southern WP4/WP5 and
-- march to Nod, rather than remaining idle at the original northern waypoints.
-- The northern bases also produce paid units, launch local attacks, and fund
-- occasional airlifts. This is new behaviour, not TD's autocreate manager.
-- The northern offensive and MSAM production only begin after Nod has captured
-- its fourth northern building; the western Ion Cannon starts 60 seconds
-- after the first airstrike. A north base that loses its last harvester or is
-- attacked locally launches an all-out counterattack with its remaining units.

local difficulty
local land = {}
local bases
local assigned = {}
local guarded = {}
local defenseResponses = {}
local autoStarted = false
local atk4Triggered = false
local grd1Triggered = false
local airstrikeStopped = false
local airliftBaseIndex = 1
local armorEscalated = false
local NorthernAttacks
local IonCannonStrike
local AirstrikeFirstDelay = { easy = 780, normal = 600, hard = 480 }
local AirstrikeInterval = { easy = 900, normal = 720, hard = 600 }
local IonCannonInterval = { easy = 360, normal = 300, hard = 240 }

local passable = { Clear = true, Rough = true, Road = true, Bridge = true,
	Tiberium = true, BlueTiberium = true, Beach = true }

local function IsLive(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

local function CellKey(x, y)
	return y * 64 + x
end

-- This excludes water, not walls or dynamic pathfinding obstacles.
local function BuildLandRegions()
	local nextRegion = 0
	for y = 2, 61 do
		for x = 2, 61 do
			local key = CellKey(x, y)
			if land[key] == nil then
				if not passable[Map.TerrainType(CPos.New(x, y))] then
					land[key] = 0
				else
					nextRegion = nextRegion + 1
					land[key] = nextRegion
					local queue, index = { { x, y } }, 1
					while index <= #queue do
						local cell = queue[index]
						index = index + 1
					for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 },
						{ 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }) do
							local nx, ny = cell[1] + d[1], cell[2] + d[2]
							local nk = CellKey(nx, ny)
							if nx >= 2 and nx <= 61 and ny >= 2 and ny <= 61 and land[nk] == nil then
								if passable[Map.TerrainType(CPos.New(nx, ny))] then
									land[nk] = nextRegion
									table.insert(queue, { nx, ny })
								else
									land[nk] = 0
								end
							end
						end
					end
				end
			end
		end
	end
end

local function Region(cell)
	if cell.X < 2 or cell.X > 61 or cell.Y < 2 or cell.Y > 61 then return 0 end
	return land[CellKey(cell.X, cell.Y)] or 0
end

local function SameLand(a, b)
	local region = Region(a)
	return region ~= 0 and region == Region(b)
end

local function IsNorthernRegion(region)
	return region ~= 0 and region ~= Region(CPos.New(5, 43))
		and (region == Region(waypoint8.Location) or region == Region(waypoint10.Location)
			or region == Region(DefaultFlareLocation.Location))
end

local function ActorRegion(actor)
	local center = CPos.New(math.floor(actor.CenterPosition.X / 1024), math.floor(actor.CenterPosition.Y / 1024))
	local region = Region(center)
	if region == 0 then region = Region(actor.Location) end
	return region
end

local function IsNorthernBuilding(actor)
	if not IsLive(actor) or actor.Owner ~= Nod or not actor.IsInWorld
		or not actor.HasProperty("StartBuildingRepairs") then return false end
	return IsNorthernRegion(ActorRegion(actor))
end

CheckArmorEscalation = function()
	if MissionFinished or armorEscalated then return end
	local count = 0
	for _, actor in ipairs(Nod.GetActors()) do
		if IsNorthernBuilding(actor) then
			count = count + 1
			if count >= 4 then
				armorEscalated = true
				if not autoStarted then
					autoStarted = true
					NorthernAttacks()
				end
				return
			end
		end
	end
end

local function IsTerrainObstacle(actor)
	return actor.Type == "brik" or actor.Type == "cycl" or actor.Type:match("^rock%d") ~= nil
		or actor.Type:match("^t%d%d") ~= nil or actor.Type:match("^split") ~= nil
end

local function GDIFunds(player)
	return player.Cash + player.Resources
end

local function SpendGDIFunds(player, amount)
	local resources = math.min(player.Resources, amount)
	player.Resources = player.Resources - resources
	player.Cash = player.Cash - amount + resources
end

local function Owned(player, kind)
	for _, actor in ipairs(player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.Owner == player and actor.IsInWorld then return actor end
	end
	return nil
end

local function IsLocal(base, actor)
	local dx, dy = actor.Location.X - base.yard[2], actor.Location.Y - base.yard[3]
	return dx * dx + dy * dy <= 24 * 24
end

local function LocalBuilding(base, kind)
	for _, actor in ipairs(base.player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.Owner == base.player and actor.IsInWorld and IsLocal(base, actor) then return actor end
	end
	return nil
end

local function LocalTech(base, kind)
	if kind == "proc" or kind == "pyle" then
		return LocalBuilding(base, "nuke") ~= nil or LocalBuilding(base, "nuk2") ~= nil
	elseif kind == "weap" or kind == "hq" or kind == "silo" or kind == "harv" then
		return LocalBuilding(base, "proc") ~= nil
	elseif kind == "gtwr" or kind == "e1" or kind == "e3" then
		return LocalBuilding(base, "pyle") ~= nil
	elseif kind == "atwr" or kind == "nuk2" or kind == "eye" or kind == "e2" or kind == "mtnk" or kind == "msam" then
		return LocalBuilding(base, "hq") ~= nil or LocalBuilding(base, "eye") ~= nil
	elseif kind == "htnk" then
		return LocalBuilding(base, "eye") ~= nil
	end
	return true
end

local function IsAssigned(actor)
	for _, other in ipairs(assigned) do
		if other == actor then return true end
	end
	return false
end

local function BaseForPlayer(player)
	for _, base in ipairs(bases) do
		if base.player == player then return base end
	end
	return nil
end

local function AssaultSlots(base)
	for _, group in ipairs({ base.assault, base.defenders }) do
		for i = #group, 1, -1 do
			local unit = group[i]
			if not IsLive(unit) or unit.Owner ~= base.player then table.remove(group, i) end
		end
	end
	return math.max(0, 4 - #base.assault - #base.defenders)
end

local function IsAssaultUnit(base, unit)
	for _, member in ipairs(base.assault) do
		if member == unit then return true end
	end
	return false
end

local function IsDefender(base, unit)
	for _, member in ipairs(base.defenders) do
		if member == unit then return true end
	end
	return false
end

local function ReleaseDefender(base, unit)
	for i = #base.defenders, 1, -1 do
		if base.defenders[i] == unit then
			table.remove(base.defenders, i)
			for j = #assigned, 1, -1 do
				if assigned[j] == unit then table.remove(assigned, j) end
			end
		end
	end
end

local function AddAssault(base, team)
	for _, unit in ipairs(team) do
		if not IsAssaultUnit(base, unit) then table.insert(base.assault, unit) end
	end
end

local function TargetOnLand(origin, target)
	if Region(target.Location) ~= 0 then return SameLand(origin, target.Location) end
	-- A building's origin can itself be unwalkable. Check its neighbouring cells.
	for dx = -1, 3 do
		for dy = -1, 3 do
			if SameLand(origin, CPos.New(target.Location.X + dx, target.Location.Y + dy)) then return true end
		end
	end
	return false
end

local function ClosestEnemy(unit, maxDistance, capture)
	local target, distance
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land")
			and TargetOnLand(unit.Location, actor)
			and (not capture or unit.CanCapture(actor)) then
			local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if (maxDistance == nil or d <= maxDistance * maxDistance) and (distance == nil or d < distance) then
				target, distance = actor, d
			end
		end
	end
	return target
end

local function SouthHasTarget()
	local start = CPos.New(5, 43)
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land") and TargetOnLand(start, actor) then
			return true
		end
	end
	return false
end

local function OrderAttack(unit)
	if unit.HasProperty("AttackMove") then
		local target = ClosestEnemy(unit)
		if target ~= nil then unit.AttackMove(target.Location, 2) end
	elseif unit.HasProperty("Capture") and unit.HasProperty("CanCapture") then
		local target = ClosestEnemy(unit, nil, true)
		if target ~= nil then
			unit.Capture(target)
		elseif unit.HasProperty("Move") then
			local enemy = ClosestEnemy(unit)
			if enemy ~= nil then unit.Move(enemy.Location) end
		end
	end
end

local function WatchAttackers(team, owner)
	if MissionFinished then return end
	local alive = false
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner then
			alive = true
			if unit.IsInWorld and unit.IsIdle then OrderAttack(unit) end
		end
	end
	if alive then Trigger.AfterDelay(DateTime.Seconds(10), function() WatchAttackers(team, owner) end) end
end

local function SendTeam(team, owner, route, attack)
	if MissionFinished then return end
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner and unit.IsInWorld then
			if unit.HasProperty("Stance") then unit.Stance = attack and "AttackAnything" or "Defend" end
			for _, waypoint in ipairs(route) do
				if SameLand(unit.Location, waypoint.Location) then
					if unit.HasProperty("AttackMove") then unit.AttackMove(waypoint.Location)
					elseif unit.HasProperty("Move") then unit.Move(waypoint.Location) end
				end
			end
			if attack and #route == 0 then OrderAttack(unit) end
		end
	end
	if attack and #team > 0 then WatchAttackers(team, owner) end
end

local function Recruit(player, types, anchor, limit)
	local team = {}
	local base = BaseForPlayer(player)
	for _, kind in ipairs(types) do
		if limit ~= nil and #team >= limit then break end
		for _, unit in ipairs(player.GetActorsByType(kind)) do
			if IsLive(unit) and unit.Owner == player and unit.IsInWorld and not IsAssigned(unit)
				and (base == nil or (not IsAssaultUnit(base, unit) and not IsDefender(base, unit)))
				and SameLand(unit.Location, anchor.Location) then
				table.insert(assigned, unit)
				table.insert(team, unit)
				break
			end
		end
	end
	return team
end

local function HasOtherHarvester(player, exclude)
	for _, actor in ipairs(player.GetActorsByType("harv")) do
		if actor ~= exclude and IsLive(actor) and actor.IsInWorld then return true end
	end
	return false
end

local function AllOutAttack(base, target)
	if MissionFinished then return end
	local team = {}
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if IsLive(unit) and unit.IsInWorld and IsNorthernRegion(Region(unit.Location)) then
			table.insert(team, unit)
		end
	end
	if #team == 0 then return end
	base.assault = {}
	base.defenders = {}
	AddAssault(base, team)
	SendTeam(team, base.player, { target or base.rally }, true)
end

local function StartBaseDefense(base, target)
	if MissionFinished or base == nil or base.defending then return end
	base.defending = true
	AllOutAttack(base, target)
end

local function TrackHarvester(base, harv)
	Trigger.OnKilled(harv, function()
		if MissionFinished then return end
		if not HasOtherHarvester(base.player, harv) then StartBaseDefense(base, harv) end
	end)
end

local function DefendAgainst(actor, attacker, damage)
	if MissionFinished or not IsLive(actor) or not actor.IsInWorld or not actor.HasProperty("Location") or damage <= 0 then return end
	local owner = actor.Owner
	if owner ~= GDI and owner ~= GDIWest and owner ~= GDISouth then return end
	if actor.HasProperty("StartBuildingRepairs") and actor.Health < actor.MaxHealth * 0.75 then
		actor.StartBuildingRepairs()
	end
	if not IsLive(attacker) or attacker.Owner ~= Nod or not attacker.IsInWorld
		or not attacker.HasProperty("Location") then return end
	local base = BaseForPlayer(owner)
	if base ~= nil and actor.HasProperty("StartBuildingRepairs") and IsLocal(base, actor) then
		StartBaseDefense(base, attacker)
	end
	for i = #defenseResponses, 1, -1 do
		local response = defenseResponses[i]
		if DateTime.GameTime >= response.expires then
			table.remove(defenseResponses, i)
		elseif response.owner == owner and response.attacker == attacker then
			return
		end
	end
	table.insert(defenseResponses, { owner = owner, attacker = attacker, expires = DateTime.GameTime + DateTime.Seconds(2) })
	local slots = base and AssaultSlots(base) or 0
	for _, unit in ipairs(owner.GetGroundAttackers()) do
		if IsLive(unit) and unit.IsInWorld and unit.HasProperty("AttackMove")
			and SameLand(unit.Location, attacker.Location) then
			local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
			local northernUnit = base ~= nil and IsNorthernRegion(Region(unit.Location))
			local alreadyResponding = northernUnit and (IsAssaultUnit(base, unit) or IsDefender(base, unit))
			if dx * dx + dy * dy <= 144 and (not northernUnit or alreadyResponding or slots > 0) then
				if northernUnit and not alreadyResponding then
					table.insert(base.defenders, unit)
					slots = slots - 1
				end
				if unit.HasProperty("Stop") then unit.Stop() end
				unit.AttackMove(attacker.Location, 2)
			end
		end
	end
end

local function Guard(actor)
	if not IsLive(actor) then return end
	for _, other in ipairs(guarded) do if other == actor then return end end
	table.insert(guarded, actor)
	Trigger.OnDamaged(actor, DefendAgainst)
	if actor.HasProperty("AttackMove") and BaseForPlayer(actor.Owner) ~= nil then
		Trigger.OnIdle(actor, function(unit)
			if IsLive(unit) then
				local base = BaseForPlayer(unit.Owner)
				if base ~= nil then ReleaseDefender(base, unit) end
			end
		end)
	end
	if actor.HasProperty("StartBuildingRepairs") then
		Trigger.OnCapture(actor, function(_, _, _, owner)
			if owner == Nod then CheckArmorEscalation() end
		end)
	end
end

local function SouthDefense()
	if MissionFinished then return end
	for _, unit in ipairs(GDISouth.GetGroundAttackers()) do
		if IsLive(unit) and unit.IsInWorld and unit.IsIdle and unit.HasProperty("AttackMove") then
			local target = ClosestEnemy(unit, 16)
			if target ~= nil then unit.AttackMove(target.Location, 2) end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), SouthDefense)
end

local function StartGrd2()
	if MissionFinished or atk4Triggered then return end
	atk4Triggered = true
	SendTeam(Recruit(GDISouth, { "e2", "e2", "jeep" }, waypoint2), GDISouth, { waypoint2 }, false)
end

local function StartGrd1()
	if MissionFinished or grd1Triggered then return end
	grd1Triggered = true
	local west = BaseForPlayer(GDIWest)
	if west ~= nil then
		local defenders = Recruit(GDIWest, { "e3", "e1", "jeep", "mtnk", "e1", "e2" }, waypoint10, AssaultSlots(west))
		AddAssault(west, defenders)
		SendTeam(defenders, GDIWest, {}, true)
	end
	local team = Recruit(GDI, { "e2", "e2", "e2", "e2" }, waypoint7)
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.HasProperty("Patrol") and SameLand(unit.Location, waypoint6.Location)
			and SameLand(unit.Location, waypoint7.Location) then
			if unit.HasProperty("Stance") then unit.Stance = "Defend" end
			unit.Patrol({ waypoint6.Location, waypoint7.Location }, true, DateTime.Seconds(12))
		end
	end
end

NorthernAttacks = function()
	if MissionFinished then return end
	for _, base in ipairs(bases) do
		local team = Recruit(base.player, { "e1", "e3", "jeep", "htnk", "mtnk", "msam", "e1", "e2" }, base.rally, AssaultSlots(base))
		AddAssault(base, team)
		SendTeam(team, base.player, { base.rally }, true)
	end
	Trigger.AfterDelay(DateTime.Seconds(({ easy = 240, normal = 180, hard = 135 })[difficulty]), NorthernAttacks)
end

local function RandomNodTarget()
	local targets = {}
	for _, actor in ipairs(Nod.GetGroundAttackers()) do
		if IsLive(actor) and actor.IsInWorld then table.insert(targets, actor) end
	end
	if #targets == 0 then
		for _, actor in ipairs(Nod.GetActors()) do
			if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Health")
				and actor.HasProperty("Location") and not actor.HasProperty("Land") then
				table.insert(targets, actor)
			end
		end
	end
	if #targets > 0 then return Utils.Random(targets) end
	return nil
end

local function FindIonCannonTarget(eye)
	local eyeCell = eye.Location
	local targets = {}
	for _, actor in ipairs(Nod.GetGroundAttackers()) do
		if IsLive(actor) and actor.IsInWorld then
			local dx = actor.Location.X - eyeCell.X
			local dy = actor.Location.Y - eyeCell.Y
			-- As in Nod 12, never aim next to the EYE: the blast would damage
			-- the GDI building that keeps the Ion Cannon online.
			if dx * dx + dy * dy > 100 then table.insert(targets, actor) end
		end
	end
	if #targets > 0 then return Utils.Random(targets) end
	return nil
end

local function Airstrike()
	if MissionFinished or airstrikeStopped then return end
	local base = BaseForPlayer(GDI)
	local hq = base ~= nil and LocalBuilding(base, "hq") or nil
	if hq == nil or not hq.HasProperty("TargetAirstrike") then
		Trigger.AfterDelay(DateTime.Seconds(60), Airstrike)
		return
	end
	local target = RandomNodTarget()
	if target ~= nil then hq.TargetAirstrike(target.CenterPosition) end
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeInterval[difficulty]), Airstrike)
end

IonCannonStrike = function()
	if MissionFinished then return end
	local base = BaseForPlayer(GDIWest)
	local eye = base ~= nil and LocalBuilding(base, "eye") or nil
	if eye == nil or not eye.HasProperty("ActivateIonCannon") then
		Trigger.AfterDelay(DateTime.Seconds(30), IonCannonStrike)
		return
	end
	local power = eye.Owner.PowerState
	if power == "Low" or power == "Critical" then
		Trigger.AfterDelay(DateTime.Seconds(60), IonCannonStrike)
		return
	end
	local target = FindIonCannonTarget(eye)
	if target == nil then
		Trigger.AfterDelay(DateTime.Seconds(15), IonCannonStrike)
		return
	end
	eye.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[difficulty]), IonCannonStrike)
end

local function LandingCell(waypoint)
	local start = CPos.New(5, 43)
	for radius = 0, 8 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				if math.abs(dx) == radius or math.abs(dy) == radius then
					local x, y = waypoint.Location.X + dx, waypoint.Location.Y + dy
					if x >= 2 and x <= 61 and y >= 2 and y <= 61 then
						local cell = CPos.New(x, y)
						local terrain = Map.TerrainType(cell)
						if (terrain == "Clear" or terrain == "Road") and SameLand(start, cell) then
							local obstacles = Map.ActorsInCircle(Map.CenterOfCell(cell), WDist.New(768), function(actor)
								return IsLive(actor) and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
							end)
							if #obstacles == 0 then return cell end
						end
					end
				end
			end
		end
	end
	return nil
end

local function SendChinook(cargo, waypoint, owner)
	if MissionFinished then return false end
	owner = owner or not GDI.HasNoRequiredUnits() and GDI or not GDIWest.HasNoRequiredUnits() and GDIWest
		or not GDISouth.HasNoRequiredUnits() and GDISouth
	if not owner then return false end
	local landing = LandingCell(waypoint)
	if landing == nil then return false end
	local entry = CPos.New(61, 2)
	local transport = Reinforcements.ReinforceWithTransport(owner, "tran.in", cargo, { entry, landing }, { entry })
	TrackGDIReinforcements(transport[2])
	if IsLive(transport[1]) then
		Trigger.OnPassengerExited(transport[1], function(_, passenger)
			Trigger.AfterDelay(1, function()
				if IsLive(passenger) and passenger.Owner == owner and passenger.IsInWorld then
					SendTeam({ passenger }, owner, {}, true)
				end
			end)
		end)
	end
	return true
end

-- Original [Base] nodes, split by owner. The south outpost has no rebuild plan.
local eastNodes = {
	{ "proc", 52, 14 }, { "weap", 58, 14 }, { "pyle", 49, 17 },
	{ "gtwr", 52, 22 }, { "gtwr", 55, 22 }, { "gtwr", 54, 10 }, { "gtwr", 58, 10 },
	{ "atwr", 49, 21 }, { "atwr", 58, 21 }, { "atwr", 60, 12 },
	{ "nuk2", 48, 12 }, { "nuk2", 46, 12 }, { "hq", 59, 19 },
	{ "silo", 42, 9 }, { "silo", 47, 20 }, { "silo", 38, 9 }, { "silo", 47, 18 },
	{ "silo", 45, 18 }, { "silo", 45, 20 }, { "silo", 40, 9 },
}
local westNodes = {
	{ "proc", 10, 12 }, { "weap", 6, 11 }, { "pyle", 7, 15 },
	{ "gtwr", 18, 15 }, { "gtwr", 20, 13 },
	{ "atwr", 3, 8 }, { "atwr", 15, 15 }, { "atwr", 17, 13 },
	{ "nuk2", 3, 16 }, { "nuk2", 3, 13 }, { "nuk2", 5, 16 }, { "nuk2", 7, 8 },
	{ "eye", 3, 10 },
}
local footprint = { fact = { 3, 3 }, proc = { 3, 4 }, weap = { 3, 3 },
	pyle = { 2, 3 }, nuk2 = { 2, 3 }, atwr = { 1, 1 }, gtwr = { 1, 1 },
	silo = { 2, 1 }, hq = { 2, 3 }, eye = { 2, 3 } }
local emptyFootprintCells = { proc = { [0] = true, [2] = true }, hq = { [1] = true }, eye = { [1] = true } }

local function OccupiesFootprint(kind, x, y)
	local empty = emptyFootprintCells[kind]
	return empty == nil or not empty[y * 4 + x]
end

local function BuildingAt(entry)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if IsLive(actor) and actor.Type == entry[1] and actor.HasProperty("Location")
			and actor.Location.X == entry[2] and actor.Location.Y == entry[3] then return actor end
	end
	return nil
end

local function SiteFree(entry, ignore, owner)
	local size = footprint[entry[1]]
	for x = entry[2], entry[2] + size[1] - 1 do
		for y = entry[3], entry[3] + size[2] - 1 do
			local terrain = Map.TerrainType(CPos.New(x, y))
			if OccupiesFootprint(entry[1], x - entry[2], y - entry[3])
				and terrain ~= "Clear" and terrain ~= "Road" then return false end
		end
	end
	local topLeft = WPos.New((entry[2] - 4) * 1024, (entry[3] - 4) * 1024, 0)
	local bottomRight = WPos.New((entry[2] + size[1] + 4) * 1024, (entry[3] + size[2] + 4) * 1024, 0)
	local blockers = Map.ActorsInBox(topLeft, bottomRight, function(actor)
		return IsLive(actor) and actor ~= ignore and actor.HasProperty("Location")
			and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
	end)
	for _, actor in ipairs(blockers) do
		local otherSize = footprint[actor.Type] or (actor.Type:match("^rock%d") and { 4, 2 })
			or (actor.Type:match("^t%d%d") and { 2, 2 })
			or (actor.HasProperty("StartBuildingRepairs") and { 3, 3 } or { 1, 1 })
		local x, y = actor.Location.X, actor.Location.Y
		for cx = math.max(x, entry[2]), math.min(x + otherSize[1], entry[2] + size[1]) - 1 do
			for cy = math.max(y, entry[3]), math.min(y + otherSize[2], entry[3] + size[2]) - 1 do
				if OccupiesFootprint(entry[1], cx - entry[2], cy - entry[3])
					and OccupiesFootprint(actor.Type, cx - x, cy - y) then
					if actor.Owner == owner and actor.HasProperty("Move") and not actor.HasProperty("Land") then
						actor.Move(CPos.New(entry[2], entry[3] + size[2] + 1))
					end
					return false
				end
			end
		end
	end
	return true
end

local function FindNearbySite(base, kind, anchor, ignore, reserved)
	local size = footprint[kind]
	for radius = 0, 8 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				local x, y = base.yard[2] + dx, base.yard[3] + dy
				local reservedSize = reserved and footprint[reserved[1]]
				local clearReserved = reserved == nil or x + size[1] <= reserved[2] or x >= reserved[2] + reservedSize[1]
					or y + size[2] <= reserved[3] or y >= reserved[3] + reservedSize[2]
				if (math.abs(dx) == radius or math.abs(dy) == radius) and clearReserved
					and x >= 2 and x + size[1] - 1 <= 61 and y >= 2 and y + size[2] - 1 <= 61 then
					local candidate = { kind, x, y }
					if SameLand(anchor, CPos.New(x + 1, y + 1)) and SiteFree(candidate, ignore, nil) then
						return candidate
					end
				end
			end
		end
	end
	return nil
end

local function WatchMCV(base, mcv, tries)
	if MissionFinished then return end
	if base.mcv ~= mcv then return end
	if not IsLive(mcv) or mcv.Owner ~= base.player then base.mcv = nil return end
	if LocalBuilding(base, "fact") ~= nil then base.mcv = nil return end
	if mcv.IsInWorld then
		local entry = base.mcvSite
		if entry == nil or not SiteFree(entry, mcv, nil) then
			entry = FindNearbySite(base, "fact", mcv.Location, mcv)
			base.mcvSite = entry
		end
		if entry ~= nil then
			local x, y = entry[2] + 1, entry[3] + 1
			if mcv.Location.X == x and mcv.Location.Y == y then
				if mcv.HasProperty("Deploy") then mcv.Deploy() end
			elseif tries % 5 == 0 and mcv.HasProperty("Move") then
				mcv.Move(CPos.New(x, y))
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), function() WatchMCV(base, mcv, tries + 1) end)
end

local prerequisites = {
	proc = { "anypower" }, weap = { "proc" }, pyle = { "anypower" },
	gtwr = { "barracks" }, atwr = { "anyhq", "~techlevel.medium" },
	nuk2 = { "anyhq", "~techlevel.medium" }, hq = { "proc", "~techlevel.medium" },
	eye = { "anyhq", "~techlevel.high" }, silo = { "proc" },
}

local function FinishBuilding(base, entry, yard, cost, attempts, emergencyRefinery)
	if MissionFinished then return end
	local player = base.player
	local harvester = Owned(player, "harv")
	if not IsLive(yard) or yard.Owner ~= player or not yard.IsInWorld or BuildingAt(entry) ~= nil
		or (not emergencyRefinery and harvester == nil)
		or (emergencyRefinery and (harvester ~= nil or LocalBuilding(base, "proc") ~= nil))
		or not LocalTech(base, entry[1])
		or not player.HasPrerequisites(prerequisites[entry[1]]) then
		player.Cash = player.Cash + cost
		base.rebuilding = false
		return
	end
	if not SiteFree(entry, nil, player) then
		if attempts < 20 then
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				FinishBuilding(base, entry, yard, cost, attempts + 1, emergencyRefinery)
			end)
		else
			player.Cash = player.Cash + cost
			base.rebuilding = false
		end
		return
	end
	-- A refinery normally includes a harvester in its price. Only the emergency
	-- recovery path needs that actor; ordinary rebuilds must not duplicate it.
	local building = Actor.Create(entry[1], true,
		{ Owner = player, Location = CPos.New(entry[2], entry[3]), FreeActor = emergencyRefinery })
	Guard(building)
	base.rebuilding = false
	if emergencyRefinery then
		Trigger.AfterDelay(1, function()
			for _, unit in ipairs(player.GetActorsByType("harv")) do
				if IsLive(unit) and unit.Owner == player and unit.IsInWorld then
					Guard(unit)
					if unit.HasProperty("FindResources") then unit.FindResources() end
				end
			end
		end)
	end
end

local function StartBuilding(base, entry, yard, emergencyRefinery)
	local player, kind = base.player, entry[1]
	local cost = Actor.Cost(kind)
	base.rebuilding = true
	SpendGDIFunds(player, cost)
	local queue = (kind == "gtwr" or kind == "atwr" or kind == "silo") and "Support.GDI" or "Building.GDI"
	local buildTime = Actor.BuildTime(kind, queue)
	if player.PowerState == "Low" or player.PowerState == "Critical" then
		buildTime = math.ceil(buildTime * 1.5)
	end
	Trigger.AfterDelay(buildTime, function() FinishBuilding(base, entry, yard, cost, 0, emergencyRefinery) end)
end

local function Rebuild(base)
	local player = base.player
	if base.rebuilding then return end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return end

	if Owned(player, "harv") == nil then
		-- A paid refinery is the only way out when both refinery and harvester
		-- are lost; it supplies the same free harvester as normal production.
		if LocalBuilding(base, "proc") == nil and GDIFunds(player) >= Actor.Cost("proc")
			and LocalTech(base, "proc") and player.HasPrerequisites(prerequisites.proc) then
			local entry = base.nodes[1]
			if BuildingAt(entry) ~= nil or not SiteFree(entry, nil, nil) then
				entry = FindNearbySite(base, "proc", yard.Location, nil)
			end
			if entry ~= nil then StartBuilding(base, entry, yard, true) end
		end
		return
	end

	-- A base stuck at low or critical power cannot use radar, support powers,
	-- or its advanced defences. Fund an additional local power plant first.
	local power = player.PowerState
	if (power == "Low" or power == "Critical")
		and GDIFunds(player) >= Actor.Cost("nuk2") + 800
		and LocalTech(base, "nuk2") and player.HasPrerequisites(prerequisites.nuk2) then
		local entry = FindNearbySite(base, "nuk2", yard.Location, nil)
		if entry ~= nil then
			StartBuilding(base, entry, yard, false)
			return
		end
	end

	-- The western EYE was the only local anyhq source after splitting houses.
	-- A paid HQ near the surviving yard restores the normal tech chain.
	if LocalBuilding(base, "hq") == nil and LocalBuilding(base, "eye") == nil
		and GDIFunds(player) >= Actor.Cost("hq") + 1100 and LocalTech(base, "hq")
		and player.HasPrerequisites(prerequisites.hq) then
		local eyeSite
		for _, planned in ipairs(base.nodes) do
			if planned[1] == "eye" then eyeSite = planned break end
		end
		local entry = FindNearbySite(base, "hq", yard.Location, nil, eyeSite)
		if entry ~= nil then
			StartBuilding(base, entry, yard, false)
			return
		end
	end

	-- The eastern base deliberately never builds an EYE: the western base is
	-- the single Ion Cannon source, while the eastern HQ controls airstrikes.

	for _, entry in ipairs(base.nodes) do
		local kind = entry[1]
		local cost = Actor.Cost(kind)
		-- Leave a reserve for combat production and harvester replacement.
		local reserve = (kind == "proc" or kind == "weap" or kind == "pyle" or kind == "gtwr" or kind == "atwr") and 1100 or 2200
		if BuildingAt(entry) == nil and (kind ~= "proc" or LocalBuilding(base, "proc") == nil)
			and (kind ~= "hq" or LocalBuilding(base, "hq") == nil) and GDIFunds(player) >= cost + reserve
			and LocalTech(base, kind) and player.HasPrerequisites(prerequisites[kind]) and SiteFree(entry, nil, player) then
			StartBuilding(base, entry, yard, false)
			return
		end
	end
end

local function Queue(factory, kind, player)
	if not IsLive(factory) or factory.Owner ~= player or not factory.IsInWorld or not factory.HasProperty("Build")
		or not factory.HasProperty("IsProducing") or factory.IsProducing(kind) or GDIFunds(player) < Actor.Cost(kind) then return false end
	-- No per-order callback: a rejected/cancelled order leaves that engine
	-- callback attached and it would consume the next unrelated product.
	return factory.Build({ kind })
end

local infantry = { "e3", "e1", "e1", "e1" }
local westVehicles = { "mtnk", "jeep", "jeep" }
local westArmoredVehicles = { "mtnk", "msam", "jeep", "jeep" }
local eastVehicles = { "jeep", "jeep", "jeep" }
local eastArmoredVehicles = { "htnk", "mtnk", "msam", "jeep" }
local unitPrerequisites = {
	e1 = { "barracks" }, e3 = { "barracks" }, e2 = { "anyhq", "~techlevel.medium" },
	jeep = { "weap" }, mtnk = { "anyhq", "~techlevel.medium" },
	msam = { "anyhq", "~techlevel.medium" }, htnk = { "eye", "~techlevel.high" },
}

-- Off-map air support is paid by the originating base, including the aircraft.
-- A base under local attack retains its money and troops for defence instead.
local function PaidAirlifts()
	if MissionFinished then return end
	local base = bases[airliftBaseIndex]
	airliftBaseIndex = airliftBaseIndex % #bases + 1
	local player = base.player
	local yard = LocalBuilding(base, "fact")
	local cargo = { "e1", "e1", "e3", "e3" }
	local cost = Actor.Cost("tran")
	for _, kind in ipairs(cargo) do cost = cost + Actor.Cost(kind) end
	if yard ~= nil and Owned(player, "harv") ~= nil and LocalBuilding(base, "pyle") ~= nil
		and LocalTech(base, "e3") and player.HasPrerequisites(unitPrerequisites.e3)
		and ClosestEnemy(yard, 16) == nil and SouthHasTarget() and GDIFunds(player) >= cost + 1100 then
		SpendGDIFunds(player, cost)
		if not SendChinook(cargo, waypoint5, player) then
			player.Cash = player.Cash + cost
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(({ easy = 450, normal = 330, hard = 240 })[difficulty]), PaidAirlifts)
end

local function CombatCounts(base)
	local counts, total, infantryCount, vehicleCount = {}, 0, 0, 0
	for _, unit in ipairs(base.player.GetGroundAttackers()) do
		if IsLive(unit) and unit.IsInWorld and IsNorthernRegion(Region(unit.Location)) then
			local kind = unit.Type
			counts[kind] = (counts[kind] or 0) + 1
			total = total + 1
			if kind == "jeep" or kind == "mtnk" or kind == "msam" or kind == "htnk" then
				vehicleCount = vehicleCount + 1
			else
				infantryCount = infantryCount + 1
			end
		end
	end
	return counts, total, infantryCount, vehicleCount
end

local function CombatOrderPending(base)
	local pending = base.combatPending
	if pending == nil then return false end
	local factory = pending.factory
	if not IsLive(factory) or factory.Owner ~= base.player or not factory.IsInWorld
		or not factory.HasProperty("IsProducing") then
		base.combatPending = nil
		return false
	end
	if factory.IsProducing(pending.kind) then
		pending.emptySince = nil
	elseif pending.emptySince == nil then
		-- The queue can empty before its product is added at frame end.
		pending.emptySince = DateTime.GameTime
	elseif DateTime.GameTime > pending.emptySince then
		base.combatPending = nil
		return false
	end
	return true
end

local function Produce(base)
	local player = base.player
	local factory = LocalBuilding(base, "weap")
	local harvester = Owned(player, "harv")
	if harvester == nil then
		if factory ~= nil and LocalTech(base, "harv") and player.HasPrerequisites({ "proc" }) then
			Queue(factory, "harv", player)
		end
		return
	end
	if LocalBuilding(base, "fact") == nil then
		local mcv = LocalBuilding(base, "mcv")
		if base.mcv == nil and mcv ~= nil then
			base.mcv = mcv
			WatchMCV(base, mcv, 0)
		elseif base.mcv == nil and factory ~= nil then
			Queue(factory, "mcv", player)
		end
		return
	end
	if CombatOrderPending(base) then return end
	local pyle = LocalBuilding(base, "pyle")
	if (pyle ~= nil and pyle.HasProperty("IsProducing") and pyle.IsProducing("e1"))
		or (factory ~= nil and factory.HasProperty("IsProducing") and factory.IsProducing("jeep")) then return end

	local counts, total, infantryCount, vehicleCount = CombatCounts(base)
	if total >= 8 then return end
	local vehicles = base.player == GDIWest
		and (armorEscalated and westArmoredVehicles or westVehicles)
		or (armorEscalated and eastArmoredVehicles or eastVehicles)
	local specs = base.nextProduction == "vehicle"
		and { { "weap", vehicles, vehicleCount }, { "pyle", infantry, infantryCount } }
		or { { "pyle", infantry, infantryCount }, { "weap", vehicles, vehicleCount } }
	for _, spec in ipairs(specs) do
		local producer = LocalBuilding(base, spec[1])
		if producer ~= nil and spec[3] < 4 then
			if producer.HasProperty("RallyPoint") then producer.RallyPoint = base.rally.Location end
			local desired = {}
			for _, kind in ipairs(spec[2]) do
				desired[kind] = (desired[kind] or 0) + 1
				if (counts[kind] or 0) < desired[kind] and LocalTech(base, kind)
					and player.HasPrerequisites(unitPrerequisites[kind]) and GDIFunds(player) >= Actor.Cost(kind) + 500
					and Queue(producer, kind, player) and producer.IsProducing(kind) then
					base.combatPending = { factory = producer, kind = kind }
					base.nextProduction = spec[1] == "pyle" and "vehicle" or "infantry"
					return
				end
			end
		end
	end
end

local function OnProduced(factory, unit, productionType)
	if MissionFinished or not IsLive(factory) or not IsLive(unit) then return end
	for _, base in ipairs(bases) do
		if factory.Owner == base.player and unit.Owner == base.player and IsLocal(base, factory) then
			if base.combatPending ~= nil and base.combatPending.factory == factory
				and base.combatPending.kind == unit.Type then base.combatPending = nil end
			if factory.Type == "weap" and productionType == "Vehicle.GDI" then
				if unit.Type == "harv" then
					Guard(unit)
					TrackHarvester(base, unit)
					if unit.HasProperty("FindResources") then unit.FindResources() end
				elseif unit.Type == "mcv" and base.mcv == nil then
					base.mcv = unit
					WatchMCV(base, unit, 0)
				elseif unit.HasProperty("AttackMove") then
					Guard(unit)
				end
			elseif factory.Type == "pyle" and productionType == "Infantry.GDI" and unit.HasProperty("AttackMove") then
				Guard(unit)
			end
			if base.defending and unit.HasProperty("AttackMove") then
				AddAssault(base, { unit })
				SendTeam({ unit }, base.player, { base.rally }, true)
			end
			return
		end
	end
end

local function CheckGDIEconomy()
	if MissionFinished then return end
	CheckArmorEscalation()
	for _, base in ipairs(bases) do
		local harv = Owned(base.player, "harv")
		if harv ~= nil then
			base.hadHarvester = true
		elseif base.hadHarvester then
			base.hadHarvester = false
			StartBaseDefense(base)
		end
		Produce(base)
		Rebuild(base)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), CheckGDIEconomy)
end

SetupAI = function()
	difficulty = Difficulty or "normal"
	-- Split the original house's 4000 credits; the south outpost cannot build.
	GDI.Cash, GDIWest.Cash, GDISouth.Cash = 2000, 2000, 0
	bases = {
		{ player = GDI, rally = waypoint8, yard = { "fact", 50, 12 }, nodes = eastNodes, nextProduction = "infantry", assault = {}, defenders = {} },
		{ player = GDIWest, rally = waypoint10, yard = { "fact", 4, 8 }, nodes = westNodes, nextProduction = "infantry", assault = {}, defenders = {} },
	}
	for _, base in ipairs(bases) do
		base.defending = false
		base.hadHarvester = Owned(base.player, "harv") ~= nil
	end
	BuildLandRegions()
	Trigger.OnBuildingPlaced(Nod, function()
		CheckArmorEscalation()
	end)
	Trigger.OnAnyProduction(OnProduced)
	for _, player in ipairs({ GDI, GDIWest, GDISouth }) do
		for _, actor in ipairs(player.GetActors()) do
			if IsLive(actor) and (actor.HasProperty("StartBuildingRepairs") or actor.Type == "harv"
				or actor.HasProperty("AttackMove")) then
				Guard(actor)
				if actor.Type == "harv" then
					local base = BaseForPlayer(actor.Owner)
					if base ~= nil then TrackHarvester(base, actor) end
				end
			end
		end
	end
	for _, tower in ipairs({ Actor219, Actor220 }) do
		if IsLive(tower) then
			Trigger.OnDamaged(tower, function(_, attacker, damage)
				if damage > 0 and IsLive(attacker) and attacker.Owner == Nod then StartGrd2() end
			end)
		end
	end
	for _, building in ipairs({ Actor229, Actor230 }) do
		if IsLive(building) then
			Trigger.OnCapture(building, function(_, _, oldOwner, newOwner)
				if oldOwner == GDIWest and newOwner == Nod then StartGrd1() end
			end)
		end
	end
	if IsLive(Actor245) then
		Trigger.OnKilled(Actor245, function() airstrikeStopped = true end)
		Trigger.OnCapture(Actor245, function(_, _, oldOwner, newOwner)
			if oldOwner == GDI and newOwner ~= GDI then airstrikeStopped = true end
		end)
	end
	local scale = ({ easy = 1, normal = 0.8, hard = 0.65 })[difficulty]
	Trigger.AfterDelay(DateTime.Seconds(300 * scale), function()
		SendTeam(Recruit(GDISouth, { "e1", "e1", "e2", "e2" }, waypoint2), GDISouth, { waypoint2, waypoint3, waypoint4 }, true)
	end)
	Trigger.AfterDelay(DateTime.Seconds(420 * scale), function()
		SendTeam(Recruit(GDISouth, { "e1", "e1", "jeep" }, waypoint2), GDISouth, { waypoint2, waypoint5 }, true)
	end)
	Trigger.AfterDelay(DateTime.Seconds(660 * scale), function()
		SendTeam(Recruit(GDISouth, { "e2", "e2", "jeep" }, waypoint2), GDISouth, { waypoint2, waypoint3, waypoint4 }, true)
	end)
	Trigger.AfterDelay(DateTime.Seconds(120), function() SendChinook({ "e2", "e2", "e6", "e6", "e6" }, waypoint5) end)
	Trigger.AfterDelay(DateTime.Seconds(180), function() SendChinook({ "e1", "e1", "e3", "e6", "e6" }, waypoint4) end)
	Trigger.AfterDelay(DateTime.Seconds(({ easy = 2700, normal = 1800, hard = 1500 })[difficulty]), function()
		SendChinook({ "e2", "e2", "e6", "e6", "rmbo" }, waypoint5)
	end)
	Trigger.AfterDelay(DateTime.Seconds(({ easy = 450, normal = 330, hard = 240 })[difficulty]), PaidAirlifts)
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeFirstDelay[difficulty]), Airstrike)
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeFirstDelay[difficulty] + 60), IonCannonStrike)
	Media.PlaySpeechNotification(Nod, "IonCannonCharging")
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	CheckGDIEconomy()
	SouthDefense()
end
