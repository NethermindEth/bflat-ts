#!/usr/bin/env python3
# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
"""Structural validation of the bflat-ts test-suite tree.

Checks, in order:
  1. every tracked XML file is well-formed (package.xml, conf/trc.xml,
     conf/tester.conf);
  2. the three package registries agree: subdirectories of bflat-ts/ that
     carry a package.xml, the packages list in bflat-ts/meson.build, and the
     <run><package> list in the root bflat-ts/package.xml;
  3. every package directory has a meson.build;
  4. inside each package.xml: run names are unique, every <run> references an
     existing run-template, every cs_file value exists on disk, and every
     cs/*.cs file is registered by exactly one run (no orphan test sources);
  5. conf/trc.xml entries that reference cs_file values which no longer exist
     are reported as warnings (TRC intentionally lags behind the tree).

Errors are fatal (exit 1); warnings are informational only.
"""

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

TOP = Path(__file__).resolve().parent.parent
SUITE = TOP / "bflat-ts"

errors = []
warnings = []


def error(msg):
    errors.append(msg)


def warn(msg):
    warnings.append(msg)


def parse_xml(path):
    try:
        return ET.parse(path)
    except ET.ParseError as e:
        error(f"{path.relative_to(TOP)}: XML parse error: {e}")
        return None


def package_dirs():
    return sorted(d for d in SUITE.iterdir()
                  if d.is_dir() and (d / "package.xml").is_file())


def meson_packages():
    """Extract the packages = [...] list from bflat-ts/meson.build."""
    text = (SUITE / "meson.build").read_text()
    m = re.search(r"packages\s*=\s*\[(.*?)\]", text, re.S)
    if m is None:
        error("bflat-ts/meson.build: could not find the packages = [...] list")
        return []
    return re.findall(r"'([^']+)'", m.group(1))


def root_run_packages(tree):
    return [pkg.get("name")
            for run in tree.getroot().iter("run")
            for pkg in run.findall("package")]


def check_registries():
    dirs = {d.name for d in package_dirs()}
    meson = meson_packages()
    meson_set = set(meson)

    root_tree = parse_xml(SUITE / "package.xml")
    root_list = root_run_packages(root_tree) if root_tree is not None else []
    root_set = set(root_list)

    for name in sorted(dirs - meson_set):
        error(f"package '{name}' has a package.xml but is missing from the "
              f"packages list in bflat-ts/meson.build")
    for name in sorted(meson_set - dirs):
        error(f"bflat-ts/meson.build lists package '{name}' but "
              f"bflat-ts/{name}/package.xml does not exist")
    for name in sorted(dirs - root_set):
        error(f"package '{name}' is not registered in the run list of "
              f"bflat-ts/package.xml")
    for name in sorted(root_set - dirs):
        error(f"bflat-ts/package.xml runs package '{name}' but "
              f"bflat-ts/{name}/package.xml does not exist")

    if len(meson) != len(meson_set):
        error("bflat-ts/meson.build packages list contains duplicates")
    if len(root_list) != len(root_set):
        error("bflat-ts/package.xml run list contains duplicate packages")

    for d in package_dirs():
        if not (d / "meson.build").is_file():
            error(f"package '{d.name}' has no meson.build")


def run_cs_files(run):
    """cs_file values referenced by a <run> element."""
    for arg in run.findall("arg"):
        if arg.get("name") == "cs_file":
            for value in arg.findall("value"):
                if value.text:
                    yield value.text.strip()


def check_package(pkg_dir):
    rel = pkg_dir.relative_to(TOP)
    tree = parse_xml(pkg_dir / "package.xml")
    if tree is None:
        return
    session = tree.getroot().find("session")
    if session is None:
        error(f"{rel}/package.xml: no <session> element")
        return

    templates = {t.get("name") for t in session.findall("run-template")}
    run_names = []
    referenced = set()

    for run in session.findall("run"):
        name = run.get("name")
        run_names.append(name)
        template = run.get("template")
        if template is not None and template not in templates:
            error(f"{rel}/package.xml: run '{name}' references unknown "
                  f"run-template '{template}'")
        for cs in run_cs_files(run):
            referenced.add(cs)
            if not (pkg_dir / "cs" / cs).is_file():
                error(f"{rel}/package.xml: run '{name}' references missing "
                      f"file cs/{cs}")

    seen = set()
    for name in run_names:
        if name in seen:
            error(f"{rel}/package.xml: duplicate run name '{name}'")
        seen.add(name)

    cs_dir = pkg_dir / "cs"
    if cs_dir.is_dir():
        for cs in sorted(p.name for p in cs_dir.glob("*.cs")):
            if cs not in referenced:
                error(f"{rel}/cs/{cs} is not registered by any run in "
                      f"package.xml")


def check_trc():
    trc = TOP / "conf" / "trc.xml"
    tree = parse_xml(trc)
    if tree is None:
        return

    known = {}
    for d in package_dirs():
        cs_dir = d / "cs"
        known[d.name] = ({p.name for p in cs_dir.glob("*.cs")}
                         if cs_dir.is_dir() else set())

    root = tree.getroot()

    def visit(elem, package):
        for test in elem.findall("test"):
            name = test.get("name")
            if test.get("type") == "package" and name != "bflat-ts":
                if name not in known:
                    warn(f"conf/trc.xml: package '{name}' no longer exists")
                for it in test.iter():
                    visit_pkg_args(it, name)
            else:
                for it in test.findall("iter"):
                    visit(it, package)

    def visit_pkg_args(elem, package):
        if elem.tag != "arg" or elem.get("name") != "cs_file":
            return
        cs = (elem.text or "").strip()
        if cs and package in known and cs not in known[package]:
            warn(f"conf/trc.xml: package '{package}' references stale "
                 f"cs_file '{cs}'")

    for it in root.findall("iter"):
        visit(it, None)
    visit(root, None)


def main():
    if not SUITE.is_dir():
        print(f"error: {SUITE} not found", file=sys.stderr)
        return 1

    parse_xml(TOP / "conf" / "tester.conf")
    check_registries()
    for pkg_dir in package_dirs():
        check_package(pkg_dir)
    check_trc()

    for msg in warnings:
        print(f"warning: {msg}")
    for msg in errors:
        print(f"error: {msg}")

    n_pkgs = len(package_dirs())
    if errors:
        print(f"\nFAIL: {len(errors)} error(s), {len(warnings)} warning(s) "
              f"across {n_pkgs} packages")
        return 1
    print(f"OK: {n_pkgs} packages checked, {len(warnings)} warning(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
