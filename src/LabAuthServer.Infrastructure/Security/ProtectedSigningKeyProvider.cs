using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security;

public sealed class ProtectedSigningKeyProvider : IProtectedSigningKeyProvider
{
    private readonly IOptions<TokenOptions> _tokenOptions;
    private readonly IReadOnlyDictionary<string, RSA> _configuredKeys;

    public ProtectedSigningKeyProvider(IOptions<TokenOptions> tokenOptions)
        : this(tokenOptions, Array.Empty<KeyValuePair<string, RSA>>())
    {
    }

    public ProtectedSigningKeyProvider(
        IOptions<TokenOptions> tokenOptions,
        IEnumerable<KeyValuePair<string, RSA>>? configuredKeys)
    {
        _tokenOptions = tokenOptions ?? throw new ArgumentNullException(nameof(tokenOptions));
        _configuredKeys = BuildKeyMap(configuredKeys ?? Array.Empty<KeyValuePair<string, RSA>>());
    }

    public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        return GetKeyAsync(options.ActiveKeyId, cancellationToken);
    }

    public ValueTask<SigningKeyMaterial> GetKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new InvalidOperationException("The signing key identifier is required.");
        }

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        if (_configuredKeys.TryGetValue(keyIdentifier, out var key))
        {
            return ValueTask.FromResult(new SigningKeyMaterial(keyIdentifier, RSA.Create(key.ExportParameters(true))));
        }

        if (string.Equals(options.ActiveKeyId, keyIdentifier, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Signing-key configuration is invalid: the configured active signing key is missing from the approved key set.");
        }

        if (!string.IsNullOrWhiteSpace(options.PreviousKeyId) &&
            string.Equals(options.PreviousKeyId, keyIdentifier, StringComparison.Ordinal))
        {
            if (!options.PreviousKeyExpiresAt.HasValue || options.PreviousKeyExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "Signing-key configuration is invalid: previous signing key overlap has expired or is invalid.");
            }

            throw new InvalidOperationException(
                "Signing-key configuration is invalid: the previous signing key is not approved for validation during the overlap window.");
        }

        throw new InvalidOperationException("The requested signing key identifier is not approved or is unavailable.");
    }

    public ValueTask<ValidationKeyMaterial> GetValidationKeyAsync(
        string keyIdentifier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new InvalidOperationException("The signing key identifier is required.");
        }

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        var isPreviousKey = string.Equals(options.PreviousKeyId, keyIdentifier, StringComparison.Ordinal);
        if (isPreviousKey &&
            (!options.PreviousKeyExpiresAt.HasValue || options.PreviousKeyExpiresAt.Value <= DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException(
                "Signing-key configuration is invalid: previous signing key overlap has expired or is invalid.");
        }

        if (!_configuredKeys.TryGetValue(keyIdentifier, out var key))
        {
            if (string.Equals(options.ActiveKeyId, keyIdentifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The configured active signing key is unavailable.");
            }

            if (!isPreviousKey)
            {
                throw new InvalidOperationException("The requested signing key identifier is not approved or is unavailable.");
            }

            throw new InvalidOperationException("The configured previous signing key is unavailable during the overlap window.");
        }

        return ValueTask.FromResult(
            new ValidationKeyMaterial(keyIdentifier, RSA.Create(key.ExportParameters(false))));
    }

    private static IReadOnlyDictionary<string, RSA> BuildKeyMap(IEnumerable<KeyValuePair<string, RSA>> configuredKeys)
    {
        var map = new Dictionary<string, RSA>(StringComparer.Ordinal);

        foreach (var kvp in configuredKeys)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                throw new InvalidOperationException("Signing key identifiers must not be empty.");
            }

            if (kvp.Value is null)
            {
                throw new InvalidOperationException("Signing key material must not be null.");
            }

            if (map.ContainsKey(kvp.Key))
            {
                throw new InvalidOperationException("Signing key identifiers must be unique.");
            }

            map[kvp.Key] = kvp.Value;
        }

        return map;
    }
}
