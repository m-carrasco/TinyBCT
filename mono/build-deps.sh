#!/usr/bin/env bash
set -e

# Build the required DLLs
(cd cci && msbuild Samples/PeToPe/PeToPe.csproj /t:Build)

# Overwwrite the dependencies with newly-built DLLs
for d in TinyBCT/Dependencies/Microsoft.Cci.*.dll
do
	name=$(basename $d)
	dir=$(cd $(dirname $(find cci/Samples/PeToPe -name $name)) && pwd)
	ln -fs $dir/$name TinyBCT/Dependencies/$name
done
