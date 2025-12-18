I want you to work on Issue #21461: Way to test lots of AI battles at https://github.com/OpenRA/OpenRA/issues/21461

---
The issue:
## Motivation
I'd like to develop the AI more but want to make sure I don't make it worse
 
## Proposed solution
A way to run many speed up test games to simulate lot of AI battles. Could just start with running just 1 game and outputting the results/stats. Is this functionality there and I'm missing it?

## Side effects
If time is changed for test games they may not match real games as accurately.

## Alternatives
Could be parallelized outside of OpenRA, but still need basic functionality inside it.
---

What you want to work on is adding a way for the user to run a single sped up test game to simulate AI battles (with 2-12 AI, entirely dependent on the map). It will output the results/stats. The feature has to make sure that the AI does NOT skip / do things that would not have happened in a regular game - it is simply making the game faster. There should be a timeline bar at the bottom, so the user is able to scrub the timeline after its done, to see the game at different times. Parallelizing outside of OpenRA is OUT OF SCOPE, don't do it. Also, you have to figure out the best place in the menu to add this feature - make sure it's not too intrusive and in a place that makes sense. Also, there should be a toggle for the fog of war -> start with the user seeing NO fog of war, and then a toggle that enables fog of war (so that you ONLY see what each AI sees).