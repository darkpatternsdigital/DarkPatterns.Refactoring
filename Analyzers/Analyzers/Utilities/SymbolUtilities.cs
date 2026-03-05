using System;
using System.Collections.Generic;
using System.Linq;
using DarkPatterns.Refactoring.Attributes;
using Microsoft.CodeAnalysis;

namespace DarkPatterns.Refactoring.Utilities;

public static class SymbolUtilities
{
    extension(ISymbol? target)
    {
        /// <summary>
        /// Enumerates the given symbol and all containers of that symbol
        /// </summary>
        public IEnumerable<ISymbol> AndAllContainers()
        {
            var current = target;
            while (current != null)
            {
                yield return current;
                current = current.ContainingSymbol;
            }
        }
    }

    extension(ISymbol target)
    {
        /// <returns>True if the symbol is visible outside of its container</returns>
        public bool IsVisible()
        {
            // "Friend" is `internal`; if the containing symbol is a namespace or assembly, `internal` is the best C# does.
            var privateAccessibility = target.ContainingSymbol.Kind is SymbolKind.Namespace or SymbolKind.Assembly
                ? Accessibility.Friend
                : Accessibility.Private;

            return target.DeclaredAccessibility != privateAccessibility;
        }

        public IEnumerable<PlannedRefactorAttribute> GetRefactorAttributes(Action<Diagnostic>? reportDiagnostics = null)
        {
            return target.FindAttributes<PlannedRefactorAttribute>(reportDiagnostics ?? NoopReportDiagnostics);
        }

        public PlannedRemovalAttribute? GetRemovalAttribute(Action<Diagnostic>? reportDiagnostics = null)
        {
            return target.FindAttribute<PlannedRemovalAttribute>(reportDiagnostics ?? NoopReportDiagnostics);
        }

        public IEnumerable<PlannedRefactorAttribute> GetAllRefactorAttributes(Action<Diagnostic>? reportDiagnostics = null)
        {
            return target.AndAllContainers().SelectMany(x => x.GetRefactorAttributes());
        }
        public IEnumerable<PlannedRemovalAttribute> GetAllRemovalAttributes(Action<Diagnostic>? reportDiagnostics = null)
        {
            return target.AndAllContainers().Select(x => x.GetRemovalAttribute()).OfType<PlannedRemovalAttribute>();
        }
    }

    private static void NoopReportDiagnostics(Diagnostic diagnostic)
    {
        /* Intentionally not logging here; should be caught by another analyzer */
    }
}
