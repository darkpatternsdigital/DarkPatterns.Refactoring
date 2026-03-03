using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DarkPatterns.Refactoring.Manifest;

public static class ManifestUtilities
{
    extension(CompilationStartAnalysisContext context)
    {
        public RefactoringManifest GetRefactoringManifest()
        {
            var manifestLines =
                context.Options.AdditionalFiles
                    .Where(file =>
                    {
                        var opts = context.Options.AnalyzerConfigOptionsProvider.GetOptions(file);
                        return opts.TryGetValue("build_metadata.AdditionalFiles.DPDRefactoringManifest", out var value) && value == "true";
                    })
                    .SelectMany(file => file.GetText()?.Lines ?? Enumerable.Empty<TextLine>())
                    .Select(x => x.ToString());

            return new RefactoringManifest
            {
                PlannedIssues = manifestLines
                    .Select(x =>
                    {
                        int firstSpace = x.IndexOf(' ');
                        if (firstSpace == -1)
                            return (x, string.Empty);
                        return (x.Substring(0, firstSpace), x.Substring(firstSpace + 1));
                    })
                    .Where(x => x.Item1.Length > 0)
                    .ToLookup(x => x.Item1, x => x.Item2)
            };
        }
    }
}
