using Microsoft.CodeAnalysis;

namespace DarkPatterns.Refactoring;

public static class VersionMismatch
{
    public const string DiagnosticId = "DPDREF01";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.VersionMismatchTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.VersionMismatchMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.VersionMismatchDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "DarkPatterns.Refactoring";

    public static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

}