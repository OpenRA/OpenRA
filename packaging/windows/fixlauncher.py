# Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
# This file is part of OpenRA, which is free software. It is made
# available to you under the terms of the GNU General Public License
# as published by the Free Software Foundation, either version 3 of
# the License, or (at your option) any later version. For more
# information, see COPYING.

import struct
import sys

if __name__ == "__main__":
    print('Patching ' + sys.argv[1] + ':')
    with open(sys.argv[1], 'r+b') as assembly:
        assembly.seek(0x3c)
        peOffset = struct.unpack('H', assembly.read(2))[0]

        assembly.seek(peOffset)
        peSignature = struct.unpack('I', assembly.read(4))[0]
        if peSignature != 0x4550:
            print("   ERROR: Invalid PE signature")

        print(' - Setting /LARGEADDRESSAWARE')
        assembly.seek(peOffset + 4 + 18)
        flags = struct.unpack('B', assembly.read(1))[0] | 0x20
        assembly.seek(peOffset + 4 + 18)
        assembly.write(struct.pack('B', flags))
        print(' - Setting /subsystem:windows')
        assembly.seek(peOffset + 0x5C)
        assembly.write(struct.pack("H", 0x02))
