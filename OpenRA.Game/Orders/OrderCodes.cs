#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA
{
	public enum OrderCode : byte
	{
		None = 0,

		// Server
		AuthenticationError,
		Chat,
		Command,
		Disconnected,
		HandshakeRequest,
		HandshakeResponse,
		Message,
		PauseGame,
		Ping,
		Pong,
		ServerError,
		StartGame,
		SyncClientInfo,
		SyncClientPing,
		SyncClientPings,
		SyncInfo,
		SyncLobbyClients,
		SyncLobbyGlobalSettings,
		SyncLobbyInfo,
		SyncLobbySlots,
		TeamChat,

		// Cheats
		DevBuildAnywhere,
		DevEnableTech,
		DevFastBuild,
		DevFastCharge,
		DevGiveCash,
		DevGiveExploration,
		DevGrowResources,
		DevPathDebug,
		DevResetExploration,
		DevShroudDisable,
		DevUnlimitedPower,

		// Support Powers
		AirstrikePowerInfoOrder,
		ChronoshiftPowerInfoOrder,
		GpsPowerInfoOrder,
		GrantUpgradePowerInfoOrder,
		NukePowerInfoOrder,
		ParatroopersPowerInfoOrder,
		SpawnActorPowerInfoOrder,

		// Traits
		Attack,
		AttackMove,
		BeginMinefield,
		C4,
		CancelProduction,
		CaptureActor,
		CreateGroup,
		Deliver,
		DeliverSupplies,
		DeployTransform,
		Detonate,
		DetonateAttack,
		Disguise,
		EngineerRepair,
		Enter,
		EnterTransport,
		EnterTransports,
		ExternalCaptureActor,
		Guard,
		Harvest,
		Infiltrate,
		LineBuild,
		Move,
		PauseProduction,
		PlaceBeacon,
		PlaceBuilding,
		PlaceMine,
		PlaceMinefield,
		PortableChronoDeploy,
		PortableChronoTeleport,
		PowerDown,
		PrimaryProducer,
		Repair,
		RepairBridge,
		RepairBuilding,
		RepairNear,
		ReturnToBase,
		Scatter,
		Sell,
		SetRallyPoint,
		SetStance,
		SetUnitStance,
		StartProduction,
		Stop,
		Surrender,
		Unload,
	}
}
