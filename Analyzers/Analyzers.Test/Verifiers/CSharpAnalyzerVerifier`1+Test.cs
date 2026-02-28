using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

namespace DarkPatterns.Refactoring.Verifiers;

public static class TestBuilder
{
    public static CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> TestCSharp<TAnalyzer>()
        where TAnalyzer : DiagnosticAnalyzer, new()
        => new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>()
            .AddSolutionTransform((solution, project) =>
                solution.AddMetadataReferences(project.Id, [
                    MetadataReference.CreateFromFile(typeof(PlannedRefactorAttribute).Assembly!.GetAssemblyLocation())
                ])
            )
        .AddSources("""
            global using DarkPatterns.Refactoring;
            """);
}

public static class FluentTestExtensions
{
    extension<TTest>(TTest test)
        where TTest : AnalyzerTest<DefaultVerifier>
    {
        public TTest AddRefactorManifest(string manifestContents, string filename = "/PlannedRefactoring.txt")
        {
            test.TestState.AdditionalFiles.Add((filename, manifestContents));
            test.TestState.AnalyzerConfigFiles.Add((filename + Guid.NewGuid(), $"""
            [{filename}]
            build_metadata.AdditionalFiles.Identity = {filename}
            build_metadata.AdditionalFiles.DPDRefactoringManifest = true
            """));
            //test.TestState.
            return test;
        }
    }
}