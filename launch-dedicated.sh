#!/bin/bash
# example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details
Name="Dedicated-Server"
Mod="ra"
Dedicated="True"
DedicatedLoop="True"
ListenPort=1234
ExternalPort=1234
AdvertiseOnline="False"
PortForward="False"

while true; do
     mono --debug OpenRA.Game.exe Game.Mod=$Mod Server.Dedicated=$Dedicated Server.DedicatedLoop=$DedicatedLoop \
     Server.Name=$Name Server.ListenPort=$ListenPort Server.ExternalPort=$ExternalPort \
     Server.AdvertiseOnline=$AdvertiseOnline Server.AllowPortForward=$PortForward
done
