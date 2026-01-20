namespace TheFracturedRealm.FunctionalTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestPriorityAttribute : Attribute
{
    public TestPriorityAttribute(int priority) => Priority = priority;

    public int Priority { get; private set; }
}
