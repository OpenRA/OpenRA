#!/bin/sh

# Die on any error for Travis CI to automatically retry:
set -e
if [ ! -e ./Support/Content/ra/conquer.mix ]; then
	echo "Downloading RA mod content"
	mkdir -p ./Support/Content/ra/
	cd ./Support/Content/ra/
	curl -s -L -O `curl -s -L http://www.openra.net/packages/ra-mirrors.txt | head -n1`
	unzip ra-packages.zip
	rm ra-packages.zip
	cd ../../../
fi

if [ ! -e ./Support/Content/cnc/conquer.mix ]; then
	echo "Downloading TD mod content"
	mkdir -p ./Support/Content/cnc/
	cd ./Support/Content/cnc/
	curl -s -L -O `curl -s -L http://www.openra.net/packages/cnc-mirrors.txt | head -n1`
	unzip cnc-packages.zip
	rm cnc-packages.zip
	cd ../../../
fi

if [ ! -e ./Support/Content/d2k/DATA.R8 ]; then
	echo "Downloading D2K mod content"
	mkdir -p ./Support/Content/d2k/
	cd ./Support/Content/d2k/
	curl -s -L -O `curl -s -L http://www.openra.net/packages/d2k-103-mirrors.txt | head -n1`
	unzip d2k-103-packages.zip
	rm d2k-103-packages.zip
	cd ../../../
fi

if [ ! -e ./Support/Content/ts/conquer.mix ]; then
	echo "Downloading TS mod content"
	mkdir -p ./Support/Content/ts/
	cd ./Support/Content/ts/
	curl -s -L -O `curl -s -L http://www.openra.net/packages/ts-mirrors.txt | head -n1`
	unzip ts-packages.zip
	rm ts-packages.zip
	cd ../../../
fi
