using System.Globalization;

namespace Jordiware.BencodeDotNet.Utils;

public static class FloatingPointNumberFormatter
{
    public static bool TryFormat(float value, out string result)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            result = string.Empty;
            return false;
        }

        return TryNormalize(
            value.ToString("R", CultureInfo.InvariantCulture),
            out result);
    }

    public static bool TryFormat(double value, out string result)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            result = string.Empty;
            return false;
        }

        return TryNormalize(
            value.ToString("R", CultureInfo.InvariantCulture),
            out result);
    }

    public static bool TryFormat(decimal value, out string result)
    {
        return TryNormalize(
            value.ToString("G29", CultureInfo.InvariantCulture),
            out result);
    }

    private static bool TryNormalize(string input, out string result)
    {
        var s = input;

        s = NormalizeExponent(s);
        s = NormalizeFraction(s);

        // No leading '+'
        if (s.Length > 0 && s[0] == '+')
            s = s[1..];

        result = s;
        return true;
    }

    private static string NormalizeExponent(string s)
    {
        var e = s.IndexOfAny(new[] { 'E', 'e' });
        if (e < 0)
            return s;

        // Force lowercase 'e'
        if (s[e] == 'E')
            s = s[..e] + 'e' + s[(e + 1)..];

        // Remove '+' sign in exponent
        if (e + 1 < s.Length && s[e + 1] == '+')
            s = s[..(e + 1)] + s[(e + 2)..];

        return s;
    }

    private static string NormalizeFraction(string s)
    {
        var dot = s.IndexOf('.');
        if (dot < 0)
            return s;

        var exp = s.IndexOf('e');
        var end = exp >= 0 ? exp : s.Length;

        var fraction = s[(dot + 1)..end];
        var trimmed = fraction.TrimEnd('0');

        // Remove decimal point entirely if fraction is zero
        if (trimmed.Length == 0)
            return s[..dot] + (exp >= 0 ? s[end..] : string.Empty);

        if (trimmed.Length == fraction.Length)
            return s;

        return s[..(dot + 1)] + trimmed + (exp >= 0 ? s[end..] : string.Empty);
    }
}
