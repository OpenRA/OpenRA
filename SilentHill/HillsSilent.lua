
Light = 1.2

SilentHill = function()

	Lighting.Red = 0.9
	Lighting.Green = 0.9
	Lighting.Blue = 1
	Lighting.Ambient = Light
	if Light > 1.19 then
		local delay = Utils.RandomInteger(1, 10)
		Thunderstorm = true
		Lighting.Flash("LightningStrike", delay)
		Trigger.AfterDelay(delay, function()
			Media.PlaySound("thunder" .. Utils.RandomInteger(1,6) .. ".aud")
		end)
	end
	if Light < 1.311 then
		if Light < 0.75 then
			--DemonSpawnTrigger()
			--Media.PlaySoundNotification(Multi0, "LevelUp")

			return
		else
			Light = Light - 0.01
			Trigger.AfterDelay(10, function()
				SilentHill()
			end)
		end
	end
end

BioCheck1 = function()
	if BioLab.IsInWorld == Neutral  then
		Trigger.OnCaptured(BioLab, Purgatory)
		Trigger.OnCaptured(BioLab2, Purgatory)
	end
end
--if Weather.IsAvailable then
--Weather.UseSquares = not Weather.UseSquares
--Weather.ParticleDensityFactor = Weather.ParticleDensityFactor + 0.01
--Media.DisplayMessage("" .. Weather.ParticleDensityFactor)
--end
--Weather.ParticleDensityFactor
--Weather.ChangingWindLevel
--Weather.InstantWindChanges
--Weather.UseSquares
DemonSpawnTrigger = function()
	Trigger.OnCapture(BioLab2, SilentHill)
	Trigger.OnCapture(BioLab2, SilentHill)
	--Lighting.Red = Light
	--if Light > 0.70 then
	--	Light = Light - 0.10
	--	Trigger.AfterDelay(2, function()
	--		turnOff()
	--	end)	
	--else
	--	Trigger.AfterDelay(100, function()
	--		TriggerAlarm()
	--	end)
	--end
end

Siren = function()
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySound("Siren.wav")
	end)
end
Rumble1 = function()
	Media.PlaySound("Rumble.aud")
	local delay = Utils.RandomInteger(1, 10)
	Lighting.Flash("LightningStrike", delay)
end
Rumble2 = function()
	Media.PlaySound("Rumble.aud")
	local delay = Utils.RandomInteger(1, 10)
	Lighting.Flash("LightningStrike", delay)
end
--SilentHill = function()
--	Lighting.Ambient = Light
--	--Weather = true
--	if Light < 0.8 then
--		if Light < 0.45 then
--			--Media.PlaySoundNotification(Multi0, "LevelUp")
--			SpawnDemons()
--			return
--		else
--			Light = Light - 0.01
--			Trigger.AfterDelay(30, function()
--				SilentHill()
--			end)
--		end
--	end
--end

--SpawnDemons = function()
--end
----ThuderStrikes = function()
	
--turnOn = function()
--	Lighting.Ambient = Light
--	if Light &lt; 1.111 then
--		Light = Light + 0.001
--		Trigger.AfterDelay(4, function()
--			turnOn()
--		end)
	
--	else
--		Trigger.AfterDelay(3000, function()
--			turnOff()
--		end)
--	end
--end

--turnonmore = function()
--	Lighting.Ambient = Light
--	if Light &lt; 0.7 then
--		Thunderstorm = true
--	end
--	if Light &gt; 0.45 then
--		Light = Light - 0.001
--		Trigger.AfterDelay(4, function()
--			turnOff()
--		end)	
	
--	else
--		Trigger.AfterDelay(3000, function()
--			turnOn()
--			Thunderstorm = false
--		end)
--	end
--end
BioLabs = { BioLab, BioLab2 }
BioLabsCheck = function()
	if BioLab.IsInWorld and BioLab2.IsInWorld then
		Trigger.OnAllKilledOrCaptured(BioLabs, SilentHill)
		Trigger.OnAllKilledOrCaptured(BioLabs, Siren)
		return
	end
	if not BioLab.IsInWorld then
		Trigger.OnCapture(BioLab2, SilentHill)
		Siren()
	end
	if not BioLab2.IsInWorld then
		Trigger.OnCapture(BioLab, SilentHill)
		Siren()
	end
	Trigger.AfterDelay(DateTime.Seconds(0.5), function()
		BioLabsCheck()
	end)
end
ZillaEgg = "monsteregg"
WorldLoaded = function()
	Neutral = Player.GetPlayer("Neutral")
	Multi0 = Player.GetPlayer("Multi0")
	BioLabsCheck()


	--Trigger.OnCapture(BioLab, Darken)
	--elseif
	--	if not BioLab.Owner == Neutral  then
	--		Trigger.OnCapture(BioLab2, DemonSpawnTrigger)
	--	end
	--	if not BioLab2 owner neutral then
	--		Trigger.OnCapture(BioLab2, Darken)
	--	else		 
end

Thunderstorm = false
Tick = function()
	if Thunderstorm then
		if (Utils.RandomInteger(1, 200) == 10) then
			local delay = Utils.RandomInteger(1, 10)
			Lighting.Flash("LightningStrike", delay)
			Trigger.AfterDelay(delay, function()
				Media.PlaySound("thunder" .. Utils.RandomInteger(1,6) .. ".aud")
			end)
		end
	end
	if Weather then
		if (Utils.RandomInteger(1, 200) == 10) then
			Media.PlaySound("thunder-ambient.aud")
		end
	end
end
