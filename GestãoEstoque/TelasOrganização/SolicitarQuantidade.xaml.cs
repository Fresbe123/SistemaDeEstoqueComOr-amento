using GestãoEstoque.ClassesOrganização;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for SolicitarQuantidade.xaml
    /// </summary>
    public partial class SolicitarQuantidade : Window
    {
        public decimal Quantidade { get; private set; }
        public decimal DescontoPercentual {  get; private set; }
        private ItemEstoque _item;

        public SolicitarQuantidade(ItemEstoque item)
        {
            InitializeComponent();
            _item = item;
            CarregarInformacoes();
        }

        private void CarregarInformacoes()
        {
            txtItemInfo.Text = $"{_item.Descricao} - R$ {_item.PrecoDecimal.ToString("N2")}";

            txtEstoqueInfo.Text = $"Estoque disponível: {_item.EstoqueDisponivel.ToString("N2")} {_item.Unidade}";

            AtualizarTotal();

            txtQuantidade.TextChanged += (s, e) => AtualizarTotal();
            txtDesconto.TextChanged += (s, e) => AtualizarTotal();
        }

        private void AtualizarTotal()
        {
            if (decimal.TryParse(txtQuantidade.Text, out decimal quantidade) &&
                decimal.TryParse(txtDesconto.Text, out decimal descontoPercentual))
            {
                if (descontoPercentual < 0) descontoPercentual = 0;
                if (descontoPercentual > 100) descontoPercentual = 100;

                decimal subtotal = quantidade * _item.PrecoDecimal;
                decimal valorDesconto = subtotal * (descontoPercentual / 100);
                decimal total = subtotal - valorDesconto;

                txtSubtotal.Text = $"R$ {subtotal.ToString("N2")}";
                txtValorDesconto.Text = $"-R$ {valorDesconto.ToString("N2")}";
                txtTotal.Text = $"TOTAL: R$ {total.ToString("N2")}";
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtQuantidade.Text, out decimal quantidade) && quantidade > 0 &&
                decimal.TryParse(txtDesconto.Text, out decimal descontoPercentual))
            {
                if (!_item.TemEstoqueSuficiente(quantidade))
                {
                    MessageBox.Show($"Estoque insuficiente! Disponível: {_item.EstoqueDisponivel}",
                                  "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (descontoPercentual < 0 || descontoPercentual > 100)
                {
                    MessageBox.Show("O desconto deve estar entre 0% e 100%!", "Erro",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                    
                Quantidade = quantidade;
                DescontoPercentual = descontoPercentual;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Informe valores válidos!", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
