namespace Luzart
{
    /// <summary>
    /// A gameplay component whose lifecycle is driven by a run (one Classic-mode match).
    /// The <see cref="GameCoordinator"/> calls <see cref="OnRunBegin"/> when a run starts and
    /// <see cref="OnRunEnd"/> when it ends — so each participant (enemy spawner, player, drops, …)
    /// owns its own start/stop without the coordinator knowing its internals. New participants
    /// plug in just by implementing this interface on a Domain content.
    /// </summary>
    public interface IRunParticipant
    {
        void OnRunBegin();
        void OnRunEnd();
    }
}
