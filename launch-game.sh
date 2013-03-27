#!/bin/sh
exec mono OpenRA.Game.exe Server.Dedicated=False Server.DedicatedLoop=False "$@"