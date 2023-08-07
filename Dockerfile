FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy

# install dependencies
RUN apt-get update --quiet \
 && apt-get install --no-install-recommends --yes --quiet \
    libfreetype6 \
    libgl1-mesa-dri \
    libgl1-mesa-glx \
    liblua5.1-0 \
    libopenal1 \
    libsdl2-2.0-0 \
    make \
    python3-minimal \
    zenity \
 && rm --recursive --force /var/lib/apt/lists/*

# restore NuGet packages
WORKDIR /nuget-packages
COPY . /nuget-packages
RUN make clean \
 && dotnet restore \
 && rm --recursive --force /nuget-packages/

WORKDIR /openra

CMD ["sh", "-c", "make; ./launch-game.sh"]
