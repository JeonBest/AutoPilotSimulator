This asset uses Assembly Definition (.asmdef) files. There are many benefits to assembly definitions but a downside is that the whole 
project needs to use them or they should not be used at all.

  * If the project already uses assembly definitions accessing a script that belongs to this asset can be done by adding an reference to the assembly 
  definition of the script that needs to reference the asset. E.g. to access WheelController adding a NWH.WheelController reference 
  to [MyProjectAssemblyDefinitionName].asmdef is required.

  * If the project does not use assembly definitions simply remove all the .asmdef files from the asset after import.