# Swashbuckler

This mod builds upon ThicreanPhoenix's Swashbuckler mod (and requires it as a dependency). This updates the swashbuckler class to include the changes from the remastered ruleset.

There are three known inconsistencies with the remastered rules, three of which are intentional compromises and three of which are something that I have yet to do/haven't figured out yet:

1.  In the TTRPG, you can use a bravado skill with no effect (e.g. against a creature who is immune) to provide panache. This would require some major reworking. As a compromise, I have instead repurposed the "Give Panache" test action to just assume failure, so that panache lasts until the end of the turn. This is somewhat more flexible (as you can use it without any targets whatsoever in range, and Gymnasts do not incur either opportunity attacks or multiple attack penalties), but I think it is reasonable.
2.  Flashy Roll and Leading Dance allow you to move in two sets of 5 feet, rather than one 10 foot movement. This is both stronger (since you could move 15 feet diagonally instead of 10 feet) and weaker (since you will not be able to move at all in difficult terrain). It was easier to implement this way.
3.  I have not updated Charmed Life to give panache until the end of your next turn if you succeed on the saving throw. I don't have a lot of motivation to do this, as it's not a feat I'm particularly interested in. It didn't seem particularly easy, either.
4.  I've updated the class description to refer to the Confident Evasion feature, yet at level 7, you actually gain Evasion on your character sheet. There's probably a way for me to fix that, but my initial attempt didn't work.
5.  I've updated subclass descriptions for normal swashbucklers, but not for the swashbuckler archetype. I didn't see a way to grab the swashbuckler dedication FeatName, so I'd need to think a little about how to do that.
6.  I've added skill feats at level 3, 7, and 15. These should be restricted to either acrobatics or the skill associated with your swashbuckler style, but a) some skills don't have very many skill feats in Dawnsbury Days, and b) adding restricted skill feats seems harder than just unrestricted skill feats. So, they're unrestricted.

Additionally, there are a few things that are awkward with adding this remastered class into the legacy ruleset in Dawnsbury Days.
1.  Twin Parry is no longer a Swashbuckler feat (consistent with the ORC), but it was added by the legacy Swashbuckler mod rather than the base game. I have just removed the Swashbuckler trait on these feats.
2.  I have not touched the Flamboyant Cruelty feat. I wanted to avoid changing it while keeping it in the game (since it is OGL), but parts of the feat are completely subsumed by the Remaster changes. I don't plan on touching it.
3.  In the remaster ruleset, Disarming Flair just adds the bravado trait to the Disarm action. The legacy Disarming Flair is a lot closer to the base Disarm action in the remaster. I have implemented the remastered changes to this feat by adding the bravado action to the legacy version of Disarming Flair. 