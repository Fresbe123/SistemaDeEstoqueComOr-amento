using GestãoEstoque.ClassesOrganização;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.util;
using System.Windows;
using System.Windows.Controls;

namespace GestãoEstoque
{
    public partial class CriarOrcamento : Window
    {
        private ObservableCollection<ItemEstoque> ItensDisponiveis;
        private ObservableCollection<ItemEstoque> ItensSelecionados = new();
        private decimal _valorDeslocamento = 0;
        private decimal _valorMaoDeObra = 0;
        private string _numeroOrcamento;
        private string _clienteNome = "";

        public string ClienteNome
        {
            get { return _clienteNome; }
            set { _clienteNome = value; }
        }

        public CriarOrcamento(ObservableCollection<ItemEstoque> itensDisponiveis)
        {
            InitializeComponent();
            VerificarControles();

            // Não gerar número do orçamento
            _numeroOrcamento = "A definir";

            // Inicializar campo cliente
            txtCliente.Text = "";
            txtCliente.Focus();

            // Criar cópias dos itens
            ItensDisponiveis = new ObservableCollection<ItemEstoque>(
                itensDisponiveis.Select(item => CriarCopiaItem(item)).ToList()
            );

            // Configurar listas
            lstDisponiveis.ItemsSource = ItensDisponiveis;
            lstSelecionados.ItemsSource = ItensSelecionados;

            // Definir o DataContext para binding
            this.DataContext = this;

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

        private void GerarNumeroOrcamento()
        {
            if (_numeroOrcamento == "A definir")
            {
                int numeroOrcamentoInt = SequenciaManager.ObterProximoOrcamento();
                _numeroOrcamento = $"ORÇ-{numeroOrcamentoInt}";
            }
        }

        private void AtualizarTotalGeral()
        {
            decimal totalItens = ItensSelecionados.Sum(item =>
                decimal.TryParse(item.Total, out decimal totalItem) ? totalItem : 0);

            decimal totalGeral = totalItens + _valorDeslocamento + _valorMaoDeObra;

            txtTotalOS.Text = totalGeral.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        // Botão Adicionar
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lstDisponiveis.SelectedItem is ItemEstoque itemSelecionado)
            {
                var janelaQuantidade = new SolicitarQuantidadeOrcamento(itemSelecionado);

                if (janelaQuantidade.ShowDialog() == true)
                {
                    decimal quantidade = janelaQuantidade.Quantidade;
                    decimal desconto = janelaQuantidade.DescontoPercentual;

                    var itemOrcamento = CriarItemOrcamento(itemSelecionado, quantidade, desconto);
                    ItensSelecionados.Add(itemOrcamento);

                    lstDisponiveis.Items.Refresh();
                    AtualizarTotalGeral();

                    MessageBox.Show("Item adicionado ao orçamento!", "Sucesso");
                }
            }
            else
            {
                MessageBox.Show("Selecione um item para adicionar.");
            }
        }

        private ItemEstoque CriarItemOrcamento(ItemEstoque item, decimal quantidade, decimal descontoPercentual)
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

        // Botão Remover
        private void BtnRemover_Click(object sender, RoutedEventArgs e)
        {
            if (lstSelecionados.SelectedItem is ItemEstoque itemSelecionado)
            {
                ItensSelecionados.Remove(itemSelecionado);
                AtualizarTotalGeral();
                MessageBox.Show("Item removido do orçamento.");
            }
            else
            {
                MessageBox.Show("Selecione um item para remover.");
            }
        }

        // Deslocamento
        private void TxtKmPercorridos_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

        private void TxtValorPorKm_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularDeslocamento();
        }

        // Mão de Obra
        private void TxtMaoDeObra_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularMaoDeObra();
        }

        private void CalcularDeslocamento()
        {
            try
            {
                if (txtKmPercorridos == null || txtValorPorKm == null || txtValorDeslocamento == null)
                    return;

                decimal km = 0;
                decimal valorKm = 0;

                if (!string.IsNullOrEmpty(txtKmPercorridos.Text))
                {
                    decimal.TryParse(txtKmPercorridos.Text, out km);
                }

                if (!string.IsNullOrEmpty(txtValorPorKm.Text))
                {
                    string valorTexto = txtValorPorKm.Text.Replace("R$", "").Trim();
                    decimal.TryParse(valorTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out valorKm);
                }

                _valorDeslocamento = km * valorKm;

                if (txtValorDeslocamento != null)
                {
                    txtValorDeslocamento.Text = _valorDeslocamento.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
                }

                AtualizarTotalGeral();
            }
            catch (Exception ex)
            {
                _valorDeslocamento = 0;
                if (txtValorDeslocamento != null)
                {
                    txtValorDeslocamento.Text = "R$ 0,00";
                }
                AtualizarTotalGeral();
                Console.WriteLine($"Erro no cálculo de deslocamento: {ex.Message}");
            }
        }

        private void CalcularMaoDeObra()
        {
            try
            {
                if (txtMaoDeObra == null) return;

                if (!string.IsNullOrEmpty(txtMaoDeObra.Text))
                {
                    string valorTexto = txtMaoDeObra.Text.Replace("R$", "").Trim();
                    decimal.TryParse(valorTexto, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out _valorMaoDeObra);
                }
                else
                {
                    _valorMaoDeObra = 0;
                }

                AtualizarTotalGeral();
            }
            catch (Exception ex)
            {
                _valorMaoDeObra = 0;
                AtualizarTotalGeral();
                Console.WriteLine($"Erro no cálculo da mão de obra: {ex.Message}");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Salvar Orçamento
        private void BtnSalvarOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (ItensSelecionados.Count == 0)
            {
                MessageBox.Show("Nenhum item selecionado para o orçamento.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtCliente.Text.Trim()))
            {
                MessageBox.Show("Por favor, informe o nome do cliente.", "Campo Obrigatório",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCliente.Focus();
                return;
            }

            try
            {
                GerarNumeroOrcamento();

                string caminhoArquivo = GerarPdfOrcamento();

                SalvarDadosOrcamentoCompleto(caminhoArquivo);

                var rel = new GestãoEstoque.ClassesOrganização.RelatorioOS
                {
                    Cliente = txtCliente.Text.Trim(),
                    Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    ValorTotal = CalcularTotalGeral(),
                    CaminhoArquivo = caminhoArquivo,
                    Tipo = "Orçamento",
                    NumeroDocumento = _numeroOrcamento
                };

                GestãoEstoque.ClassesOrganização.RelatorioOSmanager.Registrar(rel);

                var result = MessageBox.Show(
                    $"Orçamento salvo com sucesso!\n\n" +
                    $"Número: {_numeroOrcamento}\n" +
                    $"Cliente: {txtCliente.Text}\n" +
                    $"Valor Total: {CalcularTotalGeral():C}\n\n" +
                    $"Deseja salvar uma cópia em outra pasta?",
                    "Orçamento Salvo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SalvarCopiaOrcamento(caminhoArquivo);
                }

                MessageBox.Show($"Orçamento salvo no sistema!\nNúmero: {_numeroOrcamento}", "Sucesso",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar orçamento: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CarregarRascunho(DadosOrcamento dados)
        {
            if (dados == null) return;

            txtCliente.Text = dados.Cliente;
            _numeroOrcamento = dados.NumeroOrcamento;

            ItensSelecionados.Clear();

            if (!string.IsNullOrEmpty(dados.NumeroOrcamento))
            {
                _numeroOrcamento = dados.NumeroOrcamento;
            }
            else
            {
                _numeroOrcamento = "A definir";
            }



            foreach (var item in dados.Itens)
            {
                var itemOriginal = ItensDisponiveis.FirstOrDefault(i => i.Codigo == item.Codigo);
                if (itemOriginal != null)
                {
                    var itemOrcamento = CriarItemOrcamento(itemOriginal, item.Quantidade, item.Desconto);
                    ItensSelecionados.Add(itemOrcamento);
                }
            }

            txtKmPercorridos.Text = dados.KmPercorridos.ToString("N0");
            txtValorPorKm.Text = dados.ValorPorKm.ToString("N2");
            txtMaoDeObra.Text = dados.ValorMaoDeObra.ToString("N2");

            _valorDeslocamento = dados.ValorDeslocamento;
            _valorMaoDeObra = dados.ValorMaoDeObra;
            AtualizarTotalGeral();

            MessageBox.Show($"Rascunho carregado: {dados.Cliente}", "Rascunho Carregado",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public class DadosOrcamento
        {
            public string NumeroOrcamento { get; set; }
            public string Cliente { get; set; }
            public DateTime DataEmissao { get; set; }

            public List<ItemOrcamentoDados> Itens { get; set; }
            public decimal KmPercorridos { get; set; }
            public decimal ValorPorKm { get; set; }
            public decimal ValorDeslocamento { get; set; }
            public decimal ValorMaoDeObra { get; set; }

            public DadosOrcamento()
            {
                DataEmissao = DateTime.Now;
                int numero = SequenciaManager.ObterProximoOrcamento();
                NumeroOrcamento = $"ORÇ-{numero}";
            }

        }

        public class ItemOrcamentoDados
        {
            public string Codigo { get; set; }
            public string Nome { get; set; }
            public string Descricao { get; set; }
            public decimal Quantidade { get; set; }
            public decimal Preco { get; set; }
            public decimal Desconto { get; set; }
        }


        // Método para salvar cópia em local específico
        private void SalvarCopiaOrcamento(string caminhoOriginal)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = $"Orcamento_{txtCliente.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    DefaultExt = ".pdf",
                    Title = "Salvar Cópia do Orçamento"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(caminhoOriginal, saveDialog.FileName, true);
                    MessageBox.Show($"Cópia salva em:\n{saveDialog.FileName}", "Cópia Salva",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar cópia: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGerarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (ItensSelecionados.Count == 0)
            {
                MessageBox.Show("Nenhum item selecionado para o orçamento.");
                return;
            }

            // Verificar se o cliente foi preenchido
            if (string.IsNullOrEmpty(txtCliente.Text.Trim()))
            {
                MessageBox.Show("Por favor, informe o nome do cliente.");
                txtCliente.Focus();
                return;
            }

            try
            {
                GerarNumeroOrcamento();

                string caminhoArquivo = GerarPdfOrcamento();

                //Salvar dados completos do orçamento
                SalvarDadosOrcamentoCompleto(caminhoArquivo);

                // Registrar também quando gerar PDF (para manter compatibilidade)
                try
                {
                    var rel = new GestãoEstoque.ClassesOrganização.RelatorioOS
                    {
                        Cliente = txtCliente.Text.Trim(),
                        Data = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        ValorTotal = CalcularTotalGeral(),
                        CaminhoArquivo = caminhoArquivo,
                        Tipo = "Orçamento"
                    };
                    GestãoEstoque.ClassesOrganização.RelatorioOSmanager.Registrar(rel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao registrar orçamento: {ex.Message}");
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = caminhoArquivo,
                    UseShellExecute = true
                });

                MessageBox.Show($"Orçamento gerado com sucesso!\nNúmero: {_numeroOrcamento}\nCliente: {txtCliente.Text}", "PDF Criado");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar PDF: {ex.Message}", "Erro");
            }
        }

        private decimal CalcularTotalGeral()
        {
            decimal totalItens = ItensSelecionados.Sum(item =>
                decimal.TryParse(item.Total, out decimal totalItem) ? totalItem : 0);
            return totalItens + _valorDeslocamento + _valorMaoDeObra;
        }

        private string GerarPdfOrcamento()
        {
            string caminhoArquivo = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"ORÇAMENTO_{_numeroOrcamento.Replace("ORÇ-", "")}.pdf" // ORÇAMENTO_500.pdf
            );

            // Configurações do documento com margens REDUZIDAS
            iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 40, 35);

            // Cria o escritor PDF
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, new FileStream(caminhoArquivo, FileMode.Create));

            // Logo no Header
            writer.PageEvent = new PdfLogoHelper();

            doc.Open();

            // Cores para combinar com a logo
            BaseColor corAzul = new BaseColor(0, 51, 102);
            BaseColor corVerde = new BaseColor(0, 150, 100);

            // Fontes atualizadas com cores da logo
            iTextSharp.text.Font fTituloPrincipal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 18, iTextSharp.text.Font.BOLD, corAzul);
            iTextSharp.text.Font fTituloSecao = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 12, iTextSharp.text.Font.BOLD, corAzul);
            iTextSharp.text.Font fNormal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10);
            iTextSharp.text.Font fNegrito = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD);
            iTextSharp.text.Font fPequeno = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8);
            iTextSharp.text.Font fValor = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, corVerde);
            iTextSharp.text.Font fContato = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, corAzul);
            iTextSharp.text.Font fContatoDestaque = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, corVerde);

            // ========== INFORMAÇÕES DE CONTATO MAIS COMPACTAS ==========

            // CONTATOS ALINHADOS VERTICALMENTE com a logo
            iTextSharp.text.Paragraph contato = new iTextSharp.text.Paragraph();

            // Para alinhar os textos, posicioná-los ao lado direito da logo
            contato.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;

            // MUITO MENOS espaço no topo
            contato.SpacingBefore = 15f;

            // Email 
            contato.Add(new iTextSharp.text.Chunk("Email: ", fContato));
            contato.Add(new iTextSharp.text.Chunk("everest.arcondicionado20@gmail.com\n", fContatoDestaque));

            // Endereço (MENOS espaços)
            contato.Add(new iTextSharp.text.Chunk("Endereço: ", fContato));
            contato.Add(new iTextSharp.text.Chunk("ROD. BR 364/174, KM 380, S/N\n", fContatoDestaque));
            contato.Add(new iTextSharp.text.Chunk("     ", fContato)); // REDUZIDO
            contato.Add(new iTextSharp.text.Chunk("(Anexo Posto JK) Zona Rural - Comodoro/MT\n", fContatoDestaque));

            // Telefones
            contato.Add(new iTextSharp.text.Chunk("Telefones: ", fContato));
            contato.Add(new iTextSharp.text.Chunk("(69) 99238-8781 | (69) 99377-6707", fContatoDestaque));

            doc.Add(contato);

            // LINHA DIVISÓRIA (MENOS espaços)
            doc.Add(iTextSharp.text.Chunk.NEWLINE);
            // Remova esta linha se quiser ainda mais próximo: doc.Add(iTextSharp.text.Chunk.NEWLINE);

            iTextSharp.text.pdf.draw.LineSeparator linhaDivisoria = new iTextSharp.text.pdf.draw.LineSeparator(
                1.5f, 100f, corAzul, iTextSharp.text.Element.ALIGN_CENTER, 2f);
            doc.Add(linhaDivisoria);
            doc.Add(iTextSharp.text.Chunk.NEWLINE);

            // TÍTULO "ORÇAMENTO"
            iTextSharp.text.Paragraph titulo = new iTextSharp.text.Paragraph(
                $"ORÇAMENTO Nº {_numeroOrcamento}", fTituloPrincipal);
            titulo.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            titulo.SpacingAfter = 8f;
            doc.Add(titulo);

            // Informações do orçamento 
            iTextSharp.text.pdf.PdfPTable tabelaInfo = new iTextSharp.text.pdf.PdfPTable(2);
            tabelaInfo.WidthPercentage = 100;
            tabelaInfo.SetWidths(new float[] { 50, 50 });
            tabelaInfo.SpacingAfter = 12f;

            // Coluna esquerda - Dados do orçamento
            string dadosOrcamento = $"Número: {_numeroOrcamento}\n" +
                                   $"Data: {DateTime.Now:dd/MM/yyyy}\n" +
                                   $"Hora: {DateTime.Now:HH:mm}\n" +
                                   $"Validade: 30 dias";

            iTextSharp.text.pdf.PdfPCell cellDadosOrcamento = new iTextSharp.text.pdf.PdfPCell(
                new iTextSharp.text.Phrase(dadosOrcamento, fNormal));
            cellDadosOrcamento.BorderWidth = 1;
            cellDadosOrcamento.BorderColor = corAzul;
            cellDadosOrcamento.Padding = 10f;
            cellDadosOrcamento.BackgroundColor = new iTextSharp.text.BaseColor(230, 240, 255);
            tabelaInfo.AddCell(cellDadosOrcamento);

            // Coluna direita - Dados do cliente
            string dadosCliente = $"Cliente: {txtCliente.Text}\n" +
                                 $"Data: {DateTime.Now:dd/MM/yyyy}\n" +
                                 $"Orçamento: {_numeroOrcamento}\n" +
                                 $"Valor Total: {CalcularTotalGeral():C}";

            iTextSharp.text.pdf.PdfPCell cellDadosCliente = new iTextSharp.text.pdf.PdfPCell(
                new iTextSharp.text.Phrase(dadosCliente, fNormal));
            cellDadosCliente.BorderWidth = 1;
            cellDadosCliente.BorderColor = corVerde;
            cellDadosCliente.Padding = 10f;
            cellDadosCliente.BackgroundColor = new iTextSharp.text.BaseColor(230, 255, 240);
            tabelaInfo.AddCell(cellDadosCliente);

            doc.Add(tabelaInfo);

            // Espaçamento antes da seção de itens
            doc.Add(iTextSharp.text.Chunk.NEWLINE);

            // Itens do orçamento
            iTextSharp.text.Paragraph itensTitulo = new iTextSharp.text.Paragraph("ITENS DO ORÇAMENTO", fTituloSecao);
            itensTitulo.SpacingAfter = 8f;
            doc.Add(itensTitulo);

            // Tabela de itens
            iTextSharp.text.pdf.PdfPTable tabelaItens = new iTextSharp.text.pdf.PdfPTable(6);
            tabelaItens.WidthPercentage = 100;
            tabelaItens.SetWidths(new float[] { 8, 25, 22, 15, 15, 15 });
            tabelaItens.SpacingAfter = 15f;

            // Cabeçalho da tabela
            AdicionarCelulaTabela(tabelaItens, "ITEM", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));
            AdicionarCelulaTabela(tabelaItens, "NOME", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));
            AdicionarCelulaTabela(tabelaItens, "DESCRIÇÃO", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));
            AdicionarCelulaTabela(tabelaItens, "QUANTIDADE", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));
            AdicionarCelulaTabela(tabelaItens, "PREÇO UNIT.", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));
            AdicionarCelulaTabela(tabelaItens, "VALOR TOTAL", fNegrito, new iTextSharp.text.BaseColor(220, 220, 220));

            // Dados dos itens
            int itemNumero = 1;
            decimal subtotal = 0;

            foreach (var item in ItensSelecionados)
            {
                iTextSharp.text.BaseColor corFundo = (itemNumero % 2 == 0) ?
                    new iTextSharp.text.BaseColor(250, 250, 250) :
                    new iTextSharp.text.BaseColor(255, 255, 255);

                AdicionarCelulaTabela(tabelaItens, itemNumero.ToString(), fNormal, corFundo);
                AdicionarCelulaTabela(tabelaItens, item.Item, fNormal, corFundo);
                AdicionarCelulaTabela(tabelaItens, item.Descricao, fNormal, corFundo);
                AdicionarCelulaTabela(tabelaItens, item.Quantidade, fNormal, corFundo);
                AdicionarCelulaTabela(tabelaItens, $"R$ {decimal.Parse(item.Preco).ToString("N2")}", fNormal, corFundo);

                decimal totalItem = decimal.TryParse(item.Total, out decimal t) ? t : 0;
                subtotal += totalItem;
                AdicionarCelulaTabela(tabelaItens, $"R$ {totalItem.ToString("N2")}", fNegrito, corFundo);

                itemNumero++;
            }

            doc.Add(tabelaItens);

            // Seção de Custos Adicionais - ABAIXO DA TABELA DE ITENS
            if (_valorDeslocamento > 0 || _valorMaoDeObra > 0)
            {
                doc.Add(iTextSharp.text.Chunk.NEWLINE);

                iTextSharp.text.Paragraph custosTitulo = new iTextSharp.text.Paragraph("💰 CUSTOS ADICIONAIS", fTituloSecao);
                custosTitulo.SpacingAfter = 10f;
                custosTitulo.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
                custosTitulo.IndentationRight = 130f;
                doc.Add(custosTitulo);

                // Tabela de custos adicionais
                iTextSharp.text.pdf.PdfPTable tabelaCustos = new iTextSharp.text.pdf.PdfPTable(2);
                tabelaCustos.WidthPercentage = 50;
                tabelaCustos.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
                tabelaCustos.SetWidths(new float[] { 60, 40 });
                tabelaCustos.SpacingAfter = 15f;

                // Deslocamento (se houver)
                if (_valorDeslocamento > 0)
                {
                    decimal kmPercorridos = ObterKmPercorridos();
                    decimal valorPorKm = ObterValorPorKm();

                    AdicionarCelulaCustos(tabelaCustos, "KM Percorridos:", fNormal);
                    AdicionarCelulaCustos(tabelaCustos, $"{kmPercorridos.ToString("N0")} km", fNormal);

                    AdicionarCelulaCustos(tabelaCustos, "Valor por KM:", fNormal);
                    AdicionarCelulaCustos(tabelaCustos, $"R$ {valorPorKm.ToString("N2")}", fNormal);

                    AdicionarCelulaCustos(tabelaCustos, "Deslocamento:", fNegrito);
                    AdicionarCelulaCustos(tabelaCustos, $"R$ {_valorDeslocamento.ToString("N2")}", fNegrito);

                    // Linha separadora entre deslocamento e mão de obra
                    if (_valorMaoDeObra > 0)
                    {
                        AdicionarCelulaCustos(tabelaCustos, "", fNormal);
                        AdicionarCelulaCustos(tabelaCustos, "", fNormal);
                    }
                }

                // Mão de obra (se houver)
                if (_valorMaoDeObra > 0)
                {
                    AdicionarCelulaCustos(tabelaCustos, "Mão de Obra:", fNegrito);
                    AdicionarCelulaCustos(tabelaCustos, $"R$ {_valorMaoDeObra.ToString("N2")}", fNegrito);
                }

                doc.Add(tabelaCustos);
            }

            // Linha divisória antes do resumo
            iTextSharp.text.pdf.draw.LineSeparator linhaResumo = new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, new iTextSharp.text.BaseColor(150, 150, 150), iTextSharp.text.Element.ALIGN_CENTER, 8f);
            doc.Add(linhaResumo);

            // Resumo financeiro
            iTextSharp.text.pdf.PdfPTable tabelaResumo = new iTextSharp.text.pdf.PdfPTable(2);
            tabelaResumo.WidthPercentage = 45;
            tabelaResumo.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
            tabelaResumo.SetWidths(new float[] { 60, 40 });
            tabelaResumo.SpacingAfter = 15f;

            // Subtotal
            AdicionarCelulaResumo(tabelaResumo, "Subtotal:", fNormal, $"R$ {subtotal.ToString("N2")}", fNormal);

            // Custos adicionais (se houver)
            if (_valorDeslocamento > 0 || _valorMaoDeObra > 0)
            {
                if (_valorDeslocamento > 0)
                {
                    AdicionarCelulaResumo(tabelaResumo, "Deslocamento:", fNormal, $"R$ {_valorDeslocamento.ToString("N2")}", fNormal);
                }

                if (_valorMaoDeObra > 0)
                {
                    AdicionarCelulaResumo(tabelaResumo, "Mão de Obra:", fNormal, $"R$ {_valorMaoDeObra.ToString("N2")}", fNormal);
                }
            }

            // Linha separadora do total
            iTextSharp.text.pdf.PdfPCell cellLinha = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(""));
            cellLinha.Colspan = 2;
            cellLinha.BorderWidthTop = 0.5f;
            cellLinha.BorderColorTop = new iTextSharp.text.BaseColor(150, 150, 150);
            cellLinha.FixedHeight = 8f;
            cellLinha.BorderWidth = 0;
            tabelaResumo.AddCell(cellLinha);

            // Total
            decimal totalGeral = subtotal + _valorDeslocamento + _valorMaoDeObra;
            AdicionarCelulaResumo(tabelaResumo, "TOTAL:", fValor, $"R$ {totalGeral.ToString("N2")}", fValor);

            doc.Add(tabelaResumo);

            // Espaçamento antes das observações
            doc.Add(iTextSharp.text.Chunk.NEWLINE);

            // Observações
            iTextSharp.text.Paragraph observacoesTitulo = new iTextSharp.text.Paragraph("CONDIÇÕES E OBSERVAÇÕES", fTituloSecao);
            observacoesTitulo.SpacingAfter = 6f;
            doc.Add(observacoesTitulo);

            iTextSharp.text.Paragraph observacoes = new iTextSharp.text.Paragraph(
                "• Este orçamento tem validade de 30 dias\n" +
                "• Preços sujeitos a alteração sem aviso prévio\n" +
                "• Formas de pagamento: [ESPECIFICAR FORMAS DE PAGAMENTO]\n" +
                "• Prazo de entrega: [ESPECIFICAR PRAZO]\n" +
                "• Condições especiais: [ESPECIFICAR CONDIÇÕES]",
                fNormal);
            observacoes.SpacingAfter = 15f;
            doc.Add(observacoes);

            // Linha divisória final
            iTextSharp.text.pdf.draw.LineSeparator linhaFinal = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, new iTextSharp.text.BaseColor(0, 51, 102), iTextSharp.text.Element.ALIGN_CENTER, 5f);
            doc.Add(linhaFinal);

            // Rodapé
            iTextSharp.text.Paragraph rodape = new iTextSharp.text.Paragraph(
                $"Orçamento {_numeroOrcamento} - Emitido em {DateTime.Now:dd/MM/yyyy às HH:mm}\n" +
                $"Cliente: {txtCliente.Text}\n" +
                "Agradecemos pela preferência!\n",
                new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.GRAY));
            rodape.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            rodape.SpacingBefore = 10f;
            doc.Add(rodape);

            doc.Close();
            return caminhoArquivo;
        }

        private void AdicionarCabecalhoTexto(iTextSharp.text.Document doc)
        {
            iTextSharp.text.Paragraph cabecalho = new iTextSharp.text.Paragraph(
                "EVEREST\nAR CONDICIONADO",
                new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 16, iTextSharp.text.Font.BOLD, new iTextSharp.text.BaseColor(0, 51, 102))
            );
            cabecalho.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            cabecalho.SpacingAfter = 10f;
            doc.Add(cabecalho);
        }


        // Método auxiliar para adicionar células na tabela principal
        private void AdicionarCelulaTabela(iTextSharp.text.pdf.PdfPTable tabela, string texto, iTextSharp.text.Font fonte, iTextSharp.text.BaseColor corFundo)
        {
            iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(texto, fonte));
            cell.BorderWidth = 0.5f;
            cell.BorderColor = new iTextSharp.text.BaseColor(200, 200, 200);
            cell.BackgroundColor = corFundo;
            cell.Padding = 5f;
            cell.PaddingTop = 7f;
            cell.PaddingBottom = 7f;

            if (tabela.NumberOfColumns == 6)
            {
                int totalCelulas = 0;
                foreach (var row in tabela.Rows)
                {
                    totalCelulas += row.GetCells().Length;
                }

                int colunaAtual = (totalCelulas % 6) + 1;

                if (colunaAtual == 2 || colunaAtual == 3)
                    cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
                else if (colunaAtual == 5 || colunaAtual == 6)
                    cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
                else
                    cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
            }
            else
            {
                cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
            }

            tabela.AddCell(cell);
        }

        // Método específico para células do resumo
        private void AdicionarCelulaResumo(iTextSharp.text.pdf.PdfPTable tabela, string label, iTextSharp.text.Font fonteLabel, string valor, iTextSharp.text.Font fonteValor)
        {
            iTextSharp.text.pdf.PdfPCell cellLabel = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(label, fonteLabel));
            cellLabel.BorderWidth = 0;
            cellLabel.Padding = 4f;
            cellLabel.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
            cellLabel.BackgroundColor = new iTextSharp.text.BaseColor(250, 250, 250);
            tabela.AddCell(cellLabel);

            iTextSharp.text.pdf.PdfPCell cellValor = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(valor, fonteValor));
            cellValor.BorderWidth = 0;
            cellValor.Padding = 4f;
            cellValor.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
            cellValor.BackgroundColor = new iTextSharp.text.BaseColor(250, 250, 250);
            tabela.AddCell(cellValor);
        }

        // Método específico para células da tabela de custos adicionais
        private void AdicionarCelulaCustos(iTextSharp.text.pdf.PdfPTable tabela, string texto, iTextSharp.text.Font fonte)
        {
            iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(texto, fonte));
            cell.BorderWidth = 0.5f;
            cell.BorderColor = new iTextSharp.text.BaseColor(200, 200, 200);
            cell.Padding = 6f;
            cell.PaddingTop = 8f;
            cell.PaddingBottom = 8f;

            int totalCelulas = 0;
            foreach (var row in tabela.Rows)
            {
                totalCelulas += row.GetCells().Length;
            }

            if (totalCelulas % 2 == 0)
            {
                cell.BackgroundColor = new iTextSharp.text.BaseColor(245, 245, 245);
                cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
            }
            else
            {
                cell.BackgroundColor = new iTextSharp.text.BaseColor(250, 250, 250);
                cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
            }

            tabela.AddCell(cell);
        }

        // Métodos para obter os valores do deslocamento
        private decimal ObterKmPercorridos()
        {
            if (txtKmPercorridos != null && !string.IsNullOrEmpty(txtKmPercorridos.Text))
            {
                if (decimal.TryParse(txtKmPercorridos.Text, out decimal km))
                {
                    return km;
                }
            }
            return 0;
        }

        private decimal ObterValorPorKm()
        {
            if (txtValorPorKm != null && !string.IsNullOrEmpty(txtValorPorKm.Text))
            {
                string valorTexto = txtValorPorKm.Text.Replace("R$", "").Replace(" ", "").Trim();
                if (decimal.TryParse(valorTexto, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out decimal valor))
                {
                    return valor;
                }
            }
            return 0;
        }

        private void BtnConverterOS_Click(object sender, RoutedEventArgs e)
        {
            if (ItensSelecionados.Count == 0)
            {
                MessageBox.Show("Nenhum item selecionado para converter em OS.");
                return;
            }

            // Verificar se o cliente foi preenchido
            if (string.IsNullOrEmpty(txtCliente.Text.Trim()))
            {
                MessageBox.Show("Por favor, informe o nome do cliente antes de converter para OS.");
                txtCliente.Focus();
                return;
            }

            var itensSemEstoque = new List<string>();

            foreach (var itemOrcamento in ItensSelecionados)
            {
                var itemOriginal = ArmazenamentoTemporario.Itens
                    .FirstOrDefault(i => i.Codigo == itemOrcamento.Codigo);

                if (itemOriginal != null)
                {
                    decimal quantidadeRequerida = itemOrcamento.QuantidadeDecimal;
                    decimal estoqueDisponivel = itemOriginal.QuantidadeDecimal;

                    if (quantidadeRequerida > estoqueDisponivel)
                    {
                        itensSemEstoque.Add($"{itemOrcamento.Descricao} - Requerido: {quantidadeRequerida}, Disponível: {estoqueDisponivel}");
                    }
                }
                else
                {
                    itensSemEstoque.Add($"{itemOrcamento.Descricao} - Item não encontrado no estoque");
                }
            }

            if (itensSemEstoque.Count > 0)
            {
                string mensagemErro = "Não é possível converter para OS devido a estoque insuficiente:\n\n";
                mensagemErro += string.Join("\n", itensSemEstoque);
                mensagemErro += "\n\nAjuste as quantidades no orçamento ou no estoque antes de converter.";

                MessageBox.Show(mensagemErro, "Estoque Insuficiente", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Deseja converter este orçamento em Ordem de Serviço?",
                                       "Converter para OS",
                                       MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var itensOrcamento = new ObservableCollection<ItemEstoque>(
                        ItensSelecionados.Select(item => CriarCopiaItem(item)).ToList()
                    );

                    var emitirOS = new EmitirOS(new ObservableCollection<ItemEstoque>());

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

                    emitirOS.ReceberDadosOrcamento(itensOrcamento, _valorDeslocamento, _valorMaoDeObra, kmPercorridos, valorPorKm);
                    this.Close();
                    emitirOS.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao converter para OS: {ex.Message}", "Erro");
                }
            }
        }

        private void VerificarControles()
        {
            var controles = new List<(string Nome, object Controle)>
            {
                ("lstDisponiveis", lstDisponiveis),
                ("lstSelecionados", lstSelecionados),
                ("txtTotalOS", txtTotalOS),
                ("txtKmPercorridos", txtKmPercorridos),
                ("txtValorPorKm", txtValorPorKm),
                ("txtValorDeslocamento", txtValorDeslocamento),
                ("txtMaoDeObra", txtMaoDeObra),
                ("txtCliente", txtCliente) 
            };

            foreach (var (nome, controle) in controles)
            {
                if (controle == null)
                {
                    MessageBox.Show($"Erro: Controle '{nome}' não foi encontrado. Verifique o XAML.");
                }
            }
        }

        // Classe para dados completos do orçamento
        public class DadosOrcamentoCompleto
        {
            public string NumeroOrcamento { get; set; } = "A definir";
            public string Cliente { get; set; }
            public DateTime Data { get; set; }
            public List<ItemOrcamentoDadosCompleto> Itens { get; set; }
            public decimal KmPercorridos { get; set; }
            public decimal ValorPorKm { get; set; }
            public decimal ValorDeslocamento { get; set; }
            public decimal ValorMaoDeObra { get; set; }
            public decimal ValorTotal { get; set; }
        }

        public class ItemOrcamentoDadosCompleto
        {
            public string Codigo { get; set; }
            public string Nome { get; set; }
            public string Descricao { get; set; }
            public decimal Quantidade { get; set; }
            public decimal Preco { get; set; }
            public decimal Desconto { get; set; }
        }

        // Método para salvar dados completos do orçamento em JSON
        private void SalvarDadosOrcamentoCompleto(string caminhoArquivo)
        {
            try
            {
                var dadosOrcamento = new DadosOrcamentoCompleto
                {
                    NumeroOrcamento = _numeroOrcamento,
                    Cliente = txtCliente.Text.Trim(),
                    Data = DateTime.Now,
                    Itens = new List<ItemOrcamentoDadosCompleto>(),
                    KmPercorridos = ObterKmPercorridos(),
                    ValorPorKm = ObterValorPorKm(),
                    ValorDeslocamento = _valorDeslocamento,
                    ValorMaoDeObra = _valorMaoDeObra,
                    ValorTotal = CalcularTotalGeral()
                };

                // Converter os itens selecionados
                foreach (var item in ItensSelecionados)
                {
                    var itemDados = new ItemOrcamentoDadosCompleto
                    {
                        Codigo = item.Codigo,
                        Nome = item.Nome,
                        Descricao = item.Descricao,
                        Quantidade = item.QuantidadeDecimal,
                        Preco = item.PrecoDecimal
                    };

                    // Converter desconto de string para decimal
                    if (!string.IsNullOrEmpty(item.Desconto))
                    {
                        string descontoStr = item.Desconto.Replace("%", "").Trim();
                        if (decimal.TryParse(descontoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal descontoDecimal))
                        {
                            itemDados.Desconto = descontoDecimal;
                        }
                        else
                        {
                            itemDados.Desconto = 0;
                        }
                    }
                    else
                    {
                        itemDados.Desconto = 0;
                    }

                    dadosOrcamento.Itens.Add(itemDados);
                }

                // Salvar como JSON
                string json = System.Text.Json.JsonSerializer.Serialize(dadosOrcamento,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                string caminhoJson = caminhoArquivo.Replace(".pdf", ".json");
                File.WriteAllText(caminhoJson, json);

                Console.WriteLine($"Dados do orçamento salvos em: {caminhoJson}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar dados do orçamento: {ex.Message}");
                // Não interromper o fluxo principal se falhar
            }
        }
    }
}