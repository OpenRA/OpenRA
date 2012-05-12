#!/bin/bash
#NOTE: requires ImageMagick to be installed
convert {1614..1634}.bmp +append missile.png
#mono OpenRA.Utility.exe --shp missile.png 48
convert {1635..1666}.bmp +append trike.png
#mono OpenRA.Utility.exe --shp trike.png 32
convert {1667..1698}.bmp +append quad.png
#mono OpenRA.Utility.exe --shp quad.png 32
convert {1699..1730}.bmp +append harvester.png
#mono OpenRA.Utility.exe --shp harvester.png 48
convert {1731..1762}.bmp +append combat.png
#mono OpenRA.Utility.exe --shp combat_o.png 48
convert {1763..1794}.bmp +append siege.png
#mono OpenRA.Utility.exe --shp siege.png 48
convert {1795..1826}.bmp +append mcv.png
#mono OpenRA.Utility.exe --shp mcv.png 48
convert {1827..1858}.bmp +append sonic.png
#mono OpenRA.Utility.exe --shp sonic.png 48
convert {1859..1890}.bmp +append combat_?_turret.png
#mono OpenRA.Utility.exe --shp tankturrent.png 48
convert {1891..1922}.bmp +append siegeturret.png
#mono OpenRA.Utility.exe --shp siegeturret.png 48
convert {1923..1954}.bmp +append carryall.png
#mono OpenRA.Utility.exe --shp siegeturret.png 64
convert {1955..2050}.bmp +append orni.png
#mono OpenRA.Utility.exe --shp orni.png 48
convert {2051..2082}.bmp +append combat_h.png
#mono OpenRA.Utility.exe --shp combat_h.png 48
convert {2083..2114}.bmp +append devast.png
#mono OpenRA.Utility.exe --shp devast.png 48
convert {2115..2146}.bmp +append combat_o_turret.png
#mono OpenRA.Utility.exe --shp combat_o_turret.png 48
convert {2147..2148}.bmp +append nuke_missile.png
#mono OpenRA.Utility.exe --shp nuke_missile.png 24

convert {3549..3564}.bmp +append sandworm_mouth.png
# mono OpenRA.Utility.exe --shp sandworm_mouth.png 68
convert {3565..3585}.bmp +append sandworm_dust.png
# mono OpenRA.Utility.exe --shp sandworm_dust.png 68
convert {3586..3600}.bmp +append wormsigns1.png
# mono OpenRA.Utility.exe --shp wormsigns1.png 16
convert {3601..3610}.bmp +append wormsigns2.png
# mono OpenRA.Utility.exe --shp wormsigns2.png 16
convert {3611..3615}.bmp +append wormsigns3.png
# mono OpenRA.Utility.exe --shp wormsigns3.png 16
convert {3616..3620}.bmp +append wormsigns4.png
# mono OpenRA.Utility.exe --shp wormsigns4.png 16

convert {3679..3686}.bmp +append sell.png
# mono OpenRA.Utility.exe --shp sell.png 48

convert {4109..4150}.bmp +append mcv_deploy.png 
# mono OpenRA.Utility.exe --shp mcv_deploy.png 96

convert {4436..4449}.bmp +append crane_?.png
# mono OpenRA.Utility.exe --shp crane_?.png 96
convert {4450..4463}.bmp +append crane_?.png
# mono OpenRA.Utility.exe --shp crane_?.png 96

convert {4760..4819}.bmp +append windtrap_anim.png
# mono OpenRA.Utility.exe --shp windtrap_anim.png 96
convert {4820..4840}.bmp +append missile_launch.png
# mono OpenRA.Utility.exe --shp missile_launch.png 96
