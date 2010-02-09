set MSBUILD="c:\windows\Microsoft.NET\Framework\v3.5\msbuild.exe"

call git submodule init
call git submodule update

pushd Ijw.DirectX
call git submodule init
call git submodule update

pushd Ijw.Framework
%MSBUILD% /t:Rebuild /p:Configuration=Debug IjwFramework.sln
%MSBUILD% /t:Rebuild /p:Configuration=Release IjwFramework.sln
popd

%MSBUILD% Ijw.DirectX.sln /p:Configuration=Debug
%MSBUILD% Ijw.DirectX.sln /p:Configuration=Release
popd

%MSBUILD% OpenRA.sln /t:Rebuild /p:Configuration=Debug
