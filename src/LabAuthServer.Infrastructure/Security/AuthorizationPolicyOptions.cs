namespace LabAuthServer.Infrastructure.Security;

public sealed class AuthorizationPolicyOptions
{
    public const string SectionName = "Authorization";

    public bool? DefaultDeny { get; set; }

    public int MaximumGroupCount { get; set; }

    public List<string> PublicEndpoints { get; set; } = [];

    public Dictionary<string, string> GroupToRoleMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
