using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace TheFracturedRealm.FunctionalTests;

public class PriorityOrderer : ITestCaseOrderer
{
    private static List<IXunitTestCase> GetOrCreate(SortedDictionary<int, List<IXunitTestCase>> dictionary, int key)
    {
        if (dictionary.TryGetValue(key, out var result))
        {
            return result;
        }
        result = [];
        dictionary[key] = result;
        return result;
    }

    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var result = new List<TTestCase>();
        var sortedMethods = new SortedDictionary<int, List<IXunitTestCase>>();
        foreach (var testCase in testCases.OfType<IXunitTestCase>())
        {
            var priority = 0;
            var attr = testCase.TestMethod.Method.GetCustomAttributes<TestPriorityAttribute>().FirstOrDefault();
            if (attr is not null)
            {
                priority = attr.Priority;
            }
            GetOrCreate(sortedMethods, priority).Add(testCase);
        }
        foreach (var list in sortedMethods.Keys.Select(priority => sortedMethods[priority]))
        {
            list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
            foreach (var testCase in list)
            {
                if (testCase is TTestCase tTestCase)
                {
                    result.Add(tTestCase);
                }
            }
        }
        return result;
    }
}
