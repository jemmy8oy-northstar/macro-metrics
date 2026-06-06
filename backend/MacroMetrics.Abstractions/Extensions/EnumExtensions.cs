using System.ComponentModel;
using System.Reflection;

namespace MacroMetrics.Abstractions.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Returns the human-readable display string for any enum value that has a
    /// <see cref="DescriptionAttribute"/> applied to its members. Falls back to
    /// <see cref="Enum.ToString()"/> when no attribute is present.
    /// </summary>
    public static string ToDisplayString<T>(this T value) where T : struct, Enum
    {
        var field = typeof(T).GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}
