namespace DynamicApi.Manager; 

public static class StringExtension
{
    public static string ToCamelCase(this string str) => string.IsNullOrEmpty(str) || str.Length < 2 ? str.ToLowerInvariant() : char.ToLowerInvariant(str[0]) + str.Substring(1);

    public static string ToUpperCamelCase(this string str) {
        var result = string.Empty;
        var isUpper = true;
        foreach (var c in str) {
            if (c == '.') {
                isUpper = true;
                result += c;
                continue;
            }
            if (isUpper) {
                result += char.ToUpperInvariant(c);
                isUpper = false;
            } else {
                result += c;
            }
        }
        return result;
    }

}