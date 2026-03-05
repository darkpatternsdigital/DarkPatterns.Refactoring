using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DarkPatterns.Refactoring.Attributes;
using DarkPatterns.Refactoring.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DarkPatterns.Refactoring;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustRemovePlannedForRemovalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DPDREF05";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MustRemovePlannedForRemovalTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MustRemovePlannedForRemovalMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MustRemovePlannedForRemovalDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "DarkPatterns.Refactoring";

    public static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [Rule]; } }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Field);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Property);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.TypeParameter);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.FunctionPointerType);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Method);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbolNode(SymbolStartAnalysisContext symbolContext)
    {
        if (symbolContext.Symbol.DeclaredAccessibility == Accessibility.Private) return;

        var tickets = symbolContext.Symbol.GetAllRemovalAttributes().Select(attr => attr.TicketNumber);

        symbolContext.RegisterSyntaxNodeAction(ctx =>
        {
            if (!SymbolEqualityComparer.IncludeNullability.Equals(ctx.ContainingSymbol, symbolContext.Symbol)) return;
            if (ctx.Node.Parent.AndAllParents().OfType<BlockSyntax>().Any()) return;
            if (ctx.Node.Parent.AndAllParents().OfType<ArrowExpressionClauseSyntax>().Any()) return;
            AnalyzeSyntaxNode(ctx, ctx.ContainingSymbol, tickets);
        }, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, ISymbol owningSymbol, IEnumerable<string> plannedRefactorTickets)
    {
        var targetSymbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

        foreach (var symbol in targetSymbol.AndAllContainers())
        {
            // Check if the accessed member or its containing type has any attribute
            var plannedRemoval = symbol.FindAttribute<PlannedRemovalAttribute>(context.ReportDiagnostic);
            if (plannedRemoval == null)
                continue;

            if (plannedRefactorTickets.Contains(plannedRemoval.TicketNumber))
                continue;

            // This doesn't care about the manifest; it requires that the containing type has planned for refactor OR removal using the same TicketNumber
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), symbol.Name, owningSymbol.Name, plannedRemoval.TicketNumber, plannedRemoval.RecommendedAlternative));
        }
    }
}
