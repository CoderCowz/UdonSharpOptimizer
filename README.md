# UdonSharpOptimizer
[VCC Listing](https://blueamulet.github.io/UdonSharpOptimizer/)  
Adds a hook to UdonSharp to process the generated Udon, reducing the number of instructions and making better use of temporary variables, as well as fixing a bug resulting in unnecessary variables. This results in udon programs that are faster and smaller.

After any UdonSharp compile, a line will appear in the console similar to the following:  
`[Optimizer] Removed # instructions, # variables, and # extra __this total`

For Unity 2022, no permanent changes are made to the VRCSDK, all changes are made in memory and can be easily removed by removing this package.  
For Unity 2019, an additional file is added to the VRCSDK to allow the optimizer to function.  
This can be found at the path `Packages/com.vrchat.worlds/Integrations/UdonSharp/Editor/USOInternals.cs`

## Optimizations
There are currently 3 class of optimizations:
### COPY Removal:
UdonSharp likes to copy values into variables that only get used once or never at all, we eliminate these COPY instructions by modifying other instructions to store directly to the copy's target, or load directly from the copy's source.
### Tail Call Optimization:
Any call to another Udon function followed by a return, can have its setup and the corresponding return removed, utilizing the return instructions of the called Udon function instead.
### Variable Reduction:
UdonSharp makes a *LOT* of temporary variables. We detect places where we can reuse existing temporary variables instead of creating new ones. This does not make the program faster but does make the program smaller.

## Changelog
1.0.0  - Initial 2022 version  
1.0.1  - 2024 Update  
1.0.2  - Fixed switch statements  
1.0.3  - Reduced number of variables  
1.0.4  - __this_ fix for even less variables  
1.0.5  - Added ExternWrite+Copy check for variables, added missing jump checks  
1.0.6  - Added tail call optimization  
1.0.7  - Single .unitypackage installation  
1.0.8  - Added basic Settings panel  
1.0.9  - Moved TCO into first pass, added block based variable reduction  
1.0.9b - Fixed udon functions destroying variables in other functions  
1.0.10 - Code refactor, added additional instruction and variable optimizations  
1.0.11 - Per optimization statistics, Unity 2019 fix, expanded TCO optimization  
1.0.12 - Simplify jump chains, further expaned TCO optimization  
