using System.Reflection;

namespace ChatDragon.Api.Utilities
{
    public static class EmbeddedResource
    {
        public static string Read(string name)
        {
            using StreamReader reader = new(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"ChatDragon.Api.Prompts.{name}")!);

            return reader.ReadToEnd();
        }
    }
}
