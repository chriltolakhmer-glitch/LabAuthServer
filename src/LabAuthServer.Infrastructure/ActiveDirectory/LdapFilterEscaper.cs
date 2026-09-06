namespace LabAuthServer.Infrastructure.ActiveDirectory;

public static class LdapFilterEscaper
{
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '\0' or '(' or ')' or '*' or '\\' || character < ' ')
            {
                builder.Append('\\');
                builder.Append(((int)character).ToString("X2"));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}