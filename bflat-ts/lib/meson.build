# Copyright (C) 2025 Demerzel Solutions Limited (Nethermind)

sources = [
    'tsapi_bflat.c',
    'ts_container.c',
    'tsapi_qemu.c',
    'tsapi_zisk.c',
]

ts_lib = static_library('ts_lib', sources,
                        dependencies: test_deps,
                        include_directories: lib_dir)