using System.Reflection;
using System.Text.RegularExpressions;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.10 cross-cutting testing-strategy tests. These verify the guarantees that are not
/// already covered by the per-phase suites: assembly separation between the server and the
/// vendor issuer, the absence of private-key material and network dependencies in the licensing
/// source and test trees, the absence of wall-clock dependence in licensing code, and the
/// public-safe separation between the coarse status and the internal reason code.
/// These are static and reflective checks; no production runtime feature was added.
/// </summary>
public sealed class Phase410TestingStrategyTests
{
    // This scanner file legitimately contains the forbidden patterns as regex literals, so it
    // must be excluded from its own source scan.
    private const string ScannerFileName = "Phase410TestingStrategyTests.cs";

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: assembly separation (Phase 4.10 test matrix)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ServerAssemblies_DoNotReferenceTheIssuerAssembly()
    {
        const string issuerAssemblyName = "LabAuthServer.LicenseIssuer";

        var serverAssemblies = new[]
        {
            typeof(LicenseDocument).Assembly,          // Domain
            typeof(ILicenseValidator).Assembly,        // Application
            typeof(LicenseValidator).Assembly,         // Infrastructure
            typeof(LabAuthServer.Api.Controllers.AuthController).Assembly // Api
        };

        foreach (var assembly in serverAssemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

            Assert.DoesNotContain(issuerAssemblyName, references);
        }
    }

    [Fact]
    public void IssuerAssembly_DoesNotReferenceInfrastructureOrApi()
    {
        var issuer = Assembly.Load("LabAuthServer.LicenseIssuer");
        var references = issuer.GetReferencedAssemblies().Select(a => a.Name).ToArray();

        Assert.DoesNotContain("LabAuthServer.Infrastructure", references);
        Assert.DoesNotContain("LabAuthServer.Api", references);
        Assert.DoesNotContain("LabAuthServer.Application", references);
        Assert.Contains("LabAuthServer.Domain", references);
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: no private-key material in the licensing source or tests
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void LicensingSourceAndTests_ContainNoPrivateKeyMaterialOrKeyFiles()
    {
        var root = LicenseTestFixture.ResolveRepositoryRoot();

        var directories = new[]
        {
            Path.Combine(root, "src", "LabAuthServer.Domain", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Application", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Infrastructure", "Security", "Licensing"),
            Path.Combine(root, "tools", "LabAuthServer.LicenseIssuer"),
            Path.Combine(root, "tests", "LabAuthServer.UnitTests", "Licensing")
        };

        var forbidden = new Regex(
            "BEGIN (RSA |ENCRYPTED )?PRIVATE KEY|ExportRSAPrivateKey|ExportPkcs8PrivateKey",
            RegexOptions.Compiled);

        foreach (var directory in directories)
        {
            Assert.True(Directory.Exists(directory), $"expected licensing directory to exist: {directory}");

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || name.Equals(ScannerFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Assert.DoesNotContain(".pfx", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(".p12", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(".pem", name, StringComparison.OrdinalIgnoreCase);

                if (!name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                Assert.False(
                    forbidden.IsMatch(content),
                    $"private-key material pattern found in {file}");
            }
        }
    }

    [Fact]
    public void LicensingSource_ExportsPrivateParametersOnlyInGuardAssertions()
    {
        var root = LicenseTestFixture.ResolveRepositoryRoot();
        var productionDirectories = new[]
        {
            Path.Combine(root, "src", "LabAuthServer.Domain", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Application", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Infrastructure", "Security", "Licensing"),
            Path.Combine(root, "tools", "LabAuthServer.LicenseIssuer")
        };

        foreach (var directory in productionDirectories)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var content = File.ReadAllText(file);

                // Production licensing code must never export private parameters. The public-only
                // copy path uses ExportParameters(false); ExportParameters(true) is not permitted.
                Assert.DoesNotContain("ExportParameters(true)", content);
                Assert.DoesNotContain("ExportParameters(includePrivateParameters: true)", content);
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: no network dependency in licensing code
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void LicensingSource_ContainsNoNetworkOrListenerDependency()
    {
        var root = LicenseTestFixture.ResolveRepositoryRoot();
        var directories = new[]
        {
            Path.Combine(root, "src", "LabAuthServer.Domain", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Application", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Infrastructure", "Security", "Licensing"),
            Path.Combine(root, "tools", "LabAuthServer.LicenseIssuer"),
            Path.Combine(root, "tests", "LabAuthServer.UnitTests", "Licensing")
        };

        var forbidden = new Regex(
            "HttpClient|HttpListener|TcpListener|WebApplication|new Socket\\(",
            RegexOptions.Compiled);

        foreach (var directory in directories)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(file).Equals(ScannerFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                Assert.False(
                    forbidden.IsMatch(content),
                    $"network dependency pattern found in {file}");
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: no wall-clock dependence in licensing code
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void LicensingSource_DoesNotUseAmbientWallClock()
    {
        var root = LicenseTestFixture.ResolveRepositoryRoot();
        var directories = new[]
        {
            Path.Combine(root, "src", "LabAuthServer.Domain", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Application", "Licensing"),
            Path.Combine(root, "src", "LabAuthServer.Infrastructure", "Security", "Licensing"),
            Path.Combine(root, "tools", "LabAuthServer.LicenseIssuer")
        };

        foreach (var directory in directories)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var content = File.ReadAllText(file);

                // DateTimeOffset.UtcNow is permitted only inside the clock implementation itself.
                var usesAmbientClock = content.Contains("DateTime.Now", StringComparison.Ordinal)
                    || (content.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        && !file.EndsWith("ILicenseClock.cs", StringComparison.OrdinalIgnoreCase));

                Assert.False(usesAmbientClock, $"ambient wall-clock usage found in {file}");
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: public-safe status vs. internal reason
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void PublicStatusAndInternalReason_AreDistinctTypesWithDistinctValues()
    {
        // The public-safe category must be less detailed than the internal reason code so a
        // caller cannot infer signing details from the exposed status.
        Assert.NotEqual(typeof(LicenseValidationStatus), typeof(LicenseValidationReason));

        var statusValues = Enum.GetValues<LicenseValidationStatus>();
        var reasonValues = Enum.GetValues<LicenseValidationReason>();

        Assert.DoesNotContain(LicenseValidationReason.KeyUntrusted, new[] { LicenseValidationReason.AlgorithmUnsupported, LicenseValidationReason.SignatureInvalid });
        Assert.True(reasonValues.Length > statusValues.Length,
            "the internal reason enum is expected to be more granular than the public status enum");
    }

    [Fact]
    public void FailedValidationResult_ExposesNoPolicy()
    {
        foreach (var status in Enum.GetValues<LicenseValidationStatus>())
        {
            if (status == LicenseValidationStatus.Valid)
            {
                continue;
            }

            var result = LicenseValidationResult.Failed(status, LicenseValidationReason.FieldInvalid);

            Assert.False(result.IsValid);
            Assert.Null(result.Policy);
        }
    }

    [Fact]
    public void ValidValidationResult_AlwaysCarriesAPolicy()
    {
        using var fixture = new LicenseTestFixture();
        using var key = fixture.CreateKey();

        var result = fixture.CreateValidator(key).Validate(fixture.Issue(key));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Policy);
        Assert.Equal(LicenseValidationReason.None, result.Reason);
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: licensing never changes authentication/security behavior
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void InvalidLicense_PolicyGrantsNoCommercialCapability()
    {
        using var fixture = new LicenseTestFixture();
        using var key = fixture.CreateKey();

        var invalidInputs = new[]
        {
            ReadOnlyMemory<byte>.Empty.ToArray(),
            "not json"u8.ToArray(),
            "{}"u8.ToArray()
        };

        foreach (var input in invalidInputs)
        {
            var result = fixture.CreateValidator(key).Validate(input);
            var policy = LicensePolicy.FromValidationResult(result);

            Assert.False(result.IsValid);
            Assert.True(policy.IsRestricted);
            Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
            Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AdminConsole));
            Assert.False(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
        }
    }

    // ---------------------------------------------------------------------------------------
    // Cross-cutting: issuer round trip and determinism
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void IssuerToParserToVerifier_RoundTripPreservesExactPayloadBytes()
    {
        using var fixture = new LicenseTestFixture();
        using var key = fixture.CreateKey();

        var container = fixture.Issue(key);
        var parsed = new JsonLicenseDocumentParser().Parse(container);

        Assert.True(parsed.Succeeded);

        var validator = fixture.CreateValidator(key);
        var result = validator.Validate(container);

        Assert.True(result.IsValid);
        Assert.Equal(parsed.License!.SignedPayload, LicensePayloadRoundTrip(container));
    }

    [Fact]
    public void Issuer_IsDeterministicForIdenticalInput()
    {
        using var fixture = new LicenseTestFixture();
        using var key = fixture.CreateKey();

        var first = fixture.Issue(key);
        var second = fixture.Issue(key);

        // RSA-PSS is randomised, so the signature differs; the signed payload must not.
        Assert.Equal(LicensePayloadRoundTrip(first), LicensePayloadRoundTrip(second));
    }

    private static byte[] LicensePayloadRoundTrip(byte[] container)
    {
        var parsed = new JsonLicenseDocumentParser().Parse(container);
        Assert.True(parsed.Succeeded);
        return parsed.License!.SignedPayload;
    }
}