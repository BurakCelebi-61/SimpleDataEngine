using System.Diagnostics;
using System.Text.Json;

namespace SimpleDataEngine.Performance
{
    /// <summary>
    /// Performance report generator
    /// </summary>
    /// <summary>
    /// Performance report generator
    /// </summary>
    public class PerformanceReportGenerator
    {
        private readonly List<PerformanceResult> _results;
        private readonly object _lock = new object();

        public PerformanceReportGenerator()
        {
            _results = new List<PerformanceResult>();
        }

        /// <summary>
        /// Add performance result
        /// </summary>
        public void AddResult(PerformanceResult result)
        {
            if (result == null) return;

            lock (_lock)
            {
                _results.Add(result);
            }
        }

        /// <summary>
        /// Add multiple results
        /// </summary>
        public void AddResults(IEnumerable<PerformanceResult> results)
        {
            if (results == null) return;

            lock (_lock)
            {
                _results.AddRange(results);
            }
        }

        /// <summary>
        /// Generate summary report
        /// </summary>
        public PerformanceSummary GenerateSummary(TimeSpan? timeWindow = null)
        {
            lock (_lock)
            {
                var filteredResults = _results.AsEnumerable();

                if (timeWindow.HasValue)
                {
                    var cutoff = DateTime.UtcNow - timeWindow.Value;
                    filteredResults = filteredResults.Where(r => r.EndTime >= cutoff);
                }

                var resultsList = filteredResults.ToList();

                if (!resultsList.Any())
                {
                    return new PerformanceSummary
                    {
                        TotalOperations = 0,
                        TimeWindow = timeWindow,
                        GeneratedAt = DateTime.UtcNow
                    };
                }

                var summary = new PerformanceSummary
                {
                    TotalOperations = resultsList.Count,
                    AverageDuration = TimeSpan.FromMilliseconds(resultsList.Average(r => r.DurationMs)),
                    MinDuration = TimeSpan.FromMilliseconds(resultsList.Min(r => r.DurationMs)),
                    MaxDuration = TimeSpan.FromMilliseconds(resultsList.Max(r => r.DurationMs)),
                    MedianDuration = CalculateMedianDuration(resultsList),
                    TotalDuration = TimeSpan.FromMilliseconds(resultsList.Sum(r => r.DurationMs)),
                    TimeWindow = timeWindow,
                    GeneratedAt = DateTime.UtcNow
                };

                // Calculate percentiles
                var sortedDurations = resultsList.Select(r => r.DurationMs).OrderBy(d => d).ToList();
                summary.P95Duration = TimeSpan.FromMilliseconds(CalculatePercentile(sortedDurations, 95));
                summary.P99Duration = TimeSpan.FromMilliseconds(CalculatePercentile(sortedDurations, 99));

                // Group by operation
                summary.OperationSummaries = resultsList
                    .GroupBy(r => r.OperationName)
                    .Select(g => new OperationSummary
                    {
                        OperationName = g.Key,
                        Count = g.Count(),
                        AverageDuration = TimeSpan.FromMilliseconds(g.Average(r => r.DurationMs)),
                        MinDuration = TimeSpan.FromMilliseconds(g.Min(r => r.DurationMs)),
                        MaxDuration = TimeSpan.FromMilliseconds(g.Max(r => r.DurationMs)),
                        TotalDuration = TimeSpan.FromMilliseconds(g.Sum(r => r.DurationMs))
                    })
                    .OrderByDescending(os => os.TotalDuration)
                    .ToList();

                // Performance category breakdown
                summary.CategoryBreakdown = resultsList
                    .GroupBy(r => r.Category)
                    .ToDictionary(
                        g => g.Key,
                        g => new CategoryStats
                        {
                            Count = g.Count(),
                            Percentage = (double)g.Count() / resultsList.Count * 100,
                            AverageDuration = TimeSpan.FromMilliseconds(g.Average(r => r.DurationMs))
                        }
                    );

                return summary;
            }
        }

        /// <summary>
        /// Generate detailed report as JSON
        /// </summary>
        public async Task<string> GenerateDetailedReportAsync(TimeSpan? timeWindow = null)
        {
            var summary = GenerateSummary(timeWindow);

            var report = new
            {
                Summary = summary,
                TopSlowestOperations = GetTopSlowestOperations(10, timeWindow),
                RecentOperations = GetRecentOperations(50),
                PerformanceTrends = CalculatePerformanceTrends(timeWindow)
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(report, options);
        }

        /// <summary>
        /// Get top slowest operations
        /// </summary>
        public List<PerformanceResult> GetTopSlowestOperations(int count = 10, TimeSpan? timeWindow = null)
        {
            lock (_lock)
            {
                var filteredResults = _results.AsEnumerable();

                if (timeWindow.HasValue)
                {
                    var cutoff = DateTime.UtcNow - timeWindow.Value;
                    filteredResults = filteredResults.Where(r => r.EndTime >= cutoff);
                }

                return filteredResults
                    .OrderByDescending(r => r.DurationMs)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Get recent operations
        /// </summary>
        public List<PerformanceResult> GetRecentOperations(int count = 50)
        {
            lock (_lock)
            {
                return _results
                    .OrderByDescending(r => r.EndTime)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Clear old results
        /// </summary>
        public void ClearOldResults(TimeSpan maxAge)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - maxAge;
                _results.RemoveAll(r => r.EndTime < cutoff);
            }
        }

        /// <summary>
        /// Clear all results
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _results.Clear();
            }
        }

        private TimeSpan CalculateMedianDuration(List<PerformanceResult> results)
        {
            var sortedDurations = results.Select(r => r.DurationMs).OrderBy(d => d).ToList();
            var count = sortedDurations.Count;

            if (count % 2 == 0)
            {
                var median = (sortedDurations[count / 2 - 1] + sortedDurations[count / 2]) / 2;
                return TimeSpan.FromMilliseconds(median);
            }
            else
            {
                return TimeSpan.FromMilliseconds(sortedDurations[count / 2]);
            }
        }

        private double CalculatePercentile(List<double> sortedValues, double percentile)
        {
            if (!sortedValues.Any()) return 0;

            var index = (percentile / 100.0) * (sortedValues.Count - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);

            if (lower == upper)
            {
                return sortedValues[lower];
            }

            var weight = index - lower;
            return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
        }

        private Dictionary<string, object> CalculatePerformanceTrends(TimeSpan? timeWindow)
        {
            // Performance trend calculation logic
            var trends = new Dictionary<string, object>();

            lock (_lock)
            {
                var filteredResults = _results.AsEnumerable();
                if (timeWindow.HasValue)
                {
                    var cutoff = DateTime.UtcNow - timeWindow.Value;
                    filteredResults = filteredResults.Where(r => r.EndTime >= cutoff);
                }

                var resultsList = filteredResults.ToList();
                if (resultsList.Count >= 2)
                {
                    var first50Percent = resultsList.Take(resultsList.Count / 2);
                    var last50Percent = resultsList.Skip(resultsList.Count / 2);

                    var firstAvg = first50Percent.Average(r => r.DurationMs);
                    var lastAvg = last50Percent.Average(r => r.DurationMs);

                    trends["PerformanceChange"] = ((lastAvg - firstAvg) / firstAvg) * 100;
                    trends["Trend"] = lastAvg > firstAvg ? "Deteriorating" : "Improving";
                }
            }

            return trends;
        }
    }
}