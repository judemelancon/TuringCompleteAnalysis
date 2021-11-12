<Query Kind="Program">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Windows.Forms.DataVisualization.Charting</Namespace>
</Query>

// This is can be run in LINQPad ( http://www.linqpad.net/ ) in C# Program mode.
// If it's copy-pasted, the <Namespace> elements need to be converted to namespace imports (accessed via Ctrl+Shift+M), and all of the XML above needs to be deleted.

public static class Configuration {
    public const int LeaderboardPositionsShown = 100;
    public static readonly IReadOnlyList<int> ProfileIds = new[] { 11, 6 };
    public const bool ChartOnlyScoredLevels = false;
    public const bool ChartScoresLogarithmically = true;
    public const bool LogarithmicHistograms = false;
    public const int MaximumHistogramBuckets = 50;
    public const int HistogramTooltipLinesBeforeExcludingZeroCounts = 10;
    /// <summary>If set, scores above this threshold are removed from most statistics. null disables the censoring threshold entirely.</summary>
    /// <remarks>99,999 is currently the score for using an unscored component. It skews the statistics pretty badly to leave those in the calculations.</remarks>
    public static readonly int? ScoreCensoringThreshold = 99999;
    /// <summary>If set, this produces and dumps static images of the charts suitable for sharing.</summary>
    public static readonly Size? DumpedImageDimensions = null; // e.g. new Size(2000,1000)
    public const string NotApplicableText = "-";
    public const string IncompleteText = "i";

    internal static int GetMaximumScoreShownOnHistogram(int solvers, int minimum, decimal median, int maximum)
        => solvers <= Configuration.LeaderboardPositionsShown || (maximum - minimum + 1) <= MaximumHistogramBuckets
               ? maximum
               : Math.Min(maximum, (int)Math.Ceiling(2m * median));

    // The remaining configuration settings all relate specifically to snapshot generation.
    public const bool EnableSnapshotGeneration = false;
    public static readonly string SnapshotDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath), "..", "Snapshots"));
    public static readonly string SnapshotMarkdownFilePath = Path.Combine(SnapshotDirectory, "README.md");
    public static readonly Size LevelChartSnapshotDimensions = new Size(1500, 900);
    public static readonly Size HistogramSnapshotDimensions = new Size(1000, 500);
    public static readonly Regex SnapshotIntroductionReplacementPattern = new Regex(@"(?<comment><!--\s*IntroductionReplacementTarget.*?-->).*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    public static readonly Regex SnapshotLevelDetailsReplacementPattern = new Regex(@"(?<opener><!--\s*LevelDetailsReplacementTarget.*?-->).*(?<closer><!--/LevelDetailsReplacementTarget-->)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
}

public const string ScoresKey = "TuringCompleteGameScores";
public const string PlayersKey = "TuringCompleteGamePlayers";
public static HttpClient Client { get; } = new HttpClient();

public static IReadOnlyList<Player> Players { get; } = GetPlayersAsync().ConfigureAwait(false).GetAwaiter().GetResult();
public static IReadOnlyList<Score> Scores { get; } = GetScoresAsync().ConfigureAwait(false).GetAwaiter().GetResult();
private static IReadOnlyDictionary<int, Player> _PlayerMap;
public static IReadOnlyDictionary<int, Player> PlayerMap => _PlayerMap ??= Players.ToDictionary(p => p.Id);
private static IReadOnlyList<Level> _Levels;
public static IReadOnlyList<Level> Levels => _Levels ??= Level.ConstructLevels();

public void Main() {
    Levels.Dump("Levels", toDataGrid: true);
    Players.Dump("Players", toDataGrid: true);

    IReadOnlyList<Player> selectedPlayers = Configuration.ProfileIds
                                                         .Select(playerId => PlayerMap[playerId])
                                                         .ToArray();
    foreach (Player player in selectedPlayers)
        player.ShowPlacements();
    DumpLevelChart(selectedPlayers);

    if (Configuration.EnableSnapshotGeneration)
        SaveSnapshots();
}

public static Chart GenerateLevelChart(IReadOnlyList<Player> includedPlayers) {
    void HandleChartClick(object sender, EventArgs e) {
        if (!(sender is Chart windowsChart))
            return;
        if (!(e is MouseEventArgs typedEventArgs))
            return;
        HitTestResult hit = windowsChart.HitTest(typedEventArgs.X, typedEventArgs.Y);
        if (hit.PointIndex >= 0 && Levels[hit.PointIndex].Scored)
            Levels[hit.PointIndex].DisplayLeaderboard();
    }

    Chart chart = new Chart();
    chart.Size = Configuration.LevelChartSnapshotDimensions;
    chart.Click += HandleChartClick;
    Legend legend = chart.Legends.Add("Legend");
    legend.Position = new ElementPosition(0f, 95f, 100f, 5f);
    ChartArea area = chart.ChartAreas.Add("Levels Chart");
    area.Position = new ElementPosition(0f, 0f, 100f, 95f);
    area.AxisX.Interval = 1;
    area.AxisX.MajorGrid.LineColor = Color.FromArgb(63, Color.Black);
    area.AxisY.MajorGrid.LineColor = Color.FromArgb(63, Color.Black);
    area.AxisY.Title = (Configuration.ChartScoresLogarithmically ? "Binary Logarithm of " : string.Empty) + "Sum of Scores";
    if (Configuration.ChartScoresLogarithmically) {
        area.AxisY.IsLogarithmic = true;
        area.AxisY.LogarithmBase = 2.0;
        area.AxisY.Minimum = 1;
    }
    else {
        area.AxisY.IsStartedFromZero = true;
    }
    area.AxisY2.Title = "Solvers";
    area.AxisY2.IsStartedFromZero = true;
    area.AxisY2.MajorGrid.Enabled = false;


    {
        Series series = chart.Series.Add("Solvers");
        series.ChartType = SeriesChartType.Area;
        series.Color = System.Drawing.Color.FromArgb(63, System.Drawing.Color.Gray);
        series.YAxisType = AxisType.Secondary;
        foreach (Level level in Levels)
            series.Points.Add(new DataPoint(0.0, level.Solvers) {
                AxisLabel = level.LevelId,
                ToolTip = $"{level.SolversText} players have solved {level.LevelId}"
            });
    }
    {
        Series series = chart.Series.Add("Distribution");
        series.ChartType = SeriesChartType.Candlestick;
        series.Color = Color.LimeGreen;
        series.BorderWidth = 3;
        series.SetCustomProperty("PriceUpColor", $"#{Color.Green.ToArgb():x2}");
        series.SetCustomProperty("PriceDownColor", $"#{Color.LightGreen.ToArgb():x2}");
        foreach (Level level in Levels) {
            DataPoint point = new DataPoint {
                AxisLabel = level.LevelId,
                IsEmpty = !level.Scored
            };
            if (level.Minimum.HasValue) {
                point.ToolTip = $"{level.LevelId}{Environment.NewLine}Best {level.MinimumText}{Environment.NewLine}Median {level.MedianText}{Environment.NewLine}Mean {level.MeanText}{Environment.NewLine}Worst {level.MaximumText}";
                point.YValues = new[] {
                                        Math.Max(0.5, level.Maximum.Value),
                                        Math.Max(0.5, level.Minimum.Value),
                                        Math.Max(0.5, (double)level.Median.Value),
                                        Math.Max(0.5, level.Mean.Value)
                                      };
            }
            else {
                point.ToolTip = $"{level.LevelId} is not scored";
            }
            series.Points.Add(point);
        }
    }
    foreach (Player player in includedPlayers) {
        Series series = chart.Series.Add(player.Name);
        series.ChartType = SeriesChartType.Point;
        series.MarkerSize = 9;
        foreach (Level level in Levels) {
            int? score = level.Entries.SingleOrDefault(le => le.Score.PlayerId == player.Id)?.Score.Sum;
            double chartedScore = Configuration.ChartScoresLogarithmically && score.HasValue
                                      ? Math.Max(1, score.Value)
                                      : score ?? 0.0;
            series.Points.Add(new DataPoint(0, chartedScore) {
                AxisLabel = level.LevelId,
                IsEmpty = !score.HasValue,
                ToolTip = $"{player.Name} did {level.LevelId} in {score:n0}"
            });
        }
    }

    return chart;
}

public static void DumpLevelChart(IReadOnlyList<Player> includedPlayers) {
    using Chart chart = GenerateLevelChart(includedPlayers);
    chart.Dump();
    if (Configuration.DumpedImageDimensions.HasValue)
        chart.ToBitmap(Configuration.DumpedImageDimensions.Value.Width, Configuration.DumpedImageDimensions.Value.Height).Dump("Chart Image");
}

public static void SaveSnapshots() {
    $"Saving snapshots to {Configuration.SnapshotDirectory}".Dump();
    Directory.CreateDirectory(Configuration.SnapshotDirectory);

    foreach (Level level in Levels)
        level.Histogram?.SaveImage(Path.Combine(Configuration.SnapshotDirectory, $"histogram {level.LevelId}.png"), ChartImageFormat.Png);

    using Chart levelChart = GenerateLevelChart(new Player[0]);
    levelChart.SaveImage(Path.Combine(Configuration.SnapshotDirectory, "level chart.png"), ChartImageFormat.Png);

    UpdateSnapshotMarkdown();
}

public static void UpdateSnapshotMarkdown() {
    string initial = File.ReadAllText(Configuration.SnapshotMarkdownFilePath);
    //initial.Dump(nameof(initial));
    string processed = initial;

    processed = Configuration.SnapshotIntroductionReplacementPattern.Replace(processed,
                                                                             m => m.Groups["comment"].Value + $"This was generated at {DateTime.UtcNow:u} when there were {Scores.Count:n0} scores from {Players.Count:n0} players.");

    processed = Configuration.SnapshotLevelDetailsReplacementPattern.Replace(processed,
                                                                             m => Levels.Aggregate((new StringBuilder()).AppendLine(m.Groups["opener"].Value)
                                                                                                                        .AppendLine("Level|Solvers|in First|Best|Median|Mean|Histogram")
                                                                                                                        .AppendLine("-----|-------|--------|----|------|----|---------"),
                                                                                                   (sb, l) => sb.Append($"{l.LevelId}|{l.SolversText}|{l.TiedForFirstText}|{l.MinimumText}|{l.MedianText}|{l.MeanText}|")
                                                                                                                .AppendLine(l.Scored ? $"![Histogram for {l.LevelId}](histogram {l.LevelId}.png)" : "unscored"),
                                                                                                   sb => sb.AppendLine(m.Groups["closer"].Value)
                                                                                                           .ToString()));

    processed.Dump(nameof(processed));
    File.WriteAllText(Configuration.SnapshotMarkdownFilePath, processed);
}

public static async Task<IReadOnlyList<Player>> GetPlayersAsync() {
    string rawPlayerData = await Util.Cache(async () => await Client.GetStringAsync("https://turingcomplete.game/api_usernames"),
                                                                                    PlayersKey,
                                                                                    out bool fromCache);
    //rawPlayerData.Dump(nameof(rawPlayerData));
    IReadOnlyList<Player> players = rawPlayerData.Trim()
                                                 .Split('\n')
                                                 .Select(record => record.Split(',', 2))
                                                 .Select(fields => new Player(int.Parse(fields[0]), fields[1]))
                                                 .OrderBy(p => p.Name)
                                                 .ThenBy(p => p.Id)
                                                 .ToArray();
    $"{players.Count:n0} players{(fromCache ? " from cache" : string.Empty)}".Dump();
    return players;
}

public static async Task<IReadOnlyList<Score>> GetScoresAsync() {
    string rawScoreData = await Util.Cache(async () => await Client.GetStringAsync("https://turingcomplete.game/api_scores"),
                                                                                   ScoresKey,
                                                                                   out bool fromCache);
    //rawScoreData.Dump(nameof(rawScoreData));
    IReadOnlyList<Score> scores = rawScoreData.Trim()
                                              .Split('\n')
                                              .Select(record => record.Split(','))
                                              .Select(fields => new Score(int.Parse(fields[0]),
                                                                          fields[1],
                                                                          int.Parse(fields[2]),
                                                                          int.Parse(fields[3]),
                                                                          int.Parse(fields[4])))
                                              .ToArray();
    $"{scores.Count:n0} scores{(fromCache ? " from cache" : string.Empty)}".Dump();
    return scores;
}

public record Player(int Id, string Name) {
    private IReadOnlyList<object> _Placements;
    public IReadOnlyList<object> Placements => _Placements ??= Levels.Select(level => new PlacementEntry(level, level.Entries.SingleOrDefault(le => le.Score.PlayerId == Id))).ToList();
    public Hyperlinq PlacementsLink => new Hyperlinq(ShowPlacements, "Placements");
    public Hyperlinq ProfileLink { get; } = new Hyperlinq($"https://turingcomplete.game/profile/{Id}",
                                                  Name == null
                                                    ? "!![null]"
                                                    : Name.Length == 0
                                                        ? "!![empty]"
                                                        : string.IsNullOrWhiteSpace(Name)
                                                            ? "!![whitespace]"
                                                            : Name);

    private object ToDump()
        => new {
            Id,
            Name,
            ProfileLink,
            Placements = new Hyperlinq(ShowPlacements, "Placements")
        };

    public void ShowPlacements() => Placements.Dump($"Placements for {Name}", toDataGrid: true);
}

public record PlacementEntry(Level Level, LeaderboardEntry Entry) {
    private object ToDump()
        => new {
            Level = new Hyperlinq(() => Level.DisplayLeaderboard(), Level.LevelId),
            Histogram = Level.GetHistogramLink(),
            Rank = Entry == null
                       ? "incomplete"
                       : Level.Scored
                           ? Entry.DisplayRank
                           : "unscored",
            Sigma = Entry?.SigmaText ?? Configuration.IncompleteText,
            Pseudopercentile = Entry?.Pseudopercentile ?? Configuration.IncompleteText,
            NANDs = Entry?.NandsText ?? Configuration.IncompleteText,
            Delay = Entry?.DelayText ?? Configuration.IncompleteText,
            Ticks = Entry?.TicksText ?? Configuration.IncompleteText,
            Sum = Entry?.SumText ?? Configuration.IncompleteText,
            Solvers = Level.SolversText,
            TiedForFirst = Level.TiedForFirstText,
            PercentTiedForFirst = Level.PercentTiedForFirst
        };
}

public record Score(int PlayerId, string LevelId, int Nands, int Delay, int Ticks) {
    public int Sum => Nands + Delay + Ticks;
    public Player Player => PlayerMap[PlayerId];
}

public record LeaderboardEntry(bool Scored, bool TicksScored, int Rank, bool Tie, Score Score, double? Sigma) {
    public string DisplayRank => Rank + (Tie ? "*" : string.Empty);
    public string SigmaText => Sigma?.ToString("f2") ?? Configuration.NotApplicableText;
    public string Pseudopercentile => Sigma.HasValue ? $"{100.0 * CumulativeDistributionOfStandardNormalDistribution(Sigma.Value):f2}%" : Configuration.NotApplicableText;

    public string NandsText => Scored ? Score.Nands.ToString("n0") : Configuration.NotApplicableText;
    public string DelayText => Scored ? Score.Delay.ToString("n0") : Configuration.NotApplicableText;
    public string TicksText => TicksScored ? Score.Ticks.ToString("n0") : Configuration.NotApplicableText;
    public string SumText => Scored ? Score.Sum.ToString("n0") : Configuration.NotApplicableText;

    private object ToDump()
        => new {
            Rank = DisplayRank,
            NANDs = NandsText,
            Delay = DelayText,
            Ticks = TicksText,
            Sum = SumText,
            Sigma = SigmaText,
            Pseudopercentile,
            Player = Score.Player.ProfileLink,
            Placements = Score.Player.PlacementsLink
        };
}

public record Level {
    public string LevelId { get; init; }
    public IReadOnlyList<LeaderboardEntry> Entries { get; init; }

    public bool Scored { get; init; }
    public bool TicksScored { get; init; }
    public int? TiedForFirst { get; init; }
    public int? Minimum { get; init; }
    public decimal? Median { get; init; }
    public int? Maximum { get; init; }
    public double? Mean { get; init; }
    public double? StandardDeviation { get; init; }

    public int Solvers => Entries.Count;

    private IReadOnlyList<LeaderboardEntry> _Leaderboard;
    public IReadOnlyList<LeaderboardEntry> Leaderboard => _Leaderboard ??= Entries.Take(Configuration.LeaderboardPositionsShown).ToList();

    private Chart _Histogram;
    internal Chart Histogram => _Histogram ??= GenerateHistogram();

    public string SolversText => Solvers.ToString("n0");
    public string TiedForFirstText => TiedForFirst?.ToString("n0") ?? Configuration.NotApplicableText;
    public string PercentTiedForFirst => TiedForFirst.HasValue ? $"{100m * TiedForFirst.Value / Solvers:f2}%" : Configuration.NotApplicableText;
    public string MinimumText => Minimum?.ToString("n0") ?? Configuration.NotApplicableText;
    public string MedianText => Median?.ToString("n1") ?? Configuration.NotApplicableText;
    public string MaximumText => Maximum?.ToString("n0") ?? Configuration.NotApplicableText;
    public string MeanText => Mean?.ToString("n" + (3 - (int)Math.Max(0, Math.Log10(Mean.Value)))) ?? Configuration.NotApplicableText;
    public string StandardDeviationText => StandardDeviation?.ToString("n" + (3 - (int)Math.Max(0, Math.Log10(Mean.Value)))) ?? Configuration.NotApplicableText;

    private Level(string levelId, IReadOnlyList<LeaderboardEntry> entries) {
        LevelId = levelId;
        Entries = entries;
    }

    public Hyperlinq GetLeaderboardLink() {
        if (!Scored)
            return new Hyperlinq(() => { }, "unscored");
        if (TiedForFirst > Configuration.LeaderboardPositionsShown)
            return new Hyperlinq(() => { }, "large tie");
        return new Hyperlinq(() => DisplayLeaderboard(), "Leaderboard");
    }

    public Hyperlinq GetHistogramLink() {
        if (!Scored)
            return new Hyperlinq(() => { }, "unscored");
        return new Hyperlinq(() => DisplayHistogram(), "Histogram");
    }

    public void DisplayLeaderboard() => Leaderboard.Dump($"{LevelId} Leaderboard", toDataGrid: true);

    public void DisplayHistogram() {
        Histogram.Dump($"{LevelId} Histogram");

        if (Configuration.DumpedImageDimensions.HasValue)
            Histogram.ToBitmap(Configuration.DumpedImageDimensions.Value.Width, Configuration.DumpedImageDimensions.Value.Height).Dump($"{LevelId} Histogram Image");
    }

    private Chart GenerateHistogram() {
        void HandleHistogramClick(int scoresPerBucket, object sender, EventArgs e) {
            if (!(sender is Chart windowsChart))
                return;
            if (!(e is MouseEventArgs typedEventArgs))
                return;
            HitTestResult hit = windowsChart.HitTest(typedEventArgs.X, typedEventArgs.Y);
            if (hit.PointIndex < 0)
                return;
            if (Minimum + scoresPerBucket * hit.PointIndex <= Leaderboard[^1].Score.Sum)
                DisplayLeaderboard();
        }

        if (!Scored)
            return null;

        int maximumShown = Configuration.GetMaximumScoreShownOnHistogram(Solvers, Minimum.Value, Median.Value, Maximum.Value);
        int distinctScoresShown = maximumShown - Minimum.Value + 1;
        int scoresPerBucket = (int)Math.Ceiling(((decimal)distinctScoresShown) / Configuration.MaximumHistogramBuckets);
        int bucketCount = (int)Math.Ceiling((decimal)distinctScoresShown / scoresPerBucket);
        int finalBucketStart = Minimum.Value + (bucketCount - 1) * scoresPerBucket;
        Range finalBucket = finalBucketStart..Maximum.Value;

        Chart chart = new Chart();
        chart.Size = Configuration.HistogramSnapshotDimensions;
        chart.Click += (sender, e) => HandleHistogramClick(scoresPerBucket, sender, e);
        ChartArea area = chart.ChartAreas.Add("Histogram");
        area.Position = new ElementPosition(0f, 0f, 100f, 100f);
        area.AxisX.Interval = 1;
        area.AxisX.MajorGrid.LineColor = Color.FromArgb(63, Color.Black);
        area.AxisY.MajorGrid.LineColor = Color.FromArgb(63, Color.Black);
        if (Configuration.LogarithmicHistograms) {
            area.AxisY.IsLogarithmic = true;
            area.AxisY.LogarithmBase = 2.0;
            area.AxisY.Minimum = 0.5;
        }

        Series series = chart.Series.Add("Frequency");
        series.ToolTip = "#VAL scored #VALX";

        IEnumerable<DataPoint> points = Enumerable.Range(0, bucketCount - 1)
                                                  .Select(i => Minimum.Value + i * scoresPerBucket)
                                                  .Select(i => i..(i + scoresPerBucket - 1))
                                                  .Append(finalBucket)
                                                  .GroupJoin(ExtractCensoredScores(Entries),
                                                             r => r.Start,
                                                             score => (int)Math.Min(finalBucketStart,
                                                                                    Minimum.Value + scoresPerBucket * ((score - Minimum.Value) / scoresPerBucket)),
                                                             (r, iei) => (Range: r,
                                                                          RangeLength: r.End.Value - r.Start.Value + 1,
                                                                          Count: (int?)iei.Count(),
                                                                          ScoreCounts: iei.GroupBy(i => i)
                                                                                          .ToDictionary(igii => igii.Key, igii => igii.Count())))
                                                  .Select(t => (t.Range,
                                                                Count: Configuration.ChartScoresLogarithmically && t.Count == 0 ? null : t.Count,
                                                                Big: t.RangeLength > scoresPerBucket,
                                                                ToolTip: GetHistogramColumnToolTip(t.RangeLength, t.Range.Start.Value, t.ScoreCounts)))
                                                  .Select((t, i) => new DataPoint(0, t.Count ?? 0.0) {
                                                      AxisLabel = t.Range.ToTerseString(),
                                                      BackGradientStyle = GradientStyle.LeftRight,
                                                      Color = GetHistogramColumnColor(t.Big, t.Range.Start.Value),
                                                      BackSecondaryColor = GetHistogramColumnColor(t.Big, t.Range.End.Value),
                                                      IsEmpty = !t.Count.HasValue,
                                                      ToolTip = t.ToolTip
                                                  });
        foreach (DataPoint point in points)
            series.Points.Add(point);
        return chart;
    }

    private string GetHistogramColumnToolTip(int rangeLength, int rangeState, Dictionary<int, int> scoreCounts) {
        IEnumerable<int> scoresToShow = rangeLength < Configuration.HistogramTooltipLinesBeforeExcludingZeroCounts
                                            ? Enumerable.Range(rangeState, rangeLength)
                                            : scoreCounts.Keys.OrderBy(i => i);
        return scoresToShow.Aggregate(new StringBuilder(),
                                      (sb, i) => sb.AppendFormat("{0} scored {1}", scoreCounts.GetValueOrDefault(i), i)
                                                   .AppendLine(),
                                      sb => sb.ToString());
    }

    private Color GetHistogramColumnColor(bool bigBucket, int score)
        => score <= Median.Value
               ? score <= Leaderboard[^1].Score.Sum
                   ? Color.MediumSeaGreen
                   : Color.Teal
               : bigBucket
                   ? Color.Purple
                   : Color.CadetBlue;

    private object ToDump()
        => new {
            LevelId,
            Scored,
            TicksScored,
            Solvers = SolversText,
            TiedForFirst = TiedForFirstText,
            PercentTiedForFirst,
            Best = MinimumText,
            Median = MedianText,
            Worst = MaximumText,
            Mean = MeanText,
            StandardDeviation = StandardDeviationText,
            Leaderboard = GetLeaderboardLink(),
            Histogram = GetHistogramLink()
        };

    public static IReadOnlyList<Level> ConstructLevels() {
        List<LeaderboardEntry> AddRankGroupEntries(bool scored, bool ticksScored, Func<int, double?> getSigmaScore, List<LeaderboardEntry> entries, IReadOnlyList<Score> rankGroup) {
            int rank = entries.Count + 1;
            entries.AddRange(rankGroup.Select(s => new LeaderboardEntry(scored, ticksScored, rank, rankGroup.Count > 1, s, getSigmaScore(s.Sum))));
            return entries;
        }

        return Scores.GroupBy(s => s.LevelId,
                              (levelId, levelGroup) => (LevelId: levelId, Summary: new ScoreSummary(levelGroup)))
                     .Select(t => Level.Construct(t.LevelId,
                                                  t.Summary,
                                                  t.Summary.Scores.GroupBy(s => s.Sum)
                                                                  .OrderBy(rankGroup => rankGroup.Key)
                                                                  .Select(rankGroup => rankGroup.ToList())
                                                                  .Aggregate(new List<LeaderboardEntry>(), (list, rankGroup) => AddRankGroupEntries(t.Summary.Scored, t.Summary.TicksScored, t.Summary.GetSigmaScore, list, rankGroup))))
                     .OrderByDescending(l => l.Solvers)
                     .ToArray();
    }

    private static Level Construct(string levelId, ScoreSummary summary, IReadOnlyList<LeaderboardEntry> entries) {
        if (!summary.Scored)
            return new Level(levelId, entries);

        IReadOnlyList<int> censoredScores = ExtractCensoredScores(entries);

        int minimum = censoredScores.Min();
        decimal median = censoredScores.Count % 2 == 1
                                      ? censoredScores[censoredScores.Count / 2]
                                      : 0.5m * (censoredScores[censoredScores.Count / 2] + censoredScores[censoredScores.Count / 2 - 1]);
        int maximum = censoredScores.Max();

        int tiedForFirst = entries.Count(le => le.Score.Sum == minimum);

        return new Level(levelId, entries) {
            Scored = true,
            TicksScored = summary.TicksScored,
            TiedForFirst = tiedForFirst,
            Minimum = minimum,
            Median = median,
            Maximum = maximum,
            Mean = summary.Mean,
            StandardDeviation = summary.StandardDeviation
        };
    }

    private static IReadOnlyList<int> ExtractCensoredScores(IReadOnlyList<LeaderboardEntry> entries)
        => entries.Select(le => le.Score.Sum)
                  .TakeWhile(s => !Configuration.ScoreCensoringThreshold.HasValue || s < Configuration.ScoreCensoringThreshold)
                  .ToList();

    private sealed class ScoreSummary {
        public IReadOnlyList<Score> Scores { get; init; }
        public bool Scored { get; init; }
        public bool TicksScored { get; init; }
        public double? Mean { get; init; }
        public double? StandardDeviation { get; init; }

        public ScoreSummary(IEnumerable<Score> scores) {
            Scores = scores.ToList();
            Scored = !Scores.All(s => s.Sum == 0);
            IReadOnlyList<int> censoredScores = Scores.Select(s => s.Sum)
                                                      .Where(s => !Configuration.ScoreCensoringThreshold.HasValue || s < Configuration.ScoreCensoringThreshold)
                                                      .ToList();
            if (Scored) {
                TicksScored = !Scores.All(s => s.Ticks == 0);
                Mean = censoredScores.Average();
                StandardDeviation = Math.Sqrt(censoredScores.Sum(s => (s - Mean.Value) * (s - Mean.Value)) / censoredScores.Count);
            }
        }

        public double? GetSigmaScore(int rawScore) => (Mean - rawScore) / StandardDeviation;
    }
}

public static double? CumulativeDistributionOfStandardNormalDistribution(double zScore) {
    [DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "erf")]
    static extern double Erf(double x);

    double ApproximateErf(double x) {
        // Abramowitz & Stegun Handbook of Mathematical Functions 7.1.26
        double a = Math.Abs(x);
        double t = 1.0 / Math.FusedMultiplyAdd(0.3275911, a, 1.0);
        double y = 1.0 - t * Math.Exp(-a * a) * Math.FusedMultiplyAdd(Math.FusedMultiplyAdd(Math.FusedMultiplyAdd(Math.FusedMultiplyAdd(1.061405429, t, -1.453152027), t, 1.421413741), t, -0.284496736), t, 0.254829592);
        return Math.CopySign(y, x);
    }

    double x = zScore / Math.Sqrt(2.0);
    double erf;
    try {
        erf = Erf(x);
    }
    catch {
        // less accurate fallback if P/Invoke fails
        erf = ApproximateErf(x);
    }
    return 0.5 + 0.5 * erf;
}

public static class Extensions {
    public static string ToTerseString(this Range self) => self.Start.Equals(self.End) ? self.Start.ToString() : self.ToString();
}
