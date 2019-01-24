# TinyBCT #
TiniBCT is a MSIL translator to Boogie PL. It relies on a [.NET analysis Framework](https://github.com/garbervetsky/analysis-net)  to parse and analyze the MSIL.

# Usage #

TinyBCT -i [souclist of DLLs/EXEs to transalate] 

The output is a bpl file containting the translation of all source files. 

Additional options 
+ -b [list bpl files]: include this files to the output blp
+ -l true: add line numbers

# Page in construction #
