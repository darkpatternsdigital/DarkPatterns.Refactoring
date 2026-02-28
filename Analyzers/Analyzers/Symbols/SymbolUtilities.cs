using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace DarkPatterns.Refactoring.Symbols;

public static class SymbolUtilities
{
    extension(ISymbol? target)
    {
        /// <summary>
        /// Enumerates the current symbol and all containers of that symbol
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
}
