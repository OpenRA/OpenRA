#!/bin/bash

# this script installs prerequisites to build binary files on Linux with deb

sudo apt-get install git-core
sudo apt-get install g++
sudo apt-get install subversion
sudo apt-get install libgtk2.0-dev
sudo apt-get install cmake
exit

# install cmake as wxwidgets needs 2.8.4+ but "sudo apt-get install cmake" 
# only installs 2.8.0 on some systems (like Ubuntu 10.4)
 mkdir build-cmake
 cd build-cmake
 wget http://www.cmake.org/files/v2.8/cmake-2.8.10.2.tar.gz
 gunzip cmake-2.8.10.2.tar.gz 
 tar xvf cmake-2.8.10.2.tar 
 cd cmake-2.8.10.2
 ./bootstrap
 make
 sudo make install
 cd ../..
 rm -rf build-cmake
