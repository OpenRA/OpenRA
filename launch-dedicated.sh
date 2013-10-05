#!/bin/bash
Name="Dedicated-Server"
Mod="ra"
Dedicated="True"
DedicatedLoop="True"
ListenPort=1234
ExternalPort=1234
AdvertiseOnline="False"
Map="ba403f6bcb4cae934335b78be42f714992b3a71a"

while true; do
     mono --debug OpenRA.Game.exe Game.Mod=$Mod Server.Dedicated=$Dedicated Server.DedicatedLoop=$DedicatedLoop \
     Server.Name=$Name Server.ListenPort=$ListenPort Server.ExternalPort=$ExternalPort \
     Server.AdvertiseOnline=$AdvertiseOnline Server.Map=$Map 
done