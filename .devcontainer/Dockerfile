# FROM mcr.microsoft.com/devcontainers/dotnet:0-6.0
FROM mcr.microsoft.com/devcontainers/base:debian-11

# Add backports.
RUN echo "deb http://deb.debian.org/debian bullseye-backports main" > /etc/apt/sources.list.d/backports.list

# Install dependencies.
RUN apt-get update && apt-get install -y --no-install-recommends \
  file \
  nsis \
  imagemagick \
  wine64 \
  shellcheck

# Install dotnet SDK and PowerShell.
RUN curl -SOL https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb \
  && rm packages-microsoft-prod.deb \
  && apt-get update \
  && apt-get install -y --no-install-recommends \
  dotnet-sdk-6.0 \
  powershell

# Install updated packages from backports.
RUN apt-get update && sudo apt-get install -y --no-install-recommends \
  curl/bullseye-backports \
  wine64/bullseye-backports

# Install AppImageTool.
RUN curl -SLO https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage \
  && install appimagetool-x86_64.AppImage /usr/local/bin \
  && rm appimagetool-x86_64.AppImage


# Download rcdedit.
USER vscode
RUN curl -SLo /home/vscode/rcedit-x64.exe https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x64.exe
ENV PATH=/usr/lib/wine/:$PATH
