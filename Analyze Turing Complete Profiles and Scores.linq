<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

// This is can be run in LINQPad ( http://www.linqpad.net/ ) in C# Program mode.

public static class Configuration {
    public const int LeaderboardPositionsShown = 100;
    public static readonly IReadOnlyList<int> ProfileIds = new[] { 11, 6 };
}

public const string ScoresKey = "TuringCompleteGameScores";
public const string PlayersKey = "TuringCompleteGamePlayers";
public static HttpClient Client { get; } = new HttpClient();

async Task Main() {
    IReadOnlyList<Player> players = await GetPlayersAsync().ConfigureAwait(false);
    IReadOnlyList<Score> scores = await GetScoresAsync().ConfigureAwait(false);
    IReadOnlyDictionary<int, Player> playerMap = players.ToDictionary(p => p.Id);
    IReadOnlyList<Leaderboard> leaderboards = ConstructLeaderboards(scores);

    foreach (int profileId in Configuration.ProfileIds)
        ShowPlacements(leaderboards, playerMap[profileId]);
    players.Dump("Players", toDataGrid: true);
    //scores.Dump("Scores", toDataGrid: true);
    ShowLevels(leaderboards);
    foreach (Leaderboard leaderboard in leaderboards)
        leaderboard.Display(playerMap);
}

public static IReadOnlyList<Leaderboard> ConstructLeaderboards(IReadOnlyList<Score> scores)
    => scores.GroupBy(s => s.LevelId,
                      (levelId, levelGroup) => new Leaderboard(levelId,
                                                               levelGroup.GroupBy(s => s.Sum)
                                                                         .OrderBy(rankGroup => rankGroup.Key)
                                                                         .Select(rankGroup => rankGroup.ToList())
                                                                         .Aggregate(new List<LeaderboardEntry>(),
                                                                                    (list, rankGroup) => AddRankGroupEntries(list, rankGroup))))
             .ToArray();

public static void ShowPlacements(IReadOnlyList<Leaderboard> leaderboards, Player player) {
    leaderboards.Select(leaderboard => (leaderboard.LevelId,
                                        leaderboard.Unscored,
                                        Solvers: leaderboard.Entries.Count,
                                        leaderboard.TiedForFirst,
                                        Entry: leaderboard.Entries.SingleOrDefault(le => le.Score.PlayerId == player.Id)))
                .Select(t => new {
                    Level = t.LevelId,
                    Rank = t.Entry == null
                               ? "incomplete"
                               : t.Unscored
                                   ? "unscored"
                                   : t.Entry.DisplayRank,
                    t.Solvers,
                    t.TiedForFirst
                })
                .Dump($"Placements for {player.Name}", toDataGrid: true);
}

public static void ShowLevels(IReadOnlyList<Leaderboard> leaderboards)
    => leaderboards.Select(l => new { l.LevelId, l.Unscored, Solvers = l.Entries.Count, l.TiedForFirst, PercentTiedForFirst = (100m * l.TiedForFirst / l.Entries.Count).ToString("f2") })
                   .OrderByDescending(o => o.Solvers)
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
}

public record Score(int PlayerId, string LevelId, int Nands, int Delay, int Ticks) {
    public int Sum => Nands + Delay + Ticks;
}

public record LeaderboardEntry(int Rank, bool Tie, Score Score) {
    public string DisplayRank => Rank + (Tie ? "*" : string.Empty);

    public object ForDump(IReadOnlyDictionary<int, Player> playerMap)
        => new {
            Rank = DisplayRank,
            NAND = Score.Nands,
            Score.Delay,
            Score.Ticks,
            Score.Sum,
            Player = playerMap[Score.PlayerId].ProfileLink
        };
}

public record Leaderboard(string LevelId, IReadOnlyList<LeaderboardEntry> Entries) {
    private bool? _Unscored;
    public bool Unscored => _Unscored ?? (_Unscored = Entries.All(le => le.Score.Sum == 0)).Value;
    private int? _TiedForFirst;
    public int TiedForFirst => _TiedForFirst ?? (_TiedForFirst = Entries.Count(le => le.Rank == 1)).Value;

    public void Display(IReadOnlyDictionary<int, Player> playerMap) {
        if (Unscored) {
            "unscored".Dump(LevelId);
            return;
        }

        if (TiedForFirst > Configuration.LeaderboardPositionsShown) {
            $"{TiedForFirst} players tied for first with Sum {Entries.First().Score.Sum}".Dump(LevelId);
            return;
        }

        Entries.Take(Configuration.LeaderboardPositionsShown)
               .Select(le => le.ForDump(playerMap))
               .Dump(LevelId, toDataGrid: true);
    }
}
