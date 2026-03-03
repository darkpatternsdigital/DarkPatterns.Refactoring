using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DarkPatterns.Refactoring.Attributes;
using DarkPatterns.Refactoring.Manifest;
using DarkPatterns.Refactoring.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DarkPatterns.Refactoring;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MustNotUsePlannedForRemovalAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DPDREF02";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MustNotUsePlannedForRemovalTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MustNotUsePlannedForRemovalMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MustNotUsePlannedForRemovalDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "DarkPatterns.Refactoring";

    public static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
    private static readonly Action<Diagnostic> noopReportDiagnostics = _ => { /* Intentionally not logging here; should be caught by another analyzer */ };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [Rule]; } }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockStartContext =>
        {
            // Determine owning symbols and gather planned tickets (refactor/removal)

            // If the code is planned for refactor, it can use things planned for removal in the same ticket
            var refactorTickets =
                from symbol in codeBlockStartContext.OwningSymbol.AndAllContainers()
                from ticketNumber in symbol.FindAttributes<PlannedRefactorAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber)
                select ticketNumber;
            // If the code is planned for removal, it can use things planned for removal in the same ticket
            var removalTickets =
                from symbol in codeBlockStartContext.OwningSymbol.AndAllContainers()
                from ticketNumber in symbol.FindAttributes<PlannedRemovalAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber)
                select ticketNumber;

            var tickets = refactorTickets.Concat(removalTickets);

            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            codeBlockStartContext.RegisterSyntaxNodeAction(ctx => AnalyzeSyntaxNode(ctx, codeBlockStartContext.OwningSymbol, tickets), SyntaxKind.IdentifierName);

            // Can we warn for only the first instance for each ticket in a code block? Is that helpful?
        });

        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Field);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Property);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.TypeParameter);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.FunctionPointerType);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.Method);
        context.RegisterSymbolStartAction(AnalyzeSymbolNode, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbolNode(SymbolStartAnalysisContext symbolContext)
    {
        // If the code is planned for refactor, it can use things planned for removal in the same ticket
        var refactorTickets =
            from symbol in symbolContext.Symbol.AndAllContainers()
            from ticketNumber in symbol.FindAttributes<PlannedRefactorAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber)
            select ticketNumber;
        // If the code is planned for removal, it can use things planned for removal in the same ticket
        var removalTickets =
            from symbol in symbolContext.Symbol.AndAllContainers()
            from ticketNumber in symbol.FindAttributes<PlannedRemovalAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber)
            select ticketNumber;

        // "Friend" is `internal`; if the containing symbol is a namespace or assembly, `internal` is the best C# does.
        var privateAccessibility = symbolContext.Symbol.ContainingSymbol.Kind is SymbolKind.Namespace or SymbolKind.Assembly
            ? Accessibility.Friend
            : Accessibility.Private;

        var tickets = symbolContext.Symbol.DeclaredAccessibility == privateAccessibility
            ? refactorTickets.Concat(removalTickets)
            : removalTickets;

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
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), symbol.Name, owningSymbol.Name, plannedRemoval.TicketNumber));
        }
    }
}
