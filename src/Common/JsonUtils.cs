using System.Text.Encodings.Web;
using System.Text.Json;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class JsonUtils
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };


    public static string ToJson<T>(this T content)
        => JsonSerializer.Serialize(content, _options);
}
