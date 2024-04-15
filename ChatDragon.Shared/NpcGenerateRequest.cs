namespace ChatDragon.Shared
{
    public class NpcGenerateRequest(string ask) : RequestBase
    {
        public string Ask { get; set; } = ask;
    }
}
