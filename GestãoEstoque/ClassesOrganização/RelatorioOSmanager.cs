using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque.ClassesOrganização
{
    public static class RelatorioOSmanager
    {
        public static ObservableCollection<RelatorioOS> Relatorios { get; set; } = new ObservableCollection<RelatorioOS>();

        public static void Registrar(RelatorioOS r)
        {
            r.Id = Relatorios.Count + 1;
            Relatorios.Add(r);
        }

    }
}
