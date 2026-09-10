using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public sealed class LdapConnectionFactory : ILdapConnectionFactory
{
    public ILdapConnection Create(LdapOptions options) => new Connection(options);

    private sealed class Connection : ILdapConnection
    {
        private readonly LdapConnection _connection;

        public Connection(LdapOptions options)
        {
            _connection = new LdapConnection(new LdapDirectoryIdentifier(options.Host, options.Port));
            try { _connection.Timeout = options.ConnectionTimeout; }
            catch { _connection.Dispose(); throw; }
        }

        public void ConfigureSession(bool useLdaps)
        {
            _connection.SessionOptions.ProtocolVersion = 3;
            _connection.SessionOptions.SecureSocketLayer = useLdaps;
        }

        public void Bind(NetworkCredential credential) => _connection.Bind(credential);

        public LdapSearchResult? Search(SearchRequest request)
        {
            if (_connection.SendRequest(request) is not SearchResponse response) return null;
            // Keep server result codes intact; do not parse partial data on failure.
            if (response.ResultCode != ResultCode.Success)
                return new(response.ResultCode, Array.Empty<LdapSearchEntry>());
            var entries = new List<LdapSearchEntry>();
            foreach (SearchResultEntry entry in response.Entries)
            {
                var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (DirectoryAttribute attribute in entry.Attributes.Values)
                {
                    try
                    {
                        attributes.Add(attribute.Name, Array.AsReadOnly(attribute.GetValues(typeof(string)).Cast<string>().ToArray()));
                    }
                    catch (Exception exception) when (exception is ArgumentException or InvalidCastException or System.Text.DecoderFallbackException)
                    {
                        throw new LdapResponseValidationException();
                    }
                }
                entries.Add(new(entry.DistinguishedName, attributes));
            }
            return new(response.ResultCode, entries.AsReadOnly(), response.References.Count != 0 || response.Referral.Length != 0);
        }

        public void Dispose() => _connection.Dispose();
    }
}
