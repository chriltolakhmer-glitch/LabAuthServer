using System.Text;
using System.Text.Json;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

public sealed class JsonLicenseDocumentParserTests
{
    private readonly JsonLicenseDocumentParser _parser = new();

    [Fact]
    public void ValidContainer_ParsesAllRequiredProperties()
    {
        var outcome = _parser.Parse(BuildContainer());

        Assert.True(outcome.Succeeded);
        var license = Assert.IsType<SignedLicense>(outcome.License);
        Assert.Equal(1, license.Document.LicenseVersion);
        Assert.Equal("license-0001", license.Document.LicenseId);
        Assert.Equal(LicenseConstants.Product, license.Document.Product);
        Assert.Equal(LicenseEdition.Professional, license.Document.Edition);
        Assert.Equal("key-2026", license.Signature.KeyId);
        Assert.Equal(LicenseConstants.RsaPssSha256Algorithm, license.Signature.Algorithm);
        Assert.NotEmpty(license.Signature.Signature);
        Assert.NotEmpty(license.SignedPayload);
    }

    [Fact]
    public void PerpetualLicense_ParsesWithNullExpiration()
    {
        var outcome = _parser.Parse(BuildContainer(payload =>
        {
            payload["expiresAt"] = null;
        }));

        Assert.True(outcome.Succeeded);
        Assert.Null(outcome.License!.Document.ExpiresAt);
        Assert.True(outcome.License.Document.IsPerpetual);
    }

    [Fact]
    public void UnsupportedVersion_IsRejected()
    {
        var outcome = _parser.Parse(BuildContainer(payload => payload["licenseVersion"] = 99));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldInvalid, outcome.Failure);
    }

    [Fact]
    public void UnknownEdition_IsRejected()
    {
        var outcome = _parser.Parse(BuildContainer(payload => payload["edition"] = "Ultimate"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldInvalid, outcome.Failure);
    }

    [Fact]
    public void MissingRequiredField_IsRejected()
    {
        var outcome = _parser.Parse(BuildContainer(payload => payload.Remove("licenseId")));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldInvalid, outcome.Failure);
    }

    [Fact]
    public void MissingSignatureEnvelope_IsReportedAsSignatureMalformed()
    {
        var payload = new Dictionary<string, object?>
        {
            ["licenseVersion"] = 1,
            ["licenseId"] = "license-0001",
            ["product"] = LicenseConstants.Product,
            ["edition"] = "Professional",
            ["issuedAt"] = "2026-09-11T00:00:00Z"
        };

        var container = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object?>
        {
            ["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))),
            ["algorithm"] = LicenseConstants.RsaPssSha256Algorithm,
            ["keyId"] = "key-2026"
        });

        var outcome = _parser.Parse(container);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    [Fact]
    public void MalformedJson_ProducesStructuredFailure()
    {
        var outcome = _parser.Parse(Encoding.UTF8.GetBytes("{ not json"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    [Fact]
    public void Utf8Bom_IsRejected()
    {
        var bytes = new List<byte> { 0xEF, 0xBB, 0xBF };
        bytes.AddRange(BuildContainer());

        var outcome = _parser.Parse(bytes.ToArray());

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    [Fact]
    public void FloatLimit_IsRejected()
    {
        var outcome = _parser.Parse(BuildContainer(payload =>
        {
            payload["limits"] = "raw:{\"max.users\":1.5}";
        }));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldInvalid, outcome.Failure);
    }

    private static byte[] BuildContainer(Action<Dictionary<string, object?>>? mutate = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["licenseVersion"] = 1,
            ["licenseId"] = "license-0001",
            ["product"] = LicenseConstants.Product,
            ["edition"] = "Professional",
            ["customer"] = "Example Customer",
            ["issuedAt"] = "2026-09-11T00:00:00Z",
            ["expiresAt"] = "2027-09-11T00:00:00Z",
            ["features"] = new[] { "auth.ldap" },
            ["limits"] = new Dictionary<string, int> { ["max.users"] = 100 }
        };

        mutate?.Invoke(payload);

        string payloadJson;
        if (payload["limits"] is string raw && raw.StartsWith("raw:", StringComparison.Ordinal))
        {
            var clone = new Dictionary<string, object?>(payload);
            clone.Remove("limits");
            var json = JsonSerializer.Serialize(clone);
            payloadJson = json.Insert(json.Length - 1, $",\"limits\":{raw[4..]}");
        }
        else
        {
            payloadJson = JsonSerializer.Serialize(payload);
        }

        var container = new Dictionary<string, object?>
        {
            ["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)),
            ["algorithm"] = LicenseConstants.RsaPssSha256Algorithm,
            ["keyId"] = "key-2026",
            ["signature"] = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        };

        return JsonSerializer.SerializeToUtf8Bytes(container);
    }
}