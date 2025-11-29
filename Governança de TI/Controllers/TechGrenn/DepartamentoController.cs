using Governança_de_TI.Data;
using Governança_de_TI.Models.TecgreenModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Governança_de_TI.Controllers.TechGrenn
{
    public class DepartamentoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartamentoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // LISTA DE DEPARTAMENTOS (PARA SELECT)
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var lista = await _context.Departamentos
                .OrderBy(d => d.Nome)
                .ToListAsync();

            return Json(lista);
        }

        // ===============================
        // MODAL: CRIAR DEPARTAMENTO
        // ===============================
        [HttpGet]
        public IActionResult _CriarDepartamentoModal()
        {
            return PartialView("_CriarDepartamentoModal", new DepartamentoModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(DepartamentoModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_CriarDepartamentoModal", model);

            _context.Departamentos.Add(model);
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                departamento = new { id = model.Id, nome = model.Nome }
            });
        }

        [HttpGet]
        public async Task<IActionResult> _GerenciarDepartamentoModal()
        {
            var lista = await _context.Departamentos.OrderBy(x => x.Nome).ToListAsync();
            return PartialView("_GerenciarDepartamentoModal", lista);
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var dep = await _context.Departamentos.FindAsync(id);

            if (dep == null)
                return Json(new { sucesso = false, message = "Departamento não encontrado." });

            // 🔥 1) VERIFICA SE EXISTEM USUÁRIOS VINCULADOS AO DEPARTAMENTO
            bool existeUsuarios = await _context.Usuarios.AnyAsync(u => u.DepartamentoId == id);

            if (existeUsuarios)
            {
                return Json(new
                {
                    sucesso = false,
                    message = "Este departamento está em uso por um ou mais usuários e não pode ser excluído."
                });
            }

            // 🔥 2) EXCLUIÇÃO
            _context.Departamentos.Remove(dep);
            await _context.SaveChangesAsync();

            return Json(new { sucesso = true });
        }

    }
}
