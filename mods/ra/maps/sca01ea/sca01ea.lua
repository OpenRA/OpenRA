--0
team_1_scout = {"ant3"}
team_2_scout2 = {"ant3"}
--3
team_4_nwharv = {"ant3","ant3"}
team_5_seharv = {"ant3"}
team_6_m020 = {"ant3","ant3"}
team_7_loop1 = {"ant3","ant3"}

team_8_grnf1 = {"e1","e1","e1","e2","e2"} --"tran",

team_9_gcnv1 = {"2tnk","2tnk"}
team_10_gcnv2 = {"mcv"}

team_11_m025 = {"ant3","ant3"}
--12
team_13_m040 = {"ant3","ant3"}
team_14_m042 = {"ant2"}
team_15_m060a = {"ant3","ant3"}
team_16_m060b = {"ant3","ant3"}
team_17_m060c = {"ant2"}
team_18_sat1 = {"ant3"}
team_19_sat2 = {"ant3"}
team_20_wait1 = {"ant3","ant3"}
team_21_wait2 = {"ant3","ant3"}
team_22_wait3 = {"ant3","ant3"}
team_23_p2min = {"ant3"}
team_24_ditch1 = {"ant3","ant3","ant3","ant3","ant3"}
team_25_ditch2 = {"ant3","ant3","ant3","ant3","ant3"}
team_26_ditch3 = {"ant3","ant3","ant3","ant3"}
team_27_ditch4 = {"ant2","ant2"}
team_28_m130 = {"ant3","ant3"}
team_29_m140 = {"ant3","ant3"}
team_30_m145 = {"ant3","ant3"}
team_31_m149 = {"ant2"}
team_32_m165b = {"ant3","ant3","ant3"}
team_33_m180 = {"ant3","ant3"}
team_34_m186 = {"ant2"}
team_35_m210 = {"ant3","ant3","ant3"}
team_36_m214 = {"ant3","ant3"}
team_37_m280a = {"ant3","ant3"}
team_38_m280b = {"ant3","ant3"}

Global1 = false
Global2 = false

ticks = -1

WorldLoaded = function()
	Media.Debug("WorldLoaded start")
	player = Player.GetPlayer("Spain")
	ants = Player.GetPlayer("Ants")
	neutral = Player.GetPlayer("Neutral")
	
	BaseDiscoveredTrigger = Trigger.OnEnteredProximityTrigger(waypoint0.CenterPosition, WDist.FromCells(11), BaseDiscovered)
	
	Trigger.AfterDelay(DateTime.Seconds(30 * 6), function()
		if not player.IsObjectiveCompleted(DiscoverBase) then
			BaseDiscovered(Actor143,BaseDiscoveredTrigger)
		end 
	end)
	
	Camera.Position = DefaultCameraPosition.CenterPosition
	
	DiscoverBase = player.AddPrimaryObjective("Discover base")
	creepsObj = ants.AddPrimaryObjective("Deny the allies!")
	
	Trigger.AfterDelay(5, function()
		harv = Utils.Where(Map.ActorsInWorld, function(a)
			return a.Owner == neutral and a.Type == "harv"
		end)[1]
		
		harv.Stop()
	end)
	
	bridges = Utils.Where(Map.ActorsInWorld, function(a)
		return a.Type == "bridge1" or a.Type == "bridge2"
	end)
	
	--trigger_34_brdg
	Trigger.OnAllKilled(bridges, function()
		Reinforcements.Reinforce(ants, team_2_scout2, {waypoint4.Location}, 1, function(a)
			Media.Debug("trigger_34_brdg")
			a.Move(waypoint18.Location)
			a.Move(waypoint3.Location)
			a.AttackMove(waypoint26.Location)
			a.Patrol({waypoint0.Location}, false)
			a.Hunt() --1
		end)
	end)
	
	Trigger.OnDamaged(Actor82, Actor82Dead)	
end

Actor82Dead = function(a,k)
	if Actor82.Health < 10000 then
		Actor82.Destroy()
	end
end

--trigger_0_rvl
BaseDiscovered = function(actor, id)
	if actor.Owner == player and (actor.Type == "jeep" or actor.Type=="e1") then
		Media.Debug("BaseDiscovered start")
				
		Actor99.Owner = player
		Actor100.Owner = player
		Actor101.Owner = player
		Actor102.Owner = player
		harv.Owner = player
		Actor103.Owner = player
		Actor104.Owner = player
		Actor105.Owner = player
		Actor106.Owner = player
		Actor107.Owner = player
		Actor129.Owner = player
		
		Trigger.RemoveProximityTrigger(id)
		HoldBase = player.AddPrimaryObjective("Defend base")
		player.MarkCompletedObjective(DiscoverBase)
		
		Media.PlaySpeechNotification(allies, "MissionTimerInitialised")
		
		ticks = DateTime.Minutes(30)
		Global1 = true
		
		Trigger.AfterDelay(DateTime.Seconds(1), Reantforcements)
		
		Trigger.AfterDelay(DateTime.Seconds(27*6), SendInsertionHelicopter)
	end
end



--trigger_1_strt
Tick = function()
	--Media.Debug("Tick start")
	
	if player.HasNoRequiredUnits() then
		ants.MarkCompletedObjective(creepsObj)
	end
	
	if Global1 then
		if ticks >= 0 then
			UserInterface.SetMissionText("Reinforcements in " .. Utils.FormatTime(ticks), player.Color)
			ticks = ticks - 1
		else
			Global1 = false
			SendMCV()
		end
	end
end

--trigger_12_m027
SendInsertionHelicopter = function()
	Media.Debug("SendInsertionHelicopter start")
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	
	Reinforcements.ReinforceWithTransport(player, "tran.insertion",
		team_8_grnf1, {waypoint12.Location, waypoint0.Location}, { waypoint12.Location })
end


SendMCV = function()
	Media.Debug("SendMCV start")
	
	Global2 = "clear"
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	
	Reinforcements.Reinforce(player, team_9_gcnv1, {waypoint12.Location}, 1, function(a) a.Move(waypoint11.Location) end)
	
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Reinforcements.Reinforce(player, team_10_gcnv2, {waypoint12.Location}, 1, function(a) a.Move(waypoint11.Location) end)
	end)
	
	Trigger.AfterDelay(DateTime.Seconds(4), function() player.MarkCompletedObjective(HoldBase) end)
end

Reantforcements = function()
	Media.Debug("Reantforcements start")
	
	--trigger_4_m000
	Reinforcements.Reinforce(ants, team_1_scout, {waypoint4.Location}, 1, function(a)
		Media.Debug("Reantforcements trigger_4_m000 1")
		a.Move(waypoint4.Location)
		a.Move(waypoint3.Location)
		a.Move(waypoint2.Location)
		a.Move(waypoint0.Location)
		a.Hunt()
	end)
	Reinforcements.Reinforce(ants, team_2_scout2, {waypoint7.Location}, 1, function(a)
		Media.Debug("Reantforcements trigger_4_m000 2")
		a.Move(waypoint7.Location)
		a.Move(waypoint16.Location)
		a.Move(waypoint1.Location)
		a.Move(waypoint0.Location)
		a.Hunt()
	end)
	
	--trigger_5_m200
	Trigger.AfterDelay(DateTime.Seconds(200*6), function()
		Reinforcements.Reinforce(ants, team_4_nwharv, {waypoint8.Location}, 1, function(a)
			Media.Debug("Reantforcements trigger_5_m200")
			a.Move(waypoint8.Location)
			a.Move(waypoint13.Location)
			a.Move(waypoint16.Location)
			a.Move(waypoint17.Location)
			--a.GuardArea()
		end)
	end)
	
	
	--trigger_6_sehv, celltrigger
	trigger_6_sehv = false
	Trigger.OnEnteredProximityTrigger(waypoint9.CenterPosition, WDist.FromCells(5), function(actor, id)
		if not trigger_6_sehv and actor.Owner == player then
			trigger_6_sehv = true
			Media.Debug("Reantforcements trigger_6_sehv")
			Trigger.RemoveProximityTrigger(id)
			Reinforcements.Reinforce(ants, team_5_seharv, {waypoint4.Location}, 1, function(a)
				Media.Debug("Reantforcements trigger_6_sehv")
				a.Move(waypoint4.Location)
				a.Move(waypoint2.Location)
				a.Move(waypoint10.Location)
				a.Move(waypoint9.Location)
				--a.GuardArea()
			end)
		end
	end)
	
	
	--trigger_7_m020
	Trigger.AfterDelay(DateTime.Seconds(20 * 6), function()
		Media.Debug("Reantforcements trigger_7_m020")
		Reinforcements.Reinforce(ants, team_6_m020, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint4.Location)
			a.Move(waypoint5.Location)
			a.Move(waypoint15.Location)
			a.Move(waypoint0.Location)
			--a.GuardArea()
		end)
	end)
	
	
	--trigger_8_loo1, celltrigger
	trigger_8_loo1 = false
	Trigger.OnEnteredProximityTrigger(waypoint28.CenterPosition, WDist.FromCells(8), function(actor, id)
		if not trigger_8_loo1 and actor.Owner == player then
			trigger_8_loo1 = true
			Media.Debug("Reantforcements trigger_8_loo1")
			Trigger.RemoveProximityTrigger(id)
			Reinforcements.Reinforce(ants, team_7_loop1, {waypoint12.Location}, 1, function(a)
				Media.Debug("Reantforcements trigger_8_loo1")
				a.Patrol({waypoint13.Location, waypoint1.Location, waypoint16.Location}, false, 5)
			end)
		end
	end)
	
	--trigger_7_m020
	Trigger.AfterDelay(DateTime.Seconds(25 * 6), function()
		Media.Debug("Reantforcements trigger_7_m020")
		Reinforcements.Reinforce(ants, team_11_m025, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint7.Location)
			a.Move(waypoint16.Location)
			a.Move(waypoint1.Location)
			a.Move(waypoint0.Location)
			a.Hunt()
		end)
	end)
	
	--trigger_13_m040
	Trigger.AfterDelay(DateTime.Seconds(40 * 6), function()
		Media.Debug("Reantforcements trigger_13_m040")
		Reinforcements.Reinforce(ants, team_13_m040, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint8.Location)
			a.Move(waypoint9.Location)
			a.Hunt()
		end)
	end)
	
	--trigger_14_m042
	Trigger.AfterDelay(DateTime.Seconds(42 * 6), function()
		Media.Debug("Reantforcements trigger_14_m042")
		Reinforcements.Reinforce(ants, team_14_m042, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint4.Location)
			a.Move(waypoint5.Location)
			a.Move(waypoint15.Location)
			a.Hunt()
			--a.Hunt() 4,1
		end)
	end)
	
	--trigger_15_m060
	Trigger.AfterDelay(DateTime.Seconds(60 * 6), function()
		Media.Debug("Reantforcements trigger_15_m060")
		Reinforcements.Reinforce(ants, team_15_m060a, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint4.Location)
			a.Move(waypoint3.Location)
			a.Hunt()  --2
		end)
		--TimerStart
	end)
	
	--trigger_16_m064
	Trigger.AfterDelay(DateTime.Seconds(64 * 6), function()
		Media.Debug("Reantforcements trigger_16_m064")
		Reinforcements.Reinforce(ants, team_16_m060b, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint7.Location)
			a.Move(waypoint16.Location)
			a.Move(waypoint1.Location)
			a.Hunt()  --10
			a.Hunt()  --2
		end)
		Reinforcements.Reinforce(ants, team_17_m060c, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint8.Location)
			a.Move(waypoint9.Location)
			a.Move(waypoint19.Location)
			a.Hunt()  --4
			a.Hunt()  --1
		end)
	end)
	
	--trigger_17_snat, celltrigger
	trigger_17_snat = true
	Trigger.OnEnteredProximityTrigger(waypoint10.CenterPosition, WDist.FromCells(3), function(actor,id)
	
		if not trigger_17_snat then
			trigger_17_snat = true
			Trigger.RemoveProximityTrigger(id)
			Reinforcements.Reinforce(ants, team_18_sat1, {waypoint20.Location}, 1, function(a)
				Media.Debug("Reantforcements trigger_17_snat 1")
				a.Move(waypoint20.Location)
				a.Hunt()  --1
			end)
			Reinforcements.Reinforce(ants, team_19_sat2, {waypoint21.Location}, 1, function(a)
				Media.Debug("Reantforcements trigger_17_snat 2")
				a.Move(waypoint21.Location)
				a.Hunt()  --1
			end)
		end
	end)
		
	--trigger_18_m074
	Trigger.AfterDelay(DateTime.Seconds(74 * 6), function()
		Media.Debug("Reantforcements trigger_18_m074")
		Reinforcements.Reinforce(ants, team_17_m060c, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint8.Location)
			a.Move(waypoint9.Location)
			a.Move(waypoint19.Location)
			a.Hunt()  --4
			a.Hunt()  --1
		end)
	end)
	
	--trigger_19_m090
	Trigger.AfterDelay(DateTime.Seconds(90 * 6), function()
		Media.Debug("Reantforcements trigger_19_m090")
		Reinforcements.Reinforce(ants, team_20_wait1, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint12.Location)
			a.Move(waypoint11.Location)
			--a.Move(waypoint1.Location)
			--a.GuardArea(3)
			--a.Patrol({waypoint0.Location}, false)
			a.Patrol({waypoint1.Location, waypoint1.Location}, false, DateTime.Seconds(3))
			a.Hunt()
		end)
		Reinforcements.Reinforce(ants, team_21_wait2, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint8.Location)
			a.Move(waypoint10.Location)
			--a.Move(waypoint2.Location)
			--a.GuardArea(3)
			--a.Patrol({waypoint0.Location}, false)
			a.Patrol({waypoint2.Location, waypoint0.Location}, false, DateTime.Seconds(3))
			a.Hunt() --2
		end)
	end)
	
	--trigger_20_m091
	Trigger.AfterDelay(DateTime.Seconds(97 * 6), function()
		Media.Debug("Reantforcements trigger_20_m091")
		Reinforcements.Reinforce(ants, team_22_wait3, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint7.Location)
			a.Move(waypoint6.Location)
			--a.Move(waypoint15.Location)
			--a.GuardArea(3)
			--a.Patrol({waypoint0.Location}, false)
			a.Patrol({waypoint15.Location, waypoint0.Location}, false, DateTime.Seconds(3))
		end)
	end)	
	
	--trigger_21_p023
	Trigger.AfterDelay(DateTime.Seconds(23 * 6), function()
		Media.Debug("Reantforcements trigger_21_p023")
		Reinforcements.Reinforce(ants, team_23_p2min, {waypoint13.Location}, 1, function(a)
			a.Move(waypoint11.Location)
			a.Move(waypoint1.Location)
			a.Hunt()
		end)
	end)
	
	--trigger_22_m110
	Trigger.AfterDelay(DateTime.Seconds(110 * 6), function()
		Media.Debug("Reantforcements trigger_22_m110")
		Reinforcements.Reinforce(ants, team_5_seharv, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint4.Location)
			a.Move(waypoint2.Location)
			a.Move(waypoint10.Location)
			a.Move(waypoint9.Location)
			--a.GuardArea()
		end)
	end)
	
	--trigger_23_m130
	Trigger.AfterDelay(DateTime.Seconds(130 * 6), function()
		Media.Debug("Reantforcements trigger_23_m130")
		Reinforcements.Reinforce(ants, team_28_m130, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint12.Location)
			a.Move(waypoint11.Location)
			a.Move(waypoint14.Location)
			a.Move(waypoint9.Location)
			a.Hunt() --4
			a.Hunt() --10
		end)
	end)
	
	--trigger_24_m140
	Trigger.AfterDelay(DateTime.Seconds(140 * 6), function()
		Media.Debug("Reantforcements trigger_24_m140")
		Reinforcements.Reinforce(ants, team_29_m140, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint17.Location)
			a.Move(waypoint6.Location)
			a.Move(waypoint15.Location)
			a.Hunt() --8
			a.Hunt() --7
		end)
	end)
	
	--trigger_25_m145
	Trigger.AfterDelay(DateTime.Seconds(145 * 6), function()
		Media.Debug("Reantforcements trigger_25_m145")
		Reinforcements.Reinforce(ants, team_30_m145, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint18.Location)
			a.Move(waypoint3.Location)
			a.Move(waypoint2.Location)
		end)
	end)
	
	--trigger_26_m149
	Trigger.AfterDelay(DateTime.Seconds(149 * 6), function()
		Media.Debug("Reantforcements trigger_26_m149")
		Reinforcements.Reinforce(ants, team_31_m149, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint8.Location)
			a.Move(waypoint9.Location)
			a.Hunt() --4
			a.Hunt() --5
			a.Hunt() --2
		end)
	end)
	
	--trigger_27_m165
	Trigger.AfterDelay(DateTime.Seconds(165 * 6), function()
		Media.Debug("Reantforcements trigger_27_m165 1")
		Reinforcements.Reinforce(ants, team_29_m140, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint17.Location)
			a.Move(waypoint6.Location)
			a.Move(waypoint15.Location)
			a.Hunt() --8
			a.Hunt() --7
		end)
		Media.Debug("Reantforcements trigger_27_m165 2")
		Reinforcements.Reinforce(ants, team_32_m165b, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint17.Location)
			a.Move(waypoint16.Location)
			a.Move(waypoint1.Location)
			a.Hunt() --4
			a.Hunt() --5
		end)
	end)
	
	--trigger_28_m180
	Trigger.AfterDelay(DateTime.Seconds(180 * 6), function()
		Media.Debug("Reantforcements trigger_28_m180")
		Reinforcements.Reinforce(ants, team_33_m180, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint18.Location)
			a.Move(waypoint3.Location)
			a.AttackMove(waypoint26.Location)
			a.Attack(Actor82)
			Trigger.AfterDelay(DateTime.Seconds(5), function() Actor82.Destroy() end)
			a.Patrol({waypoint0.Location}, false)
			a.Hunt() --1
		end)
	end)
	
	--trigger_29_m186
	Trigger.AfterDelay(DateTime.Seconds(180 * 6), function()
		Media.Debug("Reantforcements trigger_29_m186")
		Reinforcements.Reinforce(ants, team_34_m186, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint12.Location)
			a.Move(waypoint11.Location)
			a.Move(waypoint13.Location)
			a.Move(waypoint1.Location)
			a.Hunt() --4
			a.Hunt() --5
			a.Hunt() --real hunt
		end)
	end)
	
	--trigger_32_m210
	Trigger.AfterDelay(DateTime.Seconds(210 * 6), function()
		Media.Debug("Reantforcements trigger_32_m210")
		Reinforcements.Reinforce(ants, team_35_m210, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint18.Location)
			a.Patrol({waypoint6.Location}, false)
			a.Hunt() --4
		end)
	end)
	
	--trigger_33_m214
	Trigger.AfterDelay(DateTime.Seconds(214 * 6), function()
		Media.Debug("Reantforcements trigger_33_m214")
		Reinforcements.Reinforce(ants, team_36_m214, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint11.Location)
			a.Patrol({waypoint1.Location}, false)
			a.Hunt() --4
		end)
	end)
	
	--trigger_35_m255
	Trigger.AfterDelay(DateTime.Seconds(255 * 6), function()
		Media.Debug("Reantforcements trigger_35_m255")
		Reinforcements.Reinforce(ants, team_24_ditch1, {waypoint4.Location}, 1, function(a)
			a.Move(waypoint18.Location)
			a.Move(waypoint5.Location)
			a.Hunt() --1
		end)
	end)
	
	--trigger_36_m257
	Trigger.AfterDelay(DateTime.Seconds(257 * 6), function()
		Media.Debug("Reantforcements trigger_36_m257")
		Reinforcements.Reinforce(ants, team_25_ditch2, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint17.Location)
			a.Move(waypoint16.Location)
			a.Patrol({waypoint1.Location}, false)
			a.Hunt() --1
		end)
	end)
	
	--trigger_37_m259
	Trigger.AfterDelay(DateTime.Seconds(259 * 6), function()
		Media.Debug("Reantforcements trigger_37_m259")
		Reinforcements.Reinforce(ants, team_26_ditch3, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint11.Location)
			a.Move(waypoint14.Location)
			a.Patrol({waypoint9.Location}, false)
			a.Hunt() --4
			a.Hunt() --1
		end)
	end)
	
	--trigger_38_m261
	Trigger.AfterDelay(DateTime.Seconds(261 * 6), function()
		Media.Debug("Reantforcements trigger_38_m261")
		Reinforcements.Reinforce(ants, team_27_ditch4, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint10.Location)
			a.Patrol({waypoint2.Location}, false)
			a.Hunt() --1
		end)
	end)
	
	--trigger_39_m230
	Trigger.AfterDelay(DateTime.Seconds(230 * 6), function()
		Media.Debug("Reantforcements trigger_39_m230")
		Reinforcements.Reinforce(ants, team_26_ditch3, {waypoint12.Location}, 1, function(a)
			a.Move(waypoint11.Location)
			a.Move(waypoint14.Location)
			a.Patrol({waypoint9.Location}, false)
			a.Hunt() --4
			a.Hunt() --1
		end)
	end)
	
	--trigger_40_m280
	Trigger.AfterDelay(DateTime.Seconds(280 * 6), function()
		Media.Debug("Reantforcements trigger_40_m280")
		Reinforcements.Reinforce(ants, team_37_m280a, {waypoint7.Location}, 1, function(a)
			a.Move(waypoint17.Location)
			a.Move(waypoint6.Location)
			a.Hunt() --4
			a.Hunt() --1
		end)
		Reinforcements.Reinforce(ants, team_38_m280b, {waypoint8.Location}, 1, function(a)
			a.Move(waypoint10.Location)
			a.Patrol({waypoint2.Location}, false)
			a.Hunt() --1
		end)
	end)

end
