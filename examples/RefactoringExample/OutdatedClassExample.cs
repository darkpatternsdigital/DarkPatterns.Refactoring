namespace DarkPatterns.Refactoring;

[PlannedRemoval("1", "This class is outdated; use NewerClass instead")]
public class OutdatedClass
{

}

public class DependsOnOutdatedClass
{
    [PlannedRefactor("1", "Use NewerClass instead")]
    public object UseOutdatedClass()
    {
        return new OutdatedClass();
    }
}
