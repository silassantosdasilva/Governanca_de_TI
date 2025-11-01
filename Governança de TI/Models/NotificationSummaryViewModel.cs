using System;
using System.Collections.Generic;

// Este namespace deve corresponder ao seu projeto
namespace Governança_de_TI.ViewModels
{
    /// <summary>
    /// ViewModel para agregar todos os dados do dropdown de notificações.
    /// </summary>
    public class NotificationSummaryViewModel
    {
        public GamificacaoViewModel Gamificacao { get; set; }
        public List<EquipamentoVencendoNotificacaoViewModel> EquipamentosVencendo { get; set; }
        public List<DescarteRecenteNotificacaoViewModel> DescartesRecentes { get; set; }
    }

    /// <summary>
    /// ViewModel para a barra de gamificação no dropdown.
    /// </summary>
    public class GamificacaoViewModel
    {
        public int PontosAtuais { get; set; }
        public string NivelAtual { get; set; }
        public string IconeNivel { get; set; }
        public int PontosProximoNivel { get; set; }
        public int PercentualProgresso { get; set; }
        public string MensagemNivel { get; set; }
    }

    /// <summary>
    /// ViewModel para a lista de equipamentos a vencer.
    /// </summary>
    public class EquipamentoVencendoNotificacaoViewModel
    {
        public int CodigoItem { get; set; } // Usado para o link
        public string Descricao { get; set; }
        public int DiasRestantes { get; set; }
        public string Data { get; set; } // Data do vencimento formatada
    }

    /// <summary>
    /// ViewModel para a lista de descartes recentes.
    /// </summary>
    public class DescarteRecenteNotificacaoViewModel
    {
        public int Id { get; set; } // Usado para o link
        public string DescricaoEquipamento { get; set; }
        public string EmpresaColetora { get; set; }
        public string Data { get; set; } // Data do descarte formatada
    }
}

