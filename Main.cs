// Kinda like the Solitaire game inside Shenzhen I/O.
// Created for educational / strictly non-commercial purposes.
//
// Uses normal playing cards to represent the Mahjong style cards from the Shenzhen I/O game.
// Ace to Nine for Hearts, Clubs and Diamonds are like the three suits of number cards.
// Jacks, Queens and Kinds of all four suits represent the three dragons.
// The Ten of Spades represents the Flower card.

// Object: To put all the cards to the rank tableau, collect the flower and slay the dragons.
// Rules:
// * you can pick up any card from the free cells, or the bottom of any column or the top of any rank tableau.
// * you can pick up multiple cards from a column if they are a chain of decreasing ranks with changing suits.
// * you can place your card(s):
//   * at the end of a column if it makes a chain of decreasing ranks with changing suits or,
//   * in an empty column.
// * you can store individual cards in the three free cells.
// * you can add to rank tableau in ascending order with like suit.
// * you can add place the flower in the tableau.
// * you can slay dragons (Jacks, Queens, Kings) if you:
//   * can reach all of them (all suits at the bottom of a column or in a free cell).
//   * would have a free cell space for the dragon's bones.

using System.Text.RegularExpressions;

const string ranks = "A23456789", dragons = "JQK", suits = "shcd";

// 3 free cell, tableau for flower and numbers, then 8 columns
List<string> freeCells = new(3);
List<string>[] tableau = [.. Enumerable.Range(0, 4).Select(i => new List<string>(i > 0 ? 9 : 1))];
List<string>[] columns = Deal(3);

for (;;) {
    if (Display()) break;
    // command is in the form "card, destination" or "quit"
    // destinations:
    // f    free cells
    // t    tableau (it will figure it which one)
    // 1-8  column
    string cmd = Console.ReadLine() ?? "";
    if (cmd == "quit") break;
    if (cmd == "") {
        List<string> plays = new();
        for (;;) {
            bool anyPlayed = false;
            foreach (var p in AutoPlays())
                if (Play(p) is null) {
                    plays.Add($"'{p}' played.");
                    anyPlayed = true;
                }

            if (!anyPlayed) break;
        }

        if (plays.Count == 0)
            Console.WriteLine("No auto-plays found.");
        else
            foreach (var p in plays)
                Console.WriteLine(p);
        continue;
    }

    string err = Play(cmd);
    if (err is not null) Console.WriteLine(err);
}

List<string>[] Deal(int seed) {
    List<string> deck = new(3 * 9 + 4 * 3 + 1) { "Ts" }; // ten of spades is the 'flower'

    foreach (var suit in suits) {
        if (suit != 's') // no spades in numbers
            foreach (var rank in ranks) // numbers, three suits of ace to nine
                deck.Add($"{rank}{suit}");
        foreach (var rank in dragons) // dragons (all the 'royals')
            deck.Add($"{rank}{suit}");
    }

    Random rnd = new(seed); // Fisher-Yates shuffle
    for (int i = deck.Count - 1; i > 0; i--) {
        int j = rnd.Next(i + 1);
        (deck[i], deck[j]) = (deck[j], deck[i]);
    }

    return [.. Enumerable.Range(0, 8).Select(i => new List<string>(deck[(i * 5)..(i * 5 + 5)]))];
}

bool Display() {
    // XX XX XX JQK  XX XX XX XX
    //  XX XX XX XX XX XX XX XX
    //  .. .. .. .. .. .. .. ..

    for (int i = 0; i < freeCells.Capacity; i++)
        Console.Write($"{(i < freeCells.Count ? freeCells[i] : "__")} ");
    foreach (var d in dragons)
        Console.Write(freeCells.Contains($"{d}*") ? char.ToLower(d) : d);

    Console.Write(' ');
    foreach (var t in tableau)
        Console.Write($" {(t.Count > 0 ? t[^1] : "__")}");
    Console.WriteLine();

    int lines = columns.Max(c => c.Count);
    for (int l = 0; l < lines; l++) {
        foreach (var c in columns)
            Console.Write($" {(l < c.Count ? c[l] : l == 0 ? "__" : "  ")}");
        Console.WriteLine();
    }

    return lines == 0;
}

int RankOf(string card) => ranks.IndexOf(card[0]) + 1;

IEnumerable<string> AutoPlays() {
    foreach (var d in dragons)
        yield return $"{d}s t";
    yield return "Ts t";
    foreach (var r in ranks)
        foreach(var s in suits[1..])
            yield return $"{r}{s} t"; 
}

string Play(string cmd) {
    if (!ValidCommandRegex().IsMatch(cmd))
        return "Bad command format.";

    string card = cmd[..2];
    var (cards, err) = Pickup(card);
    return err ?? ValidateAndMove(card, cmd[^1], cards);
}

(List<string> cards, string err) Pickup(string card, bool andRemove = false) {
    List<string>[] sources = [freeCells, .. tableau, .. columns];
    List<string> src = sources.FirstOrDefault(l => l.Contains(card));
    if (src is null) return (null, $"Card {card} not found.");
    bool onlyOne = src == freeCells;
    int index = src.IndexOf(card);
    int limit = onlyOne ? index + 1 : src.Count;
    for (int i = index + 1; i < limit; i++)
        if (RankOf(src[i - 1]) - 1 != RankOf(src[i]) || src[i][1] == src[i - 1][1])
            return (null, $"Cannot pick up stack starting from {card}. Blocked at {src[i]} vs {src[i - 1]}.");
    List<string> r = new(src[index..limit]);
    if (andRemove) src.RemoveRange(index, limit - index);
    return (r, null);
}

string ValidateAndMove(string card, char dst, List<string> cards) {
    if (dst is 'f') {
        if (freeCells.Count + cards.Count > freeCells.Capacity) return "Not enough free cells.";

        Pickup(card, true);
        freeCells.AddRange(cards);
    } else if (dst is 't') {
        if (cards.Count == 1 && dragons.Contains(cards[0][0])) {
            int fc = freeCells.Count(c => card[0] == c[0]);
            if (freeCells.Count - fc >= freeCells.Capacity) return "No free cells available for the dragon's bones.";
            var unreachable = suits.Select(s => "" + card[0] + s).Where(dc => Pickup(dc).err is not null).ToList();
            if (unreachable.Any())
                return $"Cannot slay that dragon. Cannot reach {string.Join(", ", unreachable)}.";

            foreach (var suit in suits)
                Pickup("" + cards[0][0] + suit, true);
            freeCells.Insert(0, cards[0][0] + "*");
        } else {
            foreach (var c in cards) {
                int si = suits.IndexOf(c[1]);
                int next = tableau[si].Count > 0 ? RankOf(tableau[si][^1]) + 1 : si == 0 ? 10 : 1;
                if ((ranks + "T").IndexOf(c[0]) + 1 != next) return $"Cannot place {c} on tableau.";
            }

            Pickup(card, true);
            foreach (var c in cards)
                tableau[suits.IndexOf(c[1])].Add(c);
        }
    } else if (dst is >= '1' and <= '8') {
        int di = dst - '1';
        if (columns[di].Count > 0)
            if (RankOf(columns[di][^1]) != RankOf(cards[0]) + 1 || cards[0][1] == columns[di][^1][1])
                return $"Cannot lay that on a {columns[di][^1]}.";

        Pickup(card, true);
        columns[di].AddRange(cards);
    } else
        return "Unknown destination.";

    return null;
}

partial class Program {
    [GeneratedRegex("[ATJQK2-9][shcd] [ft1-8]")]
    private static partial Regex ValidCommandRegex();
}