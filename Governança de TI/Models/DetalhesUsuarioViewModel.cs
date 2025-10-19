using Governança_de_TI.Models;
using System.Collections.Generic;

namespace Governança_de_TI.ViewModels
{
    public class DetalhesUsuarioViewModel
    {
        public UsuarioModel Usuario { get; set; }
        public List<string> AtividadeRecente { get; set; }
    }
}
