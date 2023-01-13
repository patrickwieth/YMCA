using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
    public class FactionCAInfo : FactionInfo
    {
        [Desc("The game that the faction belongs to.")]
        public readonly string Game = null;
    }
}
