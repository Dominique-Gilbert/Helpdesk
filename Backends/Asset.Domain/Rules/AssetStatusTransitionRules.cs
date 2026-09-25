using Helpdesk.Assets.Domain.Model;

namespace Helpdesk.Assets.Domain.Rules;

public enum AssetStatusTransitionResult
{
    Allowed,
    MustUseAssignAsset,
    MustUseReturnAssetFirst
}

public static class AssetStatusTransitionRules
{
    /// <summary>
    /// Assigned, and stepping back to InStock while an assignment is still open, are one-way
    /// doors that a plain field update must not open - AssignAsset/ReturnAsset are the only
    /// paths that also touch an AssignmentRecord, and letting Update flip Status directly would
    /// desync the two.
    /// </summary>
    public static AssetStatusTransitionResult Validate(AssetStatus requested, bool hasOpenAssignment)
    {
        if (requested == AssetStatus.Assigned) return AssetStatusTransitionResult.MustUseAssignAsset;
        if (requested == AssetStatus.InStock && hasOpenAssignment) return AssetStatusTransitionResult.MustUseReturnAssetFirst;
        return AssetStatusTransitionResult.Allowed;
    }
}
