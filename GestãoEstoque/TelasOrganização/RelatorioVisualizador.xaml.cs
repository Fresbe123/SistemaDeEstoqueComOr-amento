using GestãoEstoque.ClassesOrganização;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestãoEstoque
{
    /// <summary>
    /// Interaction logic for RelatorioVisualizador.xaml
    /// </summary>
    public partial class RelatorioVisualizador : Window
    {
        public RelatorioVisualizador()
        {
            InitializeComponent();
            dgRelatorios.ItemsSource = RelatorioOSmanager.Relatorios;   
        }

        private RelatorioOS ObterSelecionado()
        {
            return dgRelatorios.SelectedItem as RelatorioOS;
        }

        private void dgRelatorios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var sel = ObterSelecionado();
            if (sel != null && File.Exists(sel.CaminhoArquivo))
            {
                Process.Start(new ProcessStartInfo(sel.CaminhoArquivo) { UseShellExecute = true });
                
            }
            else MessageBox.Show("Arquivo não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnAbrirClick(object sender, RoutedEventArgs e)
        {
            var sel = ObterSelecionado();
            if (sel != null && File.Exists (sel.CaminhoArquivo))
                Process.Start(new ProcessStartInfo(sel.CaminhoArquivo) { UseShellExecute = true });
            else
                MessageBox.Show("Selecione um relatório válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            var all = RelatorioOSmanager.Relatorios.ToList();
            if (!all.Any()) { MessageBox.Show("Nenhum relatorio para exportar."); return;}

            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RelatoriosOS.csv");
            using (var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Id;Cliente;Data;ValorTotal;Arquivo");
                foreach (var r in all)
                    sw.WriteLine($"{r.Id};\"{r.Cliente}\";{r.Data};{r.ValorTotal};\"{r.CaminhoArquivo}\"");
            }
            MessageBox.Show($"CSV salvo em: {path}");
        }

    }
}
