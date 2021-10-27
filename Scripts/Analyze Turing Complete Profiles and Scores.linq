<Query Kind="Program">
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
    /// <summary>If set, scores above this threshold are removed from most statistics. null disables the censoring threshold entirely.</summary>
    /// <remarks>99,999 is currently the score for using an unscored component. It skews the statistics pretty badly to leave those in the calculations.</remarks>
    public static readonly int? ScoreCensoringThreshold = 99999;
    /// <summary>If set, this produces and dumps a static image of the chart suitable for sharing.</summary>
    public static readonly (int Width, int Height)? ChartImageDimensions = null; // e.g. (2000,1000)
    public const string NotApplicableText = "-";
    public const string IncompleteText = "i";
}

public const string ScoresKey = "TuringCompleteGameScores";
public const string PlayersKey = "TuringCompleteGamePlayers";
public static HttpClient Client { get; } = new HttpClient();

public static IReadOnlyList<Player> Players { get; } = GetPlayersAsync().ConfigureAwait(false).GetAwaiter().GetResult();
public static IReadOnlyList<Score> Scores { get; } = GetScoresAsync().ConfigureAwait(false).GetAwaiter().GetResult();
private static IReadOnlyDictionary<int, Player> _PlayerMap;
public static IReadOnlyDictionary<int, Player> PlayerMap => _PlayerMap ?? (_PlayerMap = Players.ToDictionary(p => p.Id));
private static IReadOnlyList<Level> _Levels;
public static IReadOnlyList<Level> Levels => _Levels ?? (_Levels = Level.ConstructLevels());

public void Main() {
    Levels.Dump("Levels", toDataGrid: true);
    Players.Dump("Players", toDataGrid: true);

    IReadOnlyList<Player> selectedPlayers = Configuration.ProfileIds
                                                         .Select(playerId => PlayerMap[playerId])
                                                         .ToArray();
    foreach (Player player in selectedPlayers)
        player.ShowPlacements();
    ChartPlayers(selectedPlayers);
}

public static void ChartPlayers(IReadOnlyList<Player> includedPlayers) {
    int? GetPlayerScore(Level leaderboard, int playerId) {
        int? scoreSum = leaderboard.Entries.SingleOrDefault(le => le.Score.PlayerId == playerId)?.Score.Sum;
        if (Configuration.ChartScoresLogarithmically && scoreSum.HasValue)
            return Math.Max(1, scoreSum.Value);
        return scoreSum;
    }

    void HandleChartClick(object sender, EventArgs e) {
        if (!(sender is Chart windowsChart))
            return;
        if (!(e is MouseEventArgs typedEventArgs))
            return;
        HitTestResult hit = windowsChart.HitTest(typedEventArgs.X, typedEventArgs.Y);
        if (hit.PointIndex >= 0 && Levels[hit.PointIndex].Scored)
            Levels[hit.PointIndex].DisplayLeaderboard();
    }

    LINQPadChart<Level> chart = Levels.Where(l => !Configuration.ChartOnlyScoredLevels || l.Scored)
                                      .Chart(l => l.LevelId)
                                      .AddYSeries(l => Configuration.ChartScoresLogarithmically
                                                           ? l.Minimum > 0
                                                               ? l.Minimum
                                                               : null
                                                           : l.Minimum,
                                                  Util.SeriesType.Line,
                                                  name: "Best")
                                      .AddYSeries(l => Configuration.ChartScoresLogarithmically
                                                           ? l.Median > 0
                                                               ? l.Median
                                                               : null
                                                           : l.Median,
                                                  Util.SeriesType.Line,
                                                  name: "Median")
                                      .AddYSeries(l => l.Solvers, Util.SeriesType.Area, name: "Solvers", useSecondaryYAxis: true);
    foreach (Player player in includedPlayers)
        chart.AddYSeries(l => GetPlayerScore(l, player.Id), Util.SeriesType.Point, name: player.Name);

    Chart windowsChart = chart.ToWindowsChart();
    windowsChart.Click += HandleChartClick;
    windowsChart.Series["Best"].ToolTip = "#VAL is the best score achieved for #VALX";
    windowsChart.Series["Median"].ToolTip = "#VAL{n1} is the median score achieved for #VALX";
    windowsChart.Series["Solvers"].Color = System.Drawing.Color.FromArgb(63, System.Drawing.Color.Gray);
    windowsChart.Series["Solvers"].ToolTip = "#VAL players have solved #VALX";
    foreach (Player player in includedPlayers) {
        Series series = windowsChart.Series[player.Name];
        series.MarkerSize = 9;
        series.ToolTip = player.Name + " did #VALX in #VAL";
    }
    ChartArea chartArea = windowsChart.ChartAreas.Single();
    chartArea.AxisY.IsStartedFromZero = true;
    chartArea.AxisY.Title = (Configuration.ChartScoresLogarithmically ? "Binary Logarithm of " : string.Empty) + "Sum of Scores";
    if (Configuration.ChartScoresLogarithmically) {
        chartArea.AxisY.IsLogarithmic = true;
        chartArea.AxisY.LogarithmBase = 2.0;
    }
    chartArea.AxisY2.Title = "Solvers";
    chartArea.AxisY2.IsStartedFromZero = true;
    chartArea.AxisY2.MajorGrid.Enabled = false;

    windowsChart.Dump();
    if (Configuration.ChartImageDimensions.HasValue)
        windowsChart.ToBitmap(Configuration.ChartImageDimensions.Value.Width, Configuration.ChartImageDimensions.Value.Height).Dump("Chart Image");
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
    $"{players.Count} players{(fromCache ? " from cache" : string.Empty)}".Dump();
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
    $"{scores.Count} scores{(fromCache ? " from cache" : string.Empty)}".Dump();
    return scores;
}

public record Player(int Id, string Name) {
    private IReadOnlyList<object> _Placements;
    public IReadOnlyList<object> Placements
        => _Placements ?? (_Placements = Levels.Select(level => new PlacementEntry(level, level.Entries.SingleOrDefault(le => le.Score.PlayerId == Id))).ToList());
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
    public IReadOnlyList<LeaderboardEntry> Leaderboard => _Leaderboard ?? (_Leaderboard = Entries.Take(Configuration.LeaderboardPositionsShown).ToList());

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

    public void DisplayLeaderboard() => Leaderboard.Dump(LevelId, toDataGrid: true);

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
            Leaderboard = GetLeaderboardLink()
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

        IReadOnlyList<int> censoredScores = entries.Select(le => le.Score.Sum)
                                                   .TakeWhile(s => !Configuration.ScoreCensoringThreshold.HasValue || s < Configuration.ScoreCensoringThreshold)
                                                   .ToList();

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
