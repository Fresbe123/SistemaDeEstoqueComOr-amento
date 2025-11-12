using GestãoEstoque.ClassesOrganização;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
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


namespace GestãoEstoque
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<ItemEstoque> ItensObservaveis => ArmazenamentoTemporario.Itens;

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
            }
        }

        // Método público para atualizar os cards de outros lugares
        public void AtualizarCardsDashboard()
        {
            CarregarDadosCards();
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

                        dgProdutos.Items.Refresh();

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

                    dgProdutos.Items.Refresh();

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

            dgProdutos.Items.Refresh();
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

        private void AbrirRelatorios_Click(object sender, RoutedEventArgs e)
        {
            var w = new RelatorioVisualizador();
            w.Show();
        }
    }
}