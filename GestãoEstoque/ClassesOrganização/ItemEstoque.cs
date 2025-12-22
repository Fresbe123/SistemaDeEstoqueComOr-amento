using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque.ClassesOrganização
{
    public class ItemEstoque : INotifyPropertyChanged
    {
        private string _quantidade;
        private string _preco;
        private string _total;


        public string Item { get; set; }
        public string Nome { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string Unidade { get; set; }
        private bool _favorito;

        public string Preco
        {
            get => _preco;
            set
            {
                _preco = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrecoDecimal));
                RecalcularTotal();
            }
        }

        public string Quantidade
        {
            get => _quantidade;
            set
            {
                _quantidade = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QuantidadeDecimal));
                OnPropertyChanged(nameof(EstoqueDisponivel));
                OnPropertyChanged(nameof(ValorEstoqueAtual));
                RecalcularTotal();
            }
        }

        public string Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged();
            }
        }

        public bool Favorito
        {
            get => _favorito;
            set
            {
                if (_favorito != value)
                {
                    _favorito = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OrdemFavorito)); // Para ordenação
                    OnPropertyChanged(nameof(IconeFavorito)); // Para ícone
                }
            }
        }

        public int OrdemFavorito => Favorito ? 0 : 1;

        public string IconeFavorito => Favorito ? "★" : "☆";

        public string Desconto { get; set; }

        //Propriedades para formatação consistente

        public string PrecoFormatado => decimal.TryParse(Preco, out var preco) ? preco.ToString("N2") : "0,00";
        public string QuantidadeFormatada => decimal.TryParse(Quantidade, out var quantidade) ? quantidade.ToString("N2") : "0,00";
        public string TotalFormatado => decimal.TryParse(Total, out var total) ? total.ToString("N2") : "0,00";

        // Propriedade para verificar se tem estoque
        public bool TemEstoque => QuantidadeDecimal > 0;

        // Propriedade para texto de status do estoque
        public string StatusEstoque => TemEstoque ? $"ESTOQUE" : "SEM ESTOQUE";

        public decimal PrecoDecimal => decimal.TryParse(Preco, out var preco) ? preco : 0;
        public decimal QuantidadeDecimal => decimal.TryParse(Quantidade, out var quantidade) ? quantidade : 0;
        public decimal TotalCadastroDecimal => decimal.TryParse(Total, out var total) ? total : 0;

        // Propriedade para mostrar valor atual do estoque
        public string ValorEstoqueAtual
        {
            get
            {
                decimal valorAtual = QuantidadeDecimal * PrecoDecimal;
                return $"R$ {valorAtual.ToString("N2")}";
            }
        }

        // Propriedade para o total baseado na quantidade selecionada na OS
        public decimal TotalOSDecimal { get; set; }

        // Propriedades para seleção de quantidade
        public decimal QuantidadeSelecionada { get; set; }
        public decimal DescontoPercentual { get; set; }
        public decimal ValorDesconto => (QuantidadeSelecionada * PrecoDecimal) * (DescontoPercentual / 100);
        public decimal TotalSelecionado => QuantidadeSelecionada * PrecoDecimal;
        public decimal EstoqueDisponivel => QuantidadeDecimal;

        // Método para validar se tem estoque suficiente
        public bool TemEstoqueSuficiente(decimal quantidadeDesejada)
            => QuantidadeDecimal >= quantidadeDesejada;

        private void RecalcularTotal()
        {
            decimal totalCalculado = QuantidadeDecimal * PrecoDecimal;
            Total = totalCalculado.ToString("N2");
        }

        public void AtualizarEstoque(decimal quantidadeVendida)
        {
            decimal estoqueAtual = QuantidadeDecimal;
            decimal novoEstoque = estoqueAtual - quantidadeVendida;
            Quantidade = novoEstoque.ToString("N2");
        }

        // Método para criar cópia do item para OS
        public ItemEstoque CriarCopiaParaOS(decimal quantidade, decimal descontoPercentual)
        {
            decimal subtotal = quantidade * PrecoDecimal;
            decimal valorDesconto = subtotal * (descontoPercentual / 100);
            decimal totalComDesconto = subtotal - valorDesconto;

            return new ItemEstoque
            {
                Item = this.Item,
                Nome = this.Nome,
                Codigo = this.Codigo,
                Descricao = this.Descricao,
                Unidade = this.Unidade,
                Preco = this.Preco,
                Quantidade = quantidade.ToString("N2"),
                Desconto = descontoPercentual > 0 ? $"{descontoPercentual.ToString("N2")}%" : "0%",
                Total = totalComDesconto.ToString("N2"), 
                TotalOSDecimal = totalComDesconto,
                Favorito = this.Favorito,
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}