using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Passaword.UI.Models
{
    public class SecretRetrieveModel
    {
        [Required]
        public string Id { get; set; }

        public string Passphrase { get; set; }
    }
}
