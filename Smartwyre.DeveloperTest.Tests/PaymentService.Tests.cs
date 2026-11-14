using Moq;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;
using Xunit;

namespace Smartwyre.DeveloperTest.Tests;

public class RebateServiceTests
{
    private readonly IRebateService _rebateService;
    private readonly Mock<IRebateDataStore> _mockRebateDataStore;
    private readonly Mock<IProductDataStore> _mockProductDataStore;
    
    public RebateServiceTests()
    {
        _mockRebateDataStore = new Mock<IRebateDataStore>();
        _mockProductDataStore = new Mock<IProductDataStore>();
        _rebateService = new RebateService(_mockRebateDataStore.Object, _mockProductDataStore.Object);
    }
    
    private Rebate MockRebate(IncentiveType incentive, decimal amount = 10, decimal percentage = 1)
    {
        var rebate = new Rebate { Identifier = "Rebate", Incentive = incentive, Amount = amount, Percentage = percentage };
        _mockRebateDataStore.Setup(x => x.GetRebate("Rebate")).Returns(rebate);
        return rebate;
    }

    private void MockProduct(SupportedIncentiveType supported, decimal price = 10)
    {
        _mockProductDataStore.Setup(x => x.GetProduct("Product")).Returns(
            new Product { Identifier = "Product", Price = price, SupportedIncentives = supported });
    }

    private CalculateRebateRequest CreateCalculateRebateRequest(decimal volume = 1)
    {
        return new CalculateRebateRequest
        {
            RebateIdentifier = "Rebate",
            ProductIdentifier = "Product",
            Volume = volume
        };
    }

    private void AssertFails(CalculateRebateResult result)
    {
        Assert.False(result.Success);
        _mockRebateDataStore
            .Verify(x => x.StoreCalculationResult(It.IsAny<Rebate>(), It.IsAny<decimal>()), Times.Never);
    }
    
    
    [Fact]
    public void ValidateRequest_RebateIsNull_ReturnFalse()
    {
        var request = CreateCalculateRebateRequest();
        
        _mockRebateDataStore
            .Setup(x => x.GetRebate("Rebate"))
            .Returns((Rebate)null);
        MockProduct(SupportedIncentiveType.FixedCashAmount);
        
        var result = _rebateService.Calculate(request);
        
        AssertFails(result);
    }
    
    [Fact]
    public void ValidateRequest_ProductIsNull_ReturnFalse()
    {
        var request = CreateCalculateRebateRequest();
        MockRebate(IncentiveType.FixedCashAmount);
        _mockProductDataStore
            .Setup(x => x.GetProduct(request.ProductIdentifier))
            .Returns((Product)null);
        
        var result = _rebateService.Calculate(request);
        
        AssertFails(result);
    }
    
    [Theory]
    [InlineData(IncentiveType.FixedCashAmount, SupportedIncentiveType.AmountPerUom,     10, 1, 10, 1)] // unsupported
    [InlineData(IncentiveType.FixedCashAmount, SupportedIncentiveType.FixedCashAmount,   0, 1, 10, 1)] // amount 0
    [InlineData(IncentiveType.FixedRateRebate, SupportedIncentiveType.FixedCashAmount,  10, 1, 10, 1)] // unsupported
    [InlineData(IncentiveType.FixedRateRebate, SupportedIncentiveType.FixedRateRebate,  10, 0, 10, 1)] // percentage 0
    [InlineData(IncentiveType.FixedRateRebate, SupportedIncentiveType.FixedRateRebate,  10, 1,  0, 1)] // price 0
    [InlineData(IncentiveType.FixedRateRebate, SupportedIncentiveType.FixedRateRebate,  10, 1, 10, 0)] // volume 0
    [InlineData(IncentiveType.AmountPerUom,    SupportedIncentiveType.FixedCashAmount,  10, 1, 10, 1)] // unsupported
    [InlineData(IncentiveType.AmountPerUom,    SupportedIncentiveType.AmountPerUom,      0, 1, 10, 1)] // amount 0
    [InlineData(IncentiveType.AmountPerUom,    SupportedIncentiveType.AmountPerUom,     10, 1, 10, 0)] // volume 0
    public void Calculate_InvalidRequest_FailsAndStoresNothing(
        IncentiveType incentive, SupportedIncentiveType supported,
        decimal amount, decimal percentage, decimal price, decimal volume)
    {
        var request = CreateCalculateRebateRequest(volume);
        MockRebate(incentive, amount, percentage);
        MockProduct(supported, price);

        var result = _rebateService.Calculate(request);
        
        AssertFails(result);
    }

    [Fact]
    public void ValidateRequest_InvalidRebateIncentive_ReturnFalse()
    {
        var request = CreateCalculateRebateRequest();
        MockRebate((IncentiveType)100);
        MockProduct(SupportedIncentiveType.FixedRateRebate);
        
        var result = _rebateService.Calculate(request);
        
        AssertFails(result);
    }
    
    [Fact]
    public void Calculate_FixedCashAmount_ReturnsRebate()
    {
        var request = CreateCalculateRebateRequest(10); // volume is ignored for a fixed cash amount
        var rebate = MockRebate(IncentiveType.FixedCashAmount, amount: 12345);
        MockProduct(SupportedIncentiveType.FixedCashAmount);

        var result = _rebateService.Calculate(request);

        Assert.True(result.Success);
        _mockRebateDataStore.Verify(x => x.StoreCalculationResult(rebate, 12345m), Times.Once);
    }
    
    [Fact]
    public void Calculate_FixedRateRebate_ReturnsRebate()
    {
        var request = CreateCalculateRebateRequest(2);
        var rebate = MockRebate(IncentiveType.FixedRateRebate, percentage: 0.10m);
        MockProduct(SupportedIncentiveType.FixedRateRebate, price: 10000);

        var result = _rebateService.Calculate(request);

        Assert.True(result.Success);
        _mockRebateDataStore.Verify(x => x.StoreCalculationResult(rebate, 2000m), Times.Once); // 10000 * 0.10 * 2
    }
    
    [Fact]
    public void Calculate_AmountPerUom_ReturnsRebate()
    {
        var request = CreateCalculateRebateRequest(2);
        var rebate = MockRebate(IncentiveType.AmountPerUom, amount: 500);
        MockProduct(SupportedIncentiveType.AmountPerUom);

        var result = _rebateService.Calculate(request);

        Assert.True(result.Success);
        _mockRebateDataStore.Verify(x => x.StoreCalculationResult(rebate, 1000m), Times.Once); // 500 * 2
    }
}
