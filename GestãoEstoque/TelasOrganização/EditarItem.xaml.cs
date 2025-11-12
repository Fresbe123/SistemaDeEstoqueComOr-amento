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
    public partial class EditarItem : Window
    {
        private ItemEstoque _itemOriginal;
        private ItemEstoque _itemEditado;

        public ItemEstoque ItemEditado => _itemEditado;

        public EditarItem(ItemEstoque itemParaEditar)
        {
            InitializeComponent();
            _itemOriginal = itemParaEditar;
            CarregarDadosItem();
        }

        private void CarregarDadosItem()
        {
            txtItem.Text = _itemOriginal.Item;
            txtCodigo.Text = _itemOriginal.Codigo;
            txtDescricao.Text = _itemOriginal.Descricao;
            txtUnidade.Text = _itemOriginal.Unidade;
            txtQuantidade.Text = _itemOriginal.Quantidade;
            txtPreco.Text = _itemOriginal.Preco;
            chkFavorito.IsChecked = _itemOriginal.Favorito;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItem.Text) ||
                string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtDescricao.Text) ||
                string.IsNullOrWhiteSpace(txtUnidade.Text) ||
                !decimal.TryParse(txtQuantidade.Text, out decimal quantidade) ||
                !decimal.TryParse(txtPreco.Text, out decimal preco))
            {
                MessageBox.Show("Preencha todos os campos corretamente.", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (quantidade < 0)
            {
                MessageBox.Show("A quantidade não pode ser negativa.", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal total = preco * quantidade;

            _itemEditado = new ItemEstoque
            {
                Item = txtItem.Text,
                Codigo = txtCodigo.Text,
                Descricao = txtDescricao.Text,
                Unidade = txtUnidade.Text,
                Quantidade = quantidade.ToString("0.00"),
                Preco = preco.ToString("0.00"),
                Desconto = _itemOriginal.Desconto,
                Total = total.ToString("0.00"),
                Favorito = chkFavorito.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

