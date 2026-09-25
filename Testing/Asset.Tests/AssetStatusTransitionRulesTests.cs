using Helpdesk.Assets.Domain.Model;
using Helpdesk.Assets.Domain.Rules;
using Xunit;

namespace Helpdesk.Assets.Tests;

public class AssetStatusTransitionRulesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Assigned_is_always_rejected_regardless_of_open_assignment_state(bool hasOpenAssignment)
    {
        var result = AssetStatusTransitionRules.Validate(AssetStatus.Assigned, hasOpenAssignment);

        Assert.Equal(AssetStatusTransitionResult.MustUseAssignAsset, result);
    }

    [Fact]
    public void InStock_with_an_open_assignment_is_rejected()
    {
        var result = AssetStatusTransitionRules.Validate(AssetStatus.InStock, hasOpenAssignment: true);

        Assert.Equal(AssetStatusTransitionResult.MustUseReturnAssetFirst, result);
    }

    [Fact]
    public void InStock_with_no_open_assignment_is_allowed()
    {
        var result = AssetStatusTransitionRules.Validate(AssetStatus.InStock, hasOpenAssignment: false);

        Assert.Equal(AssetStatusTransitionResult.Allowed, result);
    }

    [Theory]
    [InlineData(AssetStatus.InRepair, false)]
    [InlineData(AssetStatus.InRepair, true)]
    [InlineData(AssetStatus.Retired, false)]
    [InlineData(AssetStatus.Retired, true)]
    public void InRepair_and_Retired_are_always_allowed(AssetStatus status, bool hasOpenAssignment)
    {
        var result = AssetStatusTransitionRules.Validate(status, hasOpenAssignment);

        Assert.Equal(AssetStatusTransitionResult.Allowed, result);
    }
}
