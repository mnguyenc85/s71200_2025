# Mo phong
# Khi START -> MOTOR chay tien/lui
# Khi STOP -> MOTOR dung
# Trang thai: quang duong di, motor chay?, toc do
#                           byte addr
#   Start       DBX0.0      0
#   Stop        DBX0.1
#   Dir         DBX0.2
#   Motor       DBX1.0      1
#   Speed       DBD2        2
#   Distance    DBD6        6
#   ---------------------------------
#                           10 bytes

import snap7
from snap7.server import Server
from snap7.type import SrvArea
import ctypes
import time
import struct

def float2db(v, db, i):
    buf = struct.pack(">f", v)
    db[i]       = buf[0]
    db[i + 1]   = buf[1]
    db[i + 2]   = buf[2]
    db[i + 3]   = buf[3]

class Simulator:
    def __init__(self):
        self.spd = 0.05
        self.distance = 0.2
        self.start = False
        self.stop = False
        self.dir = True
        self.running = False

    def run(self, delta, db):
        # read input
        self.start = db[0] & 1 == 1
        self.stop = db[0] & 2 == 2
        self.dir = db[0] & 4 == 4

        # logic
        self.running = self.start and not self.stop

        if self.running:
            self.spd = (12 - abs(self.distance - 5)) * 0.01
            s = delta * self.spd
            if not self.dir:
                if self.distance + s < 10: self.distance += s
                else: self.distance = 10
            else:
                if self.distance - s > 0: self.distance -= s
                else: self.distance = 0
        else:
            self.spd = 0

        # write output
        if self.running: db[1] |= 1
        else: db[1] &= 14

        float2db(self.spd, db, 2)
        float2db(self.distance, db, 6)

sim = Simulator()
server = Server()

DB6 = (ctypes.c_ubyte * 10)()
server.register_area(SrvArea.DB, 6, DB6)

server.start_to("0.0.0.0")          # listen on all available network interfaces

print("Snap7 Server running... Press Ctrl+C to stop.")

try:
    while True:
        server.pick_event()  # Process client requests

        sim.run(0.01, DB6)

        time.sleep(0.01)

except KeyboardInterrupt:
    print("\nShutting down server...")
    server.stop()
    server.destroy()