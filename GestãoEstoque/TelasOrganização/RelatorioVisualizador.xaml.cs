using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using GestãoEstoque.ClassesOrganização;

namespace GestãoEstoque
{
    public partial class RelatorioVisualizador : Window
    {
        private ICollectionView _collectionView;
        private List<RelatorioOS> _todosRelatorios;

        public RelatorioVisualizador()
        {
            InitializeComponent();
            CarregarRelatorios();
        }

        private void CarregarRelatorios()
        {
            _todosRelatorios = RelatorioOSmanager.Relatorios.ToList();
            _collectionView = CollectionViewSource.GetDefaultView(_todosRelatorios);
            dgRelatorios.ItemsSource = _collectionView;
        }

        private RelatorioOS ObterSelecionado()
        {
            return dgRelatorios.SelectedItem as RelatorioOS;
        }

        // NOVO BOTÃO SIMPLES
        private void BtnGerarOS_Click(object sender, RoutedEventArgs e)
        {
            var orcamento = ObterSelecionado();

            if (orcamento == null)
            {
                MessageBox.Show("Selecione um orçamento primeiro.");
                return;
            }

            if (orcamento.Tipo != "Orçamento")
            {
                MessageBox.Show("Selecione um orçamento para converter.");
                return;
            }

            try
            {
                // Carregar dados
                var dados = CarregarOrcamento(orcamento.CaminhoArquivo);
                if (dados == null)
                {
                    MessageBox.Show("Não foi possível carregar os dados deste orçamento.");
                    return;
                }

                // Converter itens
                var itens = new ObservableCollection<ItemEstoque>();
                foreach (var item in dados.Itens)
                {
                    var itemEstoque = new ItemEstoque
                    {
                        Codigo = item.Codigo,
                        Nome = item.Nome,
                        Descricao = item.Descricao,
                        Quantidade = item.Quantidade.ToString("N2"),
                        Preco = item.Preco.ToString("N2"),
                        Desconto = $"{item.Desconto}%",
                        Total = (item.Quantidade * item.Preco * (1 - item.Desconto / 100)).ToString("N2")
                    };
                    itens.Add(itemEstoque);
                }

                // Criar OS
                var osWindow = new EmitirOS(new ObservableCollection<ItemEstoque>());

                // Tentar preencher dados
                var clienteField = osWindow.GetType().GetField("txtCliente",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (clienteField != null)
                {
                    var txtCliente = clienteField.GetValue(osWindow) as TextBox;
                    if (txtCliente != null)
                        txtCliente.Text = orcamento.Cliente;
                }

                // Adicionar itens
                var itensField = osWindow.GetType().GetField("ItensSelecionados",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (itensField != null)
                {
                    var itensSelecionados = itensField.GetValue(osWindow) as ObservableCollection<ItemEstoque>;
                    if (itensSelecionados != null)
                    {
                        foreach (var item in itens)
                            itensSelecionados.Add(item);
                    }
                }

                this.Hide();
                osWindow.ShowDialog();
                this.Show();

                MessageBox.Show("OS criada a partir do orçamento!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        // Método para carregar orçamento
        private DadosOrcamentoCompleto CarregarOrcamento(string caminhoPdf)
        {
            try
            {
                string caminhoJson = caminhoPdf.Replace(".pdf", ".json");
                if (File.Exists(caminhoJson))
                {
                    string json = File.ReadAllText(caminhoJson);
                    return System.Text.Json.JsonSerializer.Deserialize<DadosOrcamentoCompleto>(json);
                }
            }
            catch { }
            return null;
        }

        // MÉTODOS EXISTENTES (mantenha todos abaixo)

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
            if (sel != null && File.Exists(sel.CaminhoArquivo))
                Process.Start(new ProcessStartInfo(sel.CaminhoArquivo) { UseShellExecute = true });
            else
                MessageBox.Show("Selecione um relatório válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            var all = RelatorioOSmanager.Relatorios.ToList();
            if (!all.Any()) { MessageBox.Show("Nenhum relatorio para exportar."); return; }

            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RelatoriosOS.csv");
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine("Id;Cliente;Data;ValorTotal;Tipo;Arquivo");
                foreach (var r in all)
                    sw.WriteLine($"{r.Id};\"{r.Cliente}\";{r.Data};{r.ValorTotal};{r.Tipo};\"{r.CaminhoArquivo}\"");
            }
            MessageBox.Show($"CSV salvo em: {path}");
        }

        private void BtnVerOS_Click(object sender, RoutedEventArgs e)
        {
            FiltrarPorTipo("OS");
        }

        private void BtnVerOrcamentos_Click(object sender, RoutedEventArgs e)
        {
            FiltrarPorTipo("Orçamento");
        }

        private void BtnVerTodos_Click(object sender, RoutedEventArgs e)
        {
            FiltrarPorTipo("Todos");
        }

        private void FiltrarPorTipo(string tipo)
        {
            if (_collectionView == null) return;

            if (tipo == "Todos")
            {
                _collectionView.Filter = null;
            }
            else
            {
                _collectionView.Filter = item =>
                {
                    var relatorio = item as RelatorioOS;
                    return relatorio?.Tipo == tipo;
                };
            }
        }
    }
}