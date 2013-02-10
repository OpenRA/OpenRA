#!/bin/bash

echo "Installing wxlua modules from ZeroBrane Studio repository..."
sudo add-apt-repository ppa:zerobranestudio/zerobranestudio
sudo apt-get update
sudo apt-get install wxlua28

# To remove wxlua and required packages use:
# sudo apt-get purge wxlua28
# sudo apt-get autoremove
