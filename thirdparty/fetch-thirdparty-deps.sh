#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

if [ ! -f StyleCopPlus.dll ]; then
	echo "Fetching StyleCopPlus from nuget"
	nuget install StyleCopPlus.MSBuild -Version 4.7.49.5
	cp ./StyleCopPlus.MSBuild.4.7.49.5/tools/StyleCopPlus.dll .
	rm -rf StyleCopPlus.MSBuild.4.7.49.5
fi

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

if [ ! -f MaxMind.GeoIP2.dll ]; then
	echo "Fetching MaxMind.GeoIP2 from nuget"
	nuget install MaxMind.GeoIP2 -Version 2.1.0
	cp ./MaxMind.Db.1.0.0.0/lib/net40/MaxMind.Db.* .
	rm -rf MaxMind.Db.1.0.0.0
	cp ./MaxMind.GeoIP2.2.1.0.0/lib/net40/MaxMind.GeoIP2* .
	rm -rf MaxMind.GeoIP2.2.1.0.0
	cp ./Newtonsoft.Json.6.0.5/lib/net40/Newtonsoft.Json* .
	rm -rf Newtonsoft.Json.6.0.5
	cp ./RestSharp.105.0.0/lib/net4-client/RestSharp* .
	rm -rf RestSharp.105.0.0
fi

if [ ! -f SharpFont.dll ]; then
	echo "Fetching SharpFont from nuget"
	nuget install SharpFont -Version 2.5.0.1
	cp ./SharpFont.2.5.0.1/lib/net20/SharpFont* .
	sed '/osx/s@\(dll="\)[^"]*\(" />\)@\1/Library/Frameworks/Mono.framework/Libraries/libfreetype.6.dylib\2@' ./SharpFont.2.5.0.1/Content/SharpFont.dll.config > SharpFont.dll.config
	rm -rf SharpFont.2.5.0.1
fi

if [ ! -f nunit.framework.dll ]; then
	echo "Fetching NUnit from nuget"
	nuget install NUnit -Version 2.6.4
	cp ./NUnit.2.6.4/lib/nunit.framework* .
	rm -rf NUnit.2.6.4
fi

if [ ! -f Mono.Nat.dll ]; then
	echo "Fetching Mono.Nat from nuget"
	nuget install Mono.Nat -Version 1.2.21
	cp ./Mono.Nat.1.2.21.0/lib/net40/Mono.Nat.dll .
	rm -rf Mono.Nat.1.2.21.0
fi

if [ ! -f Moq.dll ]; then
	echo "Fetching Moq from NuGet."
	nuget install Moq -Version 4.2.1502.0911
	cp ./Moq.4.2.1502.0911/lib/net40/Moq.dll .
	rm -rf Moq.4.2.1502.0911
fi

if [ ! -f FuzzyLogicLibrary.dll ]; then
	echo "Fetching FuzzyLogicLibrary from NuGet."
	nuget install FuzzyLogicLibrary -Version 1.2.0
	cp ./FuzzyLogicLibrary.1.2.0/bin/Release/FuzzyLogicLibrary.dll .
	rm -rf FuzzyLogicLibrary.1.2.0
fi
