local Soviets = Player.GetPlayer("Soviets")
local France = Player.GetPlayer("France")
local Greece = Player.GetPlayer("Greece")
local Ukraine = Player.GetPlayer("Ukraine")

local GroundMineCells = { }
local WaterMineCells = { }
local MineOrderInterval = 125

local GroundSeekerOrigins =
{
	{ cell = NorthRoadEntry.Location, facing = Angle.South },
	{ cell = EastRoadEntry.Location, facing = Angle.West }
}
local WaterSeekerOrigins =
{
	{ cell = WestWaterEntry.Location, facing = Angle.East },
	{ cell = SouthWaterEntry.Location, facing = Angle.North }
}

---@param northWest cpos
---@param southEast cpos
local function CellsBetween(northWest, southEast)
	local cells = { }
	local i = 0

	for x = northWest.X, southEast.X do
		for y = northWest.Y, southEast.Y do
			i = i + 1
			cells[i] = CPos.New(x, y)
		end
	end

	return cells
end

---@param owner player
---@param inWater boolean
---@return actor|nil
local function GetRandomMine(owner, inWater)
	local mines = owner.GetActorsByType("minv")
	if #mines == 0 then
		return nil
	end

	if inWater then
		mines = Utils.Where(mines, function(mine)
			return Map.TerrainType(mine.Location) == "Water"
		end)
	else
		mines = Utils.Where(mines, function(mine)
			return Map.TerrainType(mine.Location) ~= "Water"
		end)
	end

	if #mines == 0 then
		return nil
	end

	return Utils.Random(mines)
end

--- Spawn a unit that seeks to explode itself on a mine.
---@param target actor
---@param inWater boolean
local function SendSeeker(target, inWater)
	local targetCell = target.Location
	local seekerType = "ftrk"
	local origin

	if inWater then
		origin = Utils.Random(WaterSeekerOrigins)
		seekerType = "lst"
	end

	origin = origin or Utils.Random(GroundSeekerOrigins)
	local seeker = Actor.Create(seekerType, true, { Owner = Soviets, Location = origin.cell, Facing = origin.facing })

	if seeker.HasProperty("Stance") then
		seeker.Stance = "HoldFire"
	end

	Trigger.OnIdle(seeker, function()
		if not target.IsDead and seeker.Location ~= targetCell then
			seeker.Move(targetCell)
			return
		end

		local newMine = GetRandomMine(target.Owner, inWater)
		if newMine then
			target = newMine
			targetCell = newMine.Location
			return
		end

		if seeker.Location ~= origin.cell then
			seeker.Move(origin.cell)
			return
		end

		seeker.Destroy()
	end)
end

--- Mark the target cells of a LayMines call. They may be offset by the range
--- value or discarded by the activity because of invalid/occupied terrain.
---@param owner player
---@param cells cpos[]
---@param duration integer
local function AddMineBeacons(owner, cells, duration)
	Utils.Do(cells, function(cell)
		Beacon.New(owner, Map.CenterOfCell(cell), duration)
	end)
end

---@param actor actor
---@param field cpos[]
---@param beaconTime? integer
---@param start? cpos
local function PrepareBotMinelayer(actor, field, beaconTime, start)
	start = start or actor.Location

	Trigger.OnIdle(actor, function()
		local cells = Utils.Take(5, Utils.Shuffle(field))
		actor.LayMines(cells, 2)
		actor.Move(start, 2)
		actor.Wait(MineOrderInterval)

		if beaconTime and beaconTime > 0 then
			AddMineBeacons(actor.Owner, cells, beaconTime)
		end
	end)
end

WorldLoaded = function()
	GroundMineCells = CellsBetween(EastFieldTop.Location, EastFieldBottom.Location)
	WaterMineCells = CellsBetween(WaterFieldTop.Location, WaterFieldBottom.Location)

	Utils.Do(Ukraine.GetActorsByType("mnly"), function(a)
		PrepareBotMinelayer(a, GroundMineCells, DateTime.Seconds(20))
	end)

	Utils.Do(Ukraine.GetActorsByType("lst"), function(transport)
		Trigger.OnPassengerExited(transport, function(_, passenger)
			PrepareBotMinelayer(passenger, GroundMineCells, DateTime.Seconds(20), NearDepot.Location)
		end)

		Trigger.OnIdle(transport, function()
			if transport.HasPassengers then
				transport.UnloadPassengers()
				return
			end

			if transport.Location ~= SouthWaterEntry.Location then
				transport.Move(SouthWaterEntry.Location)
				return
			end

			transport.Destroy()
		end)
	end)

	Utils.Do(France.GetActorsByType("pt"), function(a)
		PrepareBotMinelayer(a, WaterMineCells, DateTime.Seconds(10))
	end)

	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(CPos.New(16, 16)), WDist.FromCells(20), function(a)
		if a.Type ~= "minv" then
			return
		end

		SendSeeker(a, Map.TerrainType(a.Location) == "Water")
	end)
end

Tick = function()
	Greece.Cash = 1000
end
