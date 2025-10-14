using System.ComponentModel.DataAnnotations;

namespace AutoBot.Models.Api;

public class CreateManagedPositionRequest
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public long AmountInSats { get; set; }
}
