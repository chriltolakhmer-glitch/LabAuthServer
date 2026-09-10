using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;

namespace LabAuthServer.UnitTests;

public sealed class DirectoryResultTests
{
    [Fact]
    public void ResultFactories_CannotExposeContradictorySuccessAndFailure()
    {
        var failure = new DirectoryFailure(AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureStage.ResponseValidation, DirectoryFailureReason.InvalidResponse);
        var authentication = AuthenticationResult.Failed(failure);
        Assert.False(authentication.IsAuthenticated);
        Assert.Null(authentication.Username);
        Assert.Equal(AuthenticationFailureCategory.ProtocolFailure, authentication.FailureCategory);
        var success = AuthenticationResult.Succeeded("reader@lab.local");
        Assert.True(success.IsAuthenticated);
        Assert.Null(success.Failure);
        Assert.Equal(AuthenticationFailureCategory.None, success.FailureCategory);
        Assert.Null(success.ErrorMessage);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DirectoryFailure(AuthenticationFailureCategory.None, DirectoryFailureStage.UserBind, DirectoryFailureReason.UserBindRejected));
        Assert.Throws<ArgumentNullException>(() => AuthenticationResult.Failed(null!));
        Assert.Throws<ArgumentNullException>(() => GroupLookupResult.Failed(null!));
    }

    [Fact]
    public void MembershipSnapshot_IsImmutableAndEmptySuccessIsNotFailure()
    {
        var groups = new List<string> { "Reader" };
        var result = GroupLookupResult.Succeeded(groups);
        groups.Clear();
        Assert.Equal(new[] { "Reader" }, result.Groups);
        var empty = GroupLookupResult.Succeeded([]);
        Assert.True(empty.IsSuccess);
        Assert.Null(empty.Failure);
        Assert.Empty(empty.Groups);
    }

    [Fact]
    public void NumericDiagnostics_RequireAnExplicitSource()
    {
        Assert.Throws<ArgumentException>(() => new DirectoryFailure(AuthenticationFailureCategory.Unexpected,
            DirectoryFailureStage.ServiceBind, DirectoryFailureReason.UnexpectedFailure, 52));
        Assert.Throws<ArgumentException>(() => new DirectoryFailure(AuthenticationFailureCategory.Unexpected,
            DirectoryFailureStage.ServiceBind, DirectoryFailureReason.UnexpectedFailure, diagnosticSource: DirectoryDiagnosticSource.OperationResult));
    }
}
