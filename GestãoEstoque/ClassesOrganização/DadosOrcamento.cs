using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque.ClassesOrganização
{
    public class DadosOrcamentoCompleto
    {
        public string NumeroOrcamento { get; set; }
        public string Cliente { get; set; }
        public DateTime Data { get; set; }
        public List<ItemOrcamentoDados> Itens { get; set; }
        public decimal KmPercorridos { get; set; }
        public decimal ValorPorKm { get; set; }
        public decimal ValorDeslocamento { get; set; }
        public decimal ValorMaoDeObra { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class ItemOrcamentoDados
    {
        public string Item {  get; set; }
        public string Un {  get; set; }
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public decimal Preco { get; set; }
        public decimal Desconto { get; set; }
    }

}
