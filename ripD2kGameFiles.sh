#!/bin/bash
R8="$HOME/.openra/Content/d2k/DATA.R8"
PAL="mods/d2k/bits/d2k.pal"

mono OpenRA.Utility.exe --r8 $R8 $PAL 0 2 "overlay"
mono OpenRA.Utility.exe --shp overlay.png 32

#mono OpenRA.Utility.exe --r8 $R8 $PAL 40 101 "shadow"
#mono OpenRA.Utility.exe --shp shadow.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 102 105 "crates"
mono OpenRA.Utility.exe --shp crates.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 107 109 "spicebloom"
mono OpenRA.Utility.exe --shp spicebloom.png 32
# stars, arrow-up
mono OpenRA.Utility.exe --r8 $R8 $PAL 114 129 "rockcrater1"
mono OpenRA.Utility.exe --shp rockcrater1.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 130 145 "rockcrater2"
mono OpenRA.Utility.exe --shp rockcrater2.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 146 161 "sandcrater1"
mono OpenRA.Utility.exe --shp sandcrater1.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 162 177 "sandcrater2"
mono OpenRA.Utility.exe --shp sandcrater2.png 32

mono OpenRA.Utility.exe --r8 $R8 $PAL 206 381 "rifle" --infantry
mono OpenRA.Utility.exe --shp rifle.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 382 457 "rifledeath" --infantrydeath
mono OpenRA.Utility.exe --shp rifledeath.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 458 693 "rocket" --infantry
mono OpenRA.Utility.exe --shp rocket.png 48
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 1795 1826 "dmcv" --vehicle
mono OpenRA.Utility.exe --shp dmcv.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1827 1858 "sonic" --vehicle
mono OpenRA.Utility.exe --shp sonic.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1859 1890 "combataturret" --vehicle
mono OpenRA.Utility.exe --shp combataturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1891 1922 "siegeturret" --vehicle
mono OpenRA.Utility.exe --shp siegeturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 1923 1954 "carryall" --vehicle
mono OpenRA.Utility.exe --shp carryall.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 1955 2050 "orni" --vehicle
mono OpenRA.Utility.exe --shp orni.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2051 2082 "combath" --vehicle
mono OpenRA.Utility.exe --shp combath.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2083 2114 "devast" --vehicle
mono OpenRA.Utility.exe --shp devast.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2115 2146 "combathturret" --vehicle
mono OpenRA.Utility.exe --shp combathturret.png 48
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 2527 2558 "wall" --wall
mono OpenRA.Utility.exe --shp wall.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2559 2560 "conyarda" --building
mono OpenRA.Utility.exe --shp conyarda.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2561 2563 "refa" --building # 2561 is fassade, 2562 is silo top, 2563 is silo top broken
mono OpenRA.Utility.exe --shp refa.png 120
mono OpenRA.Utility.exe --r8 $R8 $PAL 2564 2565 "hightecha" --building
mono OpenRA.Utility.exe --shp hightecha.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2566 2570 "siloa" --building
mono OpenRA.Utility.exe --shp siloa.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2571 2572 "repaira" --building
mono OpenRA.Utility.exe --shp repaira.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2573 2588 "guntower" --building
mono OpenRA.Utility.exe --shp guntower.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2589 2620 "gunturret" --building
mono OpenRA.Utility.exe --shp gunturret.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2621 2636 "rockettower" --building
mono OpenRA.Utility.exe --shp rockettower.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2637 2668 "rocketturreta" --building
mono OpenRA.Utility.exe --shp rocketturreta.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2669 2670 "researcha" --building
mono OpenRA.Utility.exe --shp researcha.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2671 2672 "starporta" --building
mono OpenRA.Utility.exe --shp starporta.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2673 2675 "lighta" --building
mono OpenRA.Utility.exe --shp lighta.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2676 2677 "palacea" --building
mono OpenRA.Utility.exe --shp palacea.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2678 2680 "heavyh" --building
mono OpenRA.Utility.exe --shp heavyh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2681 2682 "radarh" --building
mono OpenRA.Utility.exe --shp radarh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2683 2684 "pwrh" --building
mono OpenRA.Utility.exe --shp pwrh.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 2685 2686 "barrh" --building
mono OpenRA.Utility.exe --shp barrh.png 64
# identical wall
mono OpenRA.Utility.exe --r8 $R8 $PAL 2719 2720 "conyardh" --building
mono OpenRA.Utility.exe --shp conyardh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2721 2723 "refh" --building
mono OpenRA.Utility.exe --shp refh.png 120
mono OpenRA.Utility.exe --r8 $R8 $PAL 2724 2725 "hightechh" --building
mono OpenRA.Utility.exe --shp hightechh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2726 2730 "siloh" --building
mono OpenRA.Utility.exe --shp siloh.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2731 2732 "repairh" --building
mono OpenRA.Utility.exe --shp repairh.png 96
#identical guntower
mono OpenRA.Utility.exe --r8 $R8 $PAL 2749 2780 "gunturreth" --building
mono OpenRA.Utility.exe --shp gunturreth.png 48
#identical rockettower
mono OpenRA.Utility.exe --r8 $R8 $PAL 2797 2828 "rocketturreth" --building
mono OpenRA.Utility.exe --shp rocketturreth.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2829 2830 "researchh" --building
mono OpenRA.Utility.exe --shp researchh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2831 2832 "starporth" --building
mono OpenRA.Utility.exe --shp starporth.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2833 2835 "lighth" --building
mono OpenRA.Utility.exe --shp lighth.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2836 2837 "palaceh" --building
mono OpenRA.Utility.exe --shp palaceh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2838 2840 "heavyo" --building
mono OpenRA.Utility.exe --shp heavyo.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2841 2842 "radaro" --building
mono OpenRA.Utility.exe --shp radaro.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2843 2844 "pwro" --building
mono OpenRA.Utility.exe --shp pwro.png 64
mono OpenRA.Utility.exe --r8 $R8 $PAL 2845 2846 "barro" --building
mono OpenRA.Utility.exe --shp barro.png 64
# identical wall
mono OpenRA.Utility.exe --r8 $R8 $PAL 2879 2880 "conyardo" --building
mono OpenRA.Utility.exe --shp conyardo.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2881 2883 "refo" --building
mono OpenRA.Utility.exe --shp refo.png 120
mono OpenRA.Utility.exe --r8 $R8 $PAL 2884 2885 "hightecho" --building
mono OpenRA.Utility.exe --shp hightecho.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2886 2890 "siloo" --building
mono OpenRA.Utility.exe --shp siloo.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 2891 2892 "repairo" --building
mono OpenRA.Utility.exe --shp repairo.png 96
#identical guntower
mono OpenRA.Utility.exe --r8 $R8 $PAL 2909 2940 "gunturreto" --building
mono OpenRA.Utility.exe --shp gunturreto.png 48
#identical rockettower
mono OpenRA.Utility.exe --r8 $R8 $PAL 2957 2988 "rocketturreto" --building
mono OpenRA.Utility.exe --shp rocketturreto.png 48
mono OpenRA.Utility.exe --r8 $R8 $PAL 2989 2990 "researcho" --building
mono OpenRA.Utility.exe --shp researcho.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2991 2992 "starporto" --building
mono OpenRA.Utility.exe --shp starporto.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2993 2995 "lighto" --building
mono OpenRA.Utility.exe --shp lighto.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 2996 2997 "palaceo" --building
mono OpenRA.Utility.exe --shp palaceo.png 96

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

mono OpenRA.Utility.exe --r8 $R8 $PAL 4011 4011 "rifleicon"
mono OpenRA.Utility.exe --shp rifleicon.png 60
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 4019 4019 "harvestericon" # = 4044
mono OpenRA.Utility.exe --shp harvestericon.png 60
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 4028 4028 "devasticon"
mono OpenRA.Utility.exe --shp devasticon.png 60
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 4046 4046 "conyardaicon" # = 4049
mono OpenRA.Utility.exe --shp conyardaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4047 4047 "conyardhicon"
mono OpenRA.Utility.exe --shp conyardhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4048 4048 "conyardoicon"
mono OpenRA.Utility.exe --shp conyardoicon.png 60
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 4096 4096 "repairaicon"
mono OpenRA.Utility.exe --shp repairaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4097 4097 "repairhicon"
mono OpenRA.Utility.exe --shp repairhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4098 4098 "repairoicon"
mono OpenRA.Utility.exe --shp repairoicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4099 4099 "researchaicon"
mono OpenRA.Utility.exe --shp researchaicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4100 4100 "researchhicon"
mono OpenRA.Utility.exe --shp researchhicon.png 60
mono OpenRA.Utility.exe --r8 $R8 $PAL 4101 4101 "researchoicon"
mono OpenRA.Utility.exe --shp researchoicon.png 60
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
mono OpenRA.Utility.exe --r8 $R8 $PAL 4254 4273 "radarmake" --building
mono OpenRA.Utility.exe --shp radarmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4274 4294 "highmake" --building
mono OpenRA.Utility.exe --shp highmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4295 4312 "lightmake" --building
mono OpenRA.Utility.exe --shp lightmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4313 4327 "silomake" --building
mono OpenRA.Utility.exe --shp silomake.png 32
mono OpenRA.Utility.exe --r8 $R8 $PAL 4328 4346 "heavymake" --building
mono OpenRA.Utility.exe --shp heavymake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4347 4369 "starportmake" --building
mono OpenRA.Utility.exe --shp starportmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4370 4390 "repairmake" --building
mono OpenRA.Utility.exe --shp repairmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4391 4412 "researchmake" --building
mono OpenRA.Utility.exe --shp researchmake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4413 4435 "palacemake" --building
mono OpenRA.Utility.exe --shp palacemake.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4436 4449 "cranea" --building
mono OpenRA.Utility.exe --shp cranea.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4450 4463 "craneh" --building
mono OpenRA.Utility.exe --shp craneh.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4463 4477 "craneo" --building
mono OpenRA.Utility.exe --shp craneo.png 96

mono OpenRA.Utility.exe --r8 $R8 $PAL 4760 4819 "windtrap_anim" --building #?
mono OpenRA.Utility.exe --shp windtrap_anim.png 96
mono OpenRA.Utility.exe --r8 $R8 $PAL 4820 4840 "missile_launch"
mono OpenRA.Utility.exe --shp missile_launch.png 96

R8="$HOME/.openra/Content/d2k/MOUSE.R8"

mono OpenRA.Utility.exe --r8 $R8 $PAL 0 264 "mouse" --transparent
mono OpenRA.Utility.exe --shp mouse.png 48

R8="$HOME/.openra/Content/d2k/BLOXBASE.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "BASE" --tileset
mono OpenRA.Utility.exe --r8 $R8 $PAL 748 749 "spice0"
mono OpenRA.Utility.exe --shp spice0.png 32
mono OpenRA.TilesetBuilder.exe "BASE.png" 32 --export "Content/d2k/Tilesets"
R8="$HOME/.openra/Content/d2k/BLOXBAT.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "BAT" --tileset
mono OpenRA.TilesetBuilder.exe "BAT.png" 32 --export "Content/d2k/Tilesets"
R8="$HOME/.openra/Content/d2k/BLOXBGBS.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "BGBS" --tileset
mono OpenRA.TilesetBuilder.exe "BGBS.png" 32 --export "Content/d2k/Tilesets"
R8="$HOME/.openra/Content/d2k/BLOXICE.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "ICE" --tileset
mono OpenRA.TilesetBuilder.exe "ICE.png" 32 --export "Content/d2k/Tilesets"
R8="$HOME/.openra/Content/d2k/BLOXTREE.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "TREE" --tileset
mono OpenRA.TilesetBuilder.exe "TREE.png" 32 --export "Content/d2k/Tilesets"
R8="$HOME/.openra/Content/d2k/BLOXWAST.R8"
mono OpenRA.Utility.exe --r8 $R8 $PAL 0 799 "XWAST" --tileset
mono OpenRA.TilesetBuilder.exe "XWAST.png" 32 --export "Content/d2k/Tilesets"

mv *.shp $HOME/.openra/Content/d2k/SHPs