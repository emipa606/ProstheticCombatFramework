# GitHub Copilot Instructions for Prosthetic Combat Framework (Continued)

## Mod Overview and Purpose
Prosthetic Combat Framework (Continued) is an extension for RimWorld modders that enhances the vanilla hediff verb system with advanced verb support, allowing customized combat prosthetics. By integrating this framework, modders can utilize ranged verb functionality from hediffs and define custom melee attacks with independent gizmos/command buttons. This framework significantly expands the flexibility and creativity available in modding RimWorld's combat mechanics.

**Key Points:**
- Provides advanced verb support, enhancing the combat prostheses capabilities.
- Allows independent control of ranged and melee attacks through custom command buttons.
- Easily modifiable via XML, making it user-friendly for mod developers.
- Designed to be placed high in the mod load order, just after core or alongside other frameworks.

## Key Features and Systems
- **Advanced Verb Support**: Enables ranged verb functionality from hediffs, leveraging unique weapons like shoulder-mounted cannons alongside regular firearms.
- **Custom Command Buttons**: Modders can define special gizmos for unique actions, complete with custom textures.
- **Intermod Compatibility**: Harmoniously works with RunAndGun patches and other custom job codes.
- **Simple XML Integration**: No heavy coding is required, making it accessible to mod creators with varying skill levels.

## Coding Patterns and Conventions
- **Nomenclature**: Follow C# naming conventions for classes and methods (PascalCase) and keep variables in camelCase for consistency.
- **Class Structures**: Organize classes by functionality, e.g., combat-related functionalities within `PCF_AttackTargetFinder` and related attack drivers like `JobDriver_AttackStaticExtended`.
- **Documentation**: Comment code where necessary for clarity, particularly in complex logic areas.
- **Modular Design**: Encourage separation of concerns, breaking down tasks into methods or smaller classes for maintainability.

## XML Integration
- XML files define job and thing definitions and are crucial for extending RimWorld functions:
  - Example files include `Jobs_Misc.xml` and `ThingDefs_LogfixingPlaceholder.xml`.
- Ensure XML files follow RimWorld's schema, maintaining `Defs` structure for items like `JobDef` and `ThingDef`.

## Harmony Patching
- Harmony is used to discreetly alter vanilla RimWorld functionalities:
  - Patch classes such as `JobDriver_SetupToils` and `BattleLogEntry_RangedImpact_GenerateGrammarRequest` with `Postfix` and `Prefix` methods.
- Focus patches on necessary alterations, aiming for high compatibility and minimal conflicts with other mods.
- Use Harmony judiciously to avoid unnecessary performance impacts.

## Suggestions for Copilot
- **Auto-Generate Stubs**: Utilize Copilot to draft method stubs for new functionality, ensuring quick implementation of standardized C# structures.
- **XML Config Suggestions**: Employ Copilot to suggest XML configurations based on existing Def patterns, expediting setup of new items and jobs.
- **Debugging Assistance**: Use Copilot to identify and suggest fixes for common C# and XML syntax errors encountered during development.
- **Code Optimizations**: Leverage Copilot to suggest refactoring opportunities for existing codebase, improving readability and performance.

By adhering to these guidelines, the Prosthetic Combat Framework (Continued) can be effectively developed and updated, maximizing its potential as a robust extension for the RimWorld modding community.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

