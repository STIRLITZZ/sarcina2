using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorApp11.Shared
{
	public class Elev
	{
		public int Id { get; set; }
		[Required(ErrorMessage ="Camp obligatoriu")]
		public string Nume { get; set; }
        [Required(ErrorMessage = "Camp obligatoriu")]
        public string Prenume { get; set; }
		[Range(1,10,ErrorMessage ="Nota trebuie sa fie cuprinsa intre 1 la 10")]
		public int Nota { get; set; }
	}
}
