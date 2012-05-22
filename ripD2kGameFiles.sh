#!/bin/bash
R8="/home/matthias/.openra/Content/d2k/DATA.R8"
PAL="mods/d2k/bits/d2k.pal"

mono OpenRA.Utility.exe --r8 $R8 $PAL 194 205 "spice"
mono OpenRA.Utility.exe --shp spice.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 206 457 "rifleinfantry" --infantry
mono OpenRA.Utility.exe --shp rifleinfantry.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 458 693 "rocketinfantry" --infantry
mono OpenRA.Utility.exe --shp rocketinfantry.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 694 929 "fremen" --infantry
mono OpenRA.Utility.exe --shp fremen.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 930 1165 "sardaukar" --infantry
mono OpenRA.Utility.exe --shp sardaukar.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1166 1221 "engineer" --infantry # death animation 1342..1401
mono OpenRA.Utility.exe --shp engineer.png 48
#rifleinfantry repetitions?
mono OpenRA.Utility.exe --r8 $R8 $PAL 1402 1502 "thumper" --infantry #death animations 1543..1602
mono OpenRA.Utility.exe --shp thumper.png 48
#rifleinfantry repetitions?
mono OpenRA.Utility.exe --r8 $R8 $PAL 1603 1634 "missile" --vehicle
mono OpenRA.Utility.exe --shp missile.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1635 1666 "trike" --vehicle
mono OpenRA.Utility.exe --shp trike.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 1667 1698 "quad" --vehicle
mono OpenRA.Utility.exe --shp quad.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 1699 1730 "harvester" --vehicle
mono OpenRA.Utility.exe --shp harvester.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1731 1762 "combata" --vehicle
mono OpenRA.Utility.exe --shp combata.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1763 1794 "siege" --vehicle
mono OpenRA.Utility.exe --shp siege.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1795 1826 "mcv" --vehicle
mono OpenRA.Utility.exe --shp mcv.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1827 1858 "sonic" --vehicle
mono OpenRA.Utility.exe --shp sonic.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1859 1890 "combataturret" --vehicle
mono OpenRA.Utility.exe --shp combataturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1891 1922 "siegeturret" --vehicle
mono OpenRA.Utility.exe --shp siegeturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1923 1954 "carryall" --vehicle # requires some reordering (again)
mono OpenRA.Utility.exe --shp carryall.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 1955 2050 "orni" --vehicle
mono OpenRA.Utility.exe --shp orni.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2051 2082 "combath" --vehicle
mono OpenRA.Utility.exe --shp combath.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2083 2114 "devast" --vehicle
mono OpenRA.Utility.exe --shp devast.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2115 2146 "combataturret" --vehicle
mono OpenRA.Utility.exe --shp combataturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2147 2148 "deathhandmissile"
mono OpenRA.Utility.exe --shp deathhandmissile.png 24
#rifleinfantry repetitions?
mono OpenRA.Utility.exe --r8 $R8 $PAL 2245 2284 "saboteur" --infantry #death animations 2325..2388
mono OpenRA.Utility.exe --shp saboteur.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2389 2420 "deviator" --vehicle
mono OpenRA.Utility.exe --shp deviator.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2421 2452 "raider" --vehicle
mono OpenRA.Utility.exe --shp raider.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2453 2484 "combato" --vehicle
mono OpenRA.Utility.exe --shp combato.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2485 2516 "combatoturret" --vehicle
mono OpenRA.Utility.exe --shp combatoturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2517 2517 "frigate" --vehicle
mono OpenRA.Utility.exe --shp frigate.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2518 2520 "heavya" --building #2518 is only the gate
mono OpenRA.Utility.exe --shp heavya.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2521 2522 "radara" --building
mono OpenRA.Utility.exe --shp radara.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2523 2524 "pwra" --building
mono OpenRA.Utility.exe --shp pwra.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 2525 2526 "barra" --building
mono OpenRA.Utility.exe --shp barra.png 80
mono OpenRA.Utility.exe --r8 $R8 $PAL 2527 2558 "wall" --building
mono OpenRA.Utility.exe --shp wall.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2559 2560 "conyarda" --building
mono OpenRA.Utility.exe --shp conyarda.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2561 2563 "refa" --building # 2561 is fassade, 2562 is silo top, 2563 is silo top broken
mono OpenRA.Utility.exe --shp refa.png 120
mono OpenRA.Utility.exe --r8 $R8 $PAL 2564 2565 "hightecha" --building
mono OpenRA.Utility.exe --shp hightecha.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2566 2570 "siloa" --building
mono OpenRA.Utility.exe --shp siloa.png 32

mono OpenRA.Utility.exe --r8 $R8 $PAL 2673 2675 "lighta" --building
mono OpenRA.Utility.exe --shp lighta.png 96

mono OpenRA.Utility.exe --r8 $R8 $PAL 3549 3564 "sandwormmouth"
mono OpenRA.Utility.exe --shp sandwormmouth.png 68
mono OpenRA.Utility.exe --r8 $R8 $PAL 3565 3585 "sandwormdust"
mono OpenRA.Utility.exe --shp sandwormdust.png 68
mono OpenRA.Utility.exe --r8 $R8 $PAL 3586 3600 "wormsigns1"
mono OpenRA.Utility.exe --shp wormsigns1.png 16
mono OpenRA.Utility.exe --r8 $R8 $PAL 3601 3610 "wormsigns2"
mono OpenRA.Utility.exe --shp wormsigns2.png 16
mono OpenRA.Utility.exe --r8 $R8 $PAL 3611 3615 "wormsigns3"
mono OpenRA.Utility.exe --shp wormsigns3.png 16
mono OpenRA.Utility.exe --r8 $R8 $PAL 3616 3620 "wormsigns4"
mono OpenRA.Utility.exe --shp wormsigns4.png 16

mono OpenRA.Utility.exe --r8 $R8 $PAL 3679 3686 "sell"
mono OpenRA.Utility.exe --shp sell.png 48
#explosions and muzzle flash

mono OpenRA.Utility.exe --r8 $R8 $PAL 4011 4011 "infrantryicon"
mono OpenRA.Utility.exe --shp infrantryicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4012 4012 "bazookaicon"
mono OpenRA.Utility.exe --shp bazookaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4013 4013 "engineericon"
mono OpenRA.Utility.exe --shp engineericon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4014 4014 "thumpericon"
mono OpenRA.Utility.exe --shp thumpericon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4015 4015 "sadaukaricon"
mono OpenRA.Utility.exe --shp sadaukaricon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4016 4016 "trikeicon"
mono OpenRA.Utility.exe --shp trikeicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4017 4017 "raidericon"
mono OpenRA.Utility.exe --shp raidericon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4018 4018 "quadicon"
mono OpenRA.Utility.exe --shp quadicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4019 4019 "harestervicon" # = 4044
mono OpenRA.Utility.exe --shp harestervicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4020 4020 "combataicon"
mono OpenRA.Utility.exe --shp combataicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4021 4021 "combathicon"
mono OpenRA.Utility.exe --shp combathicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4022 4022 "combatoicon"
mono OpenRA.Utility.exe --shp combatoicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4023 4023 "mcvicon"
mono OpenRA.Utility.exe --shp mcvicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4024 4024 "missileicon"
mono OpenRA.Utility.exe --shp missileicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4025 4025 "deviatoricon"
mono OpenRA.Utility.exe --shp deviatoricon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4026 4026 "siegeicon"
mono OpenRA.Utility.exe --shp siegeicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4027 4027 "sonicicon"
mono OpenRA.Utility.exe --shp sonicicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4028 4028 "devastatoricon"
mono OpenRA.Utility.exe --shp devastatoricon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4029 4029 "carryallicon" # = 4030
mono OpenRA.Utility.exe --shp carryallicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4031 4031 "orniicon" # = 4062
mono OpenRA.Utility.exe --shp orniicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4032 4032 "fremenicon" # = 4033
mono OpenRA.Utility.exe --shp fremenicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4034 4034 "saboteuricon"
mono OpenRA.Utility.exe --shp saboteuricon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4035 4035 "deathhandicon"
mono OpenRA.Utility.exe --shp deathhandicon.png 60
# 4036..4045 = repetitions
mono OpenRA.Utility.exe --r8 $R8 $PAL 4046 4046 "conyardicona" # = 4049
mono OpenRA.Utility.exe --shp conyardicona.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4047 4047 "conyardiconh"
mono OpenRA.Utility.exe --shp conyardiconh.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4048 4048 "conyardicono"
mono OpenRA.Utility.exe --shp conyardicono.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4050 4050 "4plateicon" # = 4051..4052
mono OpenRA.Utility.exe --shp 4plateicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4053 4053 "6plateicon" # = 4054..4055
mono OpenRA.Utility.exe --shp 6plateicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4056 4056 "pwraicon"
mono OpenRA.Utility.exe --shp pwraicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4057 4057 "pwrhicon"
mono OpenRA.Utility.exe --shp pwrhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4058 4058 "pwroicon"
mono OpenRA.Utility.exe --shp pwroicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4059 4059 "barraicon"
mono OpenRA.Utility.exe --shp barraicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4060 4060 "barrhicon"
mono OpenRA.Utility.exe --shp barrhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4061 4061 "barroicon"
mono OpenRA.Utility.exe --shp barroicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4063 4063 "wallicon" # = 4061..4062
mono OpenRA.Utility.exe --shp wallicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4066 4066 "refaicon"
mono OpenRA.Utility.exe --shp refaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4067 4067 "refhicon"
mono OpenRA.Utility.exe --shp refhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4068 4068 "refoicon"
mono OpenRA.Utility.exe --shp refoicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4069 4069 "turreticon" # = 4070..4071
mono OpenRA.Utility.exe --shp turreticon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4072 4072 "radaraicon"
mono OpenRA.Utility.exe --shp radaraicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4073 4073 "radarhicon"
mono OpenRA.Utility.exe --shp radarhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4074 4074 "radaroicon"
mono OpenRA.Utility.exe --shp radaroicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4075 4075 "rturreticon" # = 4076..4077
mono OpenRA.Utility.exe --shp rturreticon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4078 4078 "hightechaicon"
mono OpenRA.Utility.exe --shp hightechaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4079 4079 "hightechhicon"
mono OpenRA.Utility.exe --shp hightechhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4080 4080 "hightechoicon"
mono OpenRA.Utility.exe --shp hightechoicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4081 4081 "lightaicon"
mono OpenRA.Utility.exe --shp lightaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4082 4082 "lighthicon"
mono OpenRA.Utility.exe --shp lighthicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4083 4083 "lightoicon"
mono OpenRA.Utility.exe --shp lightoicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4084 4084 "siloaicon"
mono OpenRA.Utility.exe --shp siloaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4085 4085 "silohicon"
mono OpenRA.Utility.exe --shp silohicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4086 4086 "silooicon"
mono OpenRA.Utility.exe --shp silooicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4087 4087 "heavyaicon"
mono OpenRA.Utility.exe --shp heavyaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4088 4088 "heavyhicon"
mono OpenRA.Utility.exe --shp heavyhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4089 4089 "heavyoicon"
mono OpenRA.Utility.exe --shp heavyoicon.png 60
# 4090 = orniicon
# 4091 = heavyhicon
mono OpenRA.Utility.exe --r8 $R8 $PAL 4092 4092 "starportaicon"
mono OpenRA.Utility.exe --shp starportaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4093 4093 "starporthicon"
mono OpenRA.Utility.exe --shp starporthicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4094 4094 "starportoicon"
mono OpenRA.Utility.exe --shp starportoicon.png 60
# 4095 = orniicon
mono OpenRA.Utility.exe --r8 $R8 $PAL 4096 4096 "repairicon" # = 4097..4098
mono OpenRA.Utility.exe --shp repairicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4099 4099 "researchicon" # = 4100..4101
mono OpenRA.Utility.exe --shp researchicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4102 4102 "palaceaicon"
mono OpenRA.Utility.exe --shp palaceaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4103 4103 "palacehicon"
mono OpenRA.Utility.exe --shp palacehicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4104 4104 "palaceoicon"
mono OpenRA.Utility.exe --shp palaceoicon.png 60
# 4105 = orniicon
# 4106..4107 = radaraicon
# 4108 = conyardaicon
mono OpenRA.Utility.exe --r8 $R8 $PAL 4109 4150 "conmake" --building
mono OpenRA.Utility.exe --shp conmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4151 4174 "wtrpmake" --building
mono OpenRA.Utility.exe --shp wtrpmake.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 4175 4194 "barramake" --building
mono OpenRA.Utility.exe --shp barramake.png 80

mono OpenRA.Utility.exe --r8 $R8 $PAL 4231 4253 "refmake" --building
mono OpenRA.Utility.exe --shp refmake.png 120

mono OpenRA.Utility.exe --r8 $R8 $PAL 4274 4294 "highmake" --building
mono OpenRA.Utility.exe --shp highmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4275 4312 "lightmake" --building
mono OpenRA.Utility.exe --shp lightmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4313 4327 "silomake" --building
mono OpenRA.Utility.exe --shp silomake.png 32

mono OpenRA.Utility.exe --r8 $R8 $PAL 4436 4449 "cranea"
mono OpenRA.Utility.exe --shp cranea.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4450 4463 "craneh"
mono OpenRA.Utility.exe --shp craneh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4463 4477 "craneo"
mono OpenRA.Utility.exe --shp craneo.png 96

mono OpenRA.Utility.exe --r8 $R8 $PAL 4760 4819 "windtrap_anim" --building #?
mono OpenRA.Utility.exe --shp windtrap_anim.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4820 4840 "missile_launch"
mono OpenRA.Utility.exe --shp missile_launch.png 96

mv *.shp mods/d2k/bits