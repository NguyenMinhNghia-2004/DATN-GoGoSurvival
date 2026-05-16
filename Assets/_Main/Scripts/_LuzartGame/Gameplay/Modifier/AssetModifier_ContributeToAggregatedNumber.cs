using UnityEngine;
namespace Luzart
{
    public sealed class AssetModifier_ContributeToAggregatedNumber : AssetModifier
    {
        [SerializeField] private AssetNumber_Aggregation contributionNumber;
        INumberWithContribution ContributionNumber => contributionNumber;
        protected override void OnFactorIncluded(IModifierFactor factor)
        {
            base.OnFactorIncluded(factor);
            ContributionNumber.Contribute(factor.Value);
        }
        protected override void OnFactorExcluded(IModifierFactor factor)
        {
            base.OnFactorExcluded(factor);
            ContributionNumber.Uncontribute(factor.Value);
        }
    }
}
