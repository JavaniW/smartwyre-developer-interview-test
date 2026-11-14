using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Services;

public class RebateService : IRebateService
{
    public CalculateRebateResult Calculate(CalculateRebateRequest request)
    {
        var rebateDataStore = new RebateDataStore();
        var productDataStore = new ProductDataStore();

        Rebate rebate = rebateDataStore.GetRebate(request.RebateIdentifier);
        Product product = productDataStore.GetProduct(request.ProductIdentifier);

        var result = new CalculateRebateResult();

        var rebateAmount = 0m;
        
        result.Success = ValidateRequest(rebate, product, request, ref rebateAmount);

        if (result.Success)
        {
            var storeRebateDataStore = new RebateDataStore();
            storeRebateDataStore.StoreCalculationResult(rebate, rebateAmount);
        }

        return result;
    }

    // I chose to extract all the validation logic into its own method
    // This makes sure that Calculate() is only focused on calculation
    private bool ValidateRequest(Rebate rebate, Product product, CalculateRebateRequest request, ref decimal rebateAmount)
    {
        // I chose to do an early return here. There's no reason to continue executing following statements because we already know the request is invalid
        // This also fixes a bug where a Null Reference Exception would be thrown if IncentiveType is FixedCashAmount and product is NULL
        // I chose to use "is null" as opposed to "== null" to protect against the case where the "==" operator is overloaded
        if (rebate is null || product is null)
        {
            return false;
        }
        
        switch (rebate.Incentive)
        {
            case IncentiveType.FixedCashAmount:
                if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedCashAmount)
                    || rebate.Amount == 0)
                {
                    return false;
                }
                
                rebateAmount = rebate.Amount;
                break;
            
            case IncentiveType.FixedRateRebate:
                if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedRateRebate)
                    || rebate.Percentage == 0 
                    || product.Price == 0 
                    || request.Volume == 0)
                {
                    return false;
                }
                
                rebateAmount += product.Price * rebate.Percentage * request.Volume;
                break;
            
            case IncentiveType.AmountPerUom:
                if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.AmountPerUom)
                    || rebate.Amount == 0 
                    || request.Volume == 0)
                {
                    return false;
                }
                
                rebateAmount += rebate.Amount * request.Volume;
                break;
        }

        return true;
    }
}
