#!/bin/sh
# launch script (executed by Desura)
exec mono OpenRA.Game.exe Server.Dedicated=False Server.DedicatedLoop=False "$@"