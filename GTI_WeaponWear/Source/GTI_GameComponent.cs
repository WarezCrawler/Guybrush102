using Verse;

namespace GTI_WeaponWear
{
    // Clears the mod's per-game static state whenever a game starts or a save is loaded.
    // The throttle dictionaries (auto-repair scan/message timers, debug-log rate limits) are
    // keyed by thingIDNumber / situation and store ABSOLUTE TicksGame values; both restart
    // per game, so state carried over from a previous game in the same session would collide
    // and silently suppress auto-repair (or logging) until the stale timestamps expire.
    //
    // RimWorld instantiates every GameComponent subclass automatically when a Game is
    // created; FinalizeInit runs on new-game creation AND after every load.
    public class GTI_GameComponent : GameComponent
    {
        public GTI_GameComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            JobGiver_RepairEquippedWeapon.ResetState();
            GtiLog.ResetState();
            Patch_Thing_RepairInfo.ResetState(); // drops a cached Thing that would pin the old map
        }
    }
}
