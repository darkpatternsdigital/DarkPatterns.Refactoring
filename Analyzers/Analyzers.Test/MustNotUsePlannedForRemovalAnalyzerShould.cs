using System.Threading.Tasks;
using AnalyzerTesting.CSharp.Extensions;
using DarkPatterns.Refactoring.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DarkPatterns.Refactoring;

[TestClass]
public class MustNotUsePlannedForRemovalAnalyzerShould
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Produce_no_diagnostics_by_default()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources(@"")
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_methods_being_removed()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Remove this method")]
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        {|#0:OutdatedClass.MethodToRemove|}();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("MethodToRemove", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Remove this method")]
                    public static void MethodToRemove()
                    {
                    }
                }
                
                class SomeClass
                {
                    [PlannedRefactor("1", "Use something else")]
                    public void ShouldBeRefactored()
                    {
                        {|#0:OutdatedClass.MethodToRemove|}();
                    }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
}
