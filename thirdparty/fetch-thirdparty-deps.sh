#!/bin/bash
if [ ! -f StyleCop.dll ]; then
	echo "Fetching StyleCop files from nuget"
	nuget install StyleCop.MSBuild -Version 4.7.49.0
	cp ./StyleCop.MSBuild.4.7.49.0/tools/StyleCop*.dll .
	rm -rf StyleCop.MSBuild.4.7.49.0
fi