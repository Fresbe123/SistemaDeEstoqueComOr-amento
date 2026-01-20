using GestãoEstoque.ClassesOrganização;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;


namespace GestãoEstoque
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ItemEstoque> ItensObservaveis => ArmazenamentoTemporario.Itens;
        private bool _mostrandoApenasFavoritos = false;
        private ObservableCollection<ItemEstoque> _todosItensBackup;

        public MainWindow()
        {
            InitializeComponent();
            CarregarItens();
            DataContext = this;
            CarregarDadosCards();
        }

        private void CarregarDadosCards()
        {
            try
            {
                var itens = ArmazenamentoTemporario.Itens;

                if (itens == null || itens.Count == 0)
                {
                    DefinirCardsVazios();
                    return;
                }

                // Card 1: Total de Itens
                int totalItens = itens.Count;
                txtTotalItens.Text = totalItens.ToString();
                txtVariacaoItens.Text = totalItens > 0 ? $"+{totalItens} itens cadastrados" : "Nenhum item cadastrado";

                // Card 2: Em Estoque (quantidade total)
                decimal quantidadeTotal = itens.Sum(item => item.QuantidadeDecimal);
                txtEmEstoque.Text = quantidadeTotal.ToString("N0");

                int itensComEstoque = itens.Count(item => item.QuantidadeDecimal > 0);
                int totalItensComEstoque = itens.Count;
                decimal percentualComEstoque = totalItensComEstoque > 0 ?
                    (itensComEstoque * 100m) / totalItensComEstoque : 0;
                txtVariacaoEstoque.Text = $"{percentualComEstoque:N0}% com estoque";

                // Card 3: Valor Total em Estoque
                decimal valorTotalEstoque = itens.Sum(item => item.TotalCadastroDecimal);
                txtValorEstoque.Text = valorTotalEstoque.ToString("C");

                decimal valorMedio = totalItens > 0 ? valorTotalEstoque / totalItens : 0;
                txtInfoValor.Text = $"Média: {valorMedio.ToString("C")}";

                // Card 4: Itens Críticos (estoque baixo)
                int itensCriticos = itens.Count(item => item.QuantidadeDecimal > 0 && item.QuantidadeDecimal <= 5);
                txtItensCriticos.Text = itensCriticos.ToString();

                if (itensCriticos > 0)
                {
                    txtInfoCriticos.Text = "Necessitam reposição";
                }
                else
                {
                    txtInfoCriticos.Text = "Estoque normal";
                }

                // Card 5: Itens Favoritos
                int itensFavoritos = itens.Count(item => item.Favorito);
                txtItensFavoritos.Text = itensFavoritos.ToString();

                if (itensFavoritos > 0)
                {
                    decimal percentualFavoritos = (itensFavoritos * 100m) / totalItens;
                    txtInfoFavoritos.Text = $"{percentualFavoritos:N0}% do total";
                }
                else
                {
                    txtInfoFavoritos.Text = "Nenhum favorito";
                }

                AtualizarCoresCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados dos cards: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                DefinirCardsVazios();
            }
        }

        private void DefinirCardsVazios()
        {
            txtTotalItens.Text = "0";
            txtVariacaoItens.Text = "Sem dados";

            txtEmEstoque.Text = "0";
            txtVariacaoEstoque.Text = "Sem dados";

            txtValorEstoque.Text = "R$ 0,00";
            txtInfoValor.Text = "Sem dados";

            txtItensCriticos.Text = "0";
            txtInfoCriticos.Text = "Sem dados";

            txtItensFavoritos.Text = "0";
            txtInfoFavoritos.Text = "Sem dados";
        }

        private void AtualizarCoresCards()
        {
            int itensCriticos = int.Parse(txtItensCriticos.Text);
            if (itensCriticos > 10)
            {
                // Você pode adicionar lógica aqui se quiser
            }
        }

        // Método público para atualizar os cards de outros lugares
        public void AtualizarCardsDashboard()
        {
            CarregarDadosCards();
        }

        // BOTÃO FAVORITAR - NOVO
        private void BtnFavoritar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProdutos.SelectedItem is ItemEstoque itemSelecionado)
            {
                // Alterna entre favorito/não favorito
                itemSelecionado.Favorito = !itemSelecionado.Favorito;

                // Reordena a lista (favoritos primeiro)
                ReordenarListaComFavoritos();

                // Atualiza os cards
                CarregarDadosCards();

                string status = itemSelecionado.Favorito ? "favoritado" : "removido dos favoritos";
                string mensagem = itemSelecionado.Favorito ?
                    $"Item '{itemSelecionado.Descricao}' foi favoritado e movido para o topo!" :
                    $"Item '{itemSelecionado.Descricao}' foi removido dos favoritos.";

                MessageBox.Show(mensagem, "Sucesso",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Selecione um item para favoritar.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ReordenarListaComFavoritos()
        {
            // Ordena a lista: favoritos primeiro, depois pelo Item
            var listaOrdenada = ArmazenamentoTemporario.Itens
                .OrderBy(item => item.OrdemFavorito) // 0 para favoritos, 1 para não favoritos
                .ThenBy(item => item.Item) // Ordena por Item dentro de cada grupo
                .ToList();

            // Limpa e recria a coleção para refletir a nova ordem
            var novaLista = new ObservableCollection<ItemEstoque>(listaOrdenada);
            ArmazenamentoTemporario.Itens.Clear();

            foreach (var item in novaLista)
            {
                ArmazenamentoTemporario.Itens.Add(item);
            }

            // Atualiza a exibição
            dgProdutos.Items.Refresh();
        }

        // MÉTODO PARA FILTRAR APENAS FAVORITOS
        private void MostrarApenasFavoritos_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;

            if (!_mostrandoApenasFavoritos)
            {
                // Salva backup da lista completa
                _todosItensBackup = new ObservableCollection<ItemEstoque>(ArmazenamentoTemporario.Itens);

                // Filtra apenas favoritos
                var favoritos = new ObservableCollection<ItemEstoque>(
                    ArmazenamentoTemporario.Itens.Where(item => item.Favorito));

                dgProdutos.ItemsSource = favoritos;
                _mostrandoApenasFavoritos = true;
                botao.Content = "📋 Todos os Itens";
            }
            else
            {
                // Restaura lista completa
                dgProdutos.ItemsSource = _todosItensBackup;
                _mostrandoApenasFavoritos = false;
                botao.Content = "📌 Apenas Favoritos";
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProdutos.SelectedItem is ItemEstoque itemSelecionado)
            {
                var janelaEditar = new EditarItem(itemSelecionado);

                if (janelaEditar.ShowDialog() == true)
                {
                    ItemEstoque itemEditado = janelaEditar.ItemEditado;

                    int indice = ArmazenamentoTemporario.Itens.IndexOf(itemSelecionado);

                    if (indice >= 0)
                    {
                        ArmazenamentoTemporario.Itens[indice] = itemEditado;

                        // Reordena após edição (para manter favoritos no topo)
                        ReordenarListaComFavoritos();

                        MessageBox.Show("Item editado com sucesso!", "Sucesso",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecione um item para editar.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeletar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProdutos.SelectedItem is ItemEstoque itemSelecionado)
            {
                MessageBoxResult resultado = MessageBox.Show(
                    $"Tem certeza que deseja deletar o item '{itemSelecionado.Descricao}'?",
                    "Confirmar Deleção",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    ArmazenamentoTemporario.Itens.Remove(itemSelecionado);

                    // Reordena após deletar
                    ReordenarListaComFavoritos();

                    // Atualiza os cards
                    CarregarDadosCards();

                    MessageBox.Show("Item deletado com sucesso!", "Sucesso",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um item para deletar.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAdicionarItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgProdutos.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Selecione um item na tabela antes de adicionar quantidade.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string input = Microsoft.VisualBasic.Interaction.InputBox("Digite a quantidade a adicionar:", "Adicionar Quantidade", "1");
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!TryParseDecimal(input, out decimal qtdAdicional))
            {
                MessageBox.Show("Quantidade inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var prop = selected.GetType().GetProperty("Quantidade");
            if (prop == null)
            {
                MessageBox.Show("Propriedade 'Quantidade' não encontrada no item. Verifique a classe ItemEstoque.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            object currentVal = prop.GetValue(selected);
            decimal qtdAtual = 0m;
            if (currentVal != null)
            {
                if (currentVal is decimal dec) qtdAtual = dec;
                else if (currentVal is int i) qtdAtual = i;
                else if (!TryParseDecimal(currentVal.ToString(), out qtdAtual))
                {
                    MessageBox.Show("Quantidade atual inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            decimal novoTotal = qtdAtual + qtdAdicional;

            // salva de volta no tipo adequado
            try
            {
                var t = prop.PropertyType;
                if (t == typeof(string)) prop.SetValue(selected, novoTotal.ToString("0.00"));
                else if (t == typeof(decimal)) prop.SetValue(selected, novoTotal);
                else if (t == typeof(double)) prop.SetValue(selected, Convert.ToDouble(novoTotal));
                else if (t == typeof(float)) prop.SetValue(selected, Convert.ToSingle(novoTotal));
                else if (t == typeof(int)) prop.SetValue(selected, Convert.ToInt32(novoTotal));
                else prop.SetValue(selected, novoTotal.ToString("0.00"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao atualizar quantidade: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Reordena após adicionar quantidade
            ReordenarListaComFavoritos();
            CarregarDadosCards();
        }

        private bool TryParseDecimal(string s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;

            s = s.Trim();

            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out value))
                return true;

            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                return true;

            var alt = s.Replace(',', '.');
            return decimal.TryParse(alt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public void AtualizarListaItens()
        {
            dgProdutos.Items.Refresh();
            CarregarDadosCards();
        }

        private void CarregarItens()
        {
            // Ordena os itens (favoritos primeiro)
            ReordenarListaComFavoritos();

            // Atribui ao DataGrid
            dgProdutos.ItemsSource = ArmazenamentoTemporario.Itens;

            dgProdutos.RowHeight = 35;
            dgProdutos.VerticalAlignment = VerticalAlignment.Stretch;
            dgProdutos.HorizontalAlignment = HorizontalAlignment.Stretch;

            CarregarDadosCards();
        }

        private void AbrirCadastro_Click(object sender, RoutedEventArgs e)
        {
            CadastrarItem cadastrarItem = new CadastrarItem();
            cadastrarItem.ShowDialog();
            CarregarItens();
        }

        private void AbrirEmitirOS_Click(object sender, RoutedEventArgs e)
        {
            EmitirOS osWindow = new EmitirOS(ArmazenamentoTemporario.Itens);
            osWindow.Show();
        }

        private void BtnCriarOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (ItensObservaveis.Count == 0)
            {
                MessageBox.Show("Não há itens disponíveis para criar orçamento.");
                return;
            }

            var criarOrcamento = new CriarOrcamento(new ObservableCollection<ItemEstoque>(ItensObservaveis));
            criarOrcamento.ShowDialog();
        }

        private void AbrirRelatorios_Click(object sender, RoutedEventArgs e)
        {
            var w = new RelatorioVisualizador();
            w.Show();
        }
    }
}