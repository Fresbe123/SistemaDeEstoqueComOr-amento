using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque.ClassesOrganização
{
    public class RelatorioOS
    {
        public int Id { get; set; }
        public string Cliente { get; set; }
        public string Data { get; set; }
        public decimal ValorTotal { get; set; }
        public string CaminhoArquivo { get; set; }
        public string Tipo { get; set; }

        public string NumeroOrcamento { get; set; }
        public decimal ValorDeslocamento { get; set; }
        public decimal ValorMaoDeObra { get; set; }
        public decimal KmPercorridos { get; set; }
        public decimal ValorPorKm { get; set; }
        public string ItensSerializados { get; set; }

    }
}
