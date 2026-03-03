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
                        OutdatedClass.{|#0:MethodToRemove|}();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("MethodToRemove", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_fields_being_removed()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                class SomeClass
                {
                    [PlannedRemoval("1", "Remove this field")]
                    private object fieldToRemove;

                    public void ShouldBeRefactored()
                    {
                        {|#0:fieldToRemove|} = new object();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("fieldToRemove", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_properties_being_removed()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                class SomeClass
                {
                    [PlannedRemoval("1", "Remove this property")]
                    public object PropertyToRemove { get; set; }

                    public void ShouldBeRefactored()
                    {
                        {|#0:PropertyToRemove|} = new object();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("PropertyToRemove", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        {|#0:OutdatedClass|}.{|#1:MethodToRemove|}();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_constructing_classes_being_removed()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                }

                class SomeClass
                {
                    public object ShouldBeRefactored() => new {|#0:OutdatedClass|}();
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_with_static_references()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                using static OutdatedClass;

                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        {|#1:MethodToRemove|}();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_using_statement()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                using Alias = OutdatedClass;

                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        {|#0:Alias|}.{|#1:MethodToRemove|}();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_typeof_expression()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public Type ShouldBeRefactored()
                    {
                        return typeof({|#0:OutdatedClass|});
                    }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_type_argument()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this method")]
                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }

                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        GenericMethod<{|#0:OutdatedClass|}>();
                    }

                    public void GenericMethod<T>() { }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
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

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_class_is_marked_for_refactor()
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
                
                [PlannedRefactor("1", "Use something else")]
                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        OutdatedClass.MethodToRemove();
                    }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_class_is_marked_for_removal()
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
                
                [PlannedRemoval("1", "Also remove this")]
                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        OutdatedClass.MethodToRemove();
                    }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_entire_assembly_is_marked_for_removal()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [assembly: PlannedRemoval("1", "Remove this assembly")]

                class OutdatedClass
                {
                    public static void MethodToRemove()
                    {
                    }
                }
                
                class SomeClass
                {
                    public void ShouldBeRefactored()
                    {
                        OutdatedClass.MethodToRemove();
                    }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    #region fields
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_field()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} shouldBeRefactored;
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "shouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_field_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass shouldBeRefactored;
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_nonprivate_field_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    protected {|#0:OutdatedClass|} shouldBeRefactored;
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "shouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    // TODO: test field initialization
    #endregion fields

    #region properties
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_property()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} ShouldBeRefactored { get; set; }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_property_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass ShouldBeRefactored { get; set; }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_nonprivate_property_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    protected {|#0:OutdatedClass|} ShouldBeRefactored { get; set; }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_nonprivate_property_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRemoval("1", "Remove this class too")]
                class SomeClass
                {
                    protected OutdatedClass ShouldBeRefactored { get; set; }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    // TODO: test property initialization
    #endregion properties

    #region method return type
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_method_return_type()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} ShouldBeRefactored() => null;
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_method_return_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass ShouldBeRefactored() => null;
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_nonprivate_method_return_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    protected {|#0:OutdatedClass|} ShouldBeRefactored() => null;
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_nonprivate_method_return_type_when_marked_for_removal()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRemoval("1", "Remove this too")]
                class SomeClass
                {
                    protected {|#0:OutdatedClass|} ShouldBeRefactored() => null;
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion method return type

    #region method parameter type
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_method_parameter_type()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                class SomeClass
                {
                    private void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_method_parameter_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_nonprivate_method_parameter_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    protected void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .AddExpectedDiagnostics(MustNotUsePlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_nonprivate_method_parameter_type_when_marked_for_removal()
    {
        await TestBuilder.TestCSharp<MustNotUsePlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Remove this class")]
                class OutdatedClass { }

                class SomeClass
                {
                    [PlannedRemoval("1", "Needs rework")]
                    protected void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion method return type

    // TODO: test type parameter constraints
    // TODO: test delegate types
    // TODO: test parameter defaults
}
