using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DarkPatterns.Refactoring.Attributes;
using DarkPatterns.Refactoring.Manifest;
using DarkPatterns.Refactoring.Symbols;
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [Rule]; } }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCodeBlockStartAction<SyntaxKind>(codeBlockStartContext =>
        {
            Action<Diagnostic> noopReportDiagnostics = _ => { /* Intentionally not logging here; should be caught by another analyzer */ };
            // Determine owning symbols and gather planned tickets (refactor/removal)
            var tickets = from symbol in codeBlockStartContext.OwningSymbol.AndAllContainers()
                          from ticketNumber in Enumerable.Concat(
                              // If the code is planned for refactor, it can use things planned for removal in the same ticket
                              symbol.FindAttributes<PlannedRefactorAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber),
                              // If the code is planned for removal, it can use things planned for removal in the same ticket
                              symbol.FindAttributes<PlannedRemovalAttribute>(noopReportDiagnostics).Select(attr => attr.TicketNumber)
                          )
                          select ticketNumber;

            // TODO: Scan more syntax usage for symbols flagged for removal
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            codeBlockStartContext.RegisterSyntaxNodeAction(ctx => AnalyzeNode(ctx, codeBlockStartContext.OwningSymbol, tickets), SyntaxKind.SimpleMemberAccessExpression);
        });
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context, ISymbol owningSymbol, IEnumerable<string> plannedRefactorTickets)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var targetSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;

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
