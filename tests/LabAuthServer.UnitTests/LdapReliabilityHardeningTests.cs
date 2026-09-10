using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

/// <summary>
/// Phase 2B root-DSE cancellation propagation and Phase 2C bounded membership collection.
/// No test requires AD, network, IIS or SQL; the connection seam is fully synthetic.
/// </summary>
public sealed class LdapReliabilityHardeningTests
{
    private const string User = "reader@lab.local";

    // ---------------------------------------------------------------- Phase 2B

    [Fact]
    public void MaximumGroupMemberships_DefaultIsTheApprovedBound()
        => Assert.Equal(100, new LdapOptions().MaximumGroupMemberships);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void MaximumGroupMemberships_InvalidConfigurationIsRejected(int value)
    {
        var options = Settings();
        options.MaximumGroupMemberships = value;
        Assert.Contains(LdapOptionsValidator.Validate(options, true), failure => failure.Contains("group memberships"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MaximumGroupMemberships_ValidConfigurationIsAccepted(int value)
    {
        var options = Settings();
        options.MaximumGroupMemberships = value;
        Assert.Empty(LdapOptionsValidator.Validate(options, true));
    }

    [Fact]
    public async Task RootDse_AlreadyCancelledCaller_DoesNotLoadCredentialsOrTouchTheProvider()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var provider = new RecordingCredentialProvider();
        var service = new LdapService(Options.Create(Settings()), provider, NullLogger<LdapService>.Instance, new UnusedFactory());

        var result = await service.QueryRootDseAsync(caller.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal("The operation was cancelled.", result.ErrorMessage);
        Assert.False(provider.Called);
    }

    [Fact]
    public async Task RootDse_CredentialLoadReceivesTheCallerToken()
    {
        using var caller = new CancellationTokenSource();
        var provider = new WaitingCredentialProvider();
        var service = new LdapService(Options.Create(Settings()), provider, NullLogger<LdapService>.Instance, new UnusedFactory());

        var task = service.QueryRootDseAsync(caller.Token);
        var observed = await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(observed.CanBeCanceled);
        Assert.Equal(caller.Token, observed);

        caller.Cancel();
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.IsSuccess);
        Assert.Equal("The operation was cancelled.", result.ErrorMessage);
        Assert.True(provider.Finished);
    }

    [Fact]
    public async Task RootDse_WithoutServiceAccountUsername_FailsSafelyWithoutLoadingCredentials()
    {
        var options = Settings();
        options.ServiceAccountUsername = string.Empty;
        var provider = new RecordingCredentialProvider();
        var service = new LdapService(Options.Create(options), provider, NullLogger<LdapService>.Instance, new UnusedFactory());

        var result = await service.QueryRootDseAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("The directory service could not complete the Root DSE query.", result.ErrorMessage);
        Assert.False(provider.Called);
    }

    [Fact]
    public async Task RootDse_EmptyServicePassword_FailsSafelyWithoutDisclosingConfiguration()
    {
        var provider = new RecordingCredentialProvider { Password = string.Empty };
        var service = new LdapService(Options.Create(Settings()), provider, NullLogger<LdapService>.Instance, new UnusedFactory());

        var result = await service.QueryRootDseAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("The directory service could not complete the Root DSE query.", result.ErrorMessage);
        Assert.DoesNotContain("password", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- Phase 2C

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task MembershipExactlyAtBound_RemainsACompleteSuccess(int cap)
    {
        var factory = new FakeFactory(Memberships(cap));
        var result = await Groups(factory, cap).GetUserGroupsAsync(User, "synthetic");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(cap, result.Groups.Count);
        Assert.Equal("service-secret", factory.ServicePassword);
        Assert.Equal(1, factory.Disposals);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task MembershipAboveBound_IsAFailureAndIsNeverTruncated(int cap)
    {
        var factory = new FakeFactory(Memberships(cap + 1));
        var result = await Groups(factory, cap).GetUserGroupsAsync(User, "synthetic");

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthenticationFailureCategory.ProtocolFailure, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.ResponseValidation, result.Failure.Stage);
        Assert.Equal(DirectoryFailureReason.MembershipLimitExceeded, result.Failure.Reason);
        Assert.Equal("Authentication service unavailable.", result.Failure.SafeMessage);
        Assert.Throws<InvalidOperationException>(() => result.Groups);
        Assert.Equal(1, factory.Disposals);
    }

    [Fact]
    public async Task EmptyMembership_RemainsDistinctFromMembershipFailure()
    {
        var result = await Groups(new FakeFactory([]), 100).GetUserGroupsAsync(User, "synthetic");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Groups);
    }

    [Fact]
    public async Task MembershipBound_DoesNotAlterIncompleteMembershipHandling()
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["memberOf;range=0-1499"] = ["CN=GG-APP-ADMIN,DC=lab,DC=local"]
        };
        var result = await Groups(new FakeFactory(null, attributes), 100).GetUserGroupsAsync(User, "synthetic");

        Assert.False(result.IsSuccess);
        Assert.Equal(DirectoryFailureReason.IncompleteMembership, result.Failure!.Reason);
    }

    [Fact]
    public async Task MembershipBound_LogsNoSensitiveContent()
    {
        var logger = new CapturingLogger<LdapService>();
        var result = await Groups(new FakeFactory(Memberships(101)), 100, logger).GetUserGroupsAsync(User, "synthetic");

        Assert.False(result.IsSuccess);
        var entry = Assert.Single(logger.Entries);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("memberOf", entry.Text);
        Assert.DoesNotContain("DC=lab", entry.Text);
        Assert.DoesNotContain("service-secret", entry.Text);
        Assert.Contains(DirectoryFailureReason.MembershipLimitExceeded.ToString(), entry.Text);
    }

    // ---------------------------------------------------------------- helpers

    private static string[] Memberships(int count)
        => Enumerable.Range(0, count).Select(index => $"CN=GG-APP-USER{index},DC=lab,DC=local").ToArray();

    private static LdapService Groups(FakeFactory factory, int cap, CapturingLogger<LdapService>? logger = null)
    {
        var options = Settings();
        options.MaximumGroupMemberships = cap;
        return new LdapService(Options.Create(options), new CredentialProvider(), logger ?? new CapturingLogger<LdapService>(), factory);
    }

    private static LdapOptions Settings() => new()
    {
        Domain = "lab.local", Host = "directory.example.test", Port = 636, UseLdaps = true,
        BaseDn = "DC=lab,DC=local", UserSearchBaseDn = "DC=lab,DC=local",
        ServiceAccountUsername = "service@lab.local",
        ConnectionTimeout = TimeSpan.FromSeconds(10)
    };

    private sealed class CredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult("service-secret");
        }
    }

    private sealed class RecordingCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public bool Called { get; private set; }
        public string Password { get; init; } = "service-secret";
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(Password);
        }
    }

    private sealed class WaitingCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public TaskCompletionSource<CancellationToken> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Finished { get; private set; }
        public async Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            Entered.SetResult(cancellationToken);
            try { await Task.Delay(Timeout.Infinite, cancellationToken); return "unreachable"; }
            finally { Finished = true; }
        }
    }

    private sealed class UnusedFactory : ILdapConnectionFactory
    {
        public ILdapConnection Create(LdapOptions options) => throw new InvalidOperationException("Root DSE must not use the login connection seam.");
    }

    private sealed class FakeFactory(string[]? memberships, IReadOnlyDictionary<string, IReadOnlyList<string>>? attributes = null)
        : ILdapConnectionFactory, ILdapConnection
    {
        private int _binds;
        public int Disposals;
        public string? ServicePassword;
        public ILdapConnection Create(LdapOptions options) => this;
        public void ConfigureSession(bool useLdaps) { }
        public void Bind(NetworkCredential credential) { if (++_binds == 1) ServicePassword = credential.Password; }
        public LdapSearchResult Search(SearchRequest request)
        {
            const string dn = "CN=Reader,DC=lab,DC=local";
            var values = attributes ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["memberOf"] = memberships ?? []
            };
            return new(ResultCode.Success, [new(dn, values)]);
        }
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<(Exception? Exception, string Text)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((exception, formatter(state, exception)));
    }
}