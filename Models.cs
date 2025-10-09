using System.Text.Json.Serialization;

namespace TensoShippingCalculator.Models
{    /// <summary>
     /// 包裹資訊模型
     /// </summary>
  public class Package
  {
    public decimal Weight { get; set; } // 重量 (公克)
    public decimal Length { get; set; } // 長度 (公分)
    public decimal Width { get; set; }  // 寬度 (公分)
    public decimal Height { get; set; } // 高度 (公分)
    public string Name { get; set; } = string.Empty; // 包裹名稱

    /// <summary>
    /// 計算體積重量 (公克)
    /// 體積重量 = 長 × 寬 × 高 (cm³) ÷ 5000
    /// </summary>
    public decimal VolumetricWeight => (Length * Width * Height) / 5000m;

    /// <summary>
    /// 取得計費重量 (實際重量與體積重量的較大值)
    /// </summary>
    public decimal BillableWeight => Math.Max(Weight, VolumetricWeight);
  }

  /// <summary>
  /// Tenso API 請求模型
  /// </summary>
  public class TensoApiRequest
  {
    [JsonPropertyName("weight")]
    public string Weight { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public string Length { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public string Width { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public string Height { get; set; } = string.Empty;

    [JsonPropertyName("country_id")]
    public string CountryId { get; set; } = "213"; // 台灣
  }

  /// <summary>
  /// Tenso API 回應模型
  /// </summary>
  public class TensoApiResponse
  {
    [JsonPropertyName("shipping_method_id")]
    public int ShippingMethodId { get; set; }

    [JsonPropertyName("country_id")]
    public int CountryId { get; set; }

    [JsonPropertyName("deliverable_status")]
    public int DeliverableStatus { get; set; }

    [JsonPropertyName("weight_limits")]
    public object WeightLimitsRaw { get; set; } = new object();

    [JsonPropertyName("longest_side")]
    public object LongestSideRaw { get; set; } = new object();

    [JsonPropertyName("three_side_limit")]
    public object ThreeSideLimitRaw { get; set; } = new object();

    // 取得實際的長度限制值
    public double LongestSide
    {
      get
      {
        if (LongestSideRaw is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
          return element.GetDouble();
        }
        if (LongestSideRaw is System.Text.Json.JsonElement strElement && strElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
          if (double.TryParse(strElement.GetString(), out double result))
            return result;
        }
        return 0;
      }
    }

    // 取得實際的三邊限制值
    public double ThreeSideLimit
    {
      get
      {
        if (ThreeSideLimitRaw is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
          return element.GetDouble();
        }
        if (ThreeSideLimitRaw is System.Text.Json.JsonElement strElement && strElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
          if (double.TryParse(strElement.GetString(), out double result))
            return result;
        }
        return 0;
      }
    }

    [JsonPropertyName("delivery_days")]
    public string DeliveryDays { get; set; } = string.Empty;

    [JsonPropertyName("can_use")]
    public bool CanUse { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_over_weight")]
    public bool IsOverWeight { get; set; }

    [JsonPropertyName("is_over_weight_volumetric")]
    public bool IsOverWeightVolumetric { get; set; }

    [JsonPropertyName("is_over_size")]
    public bool IsOverSize { get; set; }

    [JsonPropertyName("handling_fee")]
    public int HandlingFee { get; set; }

    [JsonPropertyName("handling_fee_name")]
    public string HandlingFeeName { get; set; } = string.Empty;

    [JsonPropertyName("shipping_fee")]
    public string ShippingFee { get; set; } = string.Empty;

    [JsonPropertyName("is_volumetric_weight")]
    public bool IsVolumetricWeight { get; set; }

    [JsonPropertyName("service_fee")]
    public string ServiceFee { get; set; } = string.Empty;

    [JsonPropertyName("total_fee")]
    public string TotalFee { get; set; } = string.Empty;

    [JsonPropertyName("original_total_fee")]
    public int? OriginalTotalFee { get; set; }

    [JsonPropertyName("original_shipping_fee")]
    public int? OriginalShippingFee { get; set; }

    [JsonPropertyName("original_service_fee")]
    public int? OriginalServiceFee { get; set; }

    [JsonPropertyName("weight_limits_kg")]
    public string WeightLimitsKg { get; set; } = string.Empty;

    // 取得實際的重量限制值
    public int WeightLimits
    {
      get
      {
        if (WeightLimitsRaw is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
          return element.GetInt32();
        }
        return 0;
      }
    }

    /// <summary>
    /// 安全地取得總費用數值
    /// 優先使用 total_fee 字串（API 最終顯示值），如果無法解析則使用 original_total_fee
    /// </summary>
    public int GetTotalFeeValue()
    {
      // 優先使用 total_fee (字串型態，API 的最終顯示值)
      if (!string.IsNullOrEmpty(TotalFee) && TotalFee != "--")
      {
        // 移除逗號和其他非數字字符
        var cleanedValue = TotalFee.Replace(",", "").Replace("日元", "").Trim();
        if (int.TryParse(cleanedValue, out int result))
        {
          return result;
        }
      }

      // 如果 total_fee 無法解析，才使用 original_total_fee 作為備用
      if (OriginalTotalFee.HasValue && OriginalTotalFee.Value > 0)
      {
        return OriginalTotalFee.Value;
      }

      return 0;
    }

    /// <summary>
    /// 安全地取得運費數值
    /// 優先使用 shipping_fee 字串（API 最終顯示值），如果無法解析則使用 original_shipping_fee
    /// </summary>
    public int GetShippingFeeValue()
    {
      // 優先使用 shipping_fee (字串型態)
      if (!string.IsNullOrEmpty(ShippingFee) && ShippingFee != "--")
      {
        var cleanedValue = ShippingFee.Replace(",", "").Replace("日元", "").Trim();
        if (int.TryParse(cleanedValue, out int result))
        {
          return result;
        }
      }

      // 備用：使用 original_shipping_fee
      if (OriginalShippingFee.HasValue && OriginalShippingFee.Value > 0)
      {
        return OriginalShippingFee.Value;
      }

      return 0;
    }

    /// <summary>
    /// 安全地取得服務費數值
    /// 優先使用 service_fee 字串（API 最終顯示值），如果無法解析則使用 original_service_fee
    /// </summary>
    public int GetServiceFeeValue()
    {
      // 優先使用 service_fee (字串型態)
      if (!string.IsNullOrEmpty(ServiceFee) && ServiceFee != "--")
      {
        var cleanedValue = ServiceFee.Replace(",", "").Replace("日元", "").Trim();
        if (int.TryParse(cleanedValue, out int result))
        {
          return result;
        }
      }

      // 備用：使用 original_service_fee
      if (OriginalServiceFee.HasValue && OriginalServiceFee.Value > 0)
      {
        return OriginalServiceFee.Value;
      }

      return 0;
    }
  }

  /// <summary>
  /// 運費計算結果
  /// </summary>
  public class ShippingCalculationResult
  {
    public List<Package> Packages { get; set; } = new();
    public List<PackageShippingResult> IndividualResults { get; set; } = new();
    public ConsolidatedShippingResult? ConsolidatedResult { get; set; }
    public bool IsConsolidationBeneficial { get; set; }
    public int TotalSavings { get; set; } // 節省金額 (日元)
  }

  /// <summary>
  /// 個別包裹運費結果
  /// </summary>
  public class PackageShippingResult
  {
    public Package Package { get; set; } = new();
    public List<TensoApiResponse> ShippingOptions { get; set; } = new();
    public TensoApiResponse? BestOption { get; set; } // 最便宜的選項
  }

  /// <summary>
  /// 集中包裝運費結果
  /// </summary>
  public class ConsolidatedShippingResult
  {
    public Package ConsolidatedPackage { get; set; } = new();
    public List<TensoApiResponse> ShippingOptions { get; set; } = new();
    public TensoApiResponse? BestOption { get; set; }
    public int ConsolidationFee { get; set; } // 集中包裝手續費
    public int TotalCostWithFee { get; set; } // 包含手續費的總費用
  }
}
