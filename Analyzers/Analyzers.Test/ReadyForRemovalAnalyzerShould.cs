using System.Threading.Tasks;
using AnalyzerTesting.CSharp.Extensions;
using DarkPatterns.Refactoring.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DarkPatterns.Refactoring;

[TestClass]
public class ReadyForRemovalAnalyzerShould
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Produce_no_diagnostics_by_default()
    {
        await TestBuilder.TestCSharp<ReadyForRemovalAnalyzer>()
            .AddSources(@"")
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_classes_being_removed()
    {
        await TestBuilder.TestCSharp<ReadyForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Replace with something else")]
                class {|#0:OutdatedClass|}
                {   
                }
                """)
            .AddExpectedDiagnostics(ReadyForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_class_when_planned_for_future()
    {
        await TestBuilder.TestCSharp<ReadyForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Replace with something else")]
                class {|#0:OutdatedClass|}
                {   
                }
                """)
            .AddRefactorManifest("""
                1 - Remove OutdatedClass in favor of something else
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_methods_being_removed()
    {
        await TestBuilder.TestCSharp<ReadyForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Remove this method")]
                    public void {|#0:MethodToRemove|}()
                    {
                    }
                }
                """)
            .AddExpectedDiagnostics(ReadyForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("MethodToRemove", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_planned_for_future()
    {
        await TestBuilder.TestCSharp<ReadyForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Remove this method")]
                    public void {|#0:MethodToRemove|}()
                    {
                    }
                }
                """)
            .AddRefactorManifest("""
                1 - Remove OutdatedClass in favor of something else
                """)
            .RunAsync(TestContext.CancellationToken);
    }
}
