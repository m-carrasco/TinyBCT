#!/usr/bin/env bash
set -e

# Build the required DLLs
(cd cci && msbuild Samples/PeToPe/PeToPe.csproj /t:Build)

# Overwwrite the dependencies with newly-built DLLs
for d in TinyBCT/Dependencies/Microsoft.Cci.*
do
	ln -fs $(find ~/Code/cci/Samples/PeToPe -name $(basename $d)) TinyBCT/Dependencies/$(basename $d)
done
