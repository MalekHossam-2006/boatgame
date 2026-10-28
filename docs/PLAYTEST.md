# Playtest Kit

Use this for the midterm playtests (guidelines section G). Test with **at least two
people** who have **not** played the game before. Fill one sheet per tester, then copy
the summary into section 7 of `docs/GDD.md`.

## Before the session (2 min)

- Use the Windows build (or the Unity editor, starting from `MainMenu.unity`).
- Reset the best score so every tester starts from zero: in the Unity editor menu
  **Edit > Clear All PlayerPrefs**, or delete the key `BestMoney`.
- Tell the tester only this: *"This is a prototype of our game. Please think out loud
  while you play. We are testing the game, not you."*
- **Do not explain the rules or controls.** The menu and the game should do that.
  Watching whether they work is part of the test.

## During the session (5-10 min)

Let the tester play at least **two runs**. Stay quiet, watch, and take notes.

**What to watch for (tick or note the time):**

| Moment | Time / notes |
|---|---|
| Opened How to Play? (yes / no) | |
| Time until first barrel collected | |
| First time leaving the safe zone (buoys) | |
| First attempt at a big +50 barrel | |
| Noticed the chase banner and turned away? | |
| Was caught (how? against an island? too slow?) | |
| Used Esc / pause? | |
| Said anything confused, surprised or excited (quote it) | |
| Final money run 1 / run 2 | |

## After the session (3 min) - ask these questions

Our two main questions (they go into the GDD):
1. **"Was the goal clear within the first minute?"**
2. **"Did going for the big +50 barrels near the pirate feel like a meaningful choice?"**

Extra questions (pick a few):
3. "When the pirates caught you, did it feel fair?"
4. "Did you notice when the pirates started chasing you? How?"
5. "Could you tell your ship from the pirate ship right away?"
6. "Were the controls easy? Anything you tried that did not work?"
7. "What would make you want to play again?"
8. "On a scale of 1-5, how fun was it? Why?"

## Observation sheet (copy for each tester)

```
Tester:            Date:            Played games before? (never / sometimes / often)

Run 1 money:       Result (win/lose):     Run 2 money:       Result:

Q1 goal clear?
Q2 big barrels meaningful?
Other answers:

What worked:
What confused them / problems:
Quotes:
```

## Summary for the GDD (fill after all sessions)

- **At least 3 observations** (with tester number).
- **One thing that worked.**
- **One problem or confusion point.**
- **One change we plan to make** (and which principle / MDA example it relates to).
