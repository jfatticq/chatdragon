using System.ComponentModel.DataAnnotations;

namespace ChatDragon.Api.Options
{
    public class AzureOpenAIOptions
    {
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string DeploymentName { get; set; } = string.Empty;

        [Required]
        public string Endpoint { get; set; } = string.Empty;
    }
}
