using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection()
            .AddSingleton<IRebateDataStore, RebateDataStore>()
            .AddSingleton<IProductDataStore, ProductDataStore>()
            .AddTransient<IRebateService, RebateService>()
            .BuildServiceProvider();
        var rebateService = services.GetRequiredService<IRebateService>();
        
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: Runner <rebateIdentifier> <productIdentifier> <volume>");
            Environment.ExitCode = 1;
            return;
        }

        if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var volume))
        {
            Console.Error.WriteLine($"Invalid volume '{args[2]}'. Expected a number, e.g. 10 or 12.5.");
            Environment.ExitCode = 1;
            return;
        }

        var request = new CalculateRebateRequest
        {
            RebateIdentifier = args[0],
            ProductIdentifier = args[1],
            Volume = volume
        };
        
        try
        {
            var result = rebateService.Calculate(request);

            Console.WriteLine($"Rebate:  {request.RebateIdentifier}");
            Console.WriteLine($"Product: {request.ProductIdentifier}");
            Console.WriteLine($"Volume:  {request.Volume}");
            Console.WriteLine($"Success: {result.Success}");

            Environment.ExitCode = result.Success ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 3;
        }
    }
}
