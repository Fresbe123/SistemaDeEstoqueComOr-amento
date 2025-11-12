using GestãoEstoque.ClassesOrganização;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for CadastrarItem.xaml
    /// </summary>
    public partial class CadastrarItem : Window
    {
        public CadastrarItem()
        {
            InitializeComponent();
            CarregarProdutos();
            dgProdutos.ItemsSource = null;
            dgProdutos.ItemsSource = ArmazenamentoTemporario.Itens; 
        }

        private void CarregarProdutos()
        {
            dgProdutos.ItemsSource = null;
            dgProdutos.ItemsSource = ArmazenamentoTemporario.Itens;
        }

        private void Textbox_kDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textboxAtual = sender as TextBox;
                var proximoElemento = textboxAtual.Tag as string;

                if (proximoElemento == "chkFavorito")
                {
                    chkFavorito.Focus();
                }
                else
                {
                    var proximoControle = FindName(proximoElemento) as Control;
                    proximoControle?.Focus();
                }

                e.Handled = true;
            }
        }

        private void CheckBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var btnAdicionar = FindName("btnAdicionar") as Button;
                btnAdicionar?.Focus();
                e.Handled = true;
            }
        }

        private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItem.Text) ||
                string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                !decimal.TryParse(txtPreco.Text, out decimal preco) ||
                !decimal.TryParse(txtQuantidade.Text, out decimal qtd))
            {
                MessageBox.Show("Preencha os dados corretamente.");
                return;
            }

            if (qtd < 0)
            {
                MessageBox.Show("A quantidade não pode ser negativa.");
                return;
            }

            decimal total = preco * qtd;

            ArmazenamentoTemporario.Itens.Add(new ItemEstoque
            {
                Item = txtItem.Text.Trim(),
                Codigo = txtCodigo.Text.Trim(),
                Descricao = txtDescricao.Text.Trim(),
                Unidade = txtUnidade.Text.Trim(),
                Preco = preco.ToString("0.00"),
                Quantidade = qtd.ToString("0.00"),
                Desconto = "0%",
                Total = total.ToString("0.00"),
                Favorito = chkFavorito.IsChecked == true
            });

            txtItem.Clear();
            txtDescricao.Clear();
            txtQuantidade.Clear();
            txtPreco.Clear();
            txtUnidade.Clear();
            txtCodigo.Clear();
            chkFavorito.IsChecked = false;

            txtItem.Focus();
        }

        private void BtnFavoritar_Click(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Favorito")
            {
                e.Cancel = true;
            }

        }
    }
}


