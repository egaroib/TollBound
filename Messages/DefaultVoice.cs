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
#
# This file is never rewritten, so your edits are safe. Updates only append
# sections that are missing entirely. To silence a spirit, empty its section
# rather than deleting it -- a deleted section counts as missing and comes back.


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


# -------------------------------------------------------------------- Mountain

[mountain.restless.opener]
Wind screams down the ridge.
Frost creeps across the stones.
A shadow crosses the snow. Nothing casts it.
The cold finds the seams in your pack.
Something wheels, far above the cloud.
The air thins, and keeps thinning.

[mountain.restless.refused]
The peak will not let that pass.
Too heavy for this thin air.
She has seen it, and she says no.
The way narrows. That does not fit.

[mountain.restless.unpaid]
Moder counts what you carry.
The mountain takes its share in glands.
Pay the cold, or climb down.
Nothing crosses her sky for free.

[mountain.restless.toll]
Moder takes her share.
The wind drops, briefly.
Something above is satisfied.
Paid. The cold lets go of you.

[mountain.sated.opener]
The ridge is silent.
No shadow crosses the snow now.
The wind is only wind.
Frost gathers, and nothing watches it.
The peak keeps its own counsel.

[mountain.sated.refused]
Even her death did not open that.
The mountain is older than she was.
Still no. Some things do not fly.

[mountain.sated.unpaid]
Her nest is cold. The toll is not.
The peak collects for her.
Pay it. She is beyond spending it.

[mountain.sated.toll]
The mountain takes what she is owed.
Paid, to nothing in particular.
The wind lets you through.


# ---------------------------------------------------------------------- Plains

[plains.restless.opener]
Ash drifts across the barrow.
Something buried grinds against stone.
Heat rises off the grass with no sun to give it.
The tar goes still.
A hand's shadow falls where there is no hand.
The ground remembers a weight.

[plains.restless.refused]
The ruined king will not permit it.
That is not yours to carry out.
His hand closes. The way with it.
Refused, and not gently.

[plains.restless.unpaid]
Yagluth's hand still closes. Pay it.
The ruined king demands tribute.
Black metal answers to him first.
Tribute, or turn around.

[plains.restless.toll]
Yagluth takes his tribute.
The ash settles, satisfied.
The buried hand opens.
Paid to a king who has no kingdom.

[plains.sated.opener]
The barrow is quiet at last.
Ash falls and stays fallen.
Nothing stirs under the stones.
The tar lies flat and cold.
The plains are only grass now.

[plains.sated.refused]
Broken as he is, not that.
The ruin holds its last rule.
Even now his hand is closed on this.

[plains.sated.unpaid]
A dead king still takes tribute.
Pay the ruin. It does not forgive.
His hand is dust. It is still open.

[plains.sated.toll]
The ruin takes its tribute.
Ash accepts, as ash does.
Paid, and the way is clear.


# ------------------------------------------------------------------- Mistlands

[mistlands.restless.opener]
The mist thickens without wind.
Ten thousand small sounds stop at once.
Something in the fog counts you.
The stone hums a note you cannot hold.
Wings, somewhere, out of time with each other.
The fog leans closer.

[mistlands.restless.refused]
The hive will not pass it.
She has weighed it and found it hers.
Not through her mist.
The counting stops. So do you.

[mistlands.restless.unpaid]
The Queen's brood attends the ledger.
The hive marks what leaves the mist.
Nothing crosses without her leave.
Pay her, or be counted twice.

[mistlands.restless.toll]
The Queen takes her portion.
The counting resumes.
Something in the fog is content.
The mist opens, precisely.

[mistlands.sated.opener]
The mist drifts, and nothing moves in it.
The small sounds are only insects now.
The fog has stopped counting.
Nothing hums in the stone.
The Mistlands keep their quiet.

[mistlands.sated.refused]
Her hive is dead. Its rules are not.
Still counted. Still refused.
Not even now.

[mistlands.sated.unpaid]
The brood keeps her books.
Pay what she is no longer here to take.
The ledger outlived the Queen.

[mistlands.sated.toll]
The hive takes its portion.
The mist parts, out of habit.
Paid, and counted, and let by.


# -------------------------------------------------------------------- Ashlands

[ashlands.restless.opener]
Embers rise where there is no fire.
The ground beneath goes hot.
Something enormous draws breath.
Ash falls upward for a moment.
The stone under your boots ticks as it cools.
Far off, something turns in the fire.

[ashlands.restless.refused]
The last flame refuses it.
That does not leave the burning.
Fader will not have it carried.
The fire closes over the way.

[ashlands.restless.unpaid]
Fader knows the weight of flametal.
The ash keeps a tally.
The last flame takes its portion. Pay it.
Nothing leaves the fire unweighed.

[ashlands.restless.toll]
Fader takes his portion.
The embers settle.
Something enormous lets out its breath.
The fire allows it.

[ashlands.sated.opener]
The ash falls straight down.
Nothing breathes in the deep fire.
The ground has gone cool enough to stand on.
The embers rise and go out.
The burning is only burning now.

[ashlands.sated.refused]
The fire is out. The rule is not.
Even ended, he will not have it.
Not through the ash.

[ashlands.sated.unpaid]
The ash still tallies.
Pay the fire that killed him.
He is ash. The toll is not.

[ashlands.sated.toll]
The ash takes its portion.
Paid to the last flame, long gone out.
The fire lets you through.


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
