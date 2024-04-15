using ChatDragon.Shared.Models;

namespace ChatDragon.Shared
{
    public class NpcGenerateResponse(NonPlayerCharacter npc) : ResponseBase
    {
        public NonPlayerCharacter NonPlayerCharacter { get; set; } = npc;
    }
}
