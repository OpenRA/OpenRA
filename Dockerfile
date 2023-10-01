# This file permits OpenRA to be built on Windows without the need to install
# any local development tools or SDKs. From the repository root, build using:
#
# docker build -t openra .
# 
# Then open the container using:
#
# docker run -it -v .:C:\OpenRA openra
#
# You can now run `./make.cmd`: output will be shared to the host.

FROM mcr.microsoft.com/windows/servercore:ltsc2022

RUN setx path "%path%;C:\Users\ContainerAdministrator\AppData\Local\Microsoft\dotnet"
SHELL ["powershell", "-command"]

RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -UseBasicParsing -OutFile dotnet-install.ps1
RUN ./dotnet-install.ps1

WORKDIR C:/OpenRA
ENTRYPOINT ["powershell"]
