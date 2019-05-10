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

# Page in construction #
