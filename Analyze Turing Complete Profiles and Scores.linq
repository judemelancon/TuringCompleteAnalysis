<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Windows.Forms.DataVisualization.Charting</Namespace>
</Query>

// This is can be run in LINQPad ( http://www.linqpad.net/ ) in C# Program mode.

public static class Configuration {
    public const int LeaderboardPositionsShown = 100;
    public static readonly IReadOnlyList<int> ProfileIds = new[] { 11, 6 };
    public const bool ChartOnlyScoredLevels = false;
    public const bool ChartScoresLogarithmically = true;
    public static readonly (int Width, int Height)? ChartImageDimensions = null; // e.g. (2000,1000)
}

public const string ScoresKey = "TuringCompleteGameScores";
public const string PlayersKey = "TuringCompleteGamePlayers";
public static HttpClient Client { get; } = new HttpClient();

async Task Main() {
    IReadOnlyList<Player> players = await GetPlayersAsync().ConfigureAwait(false);
    IReadOnlyList<Score> scores = await GetScoresAsync().ConfigureAwait(false);
    //scores.Dump("Scores", toDataGrid: true);
    IReadOnlyDictionary<int, Player> playerMap = players.ToDictionary(p => p.Id);
    IReadOnlyList<Leaderboard> leaderboards = ConstructLeaderboards(scores);

    ShowLevels(playerMap, leaderboards);
    players.Select(p => p.ForDump(leaderboards))
           .Dump("Players", toDataGrid: true);

    IReadOnlyList<Player> selectedPlayers = Configuration.ProfileIds
                                                         .Select(playerId => playerMap[playerId])
                                                         .ToArray();
    foreach (Player player in selectedPlayers)
        ShowPlacements(leaderboards, player);
    ChartPlayers(leaderboards, selectedPlayers);
}

public static IReadOnlyList<Leaderboard> ConstructLeaderboards(IReadOnlyList<Score> scores)
    => scores.GroupBy(s => s.LevelId,
                      (levelId, levelGroup) => new Leaderboard(levelId,
                                                               levelGroup.GroupBy(s => s.Sum)
                                                                         .OrderBy(rankGroup => rankGroup.Key)
                                                                         .Select(rankGroup => rankGroup.ToList())
                                                                         .Aggregate(new List<LeaderboardEntry>(),
                                                                                    (list, rankGroup) => AddRankGroupEntries(list, rankGroup))))
             .OrderByDescending(o => o.Solvers)
             .ToArray();

public static void ShowPlacements(IReadOnlyList<Leaderboard> leaderboards, Player player) {
    leaderboards.Select(leaderboard => (leaderboard.LevelId,
                                        leaderboard.Scored,
                                        leaderboard.Solvers,
                                        leaderboard.TiedForFirst,
                                        Entry: leaderboard.Entries.SingleOrDefault(le => le.Score.PlayerId == player.Id)))
                .Select(t => new {
                    Level = t.LevelId,
                    Rank = t.Entry == null
                               ? "incomplete"
                               : t.Scored
                                   ? t.Entry.DisplayRank
                                   : "unscored",
                    t.Solvers,
                    t.TiedForFirst
                })
                .Dump($"Placements for {player.Name}", toDataGrid: true);
}

public static void ChartPlayers(IReadOnlyList<Leaderboard> leaderboards, IReadOnlyList<Player> players) {
    int? GetPlayerScore(Leaderboard leaderboard, int playerId) {
        int? scoreSum = leaderboard.Entries.SingleOrDefault(le => le.Score.PlayerId == playerId)?.Score.Sum;
        if (Configuration.ChartScoresLogarithmically && scoreSum.HasValue)
            return Math.Max(1, scoreSum.Value);
        return scoreSum;
    }

    LINQPadChart<Leaderboard> chart = leaderboards.Where(l => !Configuration.ChartOnlyScoredLevels || l.Scored)
                                                  .Chart(l => l.LevelId)
                                                  .AddYSeries(l => Configuration.ChartScoresLogarithmically ? Math.Max(1, l.Minimum) : l.Minimum, Util.SeriesType.Line, name: "Best")
                                                  .AddYSeries(l => Configuration.ChartScoresLogarithmically ? Math.Max(1, l.Median) : l.Median, Util.SeriesType.Line, name: "Median")
                                                  .AddYSeries(l => l.Solvers, Util.SeriesType.Area, name: "Solvers", useSecondaryYAxis: true);
    foreach (Player player in players)
        chart.AddYSeries(l => GetPlayerScore(l, player.Id), Util.SeriesType.Point, name: player.Name);

    Chart windowsChart = chart.ToWindowsChart();
    windowsChart.Series["Solvers"].Color = System.Drawing.Color.FromArgb(63, System.Drawing.Color.LightGray);
    foreach (Series series in windowsChart.Series.Skip(3))
        series.MarkerSize = 9;
    ChartArea chartArea = windowsChart.ChartAreas.Single();
    chartArea.AxisY.IsStartedFromZero = true;
    chartArea.AxisY.Title = (Configuration.ChartScoresLogarithmically ? "Binary Logarithm of " : string.Empty) + "Sum of Scores";
    if (Configuration.ChartScoresLogarithmically) {
        chartArea.AxisY.IsLogarithmic = true;
        chartArea.AxisY.LogarithmBase = 2.0;
    }
    chartArea.AxisY2.IsStartedFromZero = true;
    chartArea.AxisY2.MajorGrid.Enabled = false;

    windowsChart.Dump();
    if (Configuration.ChartImageDimensions.HasValue)
        windowsChart.ToBitmap(Configuration.ChartImageDimensions.Value.Width, Configuration.ChartImageDimensions.Value.Height).Dump("Chart Image");
}

public static void ShowLevels(IReadOnlyDictionary<int, Player> playerMap, IReadOnlyList<Leaderboard> leaderboards)
    => leaderboards
                   .Select(l => new {
                       l.LevelId,
                       l.Scored,
                       l.Solvers,
                       l.TiedForFirst,
                       PercentTiedForFirst = $"{(100m * l.TiedForFirst / l.Solvers):f2}%",
                       Minimum = l.Minimum.ToString("n0"),
                       Median = l.Median.ToString("n1"),
                       Maximum = l.Maximum.ToString("n0"),
                       LeaderboardLink = l.GetLeaderboardLink(playerMap)
                   })
                   .Dump("Levels", toDataGrid: true);

private static List<LeaderboardEntry> AddRankGroupEntries(List<LeaderboardEntry> list,
                                                          IReadOnlyList<Score> rankGroup) {
    int rank = list.Count + 1;
    list.AddRange(rankGroup.Select(s => new LeaderboardEntry(rank, rankGroup.Count > 1, s)));
    return list;
}

public static async Task<IReadOnlyList<Player>> GetPlayersAsync() {
    string rawPlayerData = await Util.Cache(async () => await Client.GetStringAsync("https://turingcomplete.game/api_usernames"),
                                                                                    PlayersKey,
                                                                                    out bool fromCache);
    //rawPlayerData.Dump(nameof(rawPlayerData));
    IReadOnlyList<Player> players = rawPlayerData.Trim()
                                                 .Split('\n')
                                                 .Select(record => record.Split(',', 2))
                                                 .Select(fields => new Player(int.Parse(fields[0]),
                                                                              fields[1]))
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
    public Hyperlinq ProfileLink => new Hyperlinq($"https://turingcomplete.game/profile/{Id}",
                                                  Name == null
                                                    ? "!![null]"
                                                    : Name.Length == 0
                                                        ? "!![empty]"
                                                        : string.IsNullOrWhiteSpace(Name)
                                                            ? "!![whitespace]"
                                                            : Name);

    public object ForDump(IReadOnlyList<Leaderboard> leaderboards)
        => new {
            Id,
            Name,
            ProfileLink,
            PlacementsLink = new Hyperlinq(() => ShowPlacements(leaderboards, this),
                                           "Placements")
        };
}

public record Score(int PlayerId, string LevelId, int Nands, int Delay, int Ticks) {
    public int Sum => Nands + Delay + Ticks;
}

public record LeaderboardEntry(int Rank, bool Tie, Score Score) {
    public string DisplayRank => Rank + (Tie ? "*" : string.Empty);

    public LeaderboardEntryDisplay ForDump(IReadOnlyDictionary<int, Player> playerMap)
        => new LeaderboardEntryDisplay(DisplayRank, Score.Nands, Score.Delay, Score.Ticks, Score.Sum, playerMap[Score.PlayerId].ProfileLink);
}

public record LeaderboardEntryDisplay(string Rank, object NAND, object Delay, object Ticks, object Sum, Hyperlinq Player) { }

public record Leaderboard(string LevelId, IReadOnlyList<LeaderboardEntry> Entries) {
    public int Solvers => Entries.Count;
    public bool Scored => !(TiedForFirst == Entries.Count && Entries[0].Score.Sum == 0);
    private int? _TiedForFirst;
    public int TiedForFirst => _TiedForFirst ?? (_TiedForFirst = Entries.Count(le => le.Rank == 1)).Value;
    private int? _Minimum;
    public int Minimum => _Minimum ?? (_Minimum = Entries.Min(le => le.Score.Sum)).Value;
    private decimal? _Median;
    public decimal Median => _Median ?? (_Median = Entries.Count % 2 == 1 ? Entries[Entries.Count / 2].Score.Sum : 0.5m * (Entries[Entries.Count / 2].Score.Sum + Entries[Entries.Count / 2 - 1].Score.Sum)).Value;
    private int? _Maximum;
    public int Maximum => _Maximum ?? (_Maximum = Entries.Max(le => le.Score.Sum)).Value;

    public Hyperlinq GetLeaderboardLink(IReadOnlyDictionary<int, Player> playerMap) {
        if (!Scored)
            return new Hyperlinq(() => { }, "unscored");
        if (TiedForFirst > Configuration.LeaderboardPositionsShown)
            return new Hyperlinq(() => { }, "large tie");
        return new Hyperlinq(() => Display(playerMap), "Leaderboard");
    }

    public void Display(IReadOnlyDictionary<int, Player> playerMap)
        => Entries.Take(Configuration.LeaderboardPositionsShown)
                  .Select(le => le.ForDump(playerMap))
                  .Dump(LevelId, toDataGrid: true);
}
