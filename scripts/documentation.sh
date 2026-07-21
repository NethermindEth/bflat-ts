#!/bin/bash
# Script to create a documentation for the Test Suite

if [ "${TE_BASE}" == "" ] ; then
	echo TE_BASE is not set
	exit 1
fi

mkdir -p doc/generated
doxygen undodb-ts/Doxyfile
