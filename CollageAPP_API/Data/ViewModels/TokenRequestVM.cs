using System.ComponentModel.DataAnnotations;

namespace CollageAPP_API.Data.ViewModels
{
    public class TokenRequestVM
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}
