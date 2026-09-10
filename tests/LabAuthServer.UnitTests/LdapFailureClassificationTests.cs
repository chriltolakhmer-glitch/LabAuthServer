using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class LdapFailureClassificationTests
{
    private const string User = "reader@lab.local";
    private const string SecretMarker = "SENSITIVE-password-DN-filter-host-stack";

    public static TheoryData<string, DirectoryFailureStage, AuthenticationFailureCategory, DirectoryFailureReason> AuthenticationFailures => new()
    {
        { "49", DirectoryFailureStage.UserBind, AuthenticationFailureCategory.InvalidCredentials, DirectoryFailureReason.UserBindRejected },
        { "49", DirectoryFailureStage.ServiceBind, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.ServiceBindRejected },
        { "81", DirectoryFailureStage.ConnectionSetup, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "refused", DirectoryFailureStage.ConnectionSetup, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "dns", DirectoryFailureStage.ConnectionSetup, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "network", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "io", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "tls", DirectoryFailureStage.ConnectionSetup, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure },
        { "51", DirectoryFailureStage.ServiceBind, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.DirectoryBusy },
        { "52", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.DirectoryUnavailable },
        { "85", DirectoryFailureStage.UserBind, AuthenticationFailureCategory.Timeout, DirectoryFailureReason.OperationTimedOut },
        { "timeout", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Timeout, DirectoryFailureReason.OperationTimedOut },
        { "3", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Timeout, DirectoryFailureReason.OperationTimedOut },
        { "cancel", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Cancelled, DirectoryFailureReason.CallerCancelled },
        { "disposed-cancel", DirectoryFailureStage.UserBind, AuthenticationFailureCategory.Cancelled, DirectoryFailureReason.CallerCancelled },
        { "ldap-cancel", DirectoryFailureStage.ServiceBind, AuthenticationFailureCategory.Cancelled, DirectoryFailureReason.CallerCancelled },
        { "unassociated-cancel", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnassociatedCancellation },
        { "1", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.OperationFailed },
        { "2", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.ProtocolError },
        { "4", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.ResultLimitExceeded },
        { "11", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.ResultLimitExceeded },
        { "operation-unknown", DirectoryFailureStage.ServiceBind, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.MissingOperationResult },
        { "unexpected", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnexpectedFailure },
        { "runtime-invalid-operation", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnexpectedFailure },
        { "80", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnexpectedFailure },
        { "49", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnexpectedFailure }
    };

    [Theory]
    [MemberData(nameof(AuthenticationFailures))]
    public async Task Authentication_ClassifiesInjectedOperationAtOrigin(string kind, DirectoryFailureStage stage,
        AuthenticationFailureCategory category, DirectoryFailureReason reason)
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new FakeFactory { FailureStage = stage, FailureKind = kind, Cancellation = cancellation };
        var client = new LdapAuthenticationClient(factory);
        var result = await client.AuthenticateAsync(User, "user-secret", Options(), new CredentialProvider(), cancellation.Token);

        Assert.False(result.IsAuthenticated);
        Assert.NotNull(result.Failure);
        Assert.Equal(category, result.Failure.Category);
        Assert.Equal(stage, result.Failure.Stage);
        Assert.Equal(reason, result.Failure.Reason);
        Assert.DoesNotContain(SecretMarker, result.ErrorMessage);
        Assert.Null(result.Username);
        if (int.TryParse(kind, out var code))
        {
            Assert.Equal(code, result.Failure.DiagnosticCode);
            Assert.Equal(DirectoryDiagnosticSource.LdapError, result.Failure.DiagnosticSource);
        }
        if (kind == "operation-unknown")
        {
            Assert.Null(result.Failure.DiagnosticCode);
            Assert.Equal(DirectoryDiagnosticSource.None, result.Failure.DiagnosticSource);
        }
        Assert.True(factory.Disposed || stage == DirectoryFailureStage.ConnectionSetup);
        if (stage == DirectoryFailureStage.UserSearch) Assert.Equal(1, factory.BindCount);
    }

    [Theory]
    [InlineData(1, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(2, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(3, AuthenticationFailureCategory.Timeout)]
    [InlineData(4, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(11, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(51, AuthenticationFailureCategory.DirectoryUnavailable)]
    [InlineData(52, AuthenticationFailureCategory.DirectoryUnavailable)]
    public async Task ReturnedServerCode_IsExplicitlySourced(int code, AuthenticationFailureCategory category)
    {
        var factory = new FakeFactory { Response = new((ResultCode)code, []) };
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Options(), new CredentialProvider());
        Assert.Equal(category, result.FailureCategory);
        Assert.Equal(DirectoryFailureStage.UserSearch, result.Failure!.Stage);
        Assert.Equal(code, result.Failure.DiagnosticCode);
        Assert.Equal(DirectoryDiagnosticSource.OperationResult, result.Failure.DiagnosticSource);
        Assert.Equal(1, factory.BindCount);
    }

    [Fact]
    public async Task Success_PreservesServiceSearchUserBindSequenceAndSuppliedUpn()
    {
        var factory = new FakeFactory();
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "user-secret", Options(), new CredentialProvider());
        Assert.True(result.IsAuthenticated);
        Assert.Null(result.Failure);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(User, result.Username);
        Assert.Equal(new[] { "configure", "bind:service@lab.local", "search", "bind:" + User, "dispose" }, factory.Sequence);
        Assert.Equal(new[] { "service-secret", "user-secret" }, factory.Passwords);
        Assert.True(factory.Ldaps);
        Assert.Equal(TimeSpan.FromSeconds(10), factory.SeenTimeout);
    }

    [Theory]
    [InlineData(1, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(2, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(3, AuthenticationFailureCategory.Timeout)]
    [InlineData(4, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(11, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData(51, AuthenticationFailureCategory.DirectoryUnavailable)]
    [InlineData(52, AuthenticationFailureCategory.DirectoryUnavailable)]
    public async Task OperationException_WithResponseRetainsCodeAndSource(int code, AuthenticationFailureCategory category)
    {
        // The provider's SearchResponse constructor is internal. Reflection is
        // confined to this test fixture; production uses actual SendRequest output.
        var response = (SearchResponse)Activator.CreateInstance(typeof(SearchResponse),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null, args: [null, null, (ResultCode)code, SecretMarker, null], culture: null)!;
        var factory = new FakeFactory
        {
            FailureStage = DirectoryFailureStage.UserSearch,
            InjectedException = new DirectoryOperationException(response, SecretMarker)
        };
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Options(), new CredentialProvider());
        Assert.Equal(category, result.FailureCategory);
        Assert.Equal(code, result.Failure!.DiagnosticCode);
        Assert.Equal(DirectoryDiagnosticSource.OperationResult, result.Failure.DiagnosticSource);
        Assert.Equal(1, factory.BindCount);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("zero")]
    [InlineData("multiple")]
    [InlineData("references")]
    [InlineData("missing-uac")]
    [InlineData("malformed-uac")]
    [InlineData("multiple-uac")]
    [InlineData("missing-upn")]
    [InlineData("mismatched-upn")]
    [InlineData("missing-dn")]
    [InlineData("malformed-dn")]
    public async Task InvalidIdentity_IsProtocolFailureAndNeverBindsUser(string condition)
    {
        var factory = new FakeFactory { Response = IdentityResponse(condition) };
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Options(), new CredentialProvider());
        Assert.Equal(AuthenticationFailureCategory.ProtocolFailure, result.FailureCategory);
        Assert.Equal(DirectoryFailureStage.ResponseValidation, result.Failure!.Stage);
        Assert.Equal(1, factory.BindCount);
    }

    [Fact]
    public async Task DisabledAccount_RemainsAuthoritativeRejection()
    {
        var factory = new FakeFactory { Response = IdentityResponse("disabled") };
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Options(), new CredentialProvider());
        Assert.Equal(AuthenticationFailureCategory.InvalidCredentials, result.FailureCategory);
        Assert.Equal(DirectoryFailureReason.AccountDisabled, result.Failure!.Reason);
        Assert.Equal(1, factory.BindCount);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("io")]
    public async Task CredentialFailure_IsConfigurationAndDoesNotCreateConnection(string kind)
    {
        var factory = new FakeFactory();
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Options(), new CredentialProvider(kind));
        Assert.Equal(AuthenticationFailureCategory.Configuration, result.FailureCategory);
        Assert.Equal(DirectoryFailureStage.CredentialLoading, result.Failure!.Stage);
        Assert.Equal(0, factory.CreateCount);
        Assert.DoesNotContain(SecretMarker, result.ErrorMessage);
    }

    [Theory]
    [InlineData("groups", true)]
    [InlineData("empty", true)]
    [InlineData("missing-membership", true)]
    [InlineData("bad-dn", false)]
    [InlineData("ranged", false)]
    [InlineData("mixed-ranged", false)]
    [InlineData("multiple", false)]
    [InlineData("zero", false)]
    [InlineData("null", false)]
    [InlineData("references", false)]
    public async Task Groups_DistinguishCompleteMembershipFromInvalidResponse(string condition, bool success)
    {
        var response = GroupResponse(condition);
        var factory = new FakeFactory { Response = response };
        var result = await Groups(factory).GetUserGroupsAsync(User, "user-secret");
        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(new[] { "service-secret" }, factory.Passwords);
        if (success)
        {
            Assert.Null(result.Failure);
            Assert.Equal(condition == "groups" ? new[] { "GG-APP-USER" } : Array.Empty<string>(), result.Groups);
        }
        else
        {
            Assert.Equal(AuthenticationFailureCategory.ProtocolFailure, result.Failure!.Category);
            Assert.Equal(DirectoryFailureStage.ResponseValidation, result.Failure.Stage);
            Assert.Throws<InvalidOperationException>(() => result.Groups);
        }
    }

    [Theory]
    [InlineData("49", DirectoryFailureStage.ServiceBind, AuthenticationFailureCategory.DirectoryUnavailable)]
    [InlineData("81", DirectoryFailureStage.ConnectionSetup, AuthenticationFailureCategory.DirectoryUnavailable)]
    [InlineData("52", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.DirectoryUnavailable)]
    [InlineData("timeout", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Timeout)]
    [InlineData("cancel", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Cancelled)]
    [InlineData("2", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.ProtocolFailure)]
    [InlineData("unexpected", DirectoryFailureStage.UserSearch, AuthenticationFailureCategory.Unexpected)]
    public async Task GroupFailures_AreTypedAndLoggedWithoutExceptionObjects(string kind, DirectoryFailureStage stage, AuthenticationFailureCategory category)
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new FakeFactory { FailureKind = kind, FailureStage = stage, Cancellation = cancellation };
        var logger = new CapturingLogger<LdapService>();
        var result = await Groups(factory, logger).GetUserGroupsAsync(User, "synthetic", cancellation.Token);
        Assert.False(result.IsSuccess);
        Assert.Equal(category, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.GroupSearch, result.Failure.Stage);
        Assert.Throws<InvalidOperationException>(() => result.Groups);
        var log = Assert.Single(logger.Entries);
        Assert.Null(log.Exception);
        Assert.DoesNotContain(SecretMarker, log.Text);
        Assert.DoesNotContain("service-secret", log.Text);
        Assert.Contains(category.ToString(), log.Text);
        if (kind == "49") Assert.Equal(DirectoryFailureReason.ServiceBindRejected, result.Failure.Reason);
    }

    [Theory]
    [InlineData("CN=GG-APP-USER,OU=Groups,DC=lab,DC=local", "GG-APP-USER")]
    [InlineData("CN=Team\\, East,OU=Groups,DC=lab,DC=local", "Team, East")]
    [InlineData("CN=Jos\\C3\\A9,OU=Groups,DC=lab,DC=local", "José")]
    public void GroupDn_DecodesEscapesWithoutChangingIdentity(string dn, string expected)
    {
        Assert.True(LdapDistinguishedNameParser.TryParse(dn, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("CN=Reader+UID=other,DC=lab,DC=local")]
    [InlineData("CN=Reader\\,DC=lab,DC=local\\")]
    [InlineData("CN=Reader\\00,DC=lab,DC=local")]
    [InlineData("CN=Reader\\FF,DC=lab,DC=local")]
    [InlineData("CN=,DC=lab,DC=local")]
    public void GroupDn_RejectsAmbiguousOrMalformedValues(string dn)
        => Assert.False(LdapDistinguishedNameParser.TryParse(dn, out _));

    private static LdapService Groups(FakeFactory factory, ILogger<LdapService>? logger = null)
        => new(Microsoft.Extensions.Options.Options.Create(Options()), new CredentialProvider(), logger ?? new CapturingLogger<LdapService>(), factory);

    private static LdapOptions Options() => new()
    {
        Host = "directory.example.test", Domain = "lab.local", Port = 636, UseLdaps = true,
        ConnectionTimeout = TimeSpan.FromSeconds(10), ServiceAccountUsername = "service@lab.local",
        UserSearchBaseDn = "DC=lab,DC=local", BaseDn = "DC=lab,DC=local"
    };

    private static LdapSearchResult? IdentityResponse(string condition = "valid")
    {
        if (condition == "null") return null;
        var dn = condition == "malformed-dn" ? "not-a-distinguished-name" : "CN=Reader,DC=lab,DC=local";
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["distinguishedName"] = [dn], ["userPrincipalName"] = [User], ["userAccountControl"] = ["512"]
        };
        if (condition == "missing-uac") attributes.Remove("userAccountControl");
        if (condition == "malformed-uac") attributes["userAccountControl"] = ["not-a-number"];
        if (condition == "multiple-uac") attributes["userAccountControl"] = ["512", "514"];
        if (condition == "disabled") attributes["userAccountControl"] = ["514"];
        if (condition == "missing-upn") attributes.Remove("userPrincipalName");
        if (condition == "mismatched-upn") attributes["userPrincipalName"] = ["other@lab.local"];
        if (condition == "missing-dn") attributes.Remove("distinguishedName");
        var entry = new LdapSearchEntry(dn, attributes);
        return new(ResultCode.Success, condition == "zero" ? [] : condition == "multiple" ? [entry, entry] : [entry], condition == "references");
    }

    private static LdapSearchResult? GroupResponse(string condition)
    {
        if (condition is "null" or "zero" or "multiple" or "references") return IdentityResponse(condition);
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (condition is "groups" or "mixed-ranged") attributes["memberOf"] = ["CN=GG-APP-USER,OU=Groups,DC=lab,DC=local"];
        if (condition == "empty") attributes["memberOf"] = [];
        if (condition == "bad-dn") attributes["memberOf"] = ["malformed-secret-DN"];
        if (condition is "ranged" or "mixed-ranged") attributes["memberOf;range=0-1499"] = ["CN=GG-APP-ADMIN,DC=lab,DC=local"];
        return new(ResultCode.Success, [new("CN=Reader,DC=lab,DC=local", attributes)]);
    }

    private sealed class CredentialProvider(string? failure = null) : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return failure switch
            {
                "empty" => Task.FromResult(""),
                "missing" => throw new InvalidOperationException(SecretMarker),
                "corrupt" => throw new CryptographicException(SecretMarker),
                "io" => throw new IOException(SecretMarker),
                _ => Task.FromResult("service-secret")
            };
        }
    }

    private sealed class FakeFactory : ILdapConnectionFactory, ILdapConnection
    {
        public DirectoryFailureStage? FailureStage { get; init; }
        public string? FailureKind { get; init; }
        public Exception? InjectedException { get; init; }
        public CancellationTokenSource? Cancellation { get; init; }
        public LdapSearchResult? Response { get; init; } = IdentityResponse();
        public int CreateCount { get; private set; }
        public int BindCount { get; private set; }
        public bool Disposed { get; private set; }
        public bool Ldaps { get; private set; }
        public TimeSpan SeenTimeout { get; private set; }
        public List<string> Sequence { get; } = [];
        public List<string> Passwords { get; } = [];
        public ILdapConnection Create(LdapOptions options)
        {
            CreateCount++;
            SeenTimeout = options.ConnectionTimeout;
            Fail(DirectoryFailureStage.ConnectionSetup);
            return this;
        }
        public void ConfigureSession(bool useLdaps) { Ldaps = useLdaps; Sequence.Add("configure"); }
        public void Bind(NetworkCredential credential)
        {
            BindCount++;
            Sequence.Add("bind:" + credential.UserName);
            Passwords.Add(credential.Password);
            Fail(BindCount == 1 ? DirectoryFailureStage.ServiceBind : DirectoryFailureStage.UserBind);
        }
        public LdapSearchResult? Search(SearchRequest request) { Sequence.Add("search"); Fail(DirectoryFailureStage.UserSearch); return Response; }
        public void Dispose() { Disposed = true; Sequence.Add("dispose"); }
        private void Fail(DirectoryFailureStage stage)
        {
            if (stage != FailureStage) return;
            if (InjectedException is not null) throw InjectedException;
            if (FailureKind is "cancel" or "disposed-cancel" or "ldap-cancel") Cancellation!.Cancel();
            throw FailureKind switch
            {
                "refused" => new SocketException((int)SocketError.ConnectionRefused),
                "dns" => new SocketException((int)SocketError.HostNotFound),
                "network" => new SocketException((int)SocketError.NetworkUnreachable),
                "io" => new IOException(SecretMarker),
                "tls" => new AuthenticationException(SecretMarker),
                "timeout" => new TimeoutException(SecretMarker),
                "cancel" or "unassociated-cancel" => new OperationCanceledException(SecretMarker),
                "disposed-cancel" => new ObjectDisposedException(SecretMarker),
                "ldap-cancel" => new LdapException(81, SecretMarker),
                "operation-unknown" => new DirectoryOperationException(SecretMarker),
                "runtime-invalid-operation" => new InvalidOperationException(SecretMarker),
                "unexpected" => new NotSupportedException(SecretMarker),
                _ => new LdapException(int.Parse(FailureKind!), SecretMarker)
            };
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(Exception? Exception, string Text)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((exception, formatter(state, exception)));
    }
}
