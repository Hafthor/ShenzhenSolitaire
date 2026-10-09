// Kinda like the Solitaire game inside Shenzhen I/O.
// Created for educational / strictly non-commercial purposes.
//
// Uses normal playing cards to represent the Mahjong style cards from the Shenzhen I/O game.
// Ace to Nine for Hearts, Clubs and Diamonds are like the three suits of number cards.
// Jacks, Queens and Kinds of all four suits represent the three dragons.
// The Ten of Spades represents the Flower card.
// Note that only 40 cards are used.

// Object: To put all the cards to the rank stacks, collect the flower and slay the dragons.
// Rules:
// * you can pick up any card from the free cells, or the bottom of any column or the top of any rank stack.
// * you can pick up multiple cards from a column if they are a chain of decreasing ranks with changing suits.
// * you can place your card(s):
//   * at the end of a column if it makes a chain of decreasing ranks with changing suits or,
//   * in an empty column.
// * you can store individual cards in the three free cells.
// * you can add to rank stack in ascending order with like suit.
// * you can add place the flower in its special stack.
// * you can slay dragons (Jacks, Queens, Kings) if you:
//   * can reach all of them (all suits at the bottom of a column or in a free cell).
//   * would have a free cell space for the dragon's bones.

//          123456789
// Craks..: 🀇🀈🀉🀊🀋🀌🀍🀎🀏  Hearts   A-9
// Bams...: 🀐🀑🀒🀓🀔🀕🀖🀗🀘  Clubs    A-9
// Dots...: 🀙🀚🀛🀜🀝🀞🀟🀠🀡  Diamonds A-9
// Dragons: 🀄🀅🀆         Jacks, Queens, Kings
// Flower.: 🌸           Ten of Spades

using System.Text.RegularExpressions;

const string ranks = "A23456789", dragons = "JQK", suits = "shcd", numberSuits = "hcd";
const int seed = 4;

// three free cell, stacks for flower and numbers, then eight columns
List<string> freeCells = new(3);
List<string>[] stacks = [.. Enumerable.Range(0, 4).Select(i => new List<string>(i > 0 ? 9 : 1))];
List<string>[] columns = Deal(seed);
List<string> cmdHistory = new();

for (;;) {
    if (Display()) {
        Console.WriteLine("You win!");
        Console.WriteLine($"Seed: {seed}, History: {string.Join(",", cmdHistory)}.");
        break;
    }
    // command is in the form "card, destination" or "quit"
    // destinations:
    // f    free cells
    // s    stack (it will figure it which one)
    // 1-8  column
    string cmd = Console.ReadLine() ?? "";
    if (cmd is "quit") break;
    if (cmd is "undo") {
        if (!cmdHistory.Any()) continue;
        Console.WriteLine($"Un-did '{cmdHistory[^1]}'.");
        cmdHistory.RemoveAt(cmdHistory.Count - 1);
        foreach (var stack in stacks.Concat([freeCells])) stack.Clear();
        columns = Deal(seed);
        foreach (var hCmd in cmdHistory) Play(hCmd, false);
    } else {
        string err = Play(cmd);
        if (err is not null) Console.WriteLine(err);
        else cmdHistory.Add(cmd);
    }
}

List<string>[] Deal(int seed) {
    List<string> deck = new(3 * 9 + 4 * 3 + 1) { "Ts" }; // ten of spades is the 'flower'
    foreach (var suit in numberSuits)
        foreach (var rank in ranks) // numbers, three suits of ace to nine
            deck.Add($"{rank}{suit}");
    foreach (var suit in suits)
        foreach (var rank in dragons) // dragons (all the 'royals')
            deck.Add($"{rank}{suit}");

    Shuffle(deck, new(seed));
    return [.. Enumerable.Range(0, 8).Select(i => new List<string>(deck[(i * 5)..(i * 5 + 5)]))];
}

void Shuffle<T>(IList<T> list, Random r) { // Fisher-Yates shuffle
    for (int i = list.Count - 1; i > 0; i--) {
        int j = r.Next(i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}

bool Display() {
    // XX XX XX JQK  XX XX XX XX
    //  XX XX XX XX XX XX XX XX
    //  .. .. .. .. .. .. .. ..

    for (int i = 0; i < freeCells.Capacity; i++)
        Console.Write($"{(i < freeCells.Count ? freeCells[i] : "__")} ");
    foreach (var d in dragons)
        Console.Write(freeCells.Contains($"{d}*") ? d : char.ToLower(d));

    Console.Write(' ');
    foreach (var t in stacks)
        Console.Write($" {(t.Count > 0 ? t[^1] : "__")}");
    Console.WriteLine();

    int lines = columns.Max(c => c.Count);
    for (int l = 0; l < lines; l++) {
        foreach (var c in columns)
            Console.Write($" {(l < c.Count ? c[l] : l is 0 ? "__" : "  ")}");
        Console.WriteLine();
    }

    return lines is 0;
}

string Play(string cmd, bool display = true) {
    if (cmd is "") return AutoPlay(display);
    if (StackDefaultCommandRegex().IsMatch(cmd)) cmd += " s";
    if (!ValidCommandRegex().IsMatch(cmd)) return "Bad command format.";
    string card = cmd[..2];
    var (cards, err) = Pickup(card);
    return err ?? ValidateAndMove(card, cmd[^1], cards);
}

string AutoPlay(bool display) {
    for (bool anyFound = false;;) {
        bool anyPlayed = false;
        foreach (var p in AutoPlays())
            if (Play(p) is null) {
                if (display) Console.WriteLine($"'{p}' played.");
                anyFound = anyPlayed = true;
                break;
            }

        if (!anyPlayed) return anyFound ? null : "No auto-plays found.";
    }
}

IEnumerable<string> AutoPlays() {
    foreach (var d in dragons + 'T')
        yield return $"{d}s s";
    foreach (var r in ranks)
        foreach(var s in numberSuits)
            yield return $"{r}{s} s"; 
}

(List<string> cards, string err) Pickup(string card, bool andRemove = false) {
    List<string>[] sources = [freeCells, .. stacks, .. columns];
    List<string> src = sources.FirstOrDefault(l => l.Contains(card));
    if (src is null) return (null, $"Card {card} not found.");
    int index = src.IndexOf(card), limit = src == freeCells ? index + 1 : src.Count;
    for (int i = index + 1; i < limit; i++)
        if (ranks.IndexOf(src[i - 1][0]) - 1 != ranks.IndexOf(src[i][0]) || src[i][1] == src[i - 1][1])
            return (null, $"Cannot pick up stack starting from {card}. Blocked at {src[i]} vs {src[i - 1]}.");
    var cards = src.Slice(index, limit - index);
    if (andRemove) src.RemoveRange(index, limit - index);
    return (cards, null);
}

string ValidateAndMove(string card, char dest, List<string> cards) {
    if (dest is 'f') {
        if (freeCells.Count + cards.Count > freeCells.Capacity) return "Not enough free cells.";
        Pickup(card, true);
        freeCells.AddRange(cards);
    } else if (dest is 's') {
        if (cards.Count is 1 && dragons.Contains(cards[0][0])) {
            int used = freeCells.Count - freeCells.Count(c => card[0] == c[0]);
            if (used >= freeCells.Capacity) return "No free cells available for the dragon's bones.";
            var unreachable = suits.Select(s => "" + card[0] + s).Where(dc => Pickup(dc).err is not null).ToList();
            if (unreachable.Any()) return $"Cannot slay that dragon. Cannot reach {string.Join(", ", unreachable)}.";
            foreach (var suit in suits)
                Pickup("" + cards[0][0] + suit, true);
            freeCells.Insert(0, cards[0][0] + "*");
        } else {
            foreach (var c in cards) {
                int si = suits.IndexOf(c[1]);
                int next = stacks[si].Count > 0 ? ranks.IndexOf(stacks[si][^1][0]) + 2 : si is 0 ? 10 : 1;
                if ((ranks + "T").IndexOf(c[0]) + 1 != next) return $"Cannot place {c} on stack.";
            }

            Pickup(card, true);
            foreach (var c in cards)
                stacks[suits.IndexOf(c[1])].Add(c);
        }
    } else if (dest is >= '1' and <= '8') {
        var d = columns[dest - '1'];
        if (d.Count > 0 && (ranks.IndexOf(d[^1][0]) != ranks.IndexOf(cards[0][0]) + 1 || cards[0][1] == d[^1][1]))
            return $"Cannot lay a {cards[0]} on a {d[^1]}.";

        Pickup(card, true);
        d.AddRange(cards);
    } else
        return "Unknown destination.";

    return null;
}

partial class Program {
    [GeneratedRegex("^[ATJQK2-9][shcd] [fs1-8]$")]
    private static partial Regex ValidCommandRegex();
    
    [GeneratedRegex("^[ATJQK2-9][shcd]$")]
    private static partial Regex StackDefaultCommandRegex();

}