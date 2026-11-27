# Boss design: hard and fair

All three bosses were hardened in v0.5 to play like Dark Souls bosses: they punish greed and mistakes, and they never hit without a readable telegraph. This page lists what makes each hard, the fairness rules they must keep, and how difficulty is measured. Every rule is a headless check in `tests/Core.Tests/BossDesignChecks.cs`, so nobody can quietly ease them.

## What "Dark Souls hard" means here

| Part | In the game |
|---|---|
| Heavy hits | A direct hit from a boss costs **2 of 5 masks** (3 with the Brass Lung): three mistakes end an attempt. Waves, drips and jets cost one. |
| Delayed swings | An attack sometimes hangs longer than its tell suggests, to punish a dodge made on the first frame of the pose. |
| Combos | Phase two chains attacks with no idle beat. |
| No safe range | Every boss reaches a diver who keeps their distance. |
| No safe hugging | Standing against a boss is punished too. |
| Punished greed | Swinging without dodging never wins; the punish window after an attack is only one or two swings. |
| Earned recovery | Every 7 hook hits on a **boss** mend one mask. Ordinary enemies never heal you. A bold fight can be won, a greedy one cannot. |
| Real phases | Phase two adds new behaviour, not only speed. |

## Random fights: no two saves play alike

Each boss picks its attacks at random, so a fight never shows a fixed pattern. The move sets are small, so the order, the timing and the details change instead:

- A **run seed** is picked when a new game starts and is kept in the save, so every new save plays the bosses in a different order, and a continued save keeps its own. Every attempt at a boss also mixes in the death count, so a retry is not a replay.
- Choices are dealt from **shuffle bags**: the order is random, but a boss never goes a whole fight without, say, a delayed swing, and never hands out the same thing three times running.
- The same seed always replays the same fight, which is what the tests and the QA bot use (`-seed N` on the command line forces one; the autoplay report prints the seed it used).
- Randomness never touches the fairness rules below: telegraph and punish times are fixed, and a rush only comes when the diver is far enough to reach a ledge.

| Boss | What changes from fight to fight |
|---|---|
| Undertoll | Slam or rush (a rush only when the diver is 6+ tiles away, 70% of the time), a delayed slam about one in four slams, rain about one attack in three in phase two, a slam chained out of about half the recoveries in phase two, and a different pause between attacks every time. |
| Pump Warden | How many pulses (2 or 3, then 3 or 4 below half health), which pipe vents next (never the same one twice), which pulse covers two pipes, whether the first jet is tracked, and quick or delayed sweeps in shuffled order. |
| Archivist | Whether the real silhouette lands under the diver or on the decoy spot (shuffled, never three of a kind), which side the decoy stands on and how far (5 to 6.5 tiles), which dives are chained, and how long it hovers. |

## The bosses

**The Undertoll** (26 hits). Slam, rush and (phase two) rain.
- Idle beat cut from 0.7 s to 0.45 s; slam wind-up 0.7 s; rush 12 tiles/s; every slam or rush body hit costs 2 masks.
- About one slam in four is a **delayed slam**: the bell rises, hangs 0.35 s at the top, then drops.
- Phase two: a slam sends a **second wave pair** 0.55 s behind the first (jump twice), a **slam sometimes chains out of a recovery** with no idle beat, and rain comes about every third attack.
- Punish windows: slam 1.05 s, rush 0.95 s (0.75 times that in phase two).

**The Pump Warden** (22 hits). Pipes, then a sweep.
- Two or three pulses in a row (three or four in phase two). Each is marked 0.6 s (0.45 s in phase two) before it vents.
- If the diver keeps back 5.5 tiles or more, the first jet is **tracked** (it vents right under their feet), and sometimes it is up close too.
- Phase two: at least one pulse per attack covers **two neighbouring pipes** at once, leaving one lane quiet.
- The sweep is a **quick brace (0.6 s) or a delayed one (0.95 s)**, in shuffled order. It now starts at the Warden's own centre, so hugging its casing no longer works: jump it (it is low) or cross it. A landed sweep costs 2 masks.
- Punish window 0.85 s (0.7 s in phase two).

**The Archivist of Depths** (20 hits). Two silhouettes, one real.
- Marks last 0.7 s (0.55 s in phase two); only the real silhouette swings its clapper. A dive costs 2 masks.
- Every landing sends a **ripple pair** 0.45 s later (0.3 s in phase two) and, in phase two, an **echo pair** 0.75 s after that.
- Phase two: about one dive in three is a **chain**: 0.4 s recovery, no hover, a 0.5 s mark. Do not be greedy.
- The real mark is under the diver half the time and on the decoy spot half the time, in shuffled order.
- Punish window 0.85 s (0.7 s in phase two).

**The Tidekeeper** (56 health, hinge hits only). It stands in the Sea Gate and turns to face the diver, then locks its facing for the whole attack.
- Four attacks in shuffled order, never the same one three times running: a **hammer** (a low sweep ahead of it, 2 masks), a **floor wave** (a crest each way along the floor), a **pressure lane** (a long jet at chest height) and an **echo** (a ripple pair, then a second pair 0.75 s later).
- Telegraphs are 0.7 to 0.9 s (0.55 to 0.75 s in phase two); every attack ends in a reset of 0.8 s (0.65 s in phase two) in which the hinge is open. The hinge takes no hits at any other time.
- The hammer and the lane only reach the side it faces, so passing behind it is always safe.
- Phase two (below half health): it fights in phrases of three attacks, each phrase ending in a 0.7 s resting beat.

## Fairness rules (checked)

1. Every attack has a telegraph of at least **0.45 s**. A player needs about 0.25 s to react, which leaves time to move.
2. Every attack leaves a punish window of at least **0.35 s**, one full hook swing.
3. A flawless reader of the tells can beat every boss with ordinary inputs (no scripted shortcuts), but not instantly (at least 40 s).
4. Heavy damage is two masks, never more.
5. Healing comes only from hitting bosses.

## How difficulty is measured

`DIFFICULTY_SEEDS=1 dotnet run -c Release` in `tests/Core.Tests` fights each boss with scripted divers (`BossDifficulty.cs`) on eight different run seeds. Each death restarts the fight (the boss heals fully) with the diver back at the arena entrance. The **reader** sees every tell perfectly and plays with ordinary inputs (move, dash, jump, aimed swing). The **greedy** diver walks up and swings without ever dodging.

| Boss | Reader, 8 seeds | Greedy, 8 seeds |
|---|---|---|
| Undertoll | wins 5 of 8 within 7 minutes (0 to 3 deaths, 113 to 314 s); the other three also win with more time (4, 4 and 8 deaths, 583 to 809 s) | first death at 5 to 6 s, never wins |
| Pump Warden | wins all 8 with no deaths (66 to 81 s) | first death at 10 to 17 s, never wins |
| Archivist | wins all 8 with no deaths (64 to 93 s) | first death at 21 to 22 s, never wins |
| Tidekeeper | wins all 8 with no deaths (62 to 65 s) | first death at 11 to 12 s, never wins |

An idle diver dies to every boss too (checked on the default seed).

**What this does and does not show.** The reader is a script that knows every state exactly, so it is a no-hit ceiling, not a human: it shows each boss can be beaten on every tested order, that the Undertoll is the hardest, and that greedy play is punished. It cannot say how many tries a real first-time player will need, and it should not be read as "as hard as a specific Dark Souls boss". That needs people playing it. If real players find a boss too easy or too cruel, the numbers to move are in `BossTuning` (Undertoll), `PumpMachinery.cs` (Warden) and `ArchiveFoes.cs` (Archivist) and `EngineFoes.cs` (Tidekeeper); the checks above show which rules must stay.
