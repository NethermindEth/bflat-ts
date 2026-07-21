# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)

# build_perf.c is local to this package (unlike other packages that share
# ../build.c) because it adds wall-clock timing around the build job.
executable('build_perf', 'build_perf.c',
           c_args: ['-DTE_TEST_NAME="build_perf/build_perf"'],
           install: true, install_dir: package_dir,
           dependencies: test_deps)

install_data(['package.xml'], install_dir: package_dir)
install_subdir('cs', install_dir: package_dir)