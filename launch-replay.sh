#!/bin/bash
# TODO choose the correct Game.Mod instead of crashing
cd ${0%/*}
exec mono bin/OpenRA.Game.exe Launch.Replay="$@"