using GestãoEstoque.ClassesOrganização;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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
using static GestãoEstoque.ClassesOrganização.Informações;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace GestãoEstoque
{
    /// <summary>
    /// Interaction logic for EmitirOS.xaml
    /// </summary>
    public partial class EmitirOS : Window
    {
        private ObservableCollection<ItemEstoque> ItensDisponiveis;
        private ObservableCollection<ItemEstoque> ItensSelecionados = new();

        // Lista para controlar o que será debitado apenas na emissão
        private List<ItemEstoque> ItensParaDebitar = new List<ItemEstoque>();

        private decimal _valorDeslocamento = 0;
        private decimal _valorMaoDeObra = 0; // NOVO CAMPO

        public EmitirOS(ObservableCollection<ItemEstoque> itensDisponiveis)
        {
            InitializeComponent();

            // Criar CÓPIAS dos itens para trabalhar sem afetar os originais
            ItensDisponiveis = new ObservableCollection<ItemEstoque>(
                itensDisponiveis.Select(item => CriarCopiaItem(item)).ToList()
            );

            lstDisponiveis.ItemsSource = ItensDisponiveis;
            lstSelecionados.ItemsSource = ItensSelecionados;

            txtData.Text = DateTime.Now.ToString("dd/MM/yyyy");
            AtualizarTotalGeral();
        }

        // Método: Criar cópia profunda de um item
        private ItemEstoque CriarCopiaItem(ItemEstoque original)
        {
            return new ItemEstoque
            {
                Item = original.Item,
                Nome = original.Nome,
                Codigo = original.Codigo,
                Descricao = original.Descricao,
                Unidade = original.Unidade,
                Preco = original.Preco,
                Quantidade = original.Quantidade,
                Desconto = original.Desconto,
                Total = original.Total,
                Favorito = original.Favorito
            };
        }

        private void AtualizarTotalGeral()
        {
            decimal totalItens = ItensSelecionados.Sum(item =>
                decimal.TryParse(item.Total, out decimal totalItem) ? totalItem : 0);

            decimal totalGeral = totalItens + _valorDeslocamento + _valorMaoDeObra; // ATUALIZADO

            txtTotalOS.Text = totalGeral.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        // Método antigo mantido para compatibilidade
        private void AtualizarTotal()
        {
            AtualizarTotalGeral();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lstDisponiveis.SelectedItem is ItemEstoque itemCopia)
            {
                var janelaQuantidade = new SolicitarQuantidade(itemCopia);

                if (janelaQuantidade.ShowDialog() == true)
                {
                    decimal quantidadeDesejada = janelaQuantidade.Quantidade;
                    decimal descontoPercentual = janelaQuantidade.DescontoPercentual;

                    if (!itemCopia.TemEstoqueSuficiente(quantidadeDesejada))
                    {
                        MessageBox.Show($"Estoque insuficiente! Disponível: {itemCopia.QuantidadeDecimal}",
                                      "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var itemExistente = ItensSelecionados.FirstOrDefault(i => i.Codigo == itemCopia.Codigo);
                    if (itemExistente != null)
                    {
                        MessageBox.Show($"Este item já está na OS. Remova-o primeiro para adicionar novamente.",
                                      "Item Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var itemParaOS = CriarCopiaParaOS(itemCopia, quantidadeDesejada, descontoPercentual);
                    ItensSelecionados.Add(itemParaOS);

                    decimal novaQuantidadeVisual = itemCopia.QuantidadeDecimal - quantidadeDesejada;
                    itemCopia.Quantidade = novaQuantidadeVisual.ToString("N2");

                    ItensParaDebitar.Add(new ItemEstoque
                    {
                        Codigo = itemCopia.Codigo,
                        Quantidade = quantidadeDesejada.ToString("N2"),
                        Descricao = itemCopia.Descricao
                    });

                    lstDisponiveis.Items.Refresh();
                    AtualizarTotalGeral();

                    if (itemCopia.QuantidadeDecimal > 0)
                    {
                        MessageBox.Show($"Item adicionado! Estoque restante: {itemCopia.QuantidadeDecimal} {itemCopia.Unidade}",
                                      "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecione um item para adicionar.", "Atenção",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método: Criar cópia para OS
        private ItemEstoque CriarCopiaParaOS(ItemEstoque item, decimal quantidade, decimal descontoPercentual)
        {
            decimal subtotal = quantidade * item.PrecoDecimal;
            decimal valorDesconto = subtotal * (descontoPercentual / 100);
            decimal totalComDesconto = subtotal - valorDesconto;

            return new ItemEstoque
            {
                Item = item.Item,
                Nome = item.Nome,
                Codigo = item.Codigo,
                Descricao = item.Descricao,
                Unidade = item.Unidade,
                Preco = item.Preco,
                Quantidade = quantidade.ToString("N2"),
                Desconto = descontoPercentual > 0 ? $"{descontoPercentual.ToString("N2")}%" : "0%",
                Total = totalComDesconto.ToString("N2"),
                Favorito = item.Favorito
            };
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Move para o próximo controle
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement elementWithFocus)
                {
                    elementWithFocus.MoveFocus(request);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Limpa o texto do TextBox quando pressionar ESC
                if (sender is TextBox textBox)
                {
                    textBox.Clear();
                    e.Handled = true;
                }
            }
        }

        private void BtnRemover_Click(object sender, RoutedEventArgs e)
        {
            if (lstSelecionados.SelectedItem is ItemEstoque itemSelecionado)
            {
                var itemCopia = ItensDisponiveis.FirstOrDefault(i => i.Codigo == itemSelecionado.Codigo);

                if (itemCopia != null)
                {
                    decimal quantidadeDevolvida = itemSelecionado.QuantidadeDecimal;
                    decimal novaQuantidadeVisual = itemCopia.QuantidadeDecimal + quantidadeDevolvida;
                    itemCopia.Quantidade = novaQuantidadeVisual.ToString("N2");

                    var itemDebito = ItensParaDebitar.FirstOrDefault(i => i.Codigo == itemSelecionado.Codigo);
                    if (itemDebito != null)
                    {
                        ItensParaDebitar.Remove(itemDebito);
                    }
                }

                ItensSelecionados.Remove(itemSelecionado);

                lstDisponiveis.Items.Refresh();
                AtualizarTotalGeral();

                MessageBox.Show("Item removido da OS.",
                              "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Selecione um item para remover.", "Atenção",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Métodos para cálculo do deslocamento
        private void TxtKmPercorridos_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

        private void TxtValorPorKm_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

        // NOVO MÉTODO: Cálculo da mão de obra
        private void TxtMaoDeObra_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularMaoDeObra();
        }

        private void CalcularDeslocamento()
        {
            try
            {
                decimal kmPercorridos = 0;
                decimal valorPorKm = 0;

                if (!string.IsNullOrEmpty(txtKmPercorridos.Text))
                {
                    decimal.TryParse(txtKmPercorridos.Text, out kmPercorridos);
                }

                if (!string.IsNullOrEmpty(txtValorPorKm.Text))
                {
                    string valorPorKmTexto = txtValorPorKm.Text.Replace("R$", "").Trim();
                    decimal.TryParse(valorPorKmTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out valorPorKm);
                }

                _valorDeslocamento = kmPercorridos * valorPorKm;

                txtValorDeslocamento.Text = _valorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

                AtualizarTotalGeral();
            }
            catch
            {
                // Ignora erro
            }
        }

        // NOVO MÉTODO: Calcular mão de obra
        private void CalcularMaoDeObra()
        {
            try
            {
                if (!string.IsNullOrEmpty(txtMaoDeObra.Text))
                {
                    string valorTexto = txtMaoDeObra.Text.Replace("R$", "").Trim();
                    if (decimal.TryParse(valorTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out decimal valor))
                    {
                        _valorMaoDeObra = valor;
                    }
                    else
                    {
                        _valorMaoDeObra = 0;
                    }
                }
                else
                {
                    _valorMaoDeObra = 0;
                }

                AtualizarTotalGeral();
            }
            catch
            {
                _valorMaoDeObra = 0;
                AtualizarTotalGeral();
            }
        }

        public void ReceberDadosOrcamento(ObservableCollection<ItemEstoque> itensOrcamento,
                                         decimal valorDeslocamento,
                                         decimal valorMaoDeObra, 
                                         string numeroOrcamento,
                                         decimal kmPercorridos,
                                         decimal valorPorKm)
        {
            // Adicionar itens do orçamento
            foreach (var item in itensOrcamento)
            {
                var itemCopia = CriarCopiaItem(item);
                ItensSelecionados.Add(itemCopia);

                // Registrar para débito no estoque
                ItensParaDebitar.Add(new ItemEstoque
                {
                    Codigo = itemCopia.Codigo,
                    Quantidade = itemCopia.Quantidade,
                    Descricao = itemCopia.Descricao
                });
            }

            // Configurar deslocamento COMPLETO 
            _valorDeslocamento = valorDeslocamento;

            // Configurar mão de obra
            _valorMaoDeObra = valorMaoDeObra;

            // PREENCHER OS CAMPOS DE KM E VALOR POR KM
            txtKmPercorridos.Text = kmPercorridos.ToString();
            txtValorPorKm.Text = valorPorKm.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            txtValorDeslocamento.Text = valorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

            // Preencher campo de mão de obra
            txtMaoDeObra.Text = valorMaoDeObra.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

            // Atualizar totais
            AtualizarTotalGeral();
            lstSelecionados.Items.Refresh();
        }

        // Método: Atualizar estoque real APENAS na emissão
        private void AtualizarEstoqueReal()
        {
            foreach (var itemDebito in ItensParaDebitar)
            {
                // Encontra o item ORIGINAL no ArmazenamentoTemporario
                var itemOriginal = ArmazenamentoTemporario.Itens
                    .FirstOrDefault(i => i.Codigo == itemDebito.Codigo);

                if (itemOriginal != null)
                {
                    decimal quantidadeDebitar = itemDebito.QuantidadeDecimal;
                    decimal estoqueAtual = itemOriginal.QuantidadeDecimal;
                    decimal novoEstoque = estoqueAtual - quantidadeDebitar;

                    if (novoEstoque >= 0)
                    {
                        // ATUALIZA o estoque REAL apenas aqui
                        itemOriginal.Quantidade = novoEstoque.ToString("N2");

                        // Atualizar o Total também
                        decimal valorEstoqueAtual = novoEstoque * itemOriginal.PrecoDecimal;
                        itemOriginal.Total = valorEstoqueAtual.ToString("N2");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Estoque insuficiente para {itemOriginal.Descricao}. " +
                            $"Tentativa: {quantidadeDebitar}, Disponível: {estoqueAtual}");
                    }
                }
            }
            ItensParaDebitar.Clear();
        }

        private void AtualizarMainWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                mainWindow.AtualizarListaItens();
            }
        }

        private void BtnEmitirOS_Click(object sender, RoutedEventArgs e)
        {
            if (ItensSelecionados.Count == 0)
            {
                MessageBox.Show("Nenhum item selecionado para emissão.");
                return;
            }

            try
            {
                // atualiza o estoque real
                AtualizarEstoqueReal();

                // atualiza Mainwindow
                AtualizarMainWindow();

                // Criar uma instância do orçamento usando a classe Informações
                var orcamento = new ClassesOrganização.Informações.Orcamento();

                // VERIFICAÇÃO EXTRA: Garantir que Deslocamento foi instanciado
                if (orcamento.Deslocamento == null)
                {
                    orcamento.Deslocamento = new ClassesOrganização.Informações.Deslocamento();
                }

                // Preencher os dados do orçamento com os valores dos controles da interface
                orcamento.Emitente.Nome = txtEmitenteNome.Text;
                orcamento.Emitente.CNPJ = txtEmitenteCNPJ.Text;
                orcamento.Emitente.Endereco = txtEmitenteEndereco.Text;
                orcamento.Emitente.Bairro = txtEmitenteBairro.Text;
                orcamento.Emitente.Cidade = txtEmitenteCidade.Text;
                orcamento.Emitente.Estado = txtEmitenteEstado.Text;
                orcamento.Emitente.CEP = txtEmitenteCEP.Text;
                orcamento.Emitente.Telefone = txtEmitenteTelefone.Text;

                orcamento.Destinatario.Nome = txtCliente.Text;
                orcamento.Destinatario.CNPJ_CPF = txtCpfCnpj.Text;
                orcamento.Destinatario.Endereco = txtEndereco.Text;
                orcamento.Destinatario.Bairro = txtBairro.Text;
                orcamento.Destinatario.Cidade = txtCidade.Text;
                orcamento.Destinatario.Estado = txtEstado.Text;
                orcamento.Destinatario.CEP = txtCEP.Text;
                orcamento.Destinatario.Telefone = txtTelefone.Text;
                orcamento.Destinatario.Email = txtEmail.Text;

                orcamento.DadosAdicionais.Data = txtData.Text;
                orcamento.DadosAdicionais.HoraKM = txtHoraKm.Text;
                orcamento.DadosAdicionais.Veiculo = txtVeiculo.Text;
                orcamento.DadosAdicionais.Modelo = txtModelo.Text;
                orcamento.DadosAdicionais.Marca = txtMarca.Text;
                orcamento.DadosAdicionais.Cliente = txtCliente.Text;
                orcamento.DadosAdicionais.Telefone = txtTelefone.Text;
                orcamento.DadosAdicionais.PatrimonioPlaca = txtPatrimonioPlaca.Text;

                orcamento.Transportadora.Nome = txtTransportadora.Text;
                orcamento.Transportadora.TipoFrete = cmbTipoFrete.Text;
                orcamento.Transportadora.NumeroDocumento = txtNumeroDocumento.Text;
                orcamento.Transportadora.Validade = txtValidade.Text;
                orcamento.Transportadora.Emissao = txtEmissao.Text;
                orcamento.Transportadora.CondicaoPagamento = txtCondicaoPagamento.Text;
                orcamento.Transportadora.Vendedor = txtVendedor.Text;
                orcamento.Transportadora.Observacoes = txtObservacoes.Text;

                // PREENCHER DADOS DE DESLOCAMENTO COM VERIFICAÇÃO
                try
                {
                    // Verificar novamente se Deslocamento existe
                    if (orcamento.Deslocamento == null)
                    {
                        orcamento.Deslocamento = new ClassesOrganização.Informações.Deslocamento();
                    }

                    orcamento.Deslocamento.KmPercorridos = string.IsNullOrEmpty(txtKmPercorridos.Text) ? 0 : decimal.Parse(txtKmPercorridos.Text);

                    string valorPorKmTexto = txtValorPorKm.Text.Replace("R$", "").Trim();
                    orcamento.Deslocamento.ValorPorKm = string.IsNullOrEmpty(valorPorKmTexto) ? 0 :
                        decimal.Parse(valorPorKmTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"));

                    orcamento.Deslocamento.ValorDeslocamento = _valorDeslocamento;
                }
                catch (Exception ex)
                {
                    // Em caso de erro nos cálculos, define zero
                    if (orcamento.Deslocamento == null)
                    {
                        orcamento.Deslocamento = new ClassesOrganização.Informações.Deslocamento();
                    }
                    orcamento.Deslocamento.KmPercorridos = 0;
                    orcamento.Deslocamento.ValorPorKm = 0;
                    orcamento.Deslocamento.ValorDeslocamento = 0;

                    Console.WriteLine($"Erro ao calcular deslocamento: {ex.Message}");
                }

                // NOVO: Adicionar mão de obra ao orçamento
                orcamento.ValorMaoDeObra = _valorMaoDeObra;

                orcamento.Itens = ItensSelecionados.ToList();
                orcamento.ValorTotal = ItensSelecionados.Sum(item => decimal.TryParse(item.Total, out var total) ? total : 0);

                string caminhoArquivo = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"Orcamento_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                );

                // Configurações do documento
                iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4, 20, 20, 60, 20); 
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, new FileStream(caminhoArquivo, FileMode.Create));

                writer.PageEvent = new PdfLogoHelper2();

                doc.Open();

                // --- FONTES ---
                iTextSharp.text.Font fTituloPrincipal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 14, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
                iTextSharp.text.Font fTituloSecao = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
                iTextSharp.text.Font fNormal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);
                iTextSharp.text.Font fNegrito = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);
                iTextSharp.text.Font fPequeno = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK);

                // --- CABEÇALHO DO DOCUMENTO ---
                iTextSharp.text.Paragraph tituloDoc = new iTextSharp.text.Paragraph("ORDEM DE SERVIÇO - ORÇAMENTO", fTituloPrincipal);
                tituloDoc.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
                tituloDoc.SpacingAfter = 5f;
                doc.Add(tituloDoc);

                iTextSharp.text.Paragraph subtituloDoc = new iTextSharp.text.Paragraph("NÃO É DOCUMENTO FISCAL - NÃO É VÁLIDO COMO RECIBO E COMO GARANTIA DE MERCADORIA -\n NÃO COMPROVA PAGAMENTO", fPequeno);
                subtituloDoc.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
                subtituloDoc.SpacingAfter = 15f;
                doc.Add(subtituloDoc);

                // --- IDENTIFICAÇÃO DO ESTABELECIMENTO EMITENTE ---
                iTextSharp.text.Paragraph tituloEmitente = new iTextSharp.text.Paragraph("Identificação do Estabelecimento Emitente", fTituloSecao);
                tituloEmitente.SpacingAfter = 5f;
                doc.Add(tituloEmitente);

                iTextSharp.text.pdf.PdfPTable tabelaEmitente = new iTextSharp.text.pdf.PdfPTable(1);
                tabelaEmitente.WidthPercentage = 100;
                tabelaEmitente.SpacingAfter = 10f;

                string textoEmitente = $"Denominação: {orcamento.Emitente.Nome}    " +
                                      $"CNPJ: {orcamento.Emitente.CNPJ}\n" +
                                      $"Endereço: {orcamento.Emitente.Endereco}\n" +
                                      $"Bairro: {orcamento.Emitente.Bairro}    " +
                                      $"Cidade: {orcamento.Emitente.Cidade}    " +
                                      $"Estado: {orcamento.Emitente.Estado}\n" +
                                      $"CEP: {orcamento.Emitente.CEP}    " +
                                      $"Telefone: {orcamento.Emitente.Telefone}";

                iTextSharp.text.pdf.PdfPCell cellEmitente = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(textoEmitente, fNormal));
                cellEmitente.BorderWidth = 1;
                cellEmitente.Padding = 8f;
                cellEmitente.BorderColor = iTextSharp.text.BaseColor.BLACK;
                tabelaEmitente.AddCell(cellEmitente);
                doc.Add(tabelaEmitente);

                // --- IDENTIFICAÇÃO DO DESTINATÁRIO ---
                iTextSharp.text.Paragraph tituloDestinatario = new iTextSharp.text.Paragraph("Identificação do Destinatário", fTituloSecao);
                tituloDestinatario.SpacingAfter = 5f;
                doc.Add(tituloDestinatario);

                iTextSharp.text.pdf.PdfPTable tabelaDestinatario = new iTextSharp.text.pdf.PdfPTable(1);
                tabelaDestinatario.WidthPercentage = 100;
                tabelaDestinatario.SpacingAfter = 10f;

                string textoDestinatario = $"Nome: {orcamento.Destinatario.Nome}    " +
                                          $"CNPJ/CPF: {orcamento.Destinatario.CNPJ_CPF}\n" +
                                          $"Endereço: {orcamento.Destinatario.Endereco}\n" +
                                          $"Bairro: {orcamento.Destinatario.Bairro}    " +
                                          $"Cidade: {orcamento.Destinatario.Cidade}    " +
                                          $"Estado: {orcamento.Destinatario.Estado}\n" +
                                          $"CEP: {orcamento.Destinatario.CEP}    " +
                                          $"Telefone: {orcamento.Destinatario.Telefone}\n" +
                                          $"E-mail: {orcamento.Destinatario.Email}";

                iTextSharp.text.pdf.PdfPCell cellDestinatario = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(textoDestinatario, fNormal));
                cellDestinatario.BorderWidth = 1;
                cellDestinatario.Padding = 8f;
                cellDestinatario.BorderColor = iTextSharp.text.BaseColor.BLACK;
                tabelaDestinatario.AddCell(cellDestinatario);
                doc.Add(tabelaDestinatario);

                // --- DADOS ADICIONAIS ---
                iTextSharp.text.pdf.PdfPTable tabelaDadosAdicionais = new iTextSharp.text.pdf.PdfPTable(2);
                tabelaDadosAdicionais.WidthPercentage = 100;
                tabelaDadosAdicionais.SetWidths(new float[] { 50, 50 });
                tabelaDadosAdicionais.SpacingAfter = 10f;

                string dadosEsquerda = $"Data: {orcamento.DadosAdicionais.Data}\n" +
                                      $"Hora/KM: {orcamento.DadosAdicionais.HoraKM}\n" +
                                      $"Veículo: {orcamento.DadosAdicionais.Veiculo}\n" +
                                      $"Modelo: {orcamento.DadosAdicionais.Modelo}\n" +
                                      $"Marca: {orcamento.DadosAdicionais.Marca}";

                iTextSharp.text.pdf.PdfPCell cellDadosEsquerda = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(dadosEsquerda, fNormal));
                cellDadosEsquerda.BorderWidth = 1;
                cellDadosEsquerda.Padding = 8f;
                cellDadosEsquerda.BorderColor = iTextSharp.text.BaseColor.BLACK;
                tabelaDadosAdicionais.AddCell(cellDadosEsquerda);

                string dadosDireita = $"Cliente: {orcamento.DadosAdicionais.Cliente}\n" +
                                     $"Telefone: {orcamento.DadosAdicionais.Telefone}\n" +
                                     $"Patrimônio/Placa: {orcamento.DadosAdicionais.PatrimonioPlaca}";

                iTextSharp.text.pdf.PdfPCell cellDadosDireita = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(dadosDireita, fNormal));
                cellDadosDireita.BorderWidth = 1;
                cellDadosDireita.Padding = 8f;
                cellDadosDireita.BorderColor = iTextSharp.text.BaseColor.BLACK;
                tabelaDadosAdicionais.AddCell(cellDadosDireita);

                doc.Add(tabelaDadosAdicionais);

                // --- DESLOCAMENTO ---
                if (orcamento.Deslocamento != null && orcamento.Deslocamento.ValorDeslocamento > 0)
                {
                    iTextSharp.text.Paragraph tituloDeslocamento = new iTextSharp.text.Paragraph("Deslocamento", fTituloSecao);
                    tituloDeslocamento.SpacingAfter = 5f;
                    doc.Add(tituloDeslocamento);

                    iTextSharp.text.pdf.PdfPTable tabelaDeslocamento = new iTextSharp.text.pdf.PdfPTable(3);
                    tabelaDeslocamento.WidthPercentage = 100;
                    tabelaDeslocamento.SetWidths(new float[] { 33, 33, 34 });
                    tabelaDeslocamento.SpacingAfter = 10f;

                    AdicionarCelulaComBorda(tabelaDeslocamento, $"KM Percorridos: {orcamento.Deslocamento.KmPercorridos}", fNormal, iTextSharp.text.Element.ALIGN_LEFT);
                    AdicionarCelulaComBorda(tabelaDeslocamento, $"Valor por KM: {orcamento.Deslocamento.ValorPorKm.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}", fNormal, iTextSharp.text.Element.ALIGN_LEFT);
                    AdicionarCelulaComBorda(tabelaDeslocamento, $"Valor do Deslocamento: {orcamento.Deslocamento.ValorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}", fNegrito, iTextSharp.text.Element.ALIGN_RIGHT);

                    doc.Add(tabelaDeslocamento);
                }

                // --- MÃO DE OBRA ---
                if (_valorMaoDeObra > 0)
                {
                    iTextSharp.text.Paragraph tituloMaoDeObra = new iTextSharp.text.Paragraph("Mão de Obra", fTituloSecao);
                    tituloMaoDeObra.SpacingAfter = 5f;
                    doc.Add(tituloMaoDeObra);

                    iTextSharp.text.pdf.PdfPTable tabelaMaoDeObra = new iTextSharp.text.pdf.PdfPTable(1);
                    tabelaMaoDeObra.WidthPercentage = 100;
                    tabelaMaoDeObra.SpacingAfter = 10f;

                    string textoMaoDeObra = $"Valor da Mão de Obra: {_valorMaoDeObra.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}";

                    iTextSharp.text.pdf.PdfPCell cellMaoDeObra = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(textoMaoDeObra, fNegrito));
                    cellMaoDeObra.BorderWidth = 1;
                    cellMaoDeObra.Padding = 8f;
                    cellMaoDeObra.BorderColor = iTextSharp.text.BaseColor.BLACK;
                    tabelaMaoDeObra.AddCell(cellMaoDeObra);
                    doc.Add(tabelaMaoDeObra);
                }

                // --- IDENTIFICAÇÃO DA TRANSPORTADORA ---
                iTextSharp.text.Paragraph tituloTransportadora = new iTextSharp.text.Paragraph("Identificação da Transportadora", fTituloSecao);
                tituloTransportadora.SpacingAfter = 5f;
                doc.Add(tituloTransportadora);

                iTextSharp.text.pdf.PdfPTable tabelaTransportadora = new iTextSharp.text.pdf.PdfPTable(2);
                tabelaTransportadora.WidthPercentage = 100;
                tabelaTransportadora.SetWidths(new float[] { 50, 50 });
                tabelaTransportadora.SpacingAfter = 10f;

                string transportadoraLinha1 = $"Nome: {orcamento.Transportadora.Nome}    " +
                                             $"Tipo frete: {orcamento.Transportadora.TipoFrete}";
                iTextSharp.text.pdf.PdfPCell cellTransportadora1 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(transportadoraLinha1, fNormal));
                cellTransportadora1.BorderWidth = 1;
                cellTransportadora1.Padding = 8f;
                cellTransportadora1.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellTransportadora1.Colspan = 2;
                tabelaTransportadora.AddCell(cellTransportadora1);

                string transportadoraLinha2 = $"Nº do Documento: {orcamento.Transportadora.NumeroDocumento}    " +
                                             $"Validade: {orcamento.Transportadora.Validade}";
                iTextSharp.text.pdf.PdfPCell cellTransportadora2 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(transportadoraLinha2, fNormal));
                cellTransportadora2.BorderWidth = 1;
                cellTransportadora2.Padding = 8f;
                cellTransportadora2.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellTransportadora2.Colspan = 2;
                tabelaTransportadora.AddCell(cellTransportadora2);

                string transportadoraLinha3 = $"Emissão: {orcamento.Transportadora.Emissao}";
                iTextSharp.text.pdf.PdfPCell cellTransportadora3 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(transportadoraLinha3, fNormal));
                cellTransportadora3.BorderWidth = 1;
                cellTransportadora3.Padding = 8f;
                cellTransportadora3.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellTransportadora3.Colspan = 2;
                tabelaTransportadora.AddCell(cellTransportadora3);

                string transportadoraLinha4 = $"Condição pagamento: {orcamento.Transportadora.CondicaoPagamento}    " +
                                             $"Vendedor: {orcamento.Transportadora.Vendedor}";
                iTextSharp.text.pdf.PdfPCell cellTransportadora4 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(transportadoraLinha4, fNormal));
                cellTransportadora4.BorderWidth = 1;
                cellTransportadora4.Padding = 8f;
                cellTransportadora4.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellTransportadora4.Colspan = 2;
                tabelaTransportadora.AddCell(cellTransportadora4);

                string transportadoraLinha5 = $"Observações: {orcamento.Transportadora.Observacoes}";
                iTextSharp.text.pdf.PdfPCell cellTransportadora5 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(transportadoraLinha5, fNormal));
                cellTransportadora5.BorderWidth = 1;
                cellTransportadora5.Padding = 8f;
                cellTransportadora5.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellTransportadora5.Colspan = 2;
                tabelaTransportadora.AddCell(cellTransportadora5);

                doc.Add(tabelaTransportadora);

                // Linha separadora
                doc.Add(new iTextSharp.text.Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100, iTextSharp.text.BaseColor.BLACK, iTextSharp.text.Element.ALIGN_CENTER, -1)));
                doc.Add(iTextSharp.text.Chunk.NEWLINE);

                // --- TABELA DE ITENS ---
                iTextSharp.text.pdf.PdfPTable tabelaItens = new iTextSharp.text.pdf.PdfPTable(8);
                tabelaItens.WidthPercentage = 100;
                tabelaItens.SetWidths(new float[] { 10, 10, 25, 6, 10, 12, 12, 9 });
                tabelaItens.SpacingAfter = 15f;

                string[] headers = { "Item", "Código", "Descrição", "UN", "Quantidade", "Preço", "Desconto", "Total" };

                AdicionarCelulaComBorda(tabelaItens, headers[0], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[1], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[2], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[3], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[4], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[5], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[6], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, headers[7], fNegrito, iTextSharp.text.Element.ALIGN_CENTER);

                int itemNumero = 1;
                decimal valorTotalProdutos = 0;

                foreach (var item in orcamento.Itens)
                {
                    decimal quantidade = item.QuantidadeDecimal;
                    decimal precoUnitario = item.PrecoDecimal;

                    decimal descontoPercentual = 0;
                    if (!string.IsNullOrEmpty(item.Desconto) && item.Desconto.EndsWith("%"))
                    {
                        string descontoStr = item.Desconto.Replace("%", "").Trim();
                        decimal.TryParse(descontoStr, out descontoPercentual);
                    }

                    decimal valorDesconto = (quantidade * precoUnitario) * (descontoPercentual / 100);
                    decimal totalItem = (quantidade * precoUnitario) - valorDesconto;
                    valorTotalProdutos += totalItem;

                    AdicionarCelulaComBorda(tabelaItens, item.Item, fNormal, iTextSharp.text.Element.ALIGN_CENTER);
                    AdicionarCelulaComBorda(tabelaItens, item.Codigo, fNormal, iTextSharp.text.Element.ALIGN_CENTER);
                    AdicionarCelulaComBorda(tabelaItens, item.Descricao, fNormal, iTextSharp.text.Element.ALIGN_LEFT);
                    AdicionarCelulaComBorda(tabelaItens, item.Unidade, fNormal, iTextSharp.text.Element.ALIGN_CENTER);
                    AdicionarCelulaComBorda(tabelaItens, quantidade.ToString("N2"), fNormal, iTextSharp.text.Element.ALIGN_CENTER);
                    AdicionarCelulaComBorda(tabelaItens, precoUnitario.ToString("N2"), fNormal, iTextSharp.text.Element.ALIGN_RIGHT);
                    AdicionarCelulaComBorda(tabelaItens, item.Desconto, fNormal, iTextSharp.text.Element.ALIGN_CENTER);
                    AdicionarCelulaComBorda(tabelaItens, totalItem.ToString("N2"), fNormal, iTextSharp.text.Element.ALIGN_RIGHT);

                    itemNumero++;
                }

                doc.Add(tabelaItens);

                // --- RESUMO FINANCEIRO ---
                iTextSharp.text.pdf.PdfPTable tabelaResumo = new iTextSharp.text.pdf.PdfPTable(2);
                tabelaResumo.WidthPercentage = 100;
                tabelaResumo.SetWidths(new float[] { 70, 30 });
                tabelaResumo.SpacingAfter = 10f;

                iTextSharp.text.pdf.PdfPCell cellInfoAdicional = new iTextSharp.text.pdf.PdfPCell();
                cellInfoAdicional.BorderWidth = 1;
                cellInfoAdicional.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellInfoAdicional.Padding = 5f;

                iTextSharp.text.Paragraph pEstoque = new iTextSharp.text.Paragraph("Resumo por unidade:", fNormal);
                pEstoque.SpacingAfter = 3f;
                cellInfoAdicional.AddElement(pEstoque);

                iTextSharp.text.Paragraph pVedada = new iTextSharp.text.Paragraph("É vedada a autenticação desses documentos", fPequeno);
                pVedada.SpacingAfter = 5f;
                cellInfoAdicional.AddElement(pVedada);

                tabelaResumo.AddCell(cellInfoAdicional);

                iTextSharp.text.pdf.PdfPCell cellValores = new iTextSharp.text.pdf.PdfPCell();
                cellValores.BorderWidth = 1;
                cellValores.BorderColor = iTextSharp.text.BaseColor.BLACK;
                cellValores.Padding = 5f;

                // Valor dos produtos
                AdicionarLinhaResumoSimples(cellValores, "Valor dos produtos:", valorTotalProdutos.ToString("N2"), fNormal);

                // Valor do deslocamento (se houver)
                if (orcamento.Deslocamento != null && orcamento.Deslocamento.ValorDeslocamento > 0)
                {
                    AdicionarLinhaResumoSimples(cellValores, "Valor do deslocamento:", orcamento.Deslocamento.ValorDeslocamento.ToString("N2"), fNormal);
                }

                // NOVO: Valor da mão de obra (se houver)
                if (_valorMaoDeObra > 0)
                {
                    AdicionarLinhaResumoSimples(cellValores, "Valor da mão de obra:", _valorMaoDeObra.ToString("N2"), fNormal);
                }

                // Valor líquido (total geral)
                decimal valorLiquido = valorTotalProdutos +
                                      (orcamento.Deslocamento?.ValorDeslocamento ?? 0) +
                                      _valorMaoDeObra; // ATUALIZADO
                AdicionarLinhaResumoSimples(cellValores, "Valor líquido:", valorLiquido.ToString("N2"), fNegrito);

                tabelaResumo.AddCell(cellValores);

                doc.Add(tabelaResumo);

                // --- ASSINATURA DO CLIENTE ---
                doc.Add(iTextSharp.text.Chunk.NEWLINE);
                doc.Add(iTextSharp.text.Chunk.NEWLINE);

                // Linha para assinatura
                iTextSharp.text.pdf.draw.LineSeparator linhaAssinatura = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, iTextSharp.text.BaseColor.BLACK, iTextSharp.text.Element.ALIGN_CENTER, -1);
                doc.Add(linhaAssinatura);

                // Texto da assinatura
                iTextSharp.text.Paragraph textoAssinatura = new iTextSharp.text.Paragraph("Assinatura do Cliente", fNormal);
                textoAssinatura.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                textoAssinatura.SpacingBefore = 5f;
                textoAssinatura.SpacingAfter = 20f;
                doc.Add(textoAssinatura);

                // --- DECLARAÇÃO DE CIÊNCIA ---
                iTextSharp.text.Paragraph declaracao = new iTextSharp.text.Paragraph(
                    "Declaro ter ciência das condições deste orçamento e concordo com os termos apresentados.",
                    new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.ITALIC));
                declaracao.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                declaracao.SpacingBefore = 10f;
                declaracao.SpacingAfter = 15f;
                doc.Add(declaracao);

                iTextSharp.text.Paragraph rodape = new iTextSharp.text.Paragraph($"Página 1 de 1", fPequeno);
                rodape.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                rodape.SpacingBefore = 10f;
                doc.Add(rodape);

                doc.Close();

                doc.Close();

                try
                {
                    var rel = new GestãoEstoque.ClassesOrganização.RelatorioOS
                    {
                        Cliente = orcamento.Destinatario.Nome,
                        Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        ValorTotal = valorLiquido, 
                        CaminhoArquivo = caminhoArquivo,
                        Tipo = "OS" 
                    };
                    GestãoEstoque.ClassesOrganização.RelatorioOSmanager.Registrar(rel);
                }
                catch
                {
                    // Ignora erro no registro
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = caminhoArquivo,
                    UseShellExecute = true
                });

                MessageBox.Show("Orçamento gerado com sucesso e estoque atualizado!", "PDF Criado");

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao emitir OS: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        // Métodos auxiliares para adicionar células
        private void AdicionarCelulaComBorda(iTextSharp.text.pdf.PdfPTable tabela, string texto, iTextSharp.text.Font fonte, int alinhamento)
        {
            iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(texto, fonte));
            cell.BorderWidth = 1;
            cell.BorderColor = iTextSharp.text.BaseColor.BLACK;
            cell.Padding = 5f;
            cell.HorizontalAlignment = alinhamento;
            tabela.AddCell(cell);
        }

        private void AdicionarLinhaResumoSimples(iTextSharp.text.pdf.PdfPCell cell, string label, string valor, iTextSharp.text.Font fonte)
        {
            iTextSharp.text.Paragraph p = new iTextSharp.text.Paragraph();

            iTextSharp.text.pdf.PdfPTable tabelaLinha = new iTextSharp.text.pdf.PdfPTable(2);
            tabelaLinha.WidthPercentage = 100;
            tabelaLinha.SetWidths(new float[] { 70, 30 });

            iTextSharp.text.pdf.PdfPCell cellLabel = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(label, fonte));
            cellLabel.BorderWidth = 0;
            cellLabel.Padding = 0;
            cellLabel.PaddingRight = 5f;
            cellLabel.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
            tabelaLinha.AddCell(cellLabel);

            iTextSharp.text.pdf.PdfPCell cellValor = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(valor, fonte));
            cellValor.BorderWidth = 0;
            cellValor.Padding = 0;
            cellValor.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
            tabelaLinha.AddCell(cellValor);

            cell.AddElement(tabelaLinha);
        }
    }
}