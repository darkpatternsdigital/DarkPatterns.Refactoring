# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Two NuGet packages that help track planned refactors and removals in C# codebases:

- `DarkPatterns.Refactoring.Annotations` (`Annotations/`): the `[PlannedRemoval(ticket, alternative)]` and `[PlannedRefactor(ticket, instructions)]` attributes.
- `DarkPatterns.Refactoring.Analyzers` (`Analyzers/`): Roslyn analyzers that report diagnostics `DPDREF01`–`DPDREF05` (listed in `Analyzers/Analyzers/AnalyzerReleases.Unshipped.md`).

## Commands

Solution file: `DarkPatterns.Refactoring.slnx`. Output goes to `artifacts/` (`UseArtifactsOutput`). Packages are produced on build (`GeneratePackageOnBuild`) into `artifacts/package/<config>/`.

```sh
dotnet restore
dotnet build                      # CI: dotnet build --no-restore --configuration Release
dotnet test                       # MSTest, Analyzers/Analyzers.Test
dotnet test --filter "FullyQualifiedName~MustRefactorAnalyzerShould"                         # one class
dotnet test --filter "FullyQualifiedName~MustRefactorAnalyzerShould.Warn_for_classes_being_refactored"  # one test
```

`examples/RefactoringExample` uses the analyzer as a project reference and is built with the solution. Use it to check analyzer behavior end-to-end, including the `PlannedRefactoring.txt` manifest wiring.

## Architecture

**The analyzer does not reference the Annotations assembly.** `Analyzers/Analyzers/Attributes/ExpectedAttributes.cs` mirrors the attribute classes. `AttributeDataExtensions.FindAttribute<T>` matches attributes by **class name only** and rebuilds a mirror instance via reflection, matching constructor parameters **by name**. If the shapes don't line up, it reports `DPDREF01` (VersionMismatch) instead of throwing. Any change to the constructor signature of an attribute in `Annotations/` must be matched in `ExpectedAttributes.cs`.

**Manifest (`PlannedRefactoring.txt`)** lists the ticket IDs that are still *planned*. Each line is `TICKET` or `TICKET explanation`. Flow:
1. `Analyzers.Package/Build.targets` (packed as `build/DarkPatterns.Refactoring.Analyzers.targets`) adds `PlannedRefactoring.txt` from the project root as a `DPDRefactoringManifest` item. It also adds or tags it as an `AdditionalFiles` item with metadata `DPDRefactoringManifest="true"`. Consumers can opt out with `DPDDisableDefaultRefactoringManifest=true` or supply their own `DPDRefactoringManifest` items.
2. `Build.props` makes that metadata compiler-visible through `CompilerVisibleItemMetadata`.
3. `Manifest/ManifestUtilities.GetRefactoringManifest()` reads the `AdditionalFiles` whose `build_metadata.AdditionalFiles.DPDRefactoringManifest == true` and builds an `ILookup<ticket, explanation>`.

**Analyzer semantics:**
- `ReadyForRemovalAnalyzer` (DPDREF02) and `MustRefactorAnalyzer` (DPDREF03) warn on a `PlannedRemoval` or `PlannedRefactor` symbol whose ticket is **not** in the manifest. Removing a ticket from the manifest marks the work as due.
- `MustRefactorPlannedForRemovalAnalyzer` (DPDREF04) and `MustRemovePlannedForRemovalAnalyzer` (DPDREF05) ignore the manifest. They warn when a `PlannedRemoval` symbol is used inside a symbol that lacks an attribute with the same ticket. Usage in a non-private signature (outside method bodies or arrow expressions) must be `PlannedRemoval`, which propagates the marking. Usage inside a body only needs `PlannedRefactor`.

**Diagnostic strings** live in `Analyzers/Analyzers/Resources.resx`, with a generated `Resources.Designer.cs`. Each rule has a `*Title`, `*MessageFormat` and `*Description` entry. When adding a rule, also add it to `AnalyzerReleases.Unshipped.md`, because `EnforceExtendedAnalyzerRules` is on.

**Packaging:** `Analyzers.Package` is the packable project. It packs `DarkPatterns.Refactoring.Analyzers.dll` into `analyzers/dotnet/cs` and the build props and targets into `build/`. The `Analyzers` project itself sets `IsPackable=false`. `Analyzers.CodeFixes` and `Analyzers.Vsix` exist but are not in the solution.

**Versions** are set in the root `Directory.Build.props` as `AnnotationsVersion` and `AnalyzersVersion`. Bump them there. CI publishes to nuget.org on `main` and to GitHub Packages with a `sha.<commit>` suffix on other branches. Assemblies are strong-named when `Refactoring.snk` exists at the root. CI decodes it from a secret, and it is not in the repo.

## Tests

Tests use `Microsoft.CodeAnalysis.Testing` with `AnalyzerTesting.CSharp.Extensions`. Helpers are in `Analyzers.Test/Verifiers/CSharpAnalyzerVerifier\`1+Test.cs`:
- `TestBuilder.TestCSharp<TAnalyzer>()` references the real Annotations assembly and adds global usings for `System` and `DarkPatterns.Refactoring`.
- `.AddRefactorManifest("...")` adds a manifest additional file together with the analyzer-config metadata that `Build.targets` would normally provide.
- Expected locations use markup `{|#0:Name|}` together with `Rule.AsResult().WithLocation(0).WithArguments(...)`.

Test classes are named `<Analyzer>Should`, with methods in `Snake_case_behavior`.

The codebase uses C# 14 (`LangVersion 14`) features such as `extension(...)` blocks for extension members.
