using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace DarkPatterns.Refactoring.Utilities;

public static class SyntaxNodeUtilities
{
    extension(SyntaxNode? target)
    {
        /// <summary>
        /// Enumerates the given syntax node and all parents of that syntax node
        /// </summary>
        public IEnumerable<SyntaxNode> AndAllParents()
        {
            var current = target;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }
    }
}
