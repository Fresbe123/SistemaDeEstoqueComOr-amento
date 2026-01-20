using GestãoEstoque.ClassesOrganização;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GestãoEstoque
{
    public partial class RelatorioVisualizador : Window
    {
        private ICollectionView _collectionView;
        private List<RelatorioOS> _todosRelatorios;

        public RelatorioVisualizador()
        {
            InitializeComponent();
            RelatorioOSmanager.CorrigirNumerosAusentes();
            CarregarRelatorios();
            AtualizarContadores();
        }

        private void CarregarRelatorios()
        {
            RelatorioOSmanager.Recarregar();

            _todosRelatorios = RelatorioOSmanager.Relatorios.ToList();

            _todosRelatorios = _todosRelatorios.OrderBy(r => r.Numero).ThenBy(r => r.Id).ToList();

            _collectionView = CollectionViewSource.GetDefaultView(_todosRelatorios);
            dgRelatorios.ItemsSource = _collectionView;

            Debug.WriteLine($"=== CARREGANDO RELATÓRIOS ===");
            Debug.WriteLine($"Total: {_todosRelatorios.Count}");
            foreach (var rel in _todosRelatorios)
            {
                Debug.WriteLine($"ID: {rel.Id}, Tipo: {rel.Tipo}, Número: {rel.Numero}, NumeroFormatado: {rel.NumeroFormatado}, Cliente: {rel.Cliente}");
            }
        }

        private void AtualizarContadores()
        {
            if (_todosRelatorios == null) return;

            int contadorOS = _todosRelatorios.Count(r => r.Tipo == "OS");
            int contadorOrcamentos = _todosRelatorios.Count(r => r.Tipo == "Orçamento");

            txtContadorOS.Text = contadorOS.ToString();
            txtContadorOrcamentos.Text = contadorOrcamentos.ToString();

            Debug.WriteLine($"Contadores: OS={contadorOS}, Orçamentos={contadorOrcamentos}");
        }

        private void BtnLimparJSONCompleto_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("⚠️ ⚠️ ⚠️ ATENÇÃO TOTAL ⚠️ ⚠️ ⚠️\n\n" +
                "Isso irá APAGAR COMPLETAMENTE todos os dados de sequência e relatórios.\n" +
                "Serão DELETADOS:\n" +
                "1. Arquivo de sequenciais (numeros_sequenciais.json)\n" +
                "2. Arquivo de relatórios (relatorios_os.json)\n" +
                "3. Criados novos arquivos limpos\n\n" +
                "Esta ação NÃO PODE ser desfeita!\n\n" +
                "TEM CERTEZA ABSOLUTA que deseja continuar?",
                "🚨 LIMPEZA COMPLETA DO SISTEMA 🚨",
                MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                string backupDir = $@"dados\backup\limpeza_{DateTime.Now:yyyyMMdd_HHmmss}";
                Directory.CreateDirectory(backupDir);

                string[] arquivosParaBackup =
                {
            @"dados\numeros_sequenciais.json",
            @"dados\relatorios_os.json"
        };

                foreach (var arquivo in arquivosParaBackup)
                {
                    if (File.Exists(arquivo))
                    {
                        string nomeArquivo = Path.GetFileName(arquivo);
                        File.Copy(arquivo, Path.Combine(backupDir, nomeArquivo), true);
                    }
                }


                foreach (var arquivo in arquivosParaBackup)
                {
                    if (File.Exists(arquivo))
                    {
                        File.Delete(arquivo);
                    }
                }

                var sequenciaisLimpos = new SequenciaManager.NumerosSequenciais
                {
                    ProximaOS = 3001,      
                    ProximoOrcamento = 500 
                };

                SequenciaManager.SalvarNumeros(sequenciaisLimpos);

                File.WriteAllText(@"dados\relatorios_os.json", "[]");

                RelatorioOSmanager.Recarregar();

                CarregarRelatorios();
                AtualizarContadores();

                MessageBox.Show($"✅ SISTEMA LIMPO COM SUCESSO!\n\n" +
                               $"Backup salvo em: {backupDir}\n\n" +
                               $"Novo estado do sistema:\n" +
                               $"• Próxima OS: OS_003001\n" +
                               $"• Próximo Orçamento: ORC_000500\n" +
                               $"• Total de relatórios: 0\n\n" +
                               $"O sistema está pronto para uso com numeração fresca!",
                               "🏁 Sistema Limpo",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro na limpeza: {ex.Message}",
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCorrigirNumeros_Click(object sender, RoutedEventArgs e)
        {
            RelatorioOSmanager.CorrigirNumerosAusentes();
            CarregarRelatorios();
            AtualizarContadores();
            MessageBox.Show("Números corrigidos com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDetalhes_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DETALHES DOS RELATÓRIOS ===");
            sb.AppendLine($"Total: {_todosRelatorios.Count}");
            sb.AppendLine();

            sb.AppendLine("OS:");
            foreach (var os in _todosRelatorios.Where(r => r.Tipo == "OS"))
            {
                sb.AppendLine($"  {os.NumeroFormatado} - {os.Cliente} - R$ {os.ValorTotal:N2}");
            }

            sb.AppendLine();
            sb.AppendLine("Orçamentos:");
            foreach (var orc in _todosRelatorios.Where(r => r.Tipo == "Orçamento"))
            {
                sb.AppendLine($"  {orc.NumeroFormatado} - {orc.Cliente} - R$ {orc.ValorTotal:N2}");
            }

            MessageBox.Show(sb.ToString(), "Detalhes",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private RelatorioOS ObterSelecionado()
        {
            return dgRelatorios.SelectedItem as RelatorioOS;
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

            AtualizarContadores();
        }

        private void dgRelatorios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
            if (!all.Any())
            {
                MessageBox.Show("Nenhum relatorio para exportar.");
                return;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RelatoriosOS.csv");
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.WriteLine("Tipo;Número;Cliente;Data;ValorTotal;Arquivo");
                foreach (var r in all)
                {
                    sw.WriteLine($"{r.Tipo};{r.NumeroFormatado};\"{r.Cliente}\";{r.Data};{r.ValorTotal:N2};\"{r.CaminhoArquivo}\"");
                }
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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


        private void BtnGerarOS_Click(object sender, RoutedEventArgs e)
        {
            var orcamento = ObterSelecionado();

            if (orcamento == null || orcamento.Tipo != "Orçamento")
            {
                MessageBox.Show("Selecione um orçamento válido.");
                return;
            }

            try
            {
                var dados = CarregarOrcamento(orcamento.CaminhoArquivo);
                if (dados == null)
                {
                    MessageBox.Show("Não foi possível carregar os dados deste orçamento.");
                    return;
                }

                int numeroOS = SequenciaManager.ObterProximaOS();

                var confirmacao = MessageBox.Show($"Converter orçamento {orcamento.NumeroFormatado} em OS?\n\n" +
                                                 $"Nova OS: OS-{numeroOS}\n" +
                                                 $"Cliente: {orcamento.Cliente}\n" +
                                                 $"Valor: R$ {dados.ValorTotal:N2}\n" +
                                                 $"Itens: {dados.Itens?.Count ?? 0}\n\n" +
                                                 "Deseja criar a OS agora?",
                                                 "Converter Orçamento para OS",
                                                 MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmacao != MessageBoxResult.Yes)
                {
                    return;
                }

                var estoqueAtual = ObterEstoqueAtual(); 

                var itensOrcamento = new ObservableCollection<ItemEstoque>();

                foreach (var itemDados in dados.Itens)
                {
                    var itemEstoqueCompleto = ArmazenamentoTemporario.Itens
                        .FirstOrDefault(i => i.Codigo == itemDados.Codigo);

                    if (itemEstoqueCompleto != null)
                    {
                        var itemEstoque = new ItemEstoque
                        {
                            Item = itemEstoqueCompleto.Item,        
                            Codigo = itemEstoqueCompleto.Codigo,
                            Nome = itemEstoqueCompleto.Nome,        
                            Descricao = itemEstoqueCompleto.Descricao,
                            Unidade = itemEstoqueCompleto.Unidade,  
                            Preco = itemEstoqueCompleto.Preco,
                            Quantidade = itemDados.Quantidade.ToString("N2"),
                            Desconto = $"{itemDados.Desconto}%",
                            Total = (itemDados.Quantidade * itemDados.Preco * (1 - itemDados.Desconto / 100)).ToString("N2"),
                            Favorito = itemEstoqueCompleto.Favorito
                        };
                        itensOrcamento.Add(itemEstoque);
                    }
                    else
                    {
                        MessageBox.Show($"Item {itemDados.Codigo} não encontrado no estoque atual",
                                      "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                var osWindow = new EmitirOS(estoqueAtual);

                osWindow.txtCliente.Text = orcamento.Cliente;

                osWindow.ReceberDadosOrcamento(itensOrcamento, dados.ValorDeslocamento,
                                             dados.ValorMaoDeObra, dados.KmPercorridos, dados.ValorPorKm);

                this.Hide();
                osWindow.ShowDialog();
                this.Show();


                CarregarRelatorios();
                AtualizarContadores();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro ao converter orçamento: {ex.Message}",
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private ObservableCollection<ItemEstoque> ObterEstoqueAtual()
        {
            try
            {
                if (ArmazenamentoTemporario.Itens != null && ArmazenamentoTemporario.Itens.Count > 0)
                {
                    return new ObservableCollection<ItemEstoque>(ArmazenamentoTemporario.Itens);
                }

                MessageBox.Show("Estoque vazio ou não disponível", "Aviso");
                return new ObservableCollection<ItemEstoque>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter estoque: {ex.Message}", "Erro");
                return new ObservableCollection<ItemEstoque>();
            }
        }

        private void BtnDeletarMultiplos_Click(object sender, RoutedEventArgs e)
        {
            var selecionados = dgRelatorios.SelectedItems.Cast<RelatorioOS>().ToList();

            if (!selecionados.Any())
            {
                MessageBox.Show("Selecione um ou mais itens para deletar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Itens selecionados para deleção: {selecionados.Count}");
            sb.AppendLine();

            foreach (var item in selecionados)
            {
                sb.AppendLine($"{item.Tipo} {item.NumeroFormatado} - {item.Cliente} - R$ {item.ValorTotal:N2}");
            }

            sb.AppendLine();
            sb.AppendLine("Esta ação não pode ser desfeita!");

            var result = MessageBox.Show(sb.ToString(),
                                        $"Confirmar Deleção de {selecionados.Count} Itens",
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                MessageBoxResult deletarArquivosResult = MessageBoxResult.No;
                MessageBoxResult deletarJSONResult = MessageBoxResult.No;

                bool temArquivoPDF = selecionados.Any(s => File.Exists(s.CaminhoArquivo));
                bool temOrcamentoComJSON = selecionados.Any(s =>
                    s.Tipo == "Orçamento" && File.Exists(s.CaminhoArquivo.Replace(".pdf", ".json")));

                if (temArquivoPDF)
                {
                    deletarArquivosResult = MessageBox.Show(
                        $"Há {selecionados.Count(s => File.Exists(s.CaminhoArquivo))} arquivo(s) PDF associado(s).\n\n" +
                        "Deseja deletar os arquivos físicos também?\n\n" +
                        "• SIM: Deleta todos os PDFs\n" +
                        "• NÃO: Mantém os arquivos no sistema\n" +
                        "• CANCELAR: Aborta a operação",
                        "Deletar arquivos PDF?",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (deletarArquivosResult == MessageBoxResult.Cancel) return;
                }

                if (temOrcamentoComJSON)
                {
                    int orcamentosComJSON = selecionados.Count(s =>
                        s.Tipo == "Orçamento" && File.Exists(s.CaminhoArquivo.Replace(".pdf", ".json")));

                    deletarJSONResult = MessageBox.Show(
                        $"Há {orcamentosComJSON} arquivo(s) JSON de orçamento(s).\n\n" +
                        "Deseja deletar os arquivos JSON também?",
                        "Deletar arquivos JSON?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                }

                bool deletarArquivosFisicos = (deletarArquivosResult == MessageBoxResult.Yes);
                bool deletarArquivosJSON = (deletarJSONResult == MessageBoxResult.Yes);

                string backupDir = $@"dados\backup\deletados_multiplos_{DateTime.Now:yyyyMMdd_HHmmss}";
                Directory.CreateDirectory(backupDir);

                int sucesso = 0;
                int falhas = 0;
                int arquivosDeletados = 0;
                int jsonDeletados = 0;

                foreach (var selecionado in selecionados)
                {
                    try
                    {
                        string registroBackup = Path.Combine(backupDir,
                            $"{selecionado.Tipo}_{selecionado.NumeroFormatado}_{DateTime.Now:HHmmss}.json");
                        string registroJson = Newtonsoft.Json.JsonConvert.SerializeObject(selecionado,
                            Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(registroBackup, registroJson);

                        if (deletarArquivosFisicos && File.Exists(selecionado.CaminhoArquivo))
                        {
                            try
                            {
                                string arquivoBackup = Path.Combine(backupDir, Path.GetFileName(selecionado.CaminhoArquivo));
                                File.Copy(selecionado.CaminhoArquivo, arquivoBackup, true);
                                File.Delete(selecionado.CaminhoArquivo);
                                arquivosDeletados++;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Não foi possível deletar PDF {selecionado.CaminhoArquivo}: {ex.Message}");
                            }
                        }

                        if (deletarArquivosJSON && selecionado.Tipo == "Orçamento")
                        {
                            string caminhoJson = selecionado.CaminhoArquivo.Replace(".pdf", ".json");
                            if (File.Exists(caminhoJson))
                            {
                                try
                                {
                                    string jsonBackup = Path.Combine(backupDir, Path.GetFileName(caminhoJson));
                                    File.Copy(caminhoJson, jsonBackup, true);
                                    File.Delete(caminhoJson);
                                    jsonDeletados++;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Não foi possível deletar JSON {caminhoJson}: {ex.Message}");
                                }
                            }
                        }

                        _todosRelatorios.Remove(selecionado);
                        sucesso++;
                    }
                    catch (Exception ex)
                    {
                        falhas++;
                        Debug.WriteLine($"Erro ao deletar {selecionado.NumeroFormatado}: {ex.Message}");
                    }
                }

                SalvarRelatoriosAtualizados(_todosRelatorios);

                _collectionView.Refresh();
                AtualizarContadores();

                StringBuilder resultado = new StringBuilder();
                resultado.AppendLine($"✅ Deleção concluída!\n");
                resultado.AppendLine($"• Itens deletados do sistema: {sucesso}");
                resultado.AppendLine($"• Falhas: {falhas}");

                if (deletarArquivosFisicos)
                    resultado.AppendLine($"• Arquivos PDF deletados: {arquivosDeletados}");

                if (deletarArquivosJSON)
                    resultado.AppendLine($"• Arquivos JSON deletados: {jsonDeletados}");

                resultado.AppendLine($"• Backup salvo em: {backupDir}");

                MessageBox.Show(resultado.ToString(),
                               "Deleção Múltipla Concluída",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarRelatorios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro na deleção múltipla: {ex.Message}\n\n{ex.StackTrace}",
                               "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DadosOrcamentoCompleto CarregarOrcamento(string caminhoPdf)
        {
            try
            {
                string caminhoJson = caminhoPdf.Replace(".pdf", ".json");
                if (File.Exists(caminhoJson))
                {
                    string json = File.ReadAllText(caminhoJson);
                    var dados = System.Text.Json.JsonSerializer.Deserialize<DadosOrcamentoCompleto>(json);

                    Console.WriteLine("=== DEBUG: Dados carregados do orçamento ===");
                    Console.WriteLine($"Número: {dados?.NumeroOrcamento}");
                    Console.WriteLine($"Cliente: {dados?.Cliente}");

                    if (dados?.Itens != null)
                    {
                        foreach (var item in dados.Itens)
                        {
                            Console.WriteLine($"Item JSON - Código: {item.Codigo}, Nome: {item.Nome}, Unidade: N/A no JSON");
                        }
                    }

                    return dados;
                }
                else
                {
                    MessageBox.Show($"Arquivo JSON não encontrado: {caminhoJson}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar orçamento: {ex.Message}");
            }
            return null;
        }

        // MÉTODOS DE GERENCIAMENTO

        private void BtnGerenciarNumeracao_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "⚙️ Gerenciamento de Numeração",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var grid = new Grid();
            grid.Margin = new Thickness(10);
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var scrollViewer = new ScrollViewer();
            var stackInfo = new StackPanel { Margin = new Thickness(10) };

            try
            {
                var numeros = SequenciaManager.CarregarNumeros();
                string statusAtual = $"📊 STATUS ATUAL:\n\n" +
                                   $"• Próxima OS: {numeros.ProximaOS} (OS_{numeros.ProximaOS:000000})\n" +
                                   $"• Próximo Orçamento: {numeros.ProximoOrcamento} (ORC_{numeros.ProximoOrcamento:000000})\n";

                var txtStatus = new TextBlock
                {
                    Text = statusAtual,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 20),
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.SemiBold
                };
                stackInfo.Children.Add(txtStatus);

                string contadoresAtuais = $"📈 CONTAGEM ATUAL:\n\n" +
                                        $"• Total de OS: {_todosRelatorios?.Count(r => r.Tipo == "OS") ?? 0}\n" +
                                        $"• Total de Orçamentos: {_todosRelatorios?.Count(r => r.Tipo == "Orçamento") ?? 0}\n" +
                                        $"• Total Geral: {_todosRelatorios?.Count ?? 0}";

                var txtContadores = new TextBlock
                {
                    Text = contadoresAtuais,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 20),
                    TextWrapping = TextWrapping.Wrap
                };
                stackInfo.Children.Add(txtContadores);

                var ultimaOS = _todosRelatorios?.Where(r => r.Tipo == "OS").OrderByDescending(r => r.Numero).FirstOrDefault();
                var ultimoOrc = _todosRelatorios?.Where(r => r.Tipo == "Orçamento").OrderByDescending(r => r.Numero).FirstOrDefault();

                string ultimosNumeros = $"🔢 ÚLTIMOS REGISTROS:\n\n" +
                                      $"• Última OS: {(ultimaOS != null ? ultimaOS.NumeroFormatado : "Nenhuma")}\n" +
                                      $"• Último Orçamento: {(ultimoOrc != null ? ultimoOrc.NumeroFormatado : "Nenhum")}";

                var txtUltimos = new TextBlock
                {
                    Text = ultimosNumeros,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 20),
                    TextWrapping = TextWrapping.Wrap
                };
                stackInfo.Children.Add(txtUltimos);
            }
            catch (Exception ex)
            {
                var txtErro = new TextBlock
                {
                    Text = $"❌ ERRO AO CARREGAR INFORMAÇÕES:\n{ex.Message}",
                    Foreground = Brushes.Red,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 20),
                    TextWrapping = TextWrapping.Wrap
                };
                stackInfo.Children.Add(txtErro);
            }

            var stackBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var btnResetSimples = new Button
            {
                Content = "🔄 Reset Simples",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.Orange,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                ToolTip = "Resetar apenas o arquivo JSON de numeração"
            };
            btnResetSimples.Click += (s, args) =>
            {
                dialog.Close();
                BtnResetarJSON_Click(sender, e);
            };

            var btnResetCompleto = new Button
            {
                Content = "⚠️ Reset Completo",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.Red,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                ToolTip = "Resetar JSON e renumerar todos os relatórios"
            };
            btnResetCompleto.Click += (s, args) =>
            {
                dialog.Close();
                BtnResetarComBackup_Click(sender, e);
            };

            var btnFechar = new Button
            {
                Content = "✖️ Fechar",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnFechar.Click += (s, args) => dialog.Close();

            stackBotoes.Children.Add(btnResetSimples);
            stackBotoes.Children.Add(btnResetCompleto);
            stackBotoes.Children.Add(btnFechar);

            stackInfo.Children.Add(stackBotoes);
            scrollViewer.Content = stackInfo;
            Grid.SetRow(scrollViewer, 0);
            grid.Children.Add(scrollViewer);

            var txtAviso = new TextBlock
            {
                Text = "⚠️ Atenção: Faça backup antes de qualquer reset!",
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };

            var borderAviso = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)),
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(5),
                Padding = new Thickness(5)
            };
            borderAviso.Child = txtAviso;

            Grid.SetRow(borderAviso, 1);
            grid.Children.Add(borderAviso);

            dialog.Content = grid;
            dialog.ShowDialog();
        }


        private void BtnResetarJSON_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("⚠️ ATENÇÃO ⚠️\n\n" +
                "Isso irá RESETAR COMPLETAMENTE o arquivo JSON de numeração.\n" +
                "Todos os números sequenciais voltarão aos valores iniciais:\n" +
                "• OS: 3001\n" +
                "• Orçamentos: 500\n\n" +
                "Deseja continuar?",
                "CONFIRMAR RESET",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var novosNumeros = new SequenciaManager.NumerosSequenciais
                    {
                        ProximaOS = 3001,
                        ProximoOrcamento = 500
                    };

                    SequenciaManager.SalvarNumeros(novosNumeros);

                    MessageBox.Show("✅ JSON resetado com sucesso!\n" +
                                  "Próxima OS: OS_003001\n" +
                                  "Próximo Orçamento: ORC_000500",
                                  "Sucesso",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    CarregarRelatorios();
                    AtualizarContadores();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Erro ao resetar JSON: {ex.Message}",
                                  "Erro",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void BtnResetarComBackup_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Deseja fazer backup do arquivo atual antes de resetar?",
                "Backup", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;

            try
            {
                string caminhoArquivo = @"dados\numeros_sequenciais.json";

                if (result == MessageBoxResult.Yes && File.Exists(caminhoArquivo))
                {
                    string backupDir = @"dados\backup";
                    Directory.CreateDirectory(backupDir);
                    string backupPath = Path.Combine(backupDir, $"numeros_sequenciais_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    File.Copy(caminhoArquivo, backupPath, true);

                    MessageBox.Show($"✅ Backup criado em:\n{backupPath}",
                                  "Backup",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ResetarParaValoresIniciais();

                MessageBox.Show("✅ JSON resetado para valores iniciais!",
                              "Sucesso",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ResetarParaValoresIniciais()
        {
            var novosNumeros = new SequenciaManager.NumerosSequenciais
            {
                ProximaOS = 3001,
                ProximoOrcamento = 500
            };

            SequenciaManager.SalvarNumeros(novosNumeros);

            var relatorios = RelatorioOSmanager.Relatorios.ToList();

            int contadorOS = 3000;
            foreach (var os in relatorios.Where(r => r.Tipo == "OS").OrderBy(r => r.Id))
            {
                os.Numero = ++contadorOS;
            }

            int contadorOrc = 499;
            foreach (var orc in relatorios.Where(r => r.Tipo == "Orçamento").OrderBy(r => r.Id))
            {
                orc.Numero = ++contadorOrc;
            }

            SalvarRelatoriosAtualizados(relatorios);

            CarregarRelatorios();
            AtualizarContadores();
        }


        private void SalvarRelatoriosAtualizados(List<RelatorioOS> relatorios)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(relatorios,
                    Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(@"dados\relatorios_os.json", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro ao salvar relatórios atualizados: {ex.Message}",
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}