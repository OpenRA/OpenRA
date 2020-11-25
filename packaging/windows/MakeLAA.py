# Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
# This file is part of OpenRA, which is free software. It is made
# available to you under the terms of the GNU General Public License
# as published by the Free Software Foundation, either version 3 of
# the License, or (at your option) any later version. For more
# information, see COPYING.

import struct
import sys

if __name__ == "__main__":
    print(sys.argv[1] + ': Enabling /LARGEADDRESSAWARE')
    with open(sys.argv[1], 'r+b') as assembly:
        assembly.seek(0x3c)
        peOffset = struct.unpack('i', assembly.read(4))[0]
        assembly.seek(peOffset + 4 + 18)
        flags = struct.unpack('B', assembly.read(1))[0] | 0x20
        assembly.seek(peOffset + 4 + 18)
        assembly.write(struct.pack('B', flags))
