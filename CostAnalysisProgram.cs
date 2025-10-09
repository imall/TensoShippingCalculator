using System;
using System.Collections.Generic;
using System.Linq;
using TensoShippingCalculator.Models;
using TensoShippingCalculator.Services;

namespace TensoShippingCalculator
{
    /// <summary>
    /// 成本分析程式 - 分析在什麼情況下個別寄送比集中包裝便宜
    /// </summary>
    public class CostAnalysisProgram
    {
        /// <summary>
        /// 分析不同情況下的成本效益
        /// </summary>
        public static async Task AnalyzeCostEfficiencyAsync()
        {
            Console.WriteLine("=== Tenso 運費成本分析 ===");
            Console.WriteLine("分析在什麼情況下個別寄送比集中包裝便宜\n");

            var scenarios = new List<AnalysisScenario>
            {
                // 情境1: 小包裹但數量多
                new AnalysisScenario
                {
                    Name = "小包裹多數量",
                    Description = "5個小包裹 (10×8×5cm, 200g)",
                    Packages = CreatePackages(5, 200, 10, 8, 5)
                },

                // 情境2: 中等包裹少數量
                new AnalysisScenario
                {
                    Name = "中包裹少數量",
                    Description = "2個中等包裹 (25×20×15cm, 2000g)",
                    Packages = CreatePackages(2, 2000, 25, 20, 15)
                },

                // 情境3: 大包裹少數量
                new AnalysisScenario
                {
                    Name = "大包裹少數量",
                    Description = "2個大包裹 (40×30×25cm, 5000g)",
                    Packages = CreatePackages(2, 5000, 40, 30, 25)
                },

                // 情境4: 輕但體積大的包裹
                new AnalysisScenario
                {
                    Name = "輕量大體積",
                    Description = "3個輕量大體積包裹 (50×40×30cm, 1000g)",
                    Packages = CreatePackages(3, 1000, 50, 40, 30)
                },

                // 情境5: 重但體積小的包裹
                new AnalysisScenario
                {
                    Name = "重量小體積",
                    Description = "3個重量小體積包裹 (15×10×8cm, 3000g)",
                    Packages = CreatePackages(3, 3000, 15, 10, 8)
                },

                // 情境6: 尺寸差異很大的包裹組合
                new AnalysisScenario
                {
                    Name = "尺寸差異大",
                    Description = "1大1小包裹組合",
                    Packages = new List<Package>
                    {
                        new Package { Name = "大包裹", Weight = 4000, Length = 45, Width = 35, Height = 25 },
                        new Package { Name = "小包裹", Weight = 500, Length = 12, Width = 8, Height = 6 }
                    }
                },

                // 情境7: 接近尺寸限制的包裹
                new AnalysisScenario
                {
                    Name = "接近限制",
                    Description = "2個接近各種運送方式尺寸限制的包裹",
                    Packages = new List<Package>
                    {
                        new Package { Name = "包裹1", Weight = 8000, Length = 40, Width = 35, Height = 25 },
                        new Package { Name = "包裹2", Weight = 7000, Length = 38, Width = 32, Height = 20 }
                    }
                }
            };

            using var calculationService = new ShippingCalculationService();

            Console.WriteLine("開始分析各種情境...\n");

            foreach (var scenario in scenarios)
            {
                try
                {
                    Console.WriteLine($"=== {scenario.Name} ===");
                    Console.WriteLine($"描述: {scenario.Description}");

                    var result = await calculationService.CalculateConsolidationBenefitAsync(scenario.Packages);

                    // 計算個別寄送總費用
                    var individualTotal = result.IndividualResults.Sum(r => int.TryParse(r.BestOption?.TotalFee.Replace(",", ""), out var fee) ? fee : 0);
                    var consolidatedTotal = result.ConsolidatedResult.TotalCostWithFee;
                    var consolidationFee = CalculateConsolidationFee(scenario.Packages.Count);

                    Console.WriteLine($"包裹數量: {scenario.Packages.Count}");
                    Console.WriteLine($"總重量: {scenario.Packages.Sum(p => p.Weight):N0}g");
                    Console.WriteLine($"集中包裝尺寸: {result.ConsolidatedResult.ConsolidatedPackage.Length}×{result.ConsolidatedResult.ConsolidatedPackage.Width}×{result.ConsolidatedResult.ConsolidatedPackage.Height}cm");
                    Console.WriteLine($"個別寄送總費用: {individualTotal:N0} 日元");
                    Console.WriteLine($"集中包裝運費: {result.ConsolidatedResult.BestOption?.TotalFee:N0} 日元");
                    Console.WriteLine($"集中包裝手續費: {consolidationFee:N0} 日元");
                    Console.WriteLine($"集中包裝總費用: {consolidatedTotal:N0} 日元");

                    if (result.IsConsolidationBeneficial)
                    {
                        Console.WriteLine($"✅ 集中包裝划算 - 節省 {result.TotalSavings:N0} 日元 ({(result.TotalSavings / individualTotal * 100):F1}%)");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 個別寄送划算 - 集中包裝多花 {Math.Abs(result.TotalSavings):N0} 日元 ({(Math.Abs(result.TotalSavings) / individualTotal * 100):F1}%)");
                    }

                    // 分析原因
                    AnalyzeReason(scenario, result);

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 分析 {scenario.Name} 時發生錯誤: {ex.Message}\n");
                }
            }

            // 總結分析
            Console.WriteLine("=== 分析總結 ===");
            Console.WriteLine("個別寄送比較划算的情況通常包括:");
            Console.WriteLine("1. 包裹數量少 (2-3個)");
            Console.WriteLine("2. 個別包裹都能使用便宜的運送方式 (如 tenso空運台灣)");
            Console.WriteLine("3. 集中包裝後尺寸大幅增加，導致無法使用便宜運送方式");
            Console.WriteLine("4. 集中包裝手續費相對於運費節省較高");
            Console.WriteLine("5. 包裹體積差異很大時，集中包裝效率低");
        }

        /// <summary>
        /// 建立指定數量的相同包裹
        /// </summary>
        private static List<Package> CreatePackages(int count, decimal weight, decimal length, decimal width, decimal height)
        {
            var packages = new List<Package>();
            for (int i = 1; i <= count; i++)
            {
                packages.Add(new Package
                {
                    Name = $"包裹{i}",
                    Weight = weight,
                    Length = length,
                    Width = width,
                    Height = height
                });
            }
            return packages;
        }

        /// <summary>
        /// 計算集中包裝手續費
        /// </summary>
        private static int CalculateConsolidationFee(int packageCount)
        {
            if (packageCount <= 1) return 0;
            return 200 + (packageCount - 1) * 300;
        }

        /// <summary>
        /// 分析為什麼個別寄送或集中包裝比較划算
        /// </summary>
        private static void AnalyzeReason(AnalysisScenario scenario, ShippingCalculationResult result)
        {
            Console.WriteLine("分析原因:");

            var individualTotal = result.IndividualResults.Sum(r => int.TryParse(r.BestOption?.TotalFee.Replace(",", ""), out var fee) ? fee : 0);
            var consolidationFee = CalculateConsolidationFee(scenario.Packages.Count);

            // 分析個別包裹使用的運送方式
            var individualMethods = result.IndividualResults
                .Where(r => r.BestOption != null)
                .GroupBy(r => r.BestOption.Name)
                .Select(g => $"{g.Key} ({g.Count()}個)")
                .ToList();

            Console.WriteLine($"  個別運送方式: {string.Join(", ", individualMethods)}");
            Console.WriteLine($"  集中包裝運送方式: {result.ConsolidatedResult.BestOption?.Name ?? "無可用選項"}");
            Console.WriteLine($"  集中包裝手續費占比: {(consolidationFee / (double)individualTotal * 100):F1}%");

            // 體積效率分析
            var individualVolumes = scenario.Packages.Sum(p => p.Length * p.Width * p.Height);
            var consolidatedVolume = result.ConsolidatedResult.ConsolidatedPackage.Length *
                                   result.ConsolidatedResult.ConsolidatedPackage.Width *
                                   result.ConsolidatedResult.ConsolidatedPackage.Height;
            var volumeEfficiency = (double)individualVolumes / (double)consolidatedVolume * 100;

            Console.WriteLine($"  空間利用效率: {volumeEfficiency:F1}% (個別體積總和/集中包裝體積)");

            if (volumeEfficiency < 50)
            {
                Console.WriteLine("  ⚠️ 空間利用效率低，可能導致體積重量增加");
            }
        }
    }

    /// <summary>
    /// 分析情境
    /// </summary>
    public class AnalysisScenario
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Package> Packages { get; set; } = new List<Package>();
    }
}
