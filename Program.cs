using TensoShippingCalculator.Models;
using TensoShippingCalculator.Services;

namespace TensoShippingCalculator
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.WriteLine("Tenso 集中包裝划算度計算器");
      Console.WriteLine("===============================");

      // 範例包裹資料 - 您可以修改這些資料來測試
      var packages = new List<Package>
            {
                new Package { Name = "包裹1", Weight = 1500, Length = 25, Width = 20, Height = 15 },
                new Package { Name = "包裹2", Weight = 2000, Length = 30, Width = 25, Height = 10 },
                new Package { Name = "包裹3", Weight = 800, Length = 15, Width = 12, Height = 8 },
                new Package { Name = "包裹4", Weight = 3000, Length = 35, Width = 30, Height = 20 },
                new Package { Name = "包裹5", Weight = 1200, Length = 20, Width = 18, Height = 12 }
            };

      try
      {
        using var calculationService = new ShippingCalculationService();
        var result = await calculationService.CalculateConsolidationBenefitAsync(packages);

        // 顯示詳細結果
        ShowDetailedResults(result);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"發生錯誤: {ex.Message}");
      }

      Console.WriteLine("\n按任意鍵結束...");
      Console.ReadKey();
    }

    /// <summary>
    /// 顯示詳細計算結果
    /// </summary>
    private static void ShowDetailedResults(ShippingCalculationResult result)
    {
      Console.WriteLine("\n" + new string('=', 60));
      Console.WriteLine("詳細計算結果");
      Console.WriteLine(new string('=', 60));

      // 個別包裹結果
      Console.WriteLine("\n個別包裹運費明細:");
      foreach (var individual in result.IndividualResults)
      {
        Console.WriteLine($"\n{individual.Package.Name}:");
        Console.WriteLine($"  尺寸: {individual.Package.Length}×{individual.Package.Width}×{individual.Package.Height}cm");
        Console.WriteLine($"  重量: {individual.Package.Weight}g (體積重量: {individual.Package.VolumetricWeight}g)");

        if (individual.BestOption != null)
        {
          Console.WriteLine($"  最佳選項: {individual.BestOption.Name}");
          Console.WriteLine($"  運費: {individual.BestOption.OriginalShippingFee:N0} 日元");
          Console.WriteLine($"  手續費: {individual.BestOption.OriginalServiceFee:N0} 日元");
          Console.WriteLine($"  總計: {individual.BestOption.OriginalTotalFee:N0} 日元");
        }

        // 顯示所有可用選項
        var availableOptions = individual.ShippingOptions.Where(o => o.CanUse && o.OriginalTotalFee > 0).ToList();
        if (availableOptions.Count > 1)
        {
          Console.WriteLine("  其他選項:");
          foreach (var option in availableOptions.Skip(1))
          {
            Console.WriteLine($"    {option.Name}: {option.OriginalTotalFee:N0} 日元 ({option.DeliveryDays} 天)");
          }
        }
      }

      // 集中包裝結果
      if (result.ConsolidatedResult != null)
      {
        Console.WriteLine($"\n集中包裝結果:");
        var consolidated = result.ConsolidatedResult;
        Console.WriteLine($"  合併尺寸: {consolidated.ConsolidatedPackage.Length}×{consolidated.ConsolidatedPackage.Width}×{consolidated.ConsolidatedPackage.Height}cm");
        Console.WriteLine($"  合併重量: {consolidated.ConsolidatedPackage.Weight}g");
        Console.WriteLine($"  集中包裝手續費: {consolidated.ConsolidationFee:N0} 日元");

        if (consolidated.BestOption != null)
        {
          Console.WriteLine($"  最佳運送方式: {consolidated.BestOption.Name}");
          Console.WriteLine($"  運費: {consolidated.BestOption.OriginalShippingFee:N0} 日元");
          Console.WriteLine($"  Tenso手續費: {consolidated.BestOption.OriginalServiceFee:N0} 日元");
          Console.WriteLine($"  運送總計: {consolidated.BestOption.OriginalTotalFee:N0} 日元");
          Console.WriteLine($"  含集中包裝費總計: {consolidated.TotalCostWithFee:N0} 日元");
        }
      }

      // 最終建議
      Console.WriteLine("\n" + new string('=', 60));
      Console.WriteLine("最終建議");
      Console.WriteLine(new string('=', 60));

      var totalIndividual = result.IndividualResults.Sum(r => r.BestOption?.OriginalTotalFee ?? 0);
      var totalConsolidated = result.ConsolidatedResult?.TotalCostWithFee ?? 0;

      Console.WriteLine($"個別寄送總費用: {totalIndividual:N0} 日元");
      Console.WriteLine($"集中包裝總費用: {totalConsolidated:N0} 日元");

      if (result.IsConsolidationBeneficial)
      {
        Console.WriteLine($"\n✅ 建議使用集中包裝服務");
        Console.WriteLine($"   可節省費用: {result.TotalSavings:N0} 日元");
        Console.WriteLine($"   節省比例: {(double)result.TotalSavings / totalIndividual * 100:F1}%");
      }
      else
      {
        Console.WriteLine($"\n❌ 建議個別寄送");
        Console.WriteLine($"   集中包裝會多花: {Math.Abs(result.TotalSavings):N0} 日元");
      }
    }
  }
}
