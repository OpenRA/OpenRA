#!/bin/bash

DIFF=winmergeu

#$DIFF ../OpenRA.Mods.Common/Traits/DamagedByTerrain.cs Traits/DamagedByRadioactivity.cs
#$DIFF ../OpenRA.Mods.Common/Traits/Air/BlimpFallsToEarth.cs Traits/Air/BlimpFallsToEarth.cs
#$DIFF ../OpenRA.Mods.Common/Traits/Air/FallsToEarth.cs Traits/Air/BlimpFallsToEarth.cs
#$DIFF ../OpenRA.Mods.Common/Activities/Air/FallToEarth.cs Activities/Air/BlimpFallToEarth.cs
$DIFF ../OpenRA.Mods.Common/Traits/Explodes.cs Traits/SpawnedExplodes.cs
$DIFF ../OpenRA.Mods.Common/Traits/AutoTarget.cs Traits/AegisAutoTarget.cs

# check ai deploy helper from AS
# check explodesweapon from AS
