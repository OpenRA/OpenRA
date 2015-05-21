#!/bin/bash
cd ${0%/*}
exec mono OpenRA.Game.exe Launch.Replay="$@"