using System.Text.Json;
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

      // 從 JSON 檔案讀取包裹資料
      var packages = await LoadPackagesFromJsonAsync("packages.json");

      try
      {
        using var calculationService = new ShippingCalculationService();
        var result = await calculationService.CalculateConsolidationBenefitAsync(packages);

        // 顯示詳細結果並輸出到檔案
        await ShowDetailedResultsAsync(result);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"發生錯誤: {ex.Message}");
      }

      Console.WriteLine("\n按任意鍵結束...");
      Console.ReadKey();
    }

    /// <summary>
    /// 從 JSON 檔案載入包裹資料
    /// </summary>
    /// <param name="filePath">JSON 檔案路徑</param>
    /// <returns>包裹清單</returns>
    private static async Task<List<Package>> LoadPackagesFromJsonAsync(string filePath)
    {
      try
      {
        if (!File.Exists(filePath))
        {
          Console.WriteLine($"⚠️  找不到檔案: {filePath}");
          Console.WriteLine("使用預設範例資料...");
          return GetDefaultPackages();
        }

        var jsonContent = await File.ReadAllTextAsync(filePath);
        var packages = JsonSerializer.Deserialize<List<Package>>(jsonContent);
        
        if (packages == null || packages.Count == 0)
        {
          Console.WriteLine("⚠️  JSON 檔案為空或格式錯誤，使用預設範例資料...");
          return GetDefaultPackages();
        }

        Console.WriteLine($"✓ 成功從 {filePath} 載入 {packages.Count} 個包裹");
        return packages;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"⚠️  讀取 JSON 檔案時發生錯誤: {ex.Message}");
        Console.WriteLine("使用預設範例資料...");
        return GetDefaultPackages();
      }
    }

    /// <summary>
    /// 取得預設包裹資料
    /// </summary>
    /// <returns>預設包裹清單</returns>
    private static List<Package> GetDefaultPackages()
    {
      return new List<Package>
      {
        new Package { Name = "包裹1", Weight = 1500, Length = 25, Width = 20, Height = 15 },
        new Package { Name = "包裹2", Weight = 2000, Length = 30, Width = 25, Height = 10 },
        new Package { Name = "包裹3", Weight = 800, Length = 15, Width = 12, Height = 8 },
        new Package { Name = "包裹4", Weight = 3000, Length = 35, Width = 30, Height = 20 },
        new Package { Name = "包裹5", Weight = 1200, Length = 20, Width = 18, Height = 12 }
      };
    }

    /// <summary>
    /// 顯示詳細計算結果並輸出到 Markdown 檔案
    /// </summary>
    private static async Task ShowDetailedResultsAsync(ShippingCalculationResult result)
    {
      var outputLines = new List<string>();
      var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      
      // 準備 Markdown 內容
      outputLines.Add("# Tenso 集中包裝划算度計算結果");
      outputLines.Add("");
      outputLines.Add($"**計算時間**: {timestamp}");
      outputLines.Add("");

      Console.WriteLine("\n" + new string('=', 60));
      Console.WriteLine("詳細計算結果");
      Console.WriteLine(new string('=', 60));

      // 個別包裹結果
      outputLines.Add("## 個別包裹運費明細");
      outputLines.Add("");
      Console.WriteLine("\n個別包裹運費明細:");
      foreach (var individual in result.IndividualResults)
      {
        outputLines.Add($"### {individual.Package.Name}");
        outputLines.Add("");
        outputLines.Add($"- **尺寸**: {individual.Package.Length}×{individual.Package.Width}×{individual.Package.Height}cm");
        outputLines.Add($"- **重量**: {individual.Package.Weight}g (體積重量: {individual.Package.VolumetricWeight}g)");
        
        Console.WriteLine($"\n{individual.Package.Name}:");
        Console.WriteLine($"  尺寸: {individual.Package.Length}×{individual.Package.Width}×{individual.Package.Height}cm");
        Console.WriteLine($"  重量: {individual.Package.Weight}g (體積重量: {individual.Package.VolumetricWeight}g)");

        if (individual.BestOption != null)
        {
          outputLines.Add($"- **最佳選項**: {individual.BestOption.Name}");
          outputLines.Add($"- **運費**: {individual.BestOption.OriginalShippingFee:N0} 日元");
          outputLines.Add($"- **手續費**: {individual.BestOption.OriginalServiceFee:N0} 日元");
          outputLines.Add($"- **總計**: {individual.BestOption.OriginalTotalFee:N0} 日元");
          
          Console.WriteLine($"  最佳選項: {individual.BestOption.Name}");
          Console.WriteLine($"  運費: {individual.BestOption.OriginalShippingFee:N0} 日元");
          Console.WriteLine($"  手續費: {individual.BestOption.OriginalServiceFee:N0} 日元");
          Console.WriteLine($"  總計: {individual.BestOption.OriginalTotalFee:N0} 日元");
        }

        // 顯示所有可用選項
        var availableOptions = individual.ShippingOptions.Where(o => o.CanUse && o.OriginalTotalFee > 0).ToList();
        if (availableOptions.Count > 1)
        {
          outputLines.Add("- **其他選項**:");
          Console.WriteLine("  其他選項:");
          foreach (var option in availableOptions.Skip(1))
          {
            outputLines.Add($"  - {option.Name}: {option.OriginalTotalFee:N0} 日元 ({option.DeliveryDays} 天)");
            Console.WriteLine($"    {option.Name}: {option.OriginalTotalFee:N0} 日元 ({option.DeliveryDays} 天)");
          }
        }
        outputLines.Add("");
      }

      // 集中包裝結果
      outputLines.Add("## 集中包裝結果");
      outputLines.Add("");
      if (result.ConsolidatedResult != null)
      {
        var consolidated = result.ConsolidatedResult;
        outputLines.Add($"- **合併尺寸**: {consolidated.ConsolidatedPackage.Length}×{consolidated.ConsolidatedPackage.Width}×{consolidated.ConsolidatedPackage.Height}cm");
        outputLines.Add($"- **合併重量**: {consolidated.ConsolidatedPackage.Weight}g");
        outputLines.Add($"- **集中包裝手續費**: {consolidated.ConsolidationFee:N0} 日元");
        
        Console.WriteLine($"\n集中包裝結果:");
        Console.WriteLine($"  合併尺寸: {consolidated.ConsolidatedPackage.Length}×{consolidated.ConsolidatedPackage.Width}×{consolidated.ConsolidatedPackage.Height}cm");
        Console.WriteLine($"  合併重量: {consolidated.ConsolidatedPackage.Weight}g");
        Console.WriteLine($"  集中包裝手續費: {consolidated.ConsolidationFee:N0} 日元");

        if (consolidated.BestOption != null)
        {
          outputLines.Add($"- **最佳運送方式**: {consolidated.BestOption.Name}");
          outputLines.Add($"- **運費**: {consolidated.BestOption.OriginalShippingFee:N0} 日元");
          outputLines.Add($"- **Tenso手續費**: {consolidated.BestOption.OriginalServiceFee:N0} 日元");
          outputLines.Add($"- **運送總計**: {consolidated.BestOption.OriginalTotalFee:N0} 日元");
          outputLines.Add($"- **含集中包裝費總計**: {consolidated.TotalCostWithFee:N0} 日元");
          
          Console.WriteLine($"  最佳運送方式: {consolidated.BestOption.Name}");
          Console.WriteLine($"  運費: {consolidated.BestOption.OriginalShippingFee:N0} 日元");
          Console.WriteLine($"  Tenso手續費: {consolidated.BestOption.OriginalServiceFee:N0} 日元");
          Console.WriteLine($"  運送總計: {consolidated.BestOption.OriginalTotalFee:N0} 日元");
          Console.WriteLine($"  含集中包裝費總計: {consolidated.TotalCostWithFee:N0} 日元");
        }
      }
      outputLines.Add("");

      // 最終建議
      outputLines.Add("## 最終建議");
      outputLines.Add("");
      
      Console.WriteLine("\n" + new string('=', 60));
      Console.WriteLine("最終建議");
      Console.WriteLine(new string('=', 60));

      var totalIndividual = result.IndividualResults.Sum(r => r.BestOption?.OriginalTotalFee ?? 0);
      var totalConsolidated = result.ConsolidatedResult?.TotalCostWithFee ?? 0;

      outputLines.Add($"- **個別寄送總費用**: {totalIndividual:N0} 日元");
      outputLines.Add($"- **集中包裝總費用**: {totalConsolidated:N0} 日元");
      outputLines.Add("");
      
      Console.WriteLine($"個別寄送總費用: {totalIndividual:N0} 日元");
      Console.WriteLine($"集中包裝總費用: {totalConsolidated:N0} 日元");

      if (result.IsConsolidationBeneficial)
      {
        outputLines.Add("### ✅ 建議使用集中包裝服務");
        outputLines.Add("");
        outputLines.Add($"- **可節省費用**: {result.TotalSavings:N0} 日元");
        outputLines.Add($"- **節省比例**: {(double)result.TotalSavings / totalIndividual * 100:F1}%");
        
        Console.WriteLine($"\n✅ 建議使用集中包裝服務");
        Console.WriteLine($"   可節省費用: {result.TotalSavings:N0} 日元");
        Console.WriteLine($"   節省比例: {(double)result.TotalSavings / totalIndividual * 100:F1}%");
      }
      else
      {
        outputLines.Add("### ❌ 建議個別寄送");
        outputLines.Add("");
        outputLines.Add($"- **集中包裝會多花**: {Math.Abs(result.TotalSavings):N0} 日元");
        
        Console.WriteLine($"\n❌ 建議個別寄送");
        Console.WriteLine($"   集中包裝會多花: {Math.Abs(result.TotalSavings):N0} 日元");
      }

      // 寫入 Markdown 檔案
      try
      {
        await File.WriteAllLinesAsync("result.md", outputLines);
        Console.WriteLine($"\n✓ 結果已儲存至 result.md 檔案");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"\n⚠️  儲存檔案時發生錯誤: {ex.Message}");
      }
    }
  }
}
