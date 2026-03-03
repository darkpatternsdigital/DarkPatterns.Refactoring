# DarkPattern.Refactoring

A collection of C# libraries to assist with maintenance of complicated and evolving codebases.


* ![DarkPatterns.Refactoring.Annotations NuGet](https://img.shields.io/nuget/v/DarkPatterns.Refactoring.Annotations)
  [C# attributes for Refactoring](./Annotations), including `PlannedRemoval` and `PlannedRefactor`.
* ![DarkPatterns.Refactoring.Analyzers NuGet](https://img.shields.io/nuget/v/DarkPatterns.Refactoring.Analyzers)
  [C# analyzers for Refactoring](./Analyzers/Analyzers.Package/), to generate warnings and track plans for future refactors.

## Usage

See the [refactoring example](./examples/RefactoringExample/) for an example.

1. Add both `DarkPatterns.Refactoring.Annotations` and
   `DarkPatterns.Refactoring.Analyzers` packages to your project.

2. Track classes, methods, etc. that are planned for removal by adding
   `[PlannedRemoval("TICKET", "Explanation")]` as needed. `TICKET` may be
   anything that does not include a space; `Explanation` should be a
   human-readable description about a recommended alternative.

3. Warnings will be added for any usage of items planned for removal; if part of
   a public symbol, those also must be marked `PlannedRemoval` with their own
   explanation. If the usage is kept internal, `PlannedRefactor` can be added
   instead, which does not propagate to other uses.

4. Add a list of tickets to a `PlannedRefactoring.txt` that is added to the
   project as a `DPDRefactoringManifest` build action. The format of this file
   is one line per ticket, optionally with a space followed by an explanation.

   ```text
   GH-1 Title of ticket
   GH-2
   GH-3 Another title
   ```
