namespace Tollbound.Messages
{
    /// <summary>
    /// The shipped lines, written to BepInEx/config/Tollbound/voice.txt on first run and
    /// never overwritten afterwards, so hand edits survive updates.
    ///
    /// Black Forest and Swamp are fully voiced. The rest fall through to [fallback], which
    /// is deliberately plain — adding a [mountain.restless.opener] section to the file
    /// starts using it immediately, with no rebuild and no code change.
    /// </summary>
    internal static class DefaultVoice
    {
        internal const string Text = @"# Tollbound - the voice of the spirits
#
# Sections are [spirit.mood.slot]. Lines starting with # are ignored.
#
#   spirit   blackforest swamp mountain plains mistlands ashlands, or fallback
#   mood     restless (the boss still lives) or sated (you have killed it)
#   slot     opener   atmosphere, shown first, never names the spirit
#            refused  the crossing will not carry what you have
#            unpaid   you cannot afford the toll
#            toll     the toll was taken and you crossed
#
# A message is one opener plus one line from the matching slot, so six of each
# gives thirty-six. Any spirit or slot with no lines falls back to [fallback].
#
# TWO RULES, and the whole thing falls apart without them:
#
#   1. Every line is a COMPLETE SENTENCE with its own punctuation. They are
#      assembled, never joined, so any opener can precede any demand.
#   2. NEVER put an item name, a count or a number in a line. Those live in the
#      separate ledger message. This is what makes broken grammar impossible.


# ---------------------------------------------------------------- Black Forest

[blackforest.restless.opener]
The roots tighten around the stones.
Something old shifts beneath the moss.
Bark splits somewhere behind you.
The forest leans in to watch.
Sap runs black down the frame.
The trees stand too still.

[blackforest.restless.refused]
The Elder will not have it.
These woods do not open that far.
Roots close over the way.
His reach ends here, and so does yours.

[blackforest.restless.unpaid]
The Elder will have his due.
Nothing leaves these woods unweighed.
Pay in eyes, or turn back.
He counts every stone taken from him.

[blackforest.restless.toll]
The Elder takes his share.
Roots part, grudgingly.
The debt is noted.
He allows it. He remembers it.

[blackforest.sated.opener]
The moss lies quiet.
Nothing stirs in the branches.
The roots part without complaint.
The forest is only a forest now.
Old wood creaks and settles.
Light falls where it did not before.

[blackforest.sated.refused]
Even so, the way is closed.
Killing him did not widen the road.
This much the woods still refuse.

[blackforest.sated.unpaid]
A dead king is owed all the same.
The forest keeps its tolls.
Pay him. He is past arguing.

[blackforest.sated.toll]
The old debt, settled again.
He takes it without stirring.
The woods let you by.


# ----------------------------------------------------------------------- Swamp

[swamp.restless.opener]
The bog exhales.
Something vast turns over in the mud.
The water goes still and green.
Flies lift off the reeds all at once.
The reeds bow with no wind to bow them.
A sound like breathing, from under.

[swamp.restless.refused]
The mire will not swallow that.
Too much. The mud refuses it.
That does not pass through here.

[swamp.restless.unpaid]
The mire keeps what it is owed.
Feed it, or keep what you carry.
Nothing leaves the marsh unweighed.
Bonemass counts in guts, not coin.

[swamp.restless.toll]
The mass takes its share.
Something under the water is satisfied.
The bog closes over the debt.

[swamp.sated.opener]
The mud parts where you walk.
The marsh is quiet now.
The water runs almost clear.
Nothing rises to meet you.
The reeds stand where they are put.

[swamp.sated.refused]
Quiet as it is, the way stays shut.
The marsh drowned him and kept his rules.

[swamp.sated.unpaid]
It still remembers the debt.
The mire outlived him. So did the toll.

[swamp.sated.toll]
Bonemass takes his share and lets you go.
The mud accepts it, as it always has.


# -------------------------------------------------------------------- fallback
# Used by any spirit with nothing authored. Keep these unspecific.

[fallback.restless.opener]
The air changes at the threshold.
Something on the other side takes notice.
The stones hum, low and wrong.
A cold draught comes through the frame.

[fallback.restless.refused]
The way will not take it.
This crossing is closed to that.
It does not pass.

[fallback.restless.unpaid]
The spirits want their due.
Nothing crosses unpaid.
Payment first.

[fallback.restless.toll]
The toll is taken.
Something on the other side is satisfied.
The way opens.

[fallback.sated.opener]
The threshold is quiet.
Nothing contests the crossing.
The air settles.

[fallback.sated.refused]
Even now, not this.
The way stays shut to that.

[fallback.sated.unpaid]
The dead are owed all the same.
Pay, though no one is left to ask.

[fallback.sated.toll]
The toll is taken, as ever.
The way opens.
";
    }
}
