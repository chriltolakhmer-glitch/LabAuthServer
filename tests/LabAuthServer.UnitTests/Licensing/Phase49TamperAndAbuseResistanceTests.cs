using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;
using IssuerType = LabAuthServer.LicenseIssuer.LicenseIssuer;
using LabAuthServer.LicenseIssuer;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.9 tamper and abuse resistance tests. Every fixture issues a real license through the
/// Phase 4.4 issuer with an ephemeral in-memory key, then mutates the container or payload. The
/// suite verifies that no tampered document becomes valid and that no failure path yields a
/// licensing capability. No private key, no network dependency, no production key material.
/// </summary>
public sealed class Phase49TamperAndAbuseResistanceTests
{
    private const string KeyId = "lab-license-signing-2026";

    private static readonly DateTimeOffset IssuedAt = new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = new(2027, 9, 11, 0, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------------------------
    // THREAT A/B/F: payload, signature, edition, feature, limit, expiry, issuedAt, customer
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void PayloadTamper_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = ReplacePayload(container, payload =>
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text.Replace(
                LicenseConstants.Product, "EvilProduct", StringComparison.Ordinal)));
        });

        // Signature verification precedes semantic validation, so any payload edit fails there.
        AssertDenied(tampered, key, LicenseValidationStatus.InvalidSignature);
    }

    [Fact]
    public void SignatureTamper_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root =>
        {
            var signature = Convert.FromBase64String(root["signature"]);
            signature[0] ^= 0x01;
            root["signature"] = Convert.ToBase64String(signature);
        });

        AssertDenied(tampered, key, LicenseValidationStatus.InvalidSignature);
    }

    [Fact]
    public void KeyIdTamper_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root => root["keyId"] = "unknown-key");

        AssertDenied(tampered, key, LicenseValidationStatus.UnknownKey);
    }

    [Fact]
    public void AlgorithmTamper_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root => root["algorithm"] = "RS256");

        AssertDenied(tampered, key, LicenseValidationStatus.InvalidSignature);
    }

    [Theory]
    [InlineData("edition", "Enterprise")]
    [InlineData("features", "auth.ldap,admin.console")]
    [InlineData("limits", "max.users=99999")]
    [InlineData("expiresAt", "2099-09-11T00:00:00Z")]
    [InlineData("issuedAt", "2020-01-01T00:00:00Z")]
    [InlineData("customer", "Someone Else")]
    public void PayloadFieldTamper_IsDenied(string field, string newValue)
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = ReplacePayload(container, payload =>
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(text);
            var pairs = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetRawText(), StringComparer.Ordinal);

            pairs[field] = field switch
            {
                "features" => "[\"auth.ldap\",\"admin.console\"]",
                "limits" => "{\"max.users\":99999}",
                _ => "\"" + newValue + "\""
            };

            var rebuilt = "{" + string.Join(",", pairs.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) + "}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(rebuilt));
        });

        AssertDenied(tampered, key);
    }

    // ---------------------------------------------------------------------------------------
    // THREAT E: unknown and duplicate JSON properties
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void UnknownContainerProperty_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root => root["extra"] = "value");

        AssertDenied(tampered, key, LicenseValidationStatus.InvalidContent);
    }

    [Fact]
    public void UnknownPayloadProperty_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = ReplacePayload(container, payload =>
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var injected = text.Insert(text.Length - 1, ",\"machineBinding\":\"abc\"");
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(injected));
        });

        AssertDenied(tampered, key, LicenseValidationStatus.InvalidContent);
    }

    // ---------------------------------------------------------------------------------------
    // THREAT G: malformed, oversized and pathological input
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}")]
    public void MalformedOrNonObjectContainer_IsDenied(string content)
    {
        using var key = RSA.Create(3072);
        var validator = CreateValidator(key);

        var result = validator.Validate(Encoding.UTF8.GetBytes(content));

        Assert.False(result.IsValid);
        Assert.Null(result.Policy);
    }

    [Fact]
    public void InvalidUtf8_IsDenied()
    {
        using var key = RSA.Create(3072);
        var validator = CreateValidator(key);

        var result = validator.Validate(new byte[] { 0xFF, 0xFE, 0xFD, 0x00 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Utf8Bom_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);
        var bytes = new List<byte> { 0xEF, 0xBB, 0xBF };
        bytes.AddRange(container);

        var result = CreateValidator(key).Validate(bytes.ToArray());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Malformed, result.Status);
    }

    [Fact]
    public void TrailingData_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);
        var bytes = container.Concat(Encoding.UTF8.GetBytes(" trailing")).ToArray();

        var result = CreateValidator(key).Validate(bytes);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MalformedBase64Payload_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root => root["payload"] = "!!!!");

        AssertDenied(tampered, key);
    }

    [Fact]
    public void WhitespaceBase64Payload_IsDenied()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tampered = MutateContainer(container, root => root["payload"] = root["payload"].Insert(4, " "));

        AssertDenied(tampered, key);
    }

    [Fact]
    public void OversizedFeatureArray_IsDeniedByDocumentedBound()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);

        var tooMany = string.Join(",", Enumerable.Range(0, LicenseValidationPolicy.MaximumFeatureCount + 1)
            .Select(i => "\"feature" + i + "\""));

        var tampered = ReplacePayload(container, payload =>
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var start = text.IndexOf("\"features\":[", StringComparison.Ordinal);
            var end = text.IndexOf(']', start);
            var rebuilt = text[..(start + "\"features\":".Length)] + "[" + tooMany + "]" + text[(end + 1)..];
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(rebuilt));
        });

        AssertDenied(tampered, key, LicenseValidationStatus.InvalidSignature);
    }

    [Fact]
    public void DeeplyNestedJson_IsDeniedWithoutRecursionFailure()
    {
        using var key = RSA.Create(3072);
        var validator = CreateValidator(key);

        var nested = new StringBuilder("{\"payload\":");
        for (var i = 0; i < 64; i++) nested.Append('[');
        for (var i = 0; i < 64; i++) nested.Append(']');
        nested.Append('}');

        var result = validator.Validate(Encoding.UTF8.GetBytes(nested.ToString()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void LargeMalformedInput_IsDenied()
    {
        using var key = RSA.Create(3072);
        var validator = CreateValidator(key);

        var large = new byte[1024 * 1024];
        for (var i = 0; i < large.Length; i++) large[i] = (byte)'x';

        var result = validator.Validate(large);

        Assert.False(result.IsValid);
    }

    // ---------------------------------------------------------------------------------------
    // Cryptographic tampering
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void WrongPublicKey_IsDenied()
    {
        using var signingKey = RSA.Create(3072);
        using var otherKey = RSA.Create(3072);
        var container = Issue(signingKey);

        var result = CreateValidator(otherKey).Validate(container);

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidSignature, result.Status);
    }

    [Fact]
    public void WeakTrustedKey_IsDenied()
    {
        using var signingKey = RSA.Create(3072);
        using var weakKey = RSA.Create(1024);
        var container = Issue(signingKey);

        var result = CreateValidator(weakKey).Validate(container);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RotatedTrustedKey_StillVerifiesItsOwnLicense()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);

        var containerA = Issue(keyA, "key-a");
        var containerB = Issue(keyB, "key-b");

        using var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add("key-a", keyA);
        provider.Add("key-b", keyB);
        var validator = new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new RsaPssLicenseSignatureVerifier(provider),
            new FixedClock(IssuedAt));

        Assert.True(validator.Validate(containerA).IsValid);
        Assert.True(validator.Validate(containerB).IsValid);
    }

    [Fact]
    public void DuplicateTrustedKeyId_IsRejected()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);
        using var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add("key-a", keyA);

        Assert.Throws<InvalidOperationException>(() => provider.Add("key-a", keyB));
    }

    // ---------------------------------------------------------------------------------------
    // Fail-closed guarantees
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void NoTamperedInput_ProducesValidOrPolicy()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);
        var validator = CreateValidator(key);

        var inputs = new List<byte[]>
        {
            Array.Empty<byte>(),
            Encoding.UTF8.GetBytes("{ not json"),
            Encoding.UTF8.GetBytes("{}"),
            container.Concat(Encoding.UTF8.GetBytes("x")).ToArray(),
            MutateContainer(container, r => r["algorithm"] = "none"),
            MutateContainer(container, r => r["keyId"] = "x"),
            MutateContainer(container, r => r["signature"] = "AAAA")
        };

        foreach (var input in inputs)
        {
            var result = validator.Validate(input);
            Assert.False(result.IsValid);
            Assert.Null(result.Policy);
            Assert.False(LicensePolicy.FromValidationResult(result).IsRestricted == false);
        }
    }

    [Fact]
    public void MissingLicense_DeniesEveryCommercialFeature()
    {
        using var key = RSA.Create(3072);
        var result = CreateValidator(key).Validate(ReadOnlySpan<byte>.Empty);
        var policy = LicensePolicy.FromValidationResult(result);

        Assert.False(result.IsValid);
        Assert.True(policy.IsRestricted);
        foreach (var feature in LicenseFeatureIds.All)
        {
            Assert.False(policy.IsFeatureEnabled(feature));
        }
    }

    [Fact]
    public void TamperedLicense_DeniesEveryCommercialFeature()
    {
        using var key = RSA.Create(3072);
        var container = Issue(key);
        var tampered = MutateContainer(container, root => root["keyId"] = "unknown-key");

        var policy = LicensePolicy.FromValidationResult(CreateValidator(key).Validate(tampered));

        Assert.True(policy.IsRestricted);
        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
        Assert.False(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static byte[] Issue(RSA key, string keyId = KeyId)
    {
        var issuer = new IssuerType(new EphemeralSigningKeyProvider(keyId, key));
        var result = issuer.Issue(new LicenseIssuanceRequest
        {
            LicenseId = "license-0001",
            Edition = LicenseEdition.Professional,
            Customer = "Example Customer",
            IssuedAt = IssuedAt,
            ExpiresAt = ExpiresAt,
            Features = new[] { LicenseFeatureIds.AuthLdap },
            Limits = new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 },
            KeyId = keyId
        });

        Assert.True(result.Succeeded, $"issuance failed: {result.Reason}");
        return result.ContainerBytes!;
    }

    private static LicenseValidator CreateValidator(RSA key, string keyId = KeyId)
    {
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(keyId, key);
        return new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new RsaPssLicenseSignatureVerifier(provider),
            new FixedClock(IssuedAt));
    }

    private static void AssertDenied(byte[] container, RSA key, LicenseValidationStatus? expected = null)
    {
        var result = CreateValidator(key).Validate(container);

        Assert.False(result.IsValid);
        Assert.Null(result.Policy);
        Assert.NotEqual(LicenseValidationReason.None, result.Reason);
        if (expected is { } status)
        {
            Assert.Equal(status, result.Status);
        }
    }

    private static byte[] MutateContainer(byte[] container, Action<Dictionary<string, string>> mutate)
    {
        using var document = JsonDocument.Parse(container);
        var fields = document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);

        mutate(fields);
        return JsonSerializer.SerializeToUtf8Bytes(fields);
    }

    private static byte[] ReplacePayload(byte[] container, Func<string, string> transform)
    {
        using var document = JsonDocument.Parse(container);
        var fields = document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);

        fields["payload"] = transform(fields["payload"]);
        return JsonSerializer.SerializeToUtf8Bytes(fields);
    }

    private sealed class EphemeralSigningKeyProvider : ILicenseSigningKeyProvider
    {
        private readonly RSA _key;

        public EphemeralSigningKeyProvider(string keyId, RSA key)
        {
            KeyId = keyId;
            _key = key;
        }

        public string KeyId { get; }

        public RSA? TryGetSigningKey() => _key;
    }

    private sealed class FixedClock : ILicenseClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }
}