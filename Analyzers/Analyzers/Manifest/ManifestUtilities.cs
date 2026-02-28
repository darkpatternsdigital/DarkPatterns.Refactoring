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
                    .Where(x => x.Contains(' '))
                    .Select(x =>
                    {
                        int firstSpace = x.IndexOf(' ');
                        return (x.Substring(0, firstSpace), x.Substring(firstSpace + 1));
                    })
                    .ToLookup(x => x.Item1, x => x.Item2)
            };
        }
    }
}
