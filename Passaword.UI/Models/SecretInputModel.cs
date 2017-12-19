using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Passaword.UI.Models
{
    public class SecretInputModel
    {
        [Required]
        public string Secret { get; set; }

        public string Passphrase { get; set; }

        [Required]
        public DateTime Expiry { get; set; } = DateTime.Now.AddDays(7);
    }
}
