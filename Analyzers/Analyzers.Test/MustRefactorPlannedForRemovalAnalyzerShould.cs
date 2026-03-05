using System.Threading.Tasks;
using AnalyzerTesting.CSharp.Extensions;
using DarkPatterns.Refactoring.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DarkPatterns.Refactoring;

[TestClass]
public class MustRefactorPlannedForRemovalAnalyzerShould
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Produce_no_diagnostics_by_default()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources(@"")
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_methods_being_removed()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Use NewMethod instead")]
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
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("MethodToRemove", "ShouldBeRefactored", "1", "Use NewMethod instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_fields_being_removed()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class SomeClass
                {
                    [PlannedRemoval("1", "Use newField instead")]
                    private object fieldToRemove;

                    public void ShouldBeRefactored()
                    {
                        {|#0:fieldToRemove|} = new object();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("fieldToRemove", "ShouldBeRefactored", "1", "Use newField instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_for_used_properties_being_removed()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class SomeClass
                {
                    [PlannedRemoval("1", "Use NewProperty instead")]
                    public object PropertyToRemove { get; set; }

                    public void ShouldBeRefactored()
                    {
                        {|#0:PropertyToRemove|} = new object();
                    }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("PropertyToRemove", "ShouldBeRefactored", "1", "Use NewProperty instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
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
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_constructing_classes_being_removed()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass
                {
                }

                class SomeClass
                {
                    public object ShouldBeRefactored() => new {|#0:OutdatedClass|}();
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_with_static_references()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                using static OutdatedClass;

                [PlannedRemoval("1", "Use NewClass instead")]
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
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_using_statement()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                using Alias = OutdatedClass;

                [PlannedRemoval("1", "Use NewClass instead")]
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
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(1).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_typeof_expression()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass
                {
                }

                class SomeClass
                {
                    public Type ShouldBeRefactored()
                    {
                        return typeof({|#0:OutdatedClass|});
                    }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_via_type_argument()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
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
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_on_method_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Use NewMethod instead")]
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
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Use NewMethod instead")]
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
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                class OutdatedClass
                {
                    [PlannedRemoval("1", "Use NewMethod instead")]
                    public static void MethodToRemove()
                    {
                    }
                }
                
                [PlannedRemoval("1", "Should not be used directly anyway")]
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
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [assembly: PlannedRemoval("1", "Use the Common assembly")]

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
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} shouldBeRefactored;
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "shouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_field_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass shouldBeRefactored;
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }

    // TODO: test field initialization
    #endregion fields

    #region properties
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_property()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} ShouldBeRefactored { get; set; }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_property_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass ShouldBeRefactored { get; set; }
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
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    private {|#0:OutdatedClass|} ShouldBeRefactored() => null;
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_method_return_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private OutdatedClass ShouldBeRefactored() => null;
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion method return type

    #region method parameter type
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_method_parameter_type()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    private void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_method_parameter_type_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class SomeClass
                {
                    private void ShouldBeRefactored({|#0:OutdatedClass|} outdatedClass) { }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion method parameter type

    #region method type parameter constraint
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_method_type_parameter_constraint()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    private void ShouldBeRefactored<T>()
                        where T : {|#0:OutdatedClass|}
                    {
                    }
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_method_type_parameter_constraint_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class SomeClass
                {
                    [PlannedRefactor("1", "Needs rework")]
                    private void ShouldBeRefactored<T>()
                        where T : {|#0:OutdatedClass|}
                    {
                    }
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion method type parameter constraint

    #region class type parameter constraint
    [TestMethod]
    public async Task Warn_when_using_classes_being_removed_as_a_private_class_type_parameter_constraint()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                class ShouldBeRefactored<T>
                    where T : {|#0:OutdatedClass|}
                {
                }
                """)
            .AddExpectedDiagnostics(MustRefactorPlannedForRemovalAnalyzer.Rule.AsResult().WithLocation(0).WithArguments("OutdatedClass", "ShouldBeRefactored", "1", "Use NewClass instead"))
            .RunAsync(TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task Produce_no_diagnostic_when_using_classes_being_removed_as_a_private_class_type_parameter_constraint_when_marked_for_refactor()
    {
        await TestBuilder.TestCSharp<MustRefactorPlannedForRemovalAnalyzer>()
            .AddSources("""
                [PlannedRemoval("1", "Use NewClass instead")]
                class OutdatedClass { }

                [PlannedRefactor("1", "Needs rework")]
                class ShouldBeRefactored<T>
                    where T : {|#0:OutdatedClass|}
                {
                }
                """)
            .RunAsync(TestContext.CancellationToken);
    }
    #endregion class type parameter constraint
}
