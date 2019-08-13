# TinyBCT #

TiniBCT is a MSIL translator to Boogie PL. It relies on a [.NET analysis Framework](https://github.com/garbervetsky/analysis-net)  to parse and analyze the MSIL.

## Build Status

| Master                        | Develop                         |
|-------------------------------|---------------------------------|
| [![master build status][master]][travis] | [![develop build status][develop]][travis] |

[master]: https://travis-ci.org/m7nu3l/TinyBCT.svg?branch=master
[develop]: https://travis-ci.org/m7nu3l/TinyBCT.svg?branch=develop
[travis]: https://travis-ci.org/m7nu3l/TinyBCT

# Usage #

TinyBCT -i [souclist of DLLs/EXEs to transalate]

The output is a bpl file containting the translation of all source files.

Additional options
+ -b [list bpl files]: include this files to the output blp
+ -l true: add line numbers

## Build for Linux/MacOS

The following steps are to build TinyBCT.exe only not the test unit suite. In order to do that, you may require to use a specific version of mono (check travis file for more details).

You must install:

1. https://www.mono-project.com/download/stable/
2. https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current
3. sudo apt-get install nuget

In the repository folder, you must execute:

1. git checkout develop
2. git submodule update --init --recursive
3. nuget restore
4. msbuild TinyBCT/TinyBCT.csproj


# Page in construction #
