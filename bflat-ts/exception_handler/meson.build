# Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)

# build.c lives one level up and is shared across test packages
executable('build', '../build.c',
           c_args: ['-DTE_TEST_NAME="exception_handler/build"'],
           install: true, install_dir: package_dir,
           dependencies: test_deps)

install_data(['package.xml'], install_dir: package_dir)
install_subdir('cs', install_dir: package_dir)
