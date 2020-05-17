using Verse;

namespace PawnShields
{
    /// <summary>
    /// Entry class for the shields module.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Shields
    {
        static Shields()
        {
            PawnShieldGenerator.Reset();
        }
    }
}
