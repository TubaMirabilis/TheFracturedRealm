using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace TheFracturedRealm.FunctionalTests;

public sealed class PriorityOrderer : ITestCaseOrderer
{
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : ITestCase
    {
        var buckets = new SortedDictionary<int, List<IXunitTestCase>>();
        foreach (var testCase in testCases.OfType<IXunitTestCase>())
        {
            var priority = testCase.TestMethod.Method.GetCustomAttributes<TestPriorityAttribute>().FirstOrDefault()?.Priority ?? 0;
            if (!buckets.TryGetValue(priority, out var list))
            {
                buckets[priority] = list = [];
            }
            list.Add(testCase);
        }
        var result = new List<TTestCase>(testCases.Count);
        foreach (var list in buckets.Values)
        {
            list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
            foreach (var tc in list.OfType<TTestCase>())
            {
                result.Add(tc);
            }
        }
        return result;
    }
}
