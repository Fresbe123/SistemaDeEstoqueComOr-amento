using GestãoEstoque.ClassesOrganização;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque
{
    public static class ArmazenamentoTemporario
    {
        public static ObservableCollection<ItemEstoque> Itens { get; } = new ObservableCollection<ItemEstoque>();

        public static ObservableCollection<ItemEstoque> ObterTodos()
        {
            return Itens;
        }

        public static ItemEstoque? ObterPorCodigo(string codigo)
        {
            return Itens.FirstOrDefault(item => item.Codigo == codigo);
        }
    }
}
