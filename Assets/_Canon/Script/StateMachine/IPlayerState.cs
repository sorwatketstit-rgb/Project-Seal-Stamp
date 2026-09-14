namespace SM64
{
    /// <summary>
    /// Contract for all modular player states.
    /// </summary>
    public interface IPlayerState
    {
        void Enter();
        void HandleInput();
        void LogicUpdate();
        void PhysicsUpdate();
        void Exit();
    }
}
