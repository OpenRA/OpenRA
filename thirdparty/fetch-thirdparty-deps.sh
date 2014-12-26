#!/bin/bash
if [ ! -f StyleCop.dll ]; then
	echo "Fetching StyleCop files from nuget"
	nuget install StyleCop.MSBuild -Version 4.7.49.0
	cp ./StyleCop.MSBuild.4.7.49.0/tools/StyleCop*.dll .
	rm -rf StyleCop.MSBuild.4.7.49.0
fi

if [ ! -f ICSharpCode.SharpZipLib.dll ]; then
	echo "Fetching ICSharpCode.SharpZipLib from nuget"
	nuget install SharpZipLib -Version 0.86.0
	cp ./SharpZipLib.0.86.0/lib/20/ICSharpCode.SharpZipLib.dll .
	rm -rf SharpZipLib.0.86.0
fi