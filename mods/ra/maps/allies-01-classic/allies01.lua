InsertionHelicopterType = "tran.insertion"
ExtractionHelicopterType = "tran.extraction"
JeepReinforcements = { "jeep", "jeep" }
JeepInterval = 50
JeepDelay = 125
TanyaType = "e7"
EinsteinType = "einstein"
FlareType = "flare"
Cruisers = { "ca", "ca", "ca", "ca" }
CruiserDelay = 250
CameraDelay = 125
CivilianWait = 150
BaseAlertDelay = 300

SendInsertionHelicopter = function()
	local heli, passengers = Reinforcements.Insert(player, InsertionHelicopterType, { TanyaType },
		{ InsertionEntry.Location, InsertionLZ.Location }, { InsertionEntry.Location })
	tanya = passengers[1]
	Actor.OnKilled(tanya, TanyaKilled)
end

SendJeeps = function()
	Media.PlaySpeechNotification("ReinforcementsArrived")
	Reinforcements.Reinforce(player, JeepReinforcements, InsertionEntry.Location, InsertionLZ.Location, JeepInterval)
end

RunInitialActivities = function()
	SendInsertionHelicopter()
	Actor.Hunt(Patrol1)
	Actor.Hunt(Patrol2)
	Actor.Hunt(Patrol3)
	Actor.Hunt(Patrol4)
	Actor.Harvest(Harvester)
	Team.Do(civiliansTeam, function(c)
		Actor.Wait(c, CivilianWait)
		Actor.Hunt(c)
	end)
end

LabGuardsKilled = function()
	CreateEinstein()
	
	Actor.Create(FlareType, { Owner = england, Location = ExtractionFlarePoint.Location })
	Media.PlaySpeechNotification("SignalFlareNorth")
	SendExtractionHelicopter()
	
	OpenRA.RunAfterDelay(BaseAlertDelay, function()
		local ussrUnits = Mission.GetGroundAttackersOf(ussr)
		for i, unit in ipairs(ussrUnits) do
			Actor.Hunt(unit)
		end
	end)
	
	OpenRA.RunAfterDelay(CruiserDelay, function()
		Media.PlaySpeechNotification("AlliedReinforcementsArrived")
		Actor.Create("camera", { Owner = player, Location = CruiserCameraPoint.Location })
		SendCruisers()
	end)
end

SendExtractionHelicopter = function()
	local heli = Reinforcements.Extract(player, ExtractionHelicopterType, { einstein },
		{ SouthReinforcementsPoint.Location, ExtractionLZ.Location }, { ExtractionExitPoint.Location })
	Actor.OnKilled(heli, HelicopterDestroyed)
	Actor.OnRemovedFromWorld(heli, HelicopterExtractionCompleted)
end

HelicopterExtractionCompleted = function()
	MissionAccomplished()
end

SendCruisers = function()
	for i, cruiser in ipairs(Cruisers) do
		local ca = Actor.Create(cruiser, { Owner = england, Location = SouthReinforcementsPoint.Location })
		Actor.Move(ca, Map.GetNamedActor("CruiserPoint" .. i).Location)
	end
end

LabDestroyed = function(self, e)
	if not einstein then
		MissionFailed()
	end
end

EinsteinKilled = function(self, e)
	MissionFailed()
end

HelicopterDestroyed = function(self, e)
	MissionFailed()
end

TanyaKilled = function(self, e)
	MissionFailed()
end

OilPumpDestroyed = function(self, e)
	OpenRA.RunAfterDelay(JeepDelay, SendJeeps)
end

CreateEinstein = function()
	einstein = Actor.Create(EinsteinType, { Location = EinsteinSpawnPoint.Location, Owner = player })
	Actor.Scatter(einstein)
	Actor.OnKilled(einstein, EinsteinKilled)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, true)
	--Media.PlayMovieFullscreen("snowbomb.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, true)
	Media.PlayMovieFullscreen("bmap.vqa")
end

SetUnitStances = function()
	local playerUnits = Mission.GetGroundAttackersOf(player)
	local ussrUnits = Mission.GetGroundAttackersOf(ussr)
	for i, unit in ipairs(playerUnits) do
		Actor.SetStance(unit, "Defend")
	end
end

Tick = function()
	Mission.TickTakeOre(ussr)
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Greece")
	england = OpenRA.GetPlayer("England")
	ussr = OpenRA.GetPlayer("USSR")
	
	Actor.OnKilled(Lab, LabDestroyed)
	Actor.OnKilled(OilPump, OilPumpDestroyed)
	
	labGuardsTeam = Team.New({ LabGuard1, LabGuard2, LabGuard3 })
	Team.AddEventHandler(labGuardsTeam.OnAllKilled, LabGuardsKilled)
	
	civiliansTeam = Team.New({ Civilian1, Civilian2 })
	
	RunInitialActivities()
	
	SetUnitStances()
	
	OpenRA.RunAfterDelay(CameraDelay, function() Actor.Create("camera", { Owner = player, Location = BaseCameraPoint.Location }) end)
	
	OpenRA.SetViewportCenterPosition(InsertionLZ.CenterPosition)
	
	Media.PlayMovieFullscreen("ally1.vqa", function() Media.PlayMovieFullscreen("landing.vqa", Media.PlayRandomMusic) end)
end