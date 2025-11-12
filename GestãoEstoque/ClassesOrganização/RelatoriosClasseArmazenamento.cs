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
    }
}
