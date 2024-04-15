namespace ChatDragon.Shared
{
    public class GetIntentRequest(string ask) : RequestBase
    {
        public string Ask { get; set; } = ask;
    }
}
