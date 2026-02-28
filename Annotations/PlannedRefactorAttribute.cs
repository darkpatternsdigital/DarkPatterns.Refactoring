namespace DarkPatterns.Refactoring;

[System.AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Event
    | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public sealed class PlannedRefactorAttribute(string ticketNumber, string refactorInstructions) : Attribute
{
    public string TicketNumber => ticketNumber;
    public string RefactorInstructions => refactorInstructions;
}