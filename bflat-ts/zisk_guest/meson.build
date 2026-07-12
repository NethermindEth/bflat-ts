# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)

fs = import('fs')

executable('build_guest', 'build_guest.c',
           install: true, install_dir: package_dir,
           dependencies: test_deps)

executable('zisk_guest', '../zisk_guest.c',
           install: true, install_dir: package_dir,
           dependencies: test_deps)

install_data(['package.xml'], install_dir: package_dir)

# inputs/ — pre-generated <block>.ssz inputs: version-prefixed SSZ payloads
# (schema:u16be | ssz_bytes) framed for ziskemu as len:u64le | bytes | padding
if fs.is_dir('inputs')
    install_subdir('inputs', install_dir: package_dir)
endif

# bin/ — pre-built nethermind ZiskGuest ELF (produced by scripts/build_nethermind_guest.sh)
if fs.is_dir('bin')
    install_subdir('bin', install_dir: package_dir)
endif