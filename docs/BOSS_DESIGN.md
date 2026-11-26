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

## The bosses

**The Undertoll** (26 hits). Slam, rush and (phase two) rain.
- Idle beat cut from 0.7 s to 0.45 s; slam wind-up 0.7 s; rush 12 tiles/s; every slam or rush body hit costs 2 masks.
- Every fourth attack is a **delayed slam**: the bell rises, hangs 0.35 s at the top, then drops.
- Phase two: a slam sends a **second wave pair** 0.55 s behind the first (jump twice), a **slam chains out of every rush** with no idle beat, and rain comes every third attack.
- Punish windows: slam 1.05 s, rush 0.95 s (0.75 times that in phase two).

**The Pump Warden** (22 hits). Pipes, then a sweep.
- Three pulses in a row (four in phase two). Each is marked 0.6 s (0.45 s in phase two) before it vents.
- If the diver keeps back 5.5 tiles or more, the first jet is **tracked**: it vents right under their feet.
- Phase two: every other pulse covers **two neighbouring pipes** at once, leaving one lane quiet.
- The sweep alternates a **quick brace (0.6 s) and a delayed one (0.95 s)**. It now starts at the Warden's own centre, so hugging its casing no longer works: jump it (it is low) or cross it. A landed sweep costs 2 masks.
- Punish window 0.85 s (0.7 s in phase two).

**The Archivist of Depths** (20 hits). Two silhouettes, one real.
- Marks last 0.7 s (0.55 s in phase two); only the real silhouette swings its clapper. A dive costs 2 masks.
- Every landing sends a **ripple pair** 0.45 s later (0.3 s in phase two) and, in phase two, an **echo pair** 0.75 s after that.
- Phase two: every third dive is a **chain**: 0.4 s recovery, no hover, a 0.5 s mark. Do not be greedy.
- The real mark alternates between under the diver and the decoy spot.
- Punish window 0.85 s (0.7 s in phase two).

## Fairness rules (checked)

1. Every attack has a telegraph of at least **0.45 s**. A player needs about 0.25 s to react, which leaves time to move.
2. Every attack leaves a punish window of at least **0.35 s**, one full hook swing.
3. A flawless reader of the tells can beat every boss with ordinary inputs (no scripted shortcuts), but not instantly (at least 40 s).
4. Heavy damage is two masks, never more.
5. Healing comes only from hitting bosses.

## How difficulty is measured

`DIFFICULTY_TABLE=1 dotnet run -c Release` in `tests/Core.Tests` fights each boss with three scripted divers (`BossDifficulty.cs`). Each death restarts the fight with the diver back at the arena entrance, and the boss heals fully.

| Boss | Reader (sees every tell) | Greedy (walks up, swings, never dodges) | Idle (stands still) |
|---|---|---|---|
| Undertoll | wins, 0 deaths, 99 s | never wins, first death at 5.5 s | first death at 11.6 s |
| Pump Warden | wins, 0 deaths, 100 s | never wins, first death at 16.6 s | first death at 23.3 s |
| Archivist | wins, 0 deaths, 78 s | never wins, first death at 21.3 s | first death at 7.6 s |

**What this does and does not show.** The reader is a script that knows every state exactly, so it is a no-hit ceiling, not a human: it shows each boss can be beaten fairly, and that the greedy and idle styles are punished hard. It cannot say how many tries a real first-time player will need, and it should not be read as "as hard as a specific Dark Souls boss". That needs people playing it. If real players find a boss too easy or too cruel, the numbers to move are in `BossTuning` (Undertoll), `PumpMachinery.cs` (Warden) and `ArchiveFoes.cs` (Archivist); the checks above show which rules must stay.
