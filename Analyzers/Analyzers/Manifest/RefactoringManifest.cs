using System.Linq;

namespace DarkPatterns.Refactoring.Manifest;

public class RefactoringManifest
{
    public ILookup<string, string> PlannedIssues { get; set; } = Enumerable.Empty<string>().ToLookup(x => x);
}
