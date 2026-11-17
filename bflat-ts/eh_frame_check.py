#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Check the unwind info of a RISC-V ELF against the code it describes.
#
# For every FDE of .eh_frame the prolog of its function is decoded up to the
# first branch, call or return, or to where the stack is released again, and
# the stack allocated by then is compared with the CFA offset the FDE gives
# for that address. With the CFA based on SP the offset must cover the whole
# allocation: an unwinder finds the return address and the saved registers
# relative to the CFA. A CFA on another register (a frame pointer) does not
# depend on the allocation and is not compared.
#
# A method with a frame over 2040 bytes allocates it in two steps. One
# without a frame pointer keeps its CFA on SP, so both steps have to be in
# its FDE; a missing second step is what this check is for, and it fails on
# a managed method (one with an LSDA) whose CFA offset is less than what the
# prolog allocated. Other disagreements are in hand-written stubs, which
# describe their frame ahead of allocating it or not at all; they are listed
# with --verbose and do not fail the check.
#
# Usage:
#   eh_frame_check.py ELF [--min-split N] [--verbose]
#
#   --min-split N  fail unless at least N functions keep their CFA on SP and
#                  allocate more than 2040 bytes in two or more steps, so
#                  that a run where no such function exists is not a pass
#
# Exit status: 0 the unwind info covers what the managed code allocates,
# 1 it does not (or there are fewer split frames than required), 2 the file
# cannot be checked.
#
# Requirements: Python 3.8+, no other tools.

import argparse
import bisect
import struct
import sys

SP = 2
LARGE = 2040
MAX_INSNS = 96


class Unsupported(Exception):
    pass


class Elf:
    def __init__(self, path):
        with open(path, "rb") as f:
            self.data = f.read()
        d = self.data
        if d[:4] != b"\x7fELF":
            raise Unsupported("not an ELF file")
        if d[5] != 1:
            raise Unsupported("not little-endian")
        self.is64 = d[4] == 2
        self.ptr = 8 if self.is64 else 4
        if self.is64:
            shoff, = struct.unpack_from("<Q", d, 0x28)
            shentsize, shnum, shstrndx = struct.unpack_from("<HHH", d, 0x3A)
        else:
            shoff, = struct.unpack_from("<I", d, 0x20)
            shentsize, shnum, shstrndx = struct.unpack_from("<HHH", d, 0x2E)
        machine, = struct.unpack_from("<H", d, 0x12)
        if machine != 243:
            raise Unsupported("not a RISC-V file (e_machine %d)" % machine)

        raw = []
        for i in range(shnum):
            o = shoff + i * shentsize
            if self.is64:
                name, typ, flags, addr, off, size = struct.unpack_from("<IIQQQQ", d, o)
            else:
                name, typ, flags, addr, off, size = struct.unpack_from("<IIIIII", d, o)
            raw.append((name, typ, flags, addr, off, size))
        if not raw:
            raise Unsupported("no section headers")
        stroff = raw[shstrndx][4]
        self.sections = {}
        self.code = []
        for name, typ, flags, addr, off, size in raw:
            end = d.index(b"\0", stroff + name)
            sname = d[stroff + name:end].decode()
            self.sections[sname] = (addr, off, size)
            # SHT_PROGBITS, SHF_ALLOC | SHF_EXECINSTR
            if typ == 1 and (flags & 0x6) == 0x6:
                self.code.append((addr, off, size))
        self.code.sort()

    def insn(self, pc):
        """The instruction at pc as (value, length), or None outside the code."""
        for addr, off, size in self.code:
            if addr <= pc < addr + size:
                o = off + (pc - addr)
                lo, = struct.unpack_from("<H", self.data, o)
                if lo & 3 != 3:
                    return lo, 2
                if pc + 4 > addr + size:
                    return None
                return struct.unpack_from("<I", self.data, o)[0], 4
        return None


class Reader:
    def __init__(self, data, pos, vaddr, ptr):
        self.data, self.pos, self.vaddr, self.ptr = data, pos, vaddr, ptr

    def u(self, n):
        v = int.from_bytes(self.data[self.pos:self.pos + n], "little")
        self.pos += n
        return v

    def s(self, n):
        v = int.from_bytes(self.data[self.pos:self.pos + n], "little", signed=True)
        self.pos += n
        return v

    def uleb(self):
        v = shift = 0
        while True:
            b = self.data[self.pos]
            self.pos += 1
            v |= (b & 0x7F) << shift
            shift += 7
            if not b & 0x80:
                return v

    def sleb(self):
        v = shift = 0
        while True:
            b = self.data[self.pos]
            self.pos += 1
            v |= (b & 0x7F) << shift
            shift += 7
            if not b & 0x80:
                if b & 0x40:
                    v -= 1 << shift
                return v

    def cstr(self):
        end = self.data.index(b"\0", self.pos)
        v = self.data[self.pos:end].decode()
        self.pos = end + 1
        return v

    def encoded(self, enc):
        """A DW_EH_PE pointer."""
        if enc == 0xFF:
            return None
        here = self.vaddr + self.pos
        fmt, app = enc & 0x0F, enc & 0x70
        if fmt == 0x00:
            v = self.u(self.ptr)
        elif fmt == 0x01:
            v = self.uleb()
        elif fmt == 0x02:
            v = self.u(2)
        elif fmt == 0x03:
            v = self.u(4)
        elif fmt == 0x04:
            v = self.u(8)
        elif fmt == 0x09:
            v = self.sleb()
        elif fmt == 0x0A:
            v = self.s(2)
        elif fmt == 0x0B:
            v = self.s(4)
        elif fmt == 0x0C:
            v = self.s(8)
        else:
            raise Unsupported("pointer format 0x%x" % fmt)
        if app == 0x10:
            v += here
        elif app != 0:
            raise Unsupported("pointer application 0x%x" % app)
        return v & ((1 << (8 * self.ptr)) - 1)


def run_cfa(r, end, cie, loc, state, rows):
    """Run CFA instructions up to end, appending (address, register, offset)
    to rows whenever the CFA changes. Register None: not register + offset."""
    stack = []
    caf, daf = cie["caf"], cie["daf"]

    def emit():
        rows.append((loc, state[0], state[1]))

    while r.pos < end:
        op = r.u(1)
        hi, lo = op & 0xC0, op & 0x3F
        if hi == 0x40:                  # advance_loc
            loc += lo * caf
        elif hi == 0x80:                # offset
            r.uleb()
        elif hi == 0xC0:                # restore
            pass
        elif op == 0x00:                # nop
            pass
        elif op == 0x01:                # set_loc
            loc = r.encoded(cie["fde_enc"])
        elif op == 0x02:
            loc += r.u(1) * caf
        elif op == 0x03:
            loc += r.u(2) * caf
        elif op == 0x04:
            loc += r.u(4) * caf
        elif op in (0x05, 0x14):        # offset_extended, val_offset
            r.uleb(); r.uleb()
        elif op in (0x06, 0x07, 0x08):  # restore_extended, undefined, same_value
            r.uleb()
        elif op == 0x09:                # register
            r.uleb(); r.uleb()
        elif op == 0x0A:                # remember_state
            stack.append(tuple(state))
        elif op == 0x0B:                # restore_state
            state[0], state[1] = stack.pop()
            emit()
        elif op == 0x0C:                # def_cfa
            state[0], state[1] = r.uleb(), r.uleb()
            emit()
        elif op == 0x0D:                # def_cfa_register
            state[0] = r.uleb()
            emit()
        elif op == 0x0E:                # def_cfa_offset
            state[1] = r.uleb()
            emit()
        elif op == 0x0F:                # def_cfa_expression
            r.pos += r.uleb()
            state[0], state[1] = None, 0
            emit()
        elif op in (0x10, 0x16):        # expression, val_expression
            r.uleb()
            r.pos += r.uleb()
        elif op in (0x11, 0x15):        # offset_extended_sf, val_offset_sf
            r.uleb(); r.sleb()
        elif op == 0x12:                # def_cfa_sf
            state[0], state[1] = r.uleb(), r.sleb() * daf
            emit()
        elif op == 0x13:                # def_cfa_offset_sf
            state[1] = r.sleb() * daf
            emit()
        elif op == 0x2E:                # GNU_args_size
            r.uleb()
        elif op == 0x2F:                # GNU_negative_offset_extended
            r.uleb(); r.uleb()
        else:
            raise Unsupported("CFA opcode 0x%02x" % op)
    return loc


def parse_eh_frame(elf):
    """All FDEs as (begin, end, rows, has an LSDA)."""
    if ".eh_frame" not in elf.sections:
        raise Unsupported("no .eh_frame section")
    addr, off, size = elf.sections[".eh_frame"]
    data = elf.data[off:off + size]
    cies, fdes = {}, []
    pos = 0
    while pos + 4 <= len(data):
        r = Reader(data, pos, addr, elf.ptr)
        length = r.u(4)
        if length == 0:
            break
        if length == 0xFFFFFFFF:
            length = r.u(8)
        start = r.pos
        end = start + length
        cie_id = r.u(4)
        if cie_id == 0:
            cie = {"fde_enc": 0, "z": False, "lsda": False}
            version = r.u(1)
            aug = r.cstr()
            if "eh" in aug:
                r.u(elf.ptr)
            cie["caf"] = r.uleb()
            cie["daf"] = r.sleb()
            if version == 1:
                r.u(1)
            else:
                r.uleb()
            if aug.startswith("z"):
                cie["z"] = True
                auglen = r.uleb()
                augend = r.pos + auglen
                for ch in aug[1:]:
                    if ch == "R":
                        cie["fde_enc"] = r.u(1)
                    elif ch == "P":
                        r.encoded(r.u(1))
                    elif ch == "L":
                        cie["lsda"] = r.u(1) != 0xFF
                r.pos = augend
            cie["init"] = (r.pos, end)
            cies[pos] = cie
        else:
            cie = cies.get(start - cie_id)
            if cie is None:
                raise Unsupported("FDE at 0x%x has no CIE" % pos)
            begin = r.encoded(cie["fde_enc"])
            span = r.encoded(cie["fde_enc"] & 0x0F)
            lsda = False
            if cie["z"]:
                auglen = r.uleb()
                lsda = cie["lsda"] and auglen > 0
                r.pos += auglen
            state, rows = [None, 0], []
            ir = Reader(data, cie["init"][0], addr, elf.ptr)
            run_cfa(ir, cie["init"][1], cie, begin, state, rows)
            if not rows:
                rows.append((begin, state[0], state[1]))
            run_cfa(r, end, cie, begin, state, rows)
            if span:
                fdes.append((begin, begin + span, rows, lsda))
        pos = end
    return fdes


def sext(v, bits):
    v &= (1 << bits) - 1
    return v - (1 << bits) if v >> (bits - 1) else v


def step(insn, length, pc, regs, xlen):
    """Decode one instruction. Returns (kind, value):
    'sp' with the change of SP, 'stop' with the reason, or 'none'."""

    def kill(rd):
        if rd == SP:
            return "stop", "writes sp"
        if rd:
            regs.pop(rd, None)
        return "none", 0

    def setreg(rd, v):
        if rd == SP:
            return "stop", "writes sp"
        if rd:
            regs[rd] = sext(v, xlen)
        return "none", 0

    if length == 2:
        op, f3 = insn & 3, (insn >> 13) & 7
        rd = (insn >> 7) & 31
        rs2 = (insn >> 2) & 31
        if op == 1:
            imm6 = sext(((insn >> 7) & 0x20) | ((insn >> 2) & 0x1F), 6)
            if f3 == 0:                         # c.addi
                if rd == SP:
                    return "sp", imm6
                if rd in regs:
                    return setreg(rd, regs[rd] + imm6)
                return kill(rd)
            if f3 == 1:                         # c.jal (rv32), c.addiw (rv64)
                if xlen == 32:
                    return "stop", "call"
                return kill(rd)
            if f3 == 2:                         # c.li
                return setreg(rd, imm6)
            if f3 == 3:
                if rd == SP:                    # c.addi16sp
                    imm = sext(((insn >> 3) & 0x200) | ((insn >> 2) & 0x10) |
                               ((insn << 1) & 0x40) | ((insn << 4) & 0x180) |
                               ((insn << 3) & 0x20), 10)
                    return "sp", imm
                return setreg(rd, imm6 << 12)   # c.lui
            if f3 == 4:
                return "none", 0                # rd' is x8..x15
            return "stop", "branch"             # c.j, c.beqz, c.bnez
        if op == 0:
            return "none", 0                    # rd' is x8..x15
        if f3 == 4:
            if not (insn >> 12) & 1:
                if rs2 == 0:
                    return "stop", "jump"       # c.jr
                return kill(rd)                 # c.mv
            if rs2 == 0:
                return "stop", "jump"           # c.jalr, c.ebreak
            return kill(rd)                     # c.add
        if f3 in (0, 1, 2, 3):
            return kill(rd)                     # c.slli, c.*sp loads
        return "none", 0                        # c.*sp stores

    opc = insn & 0x7F
    rd, rs1, rs2 = (insn >> 7) & 31, (insn >> 15) & 31, (insn >> 20) & 31
    f3, f7 = (insn >> 12) & 7, insn >> 25
    imm_i = sext(insn >> 20, 12)

    if opc == 0x13 and f3 == 0:                 # addi
        if rd == SP:
            return ("sp", imm_i) if rs1 == SP else ("stop", "writes sp")
        if rs1 in regs:
            return setreg(rd, regs[rs1] + imm_i)
        return kill(rd)
    if opc == 0x1B and f3 == 0:                 # addiw
        if rs1 in regs:
            return setreg(rd, sext(regs[rs1] + imm_i, 32))
        return kill(rd)
    if opc == 0x37:                             # lui
        return setreg(rd, sext(insn & 0xFFFFF000, 32))
    if opc == 0x33 and f3 == 0 and f7 in (0x00, 0x20) and rd == SP:
        if rs1 == SP and rs2 in regs:           # add/sub sp, sp, reg
            return "sp", regs[rs2] if f7 == 0 else -regs[rs2]
        if f7 == 0 and rs2 == SP and rs1 in regs:
            return "sp", regs[rs1]
        return "stop", "writes sp"
    if opc == 0x63:                             # branch: a probe loop goes back
        target = sext(((insn >> 31) << 12) | (((insn >> 7) & 1) << 11) |
                      (((insn >> 25) & 0x3F) << 5) | (((insn >> 8) & 0xF) << 1), 13)
        return ("none", 0) if target < 0 else ("stop", "branch")
    if opc in (0x6F, 0x67):
        return "stop", "jump"
    if opc == 0x73:
        return "stop", "system"
    if opc in (0x23, 0x27):                     # stores
        return "none", 0
    if opc in (0x07, 0x43, 0x47, 0x4B, 0x4F, 0x53) and not (opc == 0x53 and f7 in (0x50, 0x51, 0x60, 0x61, 0x70, 0x71)):
        return "none", 0                        # floating-point destinations
    return kill(rd)


def check_fde(elf, begin, end, rows):
    """Returns (class, allocated, steps, pc, CFA offset). Classes: ok (the CFA
    is on SP and covers the allocation), fp (the CFA is on another register),
    behind and ahead (the CFA offset is less or more than the allocation)."""
    xlen = 64 if elf.is64 else 32
    regs = {0: 0}
    allocated, steps = 0, []
    pc, count = begin, 0
    while pc < end and count < MAX_INSNS:
        got = elf.insn(pc)
        if got is None:
            break
        insn, length = got
        kind, value = step(insn, length, pc, regs, xlen)
        if kind == "stop" or (kind == "sp" and value > 0):
            break
        if kind == "sp":
            allocated -= value
            steps.append((pc, -value))
        pc += length
        count += 1

    locs = [row[0] for row in rows]
    i = bisect.bisect_right(locs, pc) - 1
    reg, off = (rows[i][1], rows[i][2]) if i >= 0 else (None, 0)
    if reg != SP:
        cls = "fp"
    elif off == allocated:
        cls = "ok"
    else:
        cls = "behind" if off < allocated else "ahead"
    return cls, allocated, steps, pc, off


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0] if __doc__ else None)
    ap.add_argument("elf")
    ap.add_argument("--min-split", type=int, default=0)
    ap.add_argument("--verbose", action="store_true")
    args = ap.parse_args()

    try:
        elf = Elf(args.elf)
        fdes = parse_eh_frame(elf)
    except (Unsupported, OSError, struct.error, IndexError, ValueError) as e:
        print("eh_frame_check: %s: %s" % (args.elf, e))
        return 2

    counts = {"ok": 0, "fp": 0, "behind": 0, "ahead": 0}
    split = 0
    bad, notes = [], []
    for begin, end, rows, lsda in fdes:
        cls, allocated, steps, pc, off = check_fde(elf, begin, end, rows)
        counts[cls] += 1
        how = "+".join(str(n) for _, n in steps) or "nothing"
        if cls in ("behind", "ahead"):
            line = ("function 0x%x-0x%x: by 0x%x the code has allocated %d bytes (%s), "
                    "the unwind info says CFA = sp + %d" % (begin, end, pc, allocated, how, off))
            if cls == "behind" and lsda:
                bad.append(line)
            else:
                notes.append(line)
        elif cls == "ok" and allocated > LARGE and len(steps) > 1:
            split += 1
            if args.verbose:
                print("split frame 0x%x-0x%x: %d bytes (%s)" % (begin, end, allocated, how))

    for line in bad:
        print("MISMATCH " + line)
    if args.verbose:
        for line in notes:
            print("note: " + line)

    print("eh_frame_check: %d FDEs: %d with the CFA on SP agree with the code, %d have it on "
          "another register, %d managed MISMATCH, %d other disagreements; %d split frames "
          "over %d bytes with the CFA on SP" %
          (len(fdes), counts["ok"], counts["fp"], len(bad), len(notes), split, LARGE))

    if bad:
        return 1
    if split < args.min_split:
        print("eh_frame_check: expected at least %d split frames with the CFA on SP, found %d"
              % (args.min_split, split))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
