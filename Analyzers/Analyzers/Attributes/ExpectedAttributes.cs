namespace DarkPatterns.Refactoring.Attributes;

public sealed class PlannedRemovalAttribute(string ticketNumber, string recommendedAlternative)
{
    public string TicketNumber => ticketNumber;
    public string RecommendedAlternative => recommendedAlternative;
}

public sealed class PlannedRefactorAttribute(string ticketNumber, string refactorInstructions)
{
    public string TicketNumber => ticketNumber;
    public string RefactorInstructions => refactorInstructions;
}
