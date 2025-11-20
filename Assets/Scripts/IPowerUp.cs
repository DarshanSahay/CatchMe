public interface IPowerUp
{
    void Apply(PlayerMovement player);
    void Remove(PlayerMovement player);
    float Duration { get; }
    string Name { get; }
}
