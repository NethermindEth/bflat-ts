# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)

# run_perf.c is local to this package: it builds the C# program, runs it
# under ziskemu with full trace (-v), then analyses startup steps.
executable('run_perf', 'run_perf.c',
           c_args: ['-DTE_TEST_NAME="run_perf/run_perf"'],
           install: true, install_dir: package_dir,
           dependencies: test_deps)

install_data(['package.xml',
              'zisk_startup_steps.py'],
             install_dir: package_dir)
install_subdir('cs', install_dir: package_dir)