using System.Text;
using System.Text.Json;
using TensoShippingCalculator.Models;
using static System.Int32;

namespace TensoShippingCalculator.Services
{
  /// <summary>
  /// Tenso API 服務
  /// </summary>
  public class TensoApiService
  {
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://www.tenso.com/api/cht/estimate";

    public TensoApiService()
    {
      _httpClient = new HttpClient();
      _httpClient.DefaultRequestHeaders.Add("User-Agent", "TensoShippingCalculator/1.0");
    }

    /// <summary>
    /// 呼叫 Tenso API 取得運費估算
    /// </summary>
    /// <param name="package">包裹資訊</param>
    /// <returns>運費選項清單</returns>
    public async Task<List<TensoApiResponse>> GetShippingEstimateAsync(Package package)
    {
      try
      {
        var request = new TensoApiRequest
        {
          Weight = package.Weight.ToString(),
          Length = package.Length.ToString(),
          Width = package.Width.ToString(),
          Height = package.Height.ToString(),
          CountryId = "213" // 台灣
        };

        var json = JsonSerializer.Serialize(request);
        Console.WriteLine(json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine($"正在查詢 {package.Name} 的運費...");
        Console.WriteLine($"參數: 重量={package.Weight}g, 尺寸={package.Length}x{package.Width}x{package.Height}cm");

        var response = await _httpClient.PostAsync(ApiUrl, content);

        if (response.IsSuccessStatusCode)
        {
          var responseJson = await response.Content.ReadAsStringAsync();
          Console.WriteLine(responseJson);
          var shippingOptions = JsonSerializer.Deserialize<List<TensoApiResponse>>(responseJson);

          if (shippingOptions != null)
          {
            Console.WriteLine($"✓ 成功取得 {shippingOptions.Count} 個運送選項");
            return shippingOptions;
          }
        }
        else
        {
          Console.WriteLine($"✗ API 調用失敗: {response.StatusCode}");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"✗ 發生錯誤: {ex.Message}");
      }

      return new List<TensoApiResponse>();
    }

    /// <summary>
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
      _httpClient?.Dispose();
    }
  }

  /// <summary>
  /// 運費計算服務
  /// </summary>
  public class ShippingCalculationService : IDisposable
  {
    private readonly TensoApiService _apiService;

    public ShippingCalculationService()
    {
      _apiService = new TensoApiService();
    }

    /// <summary>
    /// 計算集中包裝的划算度
    /// </summary>
    /// <param name="packages">包裹清單</param>
    /// <returns>計算結果</returns>
    public async Task<ShippingCalculationResult> CalculateConsolidationBenefitAsync(List<Package> packages)
    {
      var result = new ShippingCalculationResult
      {
        Packages = packages
      };

      Console.WriteLine(new string('=', 60));
      Console.WriteLine("Tenso 集中包裝划算度計算");
      Console.WriteLine(new string('=', 60));

      // 1. 計算個別包裹運費
      Console.WriteLine("\n1. 計算個別包裹運費:");
      Console.WriteLine(new string('-', 40));

      var individualResults = new List<PackageShippingResult>();
      foreach (var package in packages)
      {
        var shippingOptions = await _apiService.GetShippingEstimateAsync(package);
        var bestOption = GetBestShippingOption(shippingOptions);

        individualResults.Add(new PackageShippingResult
        {
          Package = package,
          ShippingOptions = shippingOptions,
          BestOption = bestOption
        });

        if (bestOption != null)
        {
          Console.WriteLine($"   {package.Name}: {bestOption.Name} - {bestOption.GetTotalFeeValue():N0} 日元");
        }
      }
      result.IndividualResults = individualResults;

      // 2. 計算集中包裝運費
      Console.WriteLine("\n2. 計算集中包裝運費:");
      Console.WriteLine(new string('-', 40));

      var consolidatedPackage = CreateConsolidatedPackage(packages);
      var consolidatedOptions = await _apiService.GetShippingEstimateAsync(consolidatedPackage);
      var consolidatedBestOption = GetBestShippingOption(consolidatedOptions);

      var consolidationFee = CalculateConsolidationFee(packages.Count);

      result.ConsolidatedResult = new ConsolidatedShippingResult
      {
        ConsolidatedPackage = consolidatedPackage,
        ShippingOptions = consolidatedOptions,
        BestOption = consolidatedBestOption,
        ConsolidationFee = consolidationFee,
        TotalCostWithFee = (consolidatedBestOption?.GetTotalFeeValue() ?? 0) + consolidationFee
      };

      Console.WriteLine($"   集中包裝尺寸: {consolidatedPackage.Length}x{consolidatedPackage.Width}x{consolidatedPackage.Height}cm");
      Console.WriteLine($"   集中包裝重量: {consolidatedPackage.Weight:N0}g");
      if (consolidatedBestOption != null)
      {
        Console.WriteLine($"   最佳運送方式: {consolidatedBestOption.Name}");
        Console.WriteLine($"   運費: {consolidatedBestOption.GetTotalFeeValue():N0} 日元");
        Console.WriteLine($"   集中包裝手續費: {consolidationFee:N0} 日元");
        Console.WriteLine($"   總費用: {result.ConsolidatedResult.TotalCostWithFee:N0} 日元");
      }

      // 3. 比較分析
      Console.WriteLine("\n3. 比較分析:");
      Console.WriteLine(new string('-', 40));

      var totalIndividualCost = individualResults.Sum(r => r.BestOption?.GetTotalFeeValue() ?? 0);
      var totalConsolidatedCost = result.ConsolidatedResult.TotalCostWithFee;

      result.IsConsolidationBeneficial = totalConsolidatedCost < totalIndividualCost;
      result.TotalSavings = totalIndividualCost - totalConsolidatedCost;

      Console.WriteLine($"   個別寄送總費用: {totalIndividualCost:N0} 日元");
      Console.WriteLine($"   集中包裝總費用: {totalConsolidatedCost:N0} 日元");

      if (result.IsConsolidationBeneficial)
      {
        Console.WriteLine($"   ✓ 集中包裝划算！可節省 {result.TotalSavings:N0} 日元");
      }
      else
      {
        Console.WriteLine($"   ✗ 個別寄送較划算，集中包裝多花 {Math.Abs(result.TotalSavings):N0} 日元");
      }

      // 4. 檢查集中包裝限制
      Console.WriteLine("\n4. 集中包裝限制檢查:");
      Console.WriteLine(new string('-', 40));
      CheckConsolidationLimits(packages, consolidatedPackage);

      return result;
    }

    /// <summary>
    /// 選擇最佳運送選項 (最便宜且可用的)
    /// </summary>
    private TensoApiResponse? GetBestShippingOption(List<TensoApiResponse> options)
    {
      return options
          .Where(o => o.CanUse && o.GetTotalFeeValue() > 0)
          .OrderBy(o => o.GetTotalFeeValue())
          .FirstOrDefault();
    }

    /// <summary>
    /// 建立集中包裝的虛擬包裹
    /// </summary>
    private Package CreateConsolidatedPackage(List<Package> packages)
    {
      // 簡化計算：假設所有包裹疊在一起
      // 實際情況會更複雜，需要考慮包裝效率
      var totalWeight = packages.Sum(p => p.Weight);
      var maxLength = packages.Max(p => p.Length);
      var maxWidth = packages.Max(p => p.Width);
      var totalHeight = packages.Sum(p => p.Height);

      return new Package
      {
        Name = "集中包裝",
        Weight = totalWeight,
        Length = maxLength,
        Width = maxWidth,
        Height = totalHeight
      };
    }

    /// <summary>
    /// 計算集中包裝手續費
    /// 申請費用 200 日元 + (包裹數量 - 1) × 300 日元
    /// </summary>
    private int CalculateConsolidationFee(int packageCount)
    {
      if (packageCount <= 1) return 0;
      return 200 + (packageCount - 1) * 300;
    }

    /// <summary>
    /// 檢查集中包裝限制
    /// </summary>
    private void CheckConsolidationLimits(List<Package> packages, Package consolidatedPackage)
    {
      var totalWeight = consolidatedPackage.Weight;

      Console.WriteLine($"   包裹數量: {packages.Count} 件");
      Console.WriteLine($"   總重量: {totalWeight / 1000m:F1} kg (限制: 30 kg)");

      if (totalWeight > 30000)
      {
        Console.WriteLine("   ⚠️  警告: 超過重量限制 30kg");
      }
      else
      {
        Console.WriteLine("   ✓ 重量符合限制");
      }

      Console.WriteLine("   ℹ️  其他限制:");
      Console.WriteLine("      - 包裹登錄後 30 天內申請");
      Console.WriteLine("      - 包裹合計金額 20 萬日元以內");
    }

    /// <summary>
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
      _apiService?.Dispose();
    }
  }
}
