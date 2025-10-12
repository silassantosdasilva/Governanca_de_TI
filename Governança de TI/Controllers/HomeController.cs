using Governança_de_TI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Governança_de_TI.Data;

namespace Governança_de_TI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        EquipamentoModel Objequipamento = new EquipamentoModel();


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Tela de Equipamentos
        public async Task<IActionResult> Consulta()
        {
            var listaDeEquipamentos = await _context.Equipamentos
                .Include(e => e.Usuario)
                .ToListAsync();

            return View("~/Views/Equipamentos/Consulta.cshtml", listaDeEquipamentos);
        }

        public async Task<IActionResult> Criar()
        {
     

            return View("~/Views/Equipamentos/Criar.cshtml");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
