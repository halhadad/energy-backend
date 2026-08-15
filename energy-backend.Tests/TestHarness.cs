namespace energy_backend.Tests;

// tiny test runner, no external framework
internal sealed class TestRunner
{
    private int _passed;
    private int _failed;

    public void Test(string name, Action body) => Run(name, () => { body(); return Task.CompletedTask; });

    public void Test(string name, Func<Task> body) => Run(name, body);

    private void Run(string name, Func<Task> body)
    {
        try
        {
            body().GetAwaiter().GetResult();
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {ex.Message}");
        }
    }

    public int Report()
    {
        Console.WriteLine();
        Console.WriteLine($"{_passed} passed, {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }
}

internal static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new Exception($"Expected true: {message}");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected <{expected}> but was <{actual}>");
    }

    public static void Near(double expected, double actual, double tolerance = 1e-9)
    {
        if (Math.Abs(expected - actual) > tolerance)
            throw new Exception($"Expected ~<{expected}> but was <{actual}> (tolerance {tolerance})");
    }
}
