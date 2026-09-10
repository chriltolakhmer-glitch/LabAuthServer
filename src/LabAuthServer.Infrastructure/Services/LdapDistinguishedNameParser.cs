using System.Text;

namespace LabAuthServer.Infrastructure.Services;

internal static class LdapDistinguishedNameParser
{
    // Decode the first CN RDN without treating an escaped comma as a delimiter.
    // Multi-valued/quoted RDNs are ambiguous for the existing CN mapping contract
    // and fail closed; they must never be silently shortened into another group.
    public static bool TryParse(string distinguishedName, out string name)
    {
        name = string.Empty;
        if (string.IsNullOrWhiteSpace(distinguishedName)) return false;
        var components = new List<string>();
        var start = 0;
        for (var index = 0; index < distinguishedName.Length; index++)
        {
            var character = distinguishedName[index];
            if (char.IsControl(character) || character is '+' or '"') return false;
            if (character == '\\')
            {
                if (++index == distinguishedName.Length) return false;
            }
            else if (character == ',')
            {
                components.Add(distinguishedName[start..index].Trim());
                start = index + 1;
            }
        }
        components.Add(distinguishedName[start..].Trim());
        if (components.Count < 2 || !components[0].StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) return false;
        for (var index = 0; index < components.Count; index++)
        {
            var equals = components[index].IndexOf('=');
            if (equals <= 0 || equals == components[index].Length - 1) return false;
            if (!components[index][..equals].All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.')) return false;
            if (!TryDecode(components[index][(equals + 1)..], out var decoded)) return false;
            if (index == 0) name = decoded.Trim();
        }
        return name.Length != 0;
    }

    private static bool TryDecode(string value, out string decoded)
    {
        decoded = string.Empty;
        var result = new StringBuilder();
        try
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] != '\\')
                {
                    if (value[index] is '=' or '<' or '>' or ';' || char.IsControl(value[index])) return false;
                    result.Append(value[index]);
                    continue;
                }
                if (++index >= value.Length) return false;
                if (index + 1 < value.Length && Uri.IsHexDigit(value[index]) && Uri.IsHexDigit(value[index + 1]))
                {
                    var bytes = new List<byte>();
                    while (true)
                    {
                        bytes.Add(Convert.ToByte(value.Substring(index, 2), 16));
                        index++;
                        if (index + 3 >= value.Length || value[index + 1] != '\\' || !Uri.IsHexDigit(value[index + 2]) || !Uri.IsHexDigit(value[index + 3])) break;
                        index += 2;
                    }
                    result.Append(new UTF8Encoding(false, true).GetString(bytes.ToArray()));
                }
                else
                {
                    if (!" ,+\"\\<>;=#".Contains(value[index])) return false;
                    result.Append(value[index]);
                }
            }
            decoded = result.ToString();
            return decoded.Length > 0 && !decoded.Any(char.IsControl);
        }
        catch (DecoderFallbackException) { return false; }
    }
}
