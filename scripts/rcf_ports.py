#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Print two free TCP ports for the RCF of the two agents of a run, below the
# ephemeral range of Linux (32768 and up), where outgoing connections do not
# take them.
import random
import socket

socks = []
while len(socks) < 2:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.bind(("0.0.0.0", random.randrange(20000, 32768)))
        socks.append(s)
    except OSError:
        s.close()
print(" ".join(str(s.getsockname()[1]) for s in socks))
