using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DarkPatterns.Refactoring.Attributes;
using DarkPatterns.Refactoring.Manifest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DarkPatterns.Refactoring;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadyForRemovalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DPDREF04";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ReadyForRemovalTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ReadyForRemovalMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ReadyForRemovalDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "DarkPatterns.Refactoring";

    public static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [Rule]; } }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var manifest = compilationStartContext.GetRefactoringManifest();

            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            compilationStartContext.RegisterSymbolAction(ctx => AnalyzeSymbol(ctx, manifest), SymbolKind.NamedType);
            compilationStartContext.RegisterSymbolAction(ctx => AnalyzeSymbol(ctx, manifest), SymbolKind.Method);
            compilationStartContext.RegisterSymbolAction(ctx => AnalyzeSymbol(ctx, manifest), SymbolKind.Property);
            compilationStartContext.RegisterSymbolAction(ctx => AnalyzeSymbol(ctx, manifest), SymbolKind.Field);
            compilationStartContext.RegisterSymbolAction(ctx => AnalyzeSymbol(ctx, manifest), SymbolKind.Event);

            // TODO: assembly
            // TODO: module
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, RefactoringManifest manifest)
    {
        var plannedRemoval = context.Symbol.FindAttribute<PlannedRemovalAttribute>(context.ReportDiagnostic);
        if (plannedRemoval == null)
            return;

        // check manifest - if it's in the manifest, skip
        if (manifest.PlannedIssues.Contains(plannedRemoval.TicketNumber))
            return;

        var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations[0], context.Symbol.Name, plannedRemoval.TicketNumber);
        context.ReportDiagnostic(diagnostic);
    }
}
