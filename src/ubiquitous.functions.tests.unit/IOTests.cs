using System.Text.Json;
using Xunit.Abstractions;

namespace ubiquitous.functions.tests.unit;

// This tests native functionality exposed by Extism.
// Examples:
//   * function I/O with plain strings and ints
//   * function I/O with JSON and MessagePack
//   * Builtin Extism methods
//      * Config Get
//      * Variable Get/Set
//   * WASI disabled/enabled
//      * Get WASI values
//   * Call simple Host Functions with various numbers of parameters
//   * Test memory limits and timeout limits are enforced by the Extism runtime
//   * ?? Measure Fuel
public class IOTests
{
    private readonly FunctionExecutor _func;
    private readonly ITestOutputHelper _testOutputHelper;

    public IOTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _func = new FunctionExecutor();
        var pluginBytes = File.ReadAllBytes("test-harness.wasm");
        _func.Load(pluginBytes);
        _func.Configure();
        _func.Init("test");
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("1", 1)]
    [InlineData("0001", 4)]
    [InlineData("0.001", 5)]
    [InlineData("hello", 5)]
    [InlineData("HeLlO", 5)]
    [InlineData("HELLO", 5)]
    [InlineData("Hello, World!", 13)]
    [InlineData("This is a super long sentence to test that it can measure a long string", 71)]
    [InlineData("This string has a data in it\n\t\t\nWith some more data afterward.", 62)]
    [InlineData("This string has emojis \ud83d\udd25", 25)]
    public void RawStringIO_StrlenReturnsCorrectLength(string input, int expectedLength)
    {
        var result = _func.Call("strlen", input);
        var length = int.Parse(result);
        Assert.Equal(expectedLength, length);
    }

    [Fact]
    public void VoidFunction_DoNothing_ReturnsNothing()
    {
        var result = _func.Call("doNothing", "");
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("-1", -1)]
    [InlineData("1.0", 1)]
    [InlineData("1,2,3", 3)]
    [InlineData("3,2,1", 3)]
    [InlineData("1,2,3,2,1", 3)]
    [InlineData("3,3,3", 3)]
    [InlineData("1,3,2,3,1", 3)]
    [InlineData("100000,-5", 100000)]
    public void IntArrayCSVIO_Max_CorrectlyFindsMaxValue(string input, int maxVal)
    {
        var result = _func.Call("max", input);
        Assert.Equal(maxVal, int.Parse(result));
    }

    [Theory]
    [InlineData(new[] { 1 })]
    [InlineData(new int[] { })]
    [InlineData(new[] { 1, 2, 3, 4, 5 })]
    [InlineData(new[] { 182, 539, 210, 18985, 393, 2, 3, 10, 5 })]
    [InlineData(new[] { -100, 100 })]
    [InlineData(new[] { -1, -2, -3, -4, 5, 6, 7, 8 })]
    public void IntArrayJSONIO_StatsFunction_TakesArrayAndReturnsStatsObject(int[] input)
    {
        var result = _func.Call("intArrayStatsJSON", JsonSerializer.Serialize(input));
        _testOutputHelper.WriteLine(result);
        ArrayStats expectedResults;
        if (input.Length == 0)
            expectedResults = new ArrayStats();
        else
            expectedResults = new ArrayStats
            {
                Max = input.Max(),
                Min = input.Min(),
                Sum = input.Sum(),
                Mean = (int)input.Average()
            };


        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var statsObj = JsonSerializer.Deserialize<ArrayStats>(result, options);

        Assert.Equivalent(expectedResults, statsObj);
    }

    public class ArrayStats
    {
        public int? Max { get; set; }
        public int? Min { get; set; }
        public int Sum { get; set; }
        public int? Mean { get; set; }
    }
}