#!/bin/bash

WD=$TRAVIS_BUILD_DIR/packaging

cp $WD/../OpenRA.Game.exe $WD/OpenRA.Game.exe
cp $WD/../mods/common/OpenRA.Mods.Common.dll $WD/OpenRA.Mods.Uncommon.dll
cp $WD/../mods/yupgi_alert/OpenRA.Mods.yupgi_alert.dll $WD

cd $WD
zip yupgi-$TRAVIS_COMMIT.zip OpenRA.Game.exe OpenRA.Mods.Uncommon.dll OpenRA.Mods.yupgi_alert.dll
