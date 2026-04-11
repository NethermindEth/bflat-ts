#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
# Copyright (c) 2025-2026 Demerzel Solutions Limited (Nethermind)
#
# Profile a ziskemu execution log: map pc= values to bflat/.NET
# symbols and emit a self-contained HTML report.
#
# Usage:
#   zisk_profile.py --elf ELF [--log FILE] --out REPORT [--top N]
#   ziskemu -e prog 2>&1 | zisk_profile.py --elf prog --out report.html

import argparse
import bisect
import html as _html
import re
import shutil
import subprocess
import sys
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

NAMESPACE_ROOTS = frozenset({'System', 'Microsoft', 'Internal', 'Interop'})
PC_RE = re.compile(r'\bpc=(?:0[xX]([0-9a-fA-F]+)|(\d+))')


def die(msg: str) -> None:
    print(f'error: {msg}', file=sys.stderr)
    sys.exit(1)


def find_readelf() -> str:
    for cmd in ('readelf', 'llvm-readelf'):
        if shutil.which(cmd):
            return cmd
    die('neither readelf nor llvm-readelf found in PATH')


# ---------------------------------------------------------------------------
# Symbol table
# ---------------------------------------------------------------------------

def load_symbols(elf: str, readelf: str) -> tuple[list[int], list[str]]:
    """
    Run readelf -sW on *elf* and return (addrs, names) parallel sorted lists
    containing all FUNC symbols with a defined section index.
    """
    try:
        out = subprocess.run(
            [readelf, '-sW', elf],
            capture_output=True, text=True, check=True,
        ).stdout
    except subprocess.CalledProcessError as exc:
        die(f'readelf failed: {exc.stderr.strip()}')

    addrs: list[int] = []
    names: list[str] = []
    in_symtab = False

    for line in out.splitlines():
        if not in_symtab:
            if "Symbol table '.symtab'" in line:
                in_symtab = True
            continue
        if not line.strip():
            break

        parts = line.split()
        if len(parts) < 8 or not parts[0].endswith(':'):
            continue
        try:
            addr = int(parts[1], 16)
        except ValueError:
            continue
        if parts[3] != 'FUNC' or parts[6] == 'UND' or addr == 0:
            continue

        addrs.append(addr)
        names.append(parts[7])

    pairs = sorted(zip(addrs, names))
    deduped: list[tuple[int, str]] = []
    for pair in pairs:
        if not deduped or deduped[-1][0] != pair[0]:
            deduped.append(pair)

    if not deduped:
        return [], []

    a, n = zip(*deduped)
    return list(a), list(n)


def detect_text_base(elf: str, readelf: str) -> int:
    out = subprocess.run(
        [readelf, '-SW', elf], capture_output=True, text=True,
    ).stdout
    for line in out.splitlines():
        pos = line.find('] .text')
        if pos < 0:
            continue
        rest = line[pos + 7:].split()
        if rest:
            try:
                return int(rest[0], 16)
            except ValueError:
                pass
    return 0


# ---------------------------------------------------------------------------
# Log parsing
# ---------------------------------------------------------------------------

def count_steps(
    log_lines,
    addrs: list[int],
    names: list[str],
    text_base: int,
) -> tuple[dict[str, int], int]:
    """
    Scan *log_lines* for pc= fields, resolve each to a function via binary
    search on *addrs*, and return (func -> step_count, total_steps).
    """
    min_sym = addrs[0] if addrs else 0
    counts: dict[str, int] = defaultdict(int)
    total = 0

    for line in log_lines:
        m = PC_RE.search(line)
        if not m:
            continue

        pc = int(m.group(1), 16) if m.group(1) else int(m.group(2))
        if text_base and pc < min_sym:
            pc += text_base

        idx = bisect.bisect_right(addrs, pc) - 1
        if idx < 0:
            continue

        counts[names[idx]] += 1
        total += 1

    return dict(counts), total


# ---------------------------------------------------------------------------
# bflat / NativeAOT symbol name parsing
# ---------------------------------------------------------------------------

def _find_method_sep(s: str) -> int:
    """
    Return the index of the __ separator between type path and method name,
    skipping any __ that appear inside balanced <> generic argument lists.
    Returns -1 if no separator is found.
    """
    depth = 0
    i = 0
    n = len(s)
    while i < n - 1:
        c = s[i]
        if c == '<':
            depth += 1
        elif c == '>':
            depth -= 1
        elif c == '_' and s[i + 1] == '_' and depth == 0:
            return i
        i += 1
    return -1


def _strip_assembly(s: str) -> str:
    """
    Remove the S_P_<Assembly>_ prefix that bflat uses for CoreLib and other
    private assemblies.  Drops segments before the first known namespace root.
    """
    if not s.startswith('S_P_'):
        return s
    parts = s[4:].split('_')
    for i, p in enumerate(parts):
        if p in NAMESPACE_ROOTS:
            return '_'.join(parts[i:])
    return '_'.join(parts)


def _strip_generic_args(s: str) -> str:
    """Remove <...> type parameter lists (used to simplify the class key)."""
    result = []
    depth = 0
    for c in s:
        if c == '<':
            depth += 1
        elif c == '>':
            depth -= 1
        elif depth == 0:
            result.append(c)
    return ''.join(result)


_GENERIC_RE = re.compile(r'_(\d+)(?=[_.]|$)')


def _prettify_generics(s: str) -> str:
    """Replace _N generic arity markers with <T> / <T, T> etc."""
    def sub(m: re.Match) -> str:
        n = int(m.group(1))
        return '<' + ', '.join(['T'] * n) + '>'
    return _GENERIC_RE.sub(sub, s)


def parse_symbol(sym: str) -> tuple[str, str, str]:
    """
    Return (namespace, class_name, method_name) for a bflat/.NET symbol.

    C runtime and C++ mangled symbols (_Z...) go to the '[runtime]' bucket.
    Boxed value type helpers are marked with a [boxed] suffix on the class.
    """
    if sym.startswith('_'):
        return '[runtime]', sym, ''

    boxed = sym.startswith('<Boxed>')
    s = sym[len('<Boxed>'):] if boxed else sym

    sep = _find_method_sep(s)
    if sep < 0:
        return '', ('<Boxed>' if boxed else '') + s, ''

    type_path = s[:sep]
    method = s[sep + 2:]

    # Strip <T1, T2> instantiation args before namespace/class extraction —
    # we care about the generic type definition, not the specific instantiation.
    bare = _strip_generic_args(type_path)
    stripped = _strip_assembly(bare)
    dotted = _prettify_generics(stripped).replace('_', '.')

    dot = dotted.rfind('.')
    if dot >= 0:
        namespace = dotted[:dot]
        class_name = dotted[dot + 1:]
    else:
        namespace = ''
        class_name = dotted

    if boxed:
        class_name += ' [boxed]'

    return namespace, class_name, method


# ---------------------------------------------------------------------------
# HTML report
# ---------------------------------------------------------------------------

def _e(s) -> str:
    return _html.escape(str(s))


def _bar(pct: float) -> str:
    w = min(pct, 100.0)
    return (
        f'<div class="bar-wrap">'
        f'<div class="bar" style="width:{w:.1f}%"></div>'
        f'<span class="bar-lbl">{pct:.2f}%</span>'
        f'</div>'
    )


def _tr(*cols) -> str:
    return '<tr>' + ''.join(f'<td>{c}</td>' for c in cols) + '</tr>'


def generate_html(
    func_counts: dict[str, int],
    total: int,
    elf_path: str,
    top: int,
) -> str:
    now = datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')
    func_sorted = sorted(func_counts.items(),
                         key=lambda x: x[1], reverse=True)[:top]

    class_counts: dict[tuple[str, str], int] = defaultdict(int)
    class_methods: dict[tuple[str, str], set] = defaultdict(set)
    for sym, cnt in func_counts.items():
        ns, cls, meth = parse_symbol(sym)
        class_counts[(ns, cls)] += cnt
        class_methods[(ns, cls)].add(meth or sym)

    class_sorted = sorted(
        class_counts.items(), key=lambda x: x[1], reverse=True,
    )[:top]

    def func_row(rank: int, sym: str, cnt: int) -> str:
        ns, cls, meth = parse_symbol(sym)
        pct = 100.0 * cnt / total if total else 0.0
        return _tr(
            rank,
            f'<span class="sym" title="{_e(sym)}">{_e(meth or sym)}</span>',
            f'<span class="cls">{_e(cls)}</span>',
            f'<span class="ns">{_e(ns)}</span>',
            f'{cnt:,}',
            _bar(pct),
        )

    def class_row(rank: int, ns: str, cls: str, cnt: int) -> str:
        pct = 100.0 * cnt / total if total else 0.0
        n_methods = len(class_methods[(ns, cls)])
        return _tr(
            rank,
            f'<span class="cls">{_e(cls)}</span>',
            f'<span class="ns">{_e(ns)}</span>',
            f'{cnt:,}',
            _bar(pct),
            n_methods,
        )

    func_rows = '\n'.join(
        func_row(rank, sym, cnt)
        for rank, (sym, cnt) in enumerate(func_sorted, 1)
    )
    class_rows = '\n'.join(
        class_row(rank, ns, cls, cnt)
        for rank, ((ns, cls), cnt) in enumerate(class_sorted, 1)
    )

    elf_name = _e(Path(elf_path).name)
    elf_path_e = _e(elf_path)

    return (
        '<!DOCTYPE html>\n'
        '<html lang="en">\n'
        '<head>\n'
        '<meta charset="utf-8">\n'
        f'<title>zisk profile \u2014 {elf_name}</title>\n'
        '<style>\n'
        ':root {\n'
        '  --bg: #0f1117; --bg2: #1a1d27; --bg3: #252836;\n'
        '  --border: #2e3247; --accent: #5c7cfa; --accent2: #74c0fc;\n'
        '  --text: #c9d1d9; --muted: #6e7681; --green: #3fb950;\n'
        '}\n'
        '* { box-sizing: border-box; margin: 0; padding: 0 }\n'
        'body { background: var(--bg); color: var(--text);\n'
        '  font: 13px/1.5 "JetBrains Mono","Fira Code",ui-monospace,monospace;\n'
        '  padding: 24px }\n'
        'h1 { font-size: 18px; color: #fff; margin-bottom: 4px }\n'
        '.meta { color: var(--muted); font-size: 12px; margin-bottom: 20px }\n'
        '.meta b { color: var(--accent2); font-weight: normal }\n'
        '.tabs { display: flex; gap: 2px; margin-bottom: 14px;\n'
        '  border-bottom: 1px solid var(--border) }\n'
        '.tab { padding: 7px 18px; cursor: pointer; border: none;\n'
        '  background: transparent; color: var(--muted); font: inherit;\n'
        '  border-bottom: 2px solid transparent; margin-bottom: -1px;\n'
        '  transition: color .12s }\n'
        '.tab:hover { color: var(--text) }\n'
        '.tab.on { color: var(--accent); border-bottom-color: var(--accent) }\n'
        '.pane { display: none } .pane.on { display: block }\n'
        '.toolbar { display: flex; align-items: center; gap: 10px;\n'
        '  margin-bottom: 10px }\n'
        'input[type=search] {\n'
        '  background: var(--bg2); border: 1px solid var(--border);\n'
        '  color: var(--text); font: inherit; padding: 5px 10px;\n'
        '  border-radius: 5px; width: 300px; outline: none }\n'
        'input[type=search]:focus { border-color: var(--accent) }\n'
        '.lbl { color: var(--muted); font-size: 11px }\n'
        'table { width: 100%; border-collapse: collapse }\n'
        'thead tr { background: var(--bg3); position: sticky; top: 0; z-index: 1 }\n'
        'th { text-align: left; padding: 7px 10px; color: var(--muted);\n'
        '  font-weight: 600; font-size: 11px; cursor: pointer;\n'
        '  user-select: none; white-space: nowrap;\n'
        '  border-bottom: 1px solid var(--border) }\n'
        'th:hover { color: var(--text) }\n'
        'th.asc::after  { content: " \u2191"; color: var(--accent) }\n'
        'th.desc::after { content: " \u2193"; color: var(--accent) }\n'
        'td { padding: 5px 10px; border-bottom: 1px solid var(--border);\n'
        '  vertical-align: middle }\n'
        'tr:hover td { background: var(--bg2) }\n'
        '.hidden { display: none }\n'
        '.sym { color: var(--accent2) }\n'
        '.cls { color: var(--green) }\n'
        '.ns  { color: var(--muted); font-size: 11px }\n'
        '.bar-wrap { display: flex; align-items: center; gap: 8px;\n'
        '  min-width: 160px }\n'
        '.bar { height: 5px; background: var(--accent); border-radius: 3px;\n'
        '  min-width: 1px }\n'
        '.bar-lbl { color: var(--muted); font-size: 11px; white-space: nowrap }\n'
        '</style>\n'
        '</head>\n'
        '<body>\n'
        '<h1>zisk profile report</h1>\n'
        '<p class="meta">\n'
        f'  ELF: <b>{elf_path_e}</b> &nbsp;&middot;&nbsp;\n'
        f'  Total steps: <b>{total:,}</b> &nbsp;&middot;&nbsp;\n'
        f'  Unique functions: <b>{len(func_counts):,}</b> &nbsp;&middot;&nbsp;\n'
        f'  {now}\n'
        '</p>\n'
        '\n'
        '<div class="tabs">\n'
        '  <button class="tab on" data-pane="pane-fn">Functions</button>\n'
        '  <button class="tab"    data-pane="pane-cl">Classes</button>\n'
        '</div>\n'
        '\n'
        '<div id="pane-fn" class="pane on">\n'
        '  <div class="toolbar">\n'
        '    <input type="search" placeholder="Filter\u2026"'
        ' oninput="doFilter(this,\'fn\')">\n'
        '    <span class="lbl" id="lbl-fn"></span>\n'
        '  </div>\n'
        '  <table id="tbl-fn">\n'
        '    <thead><tr>\n'
        '      <th data-c="0">#</th>\n'
        '      <th data-c="1">Method</th>\n'
        '      <th data-c="2">Class</th>\n'
        '      <th data-c="3">Namespace</th>\n'
        '      <th data-c="4" class="desc">Steps</th>\n'
        '      <th data-c="5">%</th>\n'
        '    </tr></thead>\n'
        f'    <tbody>{func_rows}</tbody>\n'
        '  </table>\n'
        '</div>\n'
        '\n'
        '<div id="pane-cl" class="pane">\n'
        '  <div class="toolbar">\n'
        '    <input type="search" placeholder="Filter\u2026"'
        ' oninput="doFilter(this,\'cl\')">\n'
        '    <span class="lbl" id="lbl-cl"></span>\n'
        '  </div>\n'
        '  <table id="tbl-cl">\n'
        '    <thead><tr>\n'
        '      <th data-c="0">#</th>\n'
        '      <th data-c="1">Class</th>\n'
        '      <th data-c="2">Namespace</th>\n'
        '      <th data-c="3" class="desc">Steps</th>\n'
        '      <th data-c="4">%</th>\n'
        '      <th data-c="5">Methods</th>\n'
        '    </tr></thead>\n'
        f'    <tbody>{class_rows}</tbody>\n'
        '  </table>\n'
        '</div>\n'
        '\n'
        '<script>\n'
        '(() => {\n'
        '  document.querySelectorAll(\'.tab\').forEach(t =>\n'
        '    t.addEventListener(\'click\', () => {\n'
        '      document.querySelectorAll(\'.tab,.pane\')\n'
        '        .forEach(x => x.classList.remove(\'on\'));\n'
        '      t.classList.add(\'on\');\n'
        '      document.getElementById(t.dataset.pane).classList.add(\'on\');\n'
        '    }));\n'
        '\n'
        '  const state = {};\n'
        '\n'
        '  function numVal(td) {\n'
        '    const s = td.textContent.trim().replace(/,/g, \'\');\n'
        '    const n = parseFloat(s);\n'
        '    return isNaN(n) ? s.toLowerCase() : n;\n'
        '  }\n'
        '\n'
        '  function sortBy(id, col) {\n'
        '    const s = state[id];\n'
        '    const asc = s.col === col ? !s.asc : false;\n'
        '    s.col = col; s.asc = asc;\n'
        '    const tbl = document.getElementById(\'tbl-\' + id);\n'
        '    tbl.querySelectorAll(\'th\')\n'
        '      .forEach(h => h.classList.remove(\'asc\', \'desc\'));\n'
        '    tbl.querySelector(`th[data-c="${col}"]`)\n'
        '      .classList.add(asc ? \'asc\' : \'desc\');\n'
        '    const tbody = tbl.querySelector(\'tbody\');\n'
        '    [...tbody.rows]\n'
        '      .sort((a, b) => {\n'
        '        const av = numVal(a.cells[col]);\n'
        '        const bv = numVal(b.cells[col]);\n'
        '        return av < bv ? (asc ? -1 : 1) : av > bv ? (asc ? 1 : -1) : 0;\n'
        '      })\n'
        '      .forEach(r => tbody.appendChild(r));\n'
        '    renumber(tbody);\n'
        '  }\n'
        '\n'
        '  function doFilter(inp, id) {\n'
        '    const q = inp.value.toLowerCase();\n'
        '    const tbody = document.getElementById(\'tbl-\' + id)\n'
        '      .querySelector(\'tbody\');\n'
        '    let vis = 0;\n'
        '    [...tbody.rows].forEach(r => {\n'
        '      const hide = q && !r.textContent.toLowerCase().includes(q);\n'
        '      r.classList.toggle(\'hidden\', hide);\n'
        '      if (!hide) vis++;\n'
        '    });\n'
        '    setLabel(id, vis, tbody.rows.length);\n'
        '  }\n'
        '\n'
        '  function renumber(tbody) {\n'
        '    let i = 1;\n'
        '    [...tbody.rows].forEach(r => {\n'
        '      if (!r.classList.contains(\'hidden\'))\n'
        '        r.cells[0].textContent = i++;\n'
        '    });\n'
        '  }\n'
        '\n'
        '  function setLabel(id, vis, total) {\n'
        '    const el = document.getElementById(\'lbl-\' + id);\n'
        '    if (el) el.textContent = vis === total\n'
        '      ? total + \' entries\'\n'
        '      : vis + \' / \' + total + \' entries\';\n'
        '  }\n'
        '\n'
        '  [\'fn\', \'cl\'].forEach(id => {\n'
        '    state[id] = { col: -1, asc: false };\n'
        '    const tbl = document.getElementById(\'tbl-\' + id);\n'
        '    tbl.querySelectorAll(\'th\').forEach(th =>\n'
        '      th.addEventListener(\'click\', () => sortBy(id, +th.dataset.c)));\n'
        '    const n = tbl.querySelectorAll(\'tbody tr\').length;\n'
        '    setLabel(id, n, n);\n'
        '  });\n'
        '})();\n'
        '</script>\n'
        '</body>\n'
        '</html>\n'
    )


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    ap = argparse.ArgumentParser(
        description='Profile a ziskemu execution log and emit an HTML report.',
    )
    ap.add_argument('--elf', required=True, metavar='ELF',
                    help='path to the ELF binary')
    ap.add_argument('--log', metavar='FILE',
                    help='execution log (default: stdin)')
    ap.add_argument('--out', required=True, metavar='FILE',
                    help='output HTML report path')
    ap.add_argument('--top', type=int, default=200, metavar='N',
                    help='top N entries per table (default: 200)')
    args = ap.parse_args()

    if not Path(args.elf).is_file():
        die(f'ELF file not found: {args.elf}')

    readelf = find_readelf()

    print(f'[1/3] Loading symbols from {args.elf} ...', file=sys.stderr)
    addrs, names = load_symbols(args.elf, readelf)
    if not addrs:
        die('no FUNC symbols found in symbol table')

    text_base = detect_text_base(args.elf, readelf)

    print('[2/3] Parsing execution log ...', file=sys.stderr)
    if args.log:
        with open(args.log) as fh:
            func_counts, total = count_steps(fh, addrs, names, text_base)
    else:
        func_counts, total = count_steps(sys.stdin, addrs, names, text_base)

    if total == 0:
        die('no pc= entries found in the log')

    print(
        f'[3/3] Generating report'
        f' ({total:,} steps, {len(func_counts):,} functions) ...',
        file=sys.stderr,
    )
    Path(args.out).write_text(
        generate_html(func_counts, total, args.elf, args.top),
        encoding='utf-8',
    )
    print(f'report: {args.out}', file=sys.stderr)


if __name__ == '__main__':
    main()
