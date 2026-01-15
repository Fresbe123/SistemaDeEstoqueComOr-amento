using GestãoEstoque.ClassesOrganização;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestãoEstoque
{
    public partial class EmitirOS : Window
    {
        private int numeroOS;
        private ObservableCollection<ItemEstoque> ItensDisponiveis;
        private ObservableCollection<ItemEstoque> ItensSelecionados = new();
        private List<ItemEstoque> ItensParaDebitar = new List<ItemEstoque>();
        private decimal _valorDeslocamento = 0;
        private decimal _valorMaoDeObra = 0;

        public EmitirOS(ObservableCollection<ItemEstoque> itensDisponiveis)
        {
            InitializeComponent();

            int proximoNumeroOS = SequenciaManager.VisualizarProximaOS();
            txtNumeroOS.Text = $"OS-{proximoNumeroOS}";

            numeroOS = proximoNumeroOS;

            ItensDisponiveis = new ObservableCollection<ItemEstoque>(
                itensDisponiveis.Select(item => CriarCopiaItem(item)).ToList()
            );

            lstDisponiveis.ItemsSource = ItensDisponiveis;
            lstSelecionados.ItemsSource = ItensSelecionados;

            txtData.Text = DateTime.Now.ToString("dd/MM/yyyy");
            AtualizarTotalGeral();
        }

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

            decimal totalGeral = totalItens + _valorDeslocamento + _valorMaoDeObra;
            txtTotalOS.Text = totalGeral.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
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
                Desconto = descontoPercentual > 0 ? $"{descontoPercentual:N2}%" : "0%",
                Total = totalComDesconto.ToString("N2"),
                Favorito = item.Favorito
            };
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement elementWithFocus)
                {
                    elementWithFocus.MoveFocus(request);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
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

                MessageBox.Show("Item removido da OS.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Selecione um item para remover.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TxtKmPercorridos_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

        private void TxtValorPorKm_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

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
                    decimal.TryParse(txtKmPercorridos.Text, out kmPercorridos);

                if (!string.IsNullOrEmpty(txtValorPorKm.Text))
                {
                    string valorPorKmTexto = txtValorPorKm.Text.Replace("R$", "").Trim();
                    decimal.TryParse(valorPorKmTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out valorPorKm);
                }

                _valorDeslocamento = kmPercorridos * valorPorKm;
                txtValorDeslocamento.Text = _valorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
                AtualizarTotalGeral();
            }
            catch { }
        }

        private void CalcularMaoDeObra()
        {
            try
            {
                if (!string.IsNullOrEmpty(txtMaoDeObra.Text))
                {
                    string valorTexto = txtMaoDeObra.Text.Replace("R$", "").Trim();
                    if (decimal.TryParse(valorTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out decimal valor))
                        _valorMaoDeObra = valor;
                    else
                        _valorMaoDeObra = 0;
                }
                else
                    _valorMaoDeObra = 0;

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
                                         decimal kmPercorridos,
                                         decimal valorPorKm)
        {
            foreach (var item in itensOrcamento)
            {
                var itemCopia = CriarCopiaItem(item);
                ItensSelecionados.Add(itemCopia);

                ItensParaDebitar.Add(new ItemEstoque
                {
                    Codigo = itemCopia.Codigo,
                    Quantidade = itemCopia.Quantidade,
                    Descricao = itemCopia.Descricao
                });
            }

            _valorDeslocamento = valorDeslocamento;
            _valorMaoDeObra = valorMaoDeObra;

            txtKmPercorridos.Text = kmPercorridos.ToString();
            txtValorPorKm.Text = valorPorKm.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            txtValorDeslocamento.Text = valorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            txtMaoDeObra.Text = valorMaoDeObra.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

            AtualizarTotalGeral();
            lstSelecionados.Items.Refresh();
        }

        private void AtualizarEstoqueReal()
        {
            foreach (var itemDebito in ItensParaDebitar)
            {
                var itemOriginal = ArmazenamentoTemporario.Itens
                    .FirstOrDefault(i => i.Codigo == itemDebito.Codigo);

                if (itemOriginal != null)
                {
                    decimal quantidadeDebitar = itemDebito.QuantidadeDecimal;
                    decimal estoqueAtual = itemOriginal.QuantidadeDecimal;
                    decimal novoEstoque = estoqueAtual - quantidadeDebitar;

                    if (novoEstoque >= 0)
                    {
                        itemOriginal.Quantidade = novoEstoque.ToString("N2");
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
                AtualizarEstoqueReal();
                AtualizarMainWindow();

                var ordemServico = new Informações.OrdemServico();
                ordemServico.NumeroOS = numeroOS;
                ordemServico.DataEmissao = DateTime.Now;

                ordemServico.Emitente.Nome = txtEmitenteNome.Text;
                ordemServico.Emitente.CNPJ = txtEmitenteCNPJ.Text;
                ordemServico.Emitente.Endereco = txtEmitenteEndereco.Text;
                ordemServico.Emitente.Bairro = txtEmitenteBairro.Text;
                ordemServico.Emitente.Cidade = txtEmitenteCidade.Text;
                ordemServico.Emitente.Estado = txtEmitenteEstado.Text;
                ordemServico.Emitente.CEP = txtEmitenteCEP.Text;
                ordemServico.Emitente.Telefone = txtEmitenteTelefone.Text;

                ordemServico.Destinatario.Nome = txtCliente.Text;
                ordemServico.Destinatario.CNPJ_CPF = txtCpfCnpj.Text;
                ordemServico.Destinatario.Endereco = txtEndereco.Text;
                ordemServico.Destinatario.Bairro = txtBairro.Text;
                ordemServico.Destinatario.Cidade = txtCidade.Text;
                ordemServico.Destinatario.CEP = txtCEP.Text;
                ordemServico.Destinatario.Telefone = txtTelefone.Text;
                ordemServico.Destinatario.Email = txtEmail.Text;

                ordemServico.DadosVeiculo.Data = txtData.Text;
                ordemServico.DadosVeiculo.HoraKM = txtHoraKm.Text;
                ordemServico.DadosVeiculo.Veiculo = txtVeiculo.Text;
                ordemServico.DadosVeiculo.Modelo = txtModelo.Text;
                ordemServico.DadosVeiculo.Cliente = txtCliente.Text;
                ordemServico.DadosVeiculo.Telefone = txtTelefone.Text;

                ordemServico.Deslocamento = new Informações.Deslocamento();
                ordemServico.Deslocamento.KmPercorridos = string.IsNullOrEmpty(txtKmPercorridos.Text) ? 0 : decimal.Parse(txtKmPercorridos.Text);

                string valorPorKmTexto = txtValorPorKm.Text.Replace("R$", "").Trim();
                ordemServico.Deslocamento.ValorPorKm = string.IsNullOrEmpty(valorPorKmTexto) ? 0 :
                    decimal.Parse(valorPorKmTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"));

                ordemServico.Deslocamento.ValorDeslocamento = _valorDeslocamento;

                ordemServico.ValorMaoDeObra = _valorMaoDeObra;
                ordemServico.Itens = ItensSelecionados.ToList();
                ordemServico.Vendedor = txtVendedor.Text;
                ordemServico.Observacoes = txtObservacoes.Text;

                decimal valorTotalProdutos = ItensSelecionados.Sum(item =>
                    decimal.TryParse(item.Total, out var total) ? total : 0);
                ordemServico.ValorTotal = valorTotalProdutos + _valorDeslocamento + _valorMaoDeObra;

                string caminhoArquivo = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"OS_{numeroOS}.pdf"
                );

                GerarPDFOrdemServico(ordemServico, caminhoArquivo);

                try
                {
                    var rel = new RelatorioOS
                    {
                        Cliente = ordemServico.Destinatario.Nome,
                        Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        ValorTotal = ordemServico.ValorTotal,
                        CaminhoArquivo = caminhoArquivo,
                        Tipo = "OS",
                        NumeroDocumento = numeroOS.ToString()
                    };
                    RelatorioOSmanager.Registrar(rel);
                }
                catch { }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = caminhoArquivo,
                    UseShellExecute = true
                });

                MessageBox.Show($"OS Nº {numeroOS} emitida com sucesso e estoque atualizado!", "OS Criada");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao emitir OS: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GerarPDFOrdemServico(Informações.OrdemServico os, string caminhoArquivo)
        {
            Document doc = new Document(PageSize.A4, 20, 20, 60, 20);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(caminhoArquivo, FileMode.Create));
            writer.PageEvent = new PdfLogoHelper2();
            doc.Open();

            Font fTituloPrincipal = new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD, BaseColor.BLACK);
            Font fTituloSecao = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.BLACK);
            Font fNormal = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.BLACK);
            Font fNegrito = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.BLACK);
            Font fPequeno = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, BaseColor.BLACK);

            Paragraph tituloDoc = new Paragraph($"ORDEM DE SERVIÇO Nº {os.NumeroOS}", fTituloPrincipal);
            tituloDoc.Alignment = Element.ALIGN_RIGHT;
            tituloDoc.SpacingAfter = 10f;
            doc.Add(tituloDoc);

            Paragraph dataOS = new Paragraph($"Data de emissão: {os.DataEmissao:dd/MM/yyyy HH:mm}", fNormal);
            dataOS.Alignment = Element.ALIGN_CENTER;
            dataOS.SpacingAfter = 10f;
            doc.Add(dataOS);

            Paragraph tituloEmitente = new Paragraph("Identificação do Estabelecimento Emitente", fTituloSecao);
            tituloEmitente.SpacingAfter = 5f;
            doc.Add(tituloEmitente);

            PdfPTable tabelaEmitente = new PdfPTable(1);
            tabelaEmitente.WidthPercentage = 100;
            tabelaEmitente.SpacingAfter = 10f;

            string textoEmitente = $"Denominação: {os.Emitente.Nome}    CNPJ: {os.Emitente.CNPJ}\n" +
                                  $"Endereço: {os.Emitente.Endereco}\n" +
                                  $"Bairro: {os.Emitente.Bairro}    Cidade: {os.Emitente.Cidade}    Estado: {os.Emitente.Estado}\n" +
                                  $"CEP: {os.Emitente.CEP}    Telefone: {os.Emitente.Telefone}";

            PdfPCell cellEmitente = new PdfPCell(new Phrase(textoEmitente, fNormal));
            cellEmitente.BorderWidth = 1;
            cellEmitente.Padding = 8f;
            cellEmitente.BorderColor = BaseColor.BLACK;
            tabelaEmitente.AddCell(cellEmitente);
            doc.Add(tabelaEmitente);

            Paragraph tituloDestinatario = new Paragraph("Identificação do Destinatário", fTituloSecao);
            tituloDestinatario.SpacingAfter = 5f;
            doc.Add(tituloDestinatario);

            PdfPTable tabelaDestinatario = new PdfPTable(1);
            tabelaDestinatario.WidthPercentage = 100;
            tabelaDestinatario.SpacingAfter = 10f;

            string textoDestinatario = $"Nome: {os.Destinatario.Nome}    CNPJ/CPF: {os.Destinatario.CNPJ_CPF}\n" +
                                      $"Endereço: {os.Destinatario.Endereco}    Bairro: {os.Destinatario.Bairro}    Cidade: {os.Destinatario.Cidade}\n" +
                                      $"CEP: {os.Destinatario.CEP}    Telefone: {os.Destinatario.Telefone}    E-mail: {os.Destinatario.Email}";

            PdfPCell cellDestinatario = new PdfPCell(new Phrase(textoDestinatario, fNormal));
            cellDestinatario.BorderWidth = 1;
            cellDestinatario.Padding = 8f;
            cellDestinatario.BorderColor = BaseColor.BLACK;
            tabelaDestinatario.AddCell(cellDestinatario);
            doc.Add(tabelaDestinatario);

            PdfPTable tabelaDadosAdicionais = new PdfPTable(2);
            tabelaDadosAdicionais.WidthPercentage = 100;
            tabelaDadosAdicionais.SetWidths(new float[] { 50, 50 });
            tabelaDadosAdicionais.SpacingAfter = 10f;

            PdfPCell cellTitulo = new PdfPCell(new Phrase("Informações do Veículo/Serviço", fTituloSecao));
            cellTitulo.Colspan = 2;
            cellTitulo.BackgroundColor = new BaseColor(240, 240, 240);
            cellTitulo.BorderWidth = 1;
            cellTitulo.BorderColor = BaseColor.BLACK;
            cellTitulo.Padding = 8f;
            cellTitulo.HorizontalAlignment = Element.ALIGN_CENTER;
            tabelaDadosAdicionais.AddCell(cellTitulo);

            string dadosEsquerda = $"Data: {os.DadosVeiculo.Data}\n" +
                                  $"Hora/KM: {os.DadosVeiculo.HoraKM}\n" +
                                  $"Veículo: {os.DadosVeiculo.Veiculo}";

            PdfPCell cellDadosEsquerda = new PdfPCell(new Phrase(dadosEsquerda, fNormal));
            cellDadosEsquerda.BorderWidth = 1;
            cellDadosEsquerda.Padding = 8f;
            cellDadosEsquerda.BorderColor = BaseColor.BLACK;
            tabelaDadosAdicionais.AddCell(cellDadosEsquerda);

            string dadosDireita = $"Cliente: {os.DadosVeiculo.Cliente}\n" +
                                 $"Telefone: {os.DadosVeiculo.Telefone}\n" +
                                 $"Modelo: {os.DadosVeiculo.Modelo}";

            PdfPCell cellDadosDireita = new PdfPCell(new Phrase(dadosDireita, fNormal));
            cellDadosDireita.BorderWidth = 1;
            cellDadosDireita.Padding = 8f;
            cellDadosDireita.BorderColor = BaseColor.BLACK;
            tabelaDadosAdicionais.AddCell(cellDadosDireita);

            doc.Add(tabelaDadosAdicionais);

            if (os.Deslocamento != null && os.Deslocamento.ValorDeslocamento > 0)
            {
                Paragraph tituloDeslocamento = new Paragraph("Deslocamento", fTituloSecao);
                tituloDeslocamento.SpacingAfter = 5f;
                doc.Add(tituloDeslocamento);

                PdfPTable tabelaDeslocamento = new PdfPTable(3);
                tabelaDeslocamento.WidthPercentage = 100;
                tabelaDeslocamento.SetWidths(new float[] { 33, 33, 34 });
                tabelaDeslocamento.SpacingAfter = 10f;

                AdicionarCelulaComBorda(tabelaDeslocamento, $"KM Percorridos: {os.Deslocamento.KmPercorridos}", fNormal, Element.ALIGN_LEFT);
                AdicionarCelulaComBorda(tabelaDeslocamento, $"Valor por KM: {os.Deslocamento.ValorPorKm:C}", fNormal, Element.ALIGN_LEFT);
                AdicionarCelulaComBorda(tabelaDeslocamento, $"Valor do Deslocamento: {os.Deslocamento.ValorDeslocamento:C}", fNegrito, Element.ALIGN_RIGHT);
                doc.Add(tabelaDeslocamento);
            }

            if (os.ValorMaoDeObra > 0)
            {
                Paragraph tituloMaoDeObra = new Paragraph("Mão de Obra", fTituloSecao);
                tituloMaoDeObra.SpacingAfter = 5f;
                doc.Add(tituloMaoDeObra);

                PdfPTable tabelaMaoDeObra = new PdfPTable(1);
                tabelaMaoDeObra.WidthPercentage = 100;
                tabelaMaoDeObra.SpacingAfter = 10f;

                string textoMaoDeObra = $"Valor da Mão de Obra: {os.ValorMaoDeObra:C}";
                PdfPCell cellMaoDeObra = new PdfPCell(new Phrase(textoMaoDeObra, fNegrito));
                cellMaoDeObra.BorderWidth = 1;
                cellMaoDeObra.Padding = 8f;
                cellMaoDeObra.BorderColor = BaseColor.BLACK;
                tabelaMaoDeObra.AddCell(cellMaoDeObra);
                doc.Add(tabelaMaoDeObra);
            }

            PdfPTable tabelaInfoAdicional = new PdfPTable(2);
            tabelaInfoAdicional.WidthPercentage = 100;
            tabelaInfoAdicional.SetWidths(new float[] { 50, 50 });
            tabelaInfoAdicional.SpacingAfter = 10f;

            string infoVendedor = $"Vendedor: {os.Vendedor}";
            string infoObservacoes = $"Observações: {os.Observacoes}";

            PdfPCell cellVendedor = new PdfPCell(new Phrase(infoVendedor, fNormal));
            cellVendedor.BorderWidth = 1;
            cellVendedor.Padding = 8f;
            cellVendedor.BorderColor = BaseColor.BLACK;
            tabelaInfoAdicional.AddCell(cellVendedor);

            PdfPCell cellObservacoes = new PdfPCell(new Phrase(infoObservacoes, fNormal));
            cellObservacoes.BorderWidth = 1;
            cellObservacoes.Padding = 8f;
            cellObservacoes.BorderColor = BaseColor.BLACK;
            tabelaInfoAdicional.AddCell(cellObservacoes);
            doc.Add(tabelaInfoAdicional);

            doc.Add(new Chunk(new LineSeparator(1f, 100, BaseColor.BLACK, Element.ALIGN_CENTER, -1)));
            doc.Add(Chunk.NEWLINE);

            PdfPTable tabelaItens = new PdfPTable(8);
            tabelaItens.WidthPercentage = 100;
            tabelaItens.SetWidths(new float[] { 10, 10, 25, 6, 10, 12, 12, 9 });
            tabelaItens.SpacingAfter = 15f;

            string[] headers = { "Item", "Código", "Descrição", "UN", "Quantidade", "Preço", "Desconto", "Total" };
            foreach (var header in headers)
                AdicionarCelulaComBorda(tabelaItens, header, fNegrito, Element.ALIGN_CENTER);

            decimal valorTotalProdutos = 0;
            foreach (var item in os.Itens)
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

                AdicionarCelulaComBorda(tabelaItens, item.Item, fNormal, Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, item.Codigo, fNormal, Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, item.Descricao, fNormal, Element.ALIGN_LEFT);
                AdicionarCelulaComBorda(tabelaItens, item.Unidade, fNormal, Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, quantidade.ToString("N2"), fNormal, Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, precoUnitario.ToString("N2"), fNormal, Element.ALIGN_RIGHT);
                AdicionarCelulaComBorda(tabelaItens, item.Desconto, fNormal, Element.ALIGN_CENTER);
                AdicionarCelulaComBorda(tabelaItens, totalItem.ToString("N2"), fNormal, Element.ALIGN_RIGHT);
            }

            doc.Add(tabelaItens);

            PdfPTable tabelaResumo = new PdfPTable(2);
            tabelaResumo.WidthPercentage = 100;
            tabelaResumo.SetWidths(new float[] { 70, 30 });
            tabelaResumo.SpacingAfter = 10f;

            PdfPCell cellInfoAdicional = new PdfPCell();
            cellInfoAdicional.BorderWidth = 1;
            cellInfoAdicional.BorderColor = BaseColor.BLACK;
            cellInfoAdicional.Padding = 5f;

            Paragraph pEstoque = new Paragraph("Resumo por unidade:", fNormal);
            pEstoque.SpacingAfter = 3f;
            cellInfoAdicional.AddElement(pEstoque);

            Paragraph pVedada = new Paragraph("É vedada a autenticação desses documentos", fPequeno);
            pVedada.SpacingAfter = 5f;
            cellInfoAdicional.AddElement(pVedada);
            tabelaResumo.AddCell(cellInfoAdicional);

            PdfPCell cellValores = new PdfPCell();
            cellValores.BorderWidth = 1;
            cellValores.BorderColor = BaseColor.BLACK;
            cellValores.Padding = 5f;

            AdicionarLinhaResumoSimples(cellValores, "Valor dos produtos:", valorTotalProdutos.ToString("N2"), fNormal);

            if (os.Deslocamento != null && os.Deslocamento.ValorDeslocamento > 0)
                AdicionarLinhaResumoSimples(cellValores, "Valor do deslocamento:", os.Deslocamento.ValorDeslocamento.ToString("N2"), fNormal);

            if (os.ValorMaoDeObra > 0)
                AdicionarLinhaResumoSimples(cellValores, "Valor da mão de obra:", os.ValorMaoDeObra.ToString("N2"), fNormal);

            AdicionarLinhaResumoSimples(cellValores, "VALOR TOTAL DA OS:", os.ValorTotal.ToString("N2"), fNegrito);
            tabelaResumo.AddCell(cellValores);
            doc.Add(tabelaResumo);

            doc.Add(Chunk.NEWLINE);
            doc.Add(Chunk.NEWLINE);

            PdfPTable tabelaAssinaturas = new PdfPTable(2);
            tabelaAssinaturas.WidthPercentage = 100;
            tabelaAssinaturas.SetWidths(new float[] { 50, 50 });
            tabelaAssinaturas.SpacingBefore = 5f;
            tabelaAssinaturas.SpacingAfter = 5f;

            PdfPCell cellClienteAss = new PdfPCell();
            cellClienteAss.BorderWidth = 0;
            cellClienteAss.Padding = 3f;
            cellClienteAss.HorizontalAlignment = Element.ALIGN_CENTER;

            LineSeparator linhaCliente1 = new LineSeparator(0.8f, 80f, BaseColor.BLACK, Element.ALIGN_CENTER, 0);
            LineSeparator linhaCliente2 = new LineSeparator(0.8f, 80f, BaseColor.BLACK, Element.ALIGN_CENTER, 0);
            linhaCliente2.Offset = -3f;

            Paragraph textoClientePara = new Paragraph("Assinatura do Cliente", fNormal);
            textoClientePara.Alignment = Element.ALIGN_CENTER;
            textoClientePara.SpacingBefore = 1f;

            cellClienteAss.AddElement(linhaCliente1);
            cellClienteAss.AddElement(linhaCliente2);
            cellClienteAss.AddElement(textoClientePara);

            PdfPCell cellEmitenteAss = new PdfPCell();
            cellEmitenteAss.BorderWidth = 0;
            cellEmitenteAss.Padding = 3f;
            cellEmitenteAss.HorizontalAlignment = Element.ALIGN_CENTER;

            LineSeparator linhaEmitente1 = new LineSeparator(0.8f, 80f, BaseColor.BLACK, Element.ALIGN_CENTER, 0);
            LineSeparator linhaEmitente2 = new LineSeparator(0.8f, 80f, BaseColor.BLACK, Element.ALIGN_CENTER, 0);
            linhaEmitente2.Offset = -3f;

            Paragraph textoEmitentePara = new Paragraph("Assinatura do Emitente", fNormal);
            textoEmitentePara.Alignment = Element.ALIGN_CENTER;
            textoEmitentePara.SpacingBefore = 1f;

            cellEmitenteAss.AddElement(linhaEmitente1);
            cellEmitenteAss.AddElement(linhaEmitente2);
            cellEmitenteAss.AddElement(textoEmitentePara);

            tabelaAssinaturas.AddCell(cellClienteAss);
            tabelaAssinaturas.AddCell(cellEmitenteAss);
            doc.Add(tabelaAssinaturas);

            Paragraph rodape = new Paragraph($"Página 1 de 1", fPequeno);
            rodape.Alignment = Element.ALIGN_CENTER;
            rodape.SpacingBefore = 10f;
            doc.Add(rodape);

            doc.Close();
        }

        private void AdicionarCelulaComBorda(PdfPTable tabela, string texto, Font fonte, int alinhamento)
        {
            PdfPCell cell = new PdfPCell(new Phrase(texto, fonte));
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.BLACK;
            cell.Padding = 5f;
            cell.HorizontalAlignment = alinhamento;
            tabela.AddCell(cell);
        }

        private void AdicionarLinhaResumoSimples(PdfPCell cell, string label, string valor, Font fonte)
        {
            Paragraph p = new Paragraph();
            PdfPTable tabelaLinha = new PdfPTable(2);
            tabelaLinha.WidthPercentage = 100;
            tabelaLinha.SetWidths(new float[] { 70, 30 });

            PdfPCell cellLabel = new PdfPCell(new Phrase(label, fonte));
            cellLabel.BorderWidth = 0;
            cellLabel.Padding = 0;
            cellLabel.PaddingRight = 5f;
            cellLabel.HorizontalAlignment = Element.ALIGN_LEFT;
            tabelaLinha.AddCell(cellLabel);

            PdfPCell cellValor = new PdfPCell(new Phrase(valor, fonte));
            cellValor.BorderWidth = 0;
            cellValor.Padding = 0;
            cellValor.HorizontalAlignment = Element.ALIGN_RIGHT;
            tabelaLinha.AddCell(cellValor);

            cell.AddElement(tabelaLinha);
        }
    }
}