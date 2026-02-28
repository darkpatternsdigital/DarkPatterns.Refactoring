namespace DarkPatterns.Refactoring;

[System.AttributeUsage(
    AttributeTargets.Assembly
    | AttributeTargets.Module
    | AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Event
    | AttributeTargets.Interface
    | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
public sealed class PlannedRemovalAttribute(string ticketNumber, string recommendedAlternative) : Attribute
{
    public string TicketNumber => ticketNumber;
    public string RecommendedAlternative => recommendedAlternative;
}
