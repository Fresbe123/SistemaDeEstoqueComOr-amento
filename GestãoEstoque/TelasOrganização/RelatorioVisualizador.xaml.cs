using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using GestãoEstoque.ClassesOrganização;

namespace GestãoEstoque
{
    public partial class RelatorioVisualizador : Window
    {
        private ICollectionView _collectionView;
        private List<RelatorioOS> _todosRelatorios;

        public RelatorioVisualizador()
        {
            InitializeComponent();
            // Corrige números ausentes antes de carregar
            RelatorioOSmanager.CorrigirNumerosAusentes();
            CarregarRelatorios();
            AtualizarContadores();
        }

        private void CarregarRelatorios()
        {
            // Recarrega do arquivo
            RelatorioOSmanager.Recarregar();

            // Converte para List para facilitar manipulação
            _todosRelatorios = RelatorioOSmanager.Relatorios.ToList();

            // Ordena por número (ou por ID se não tiver número)
            _todosRelatorios = _todosRelatorios.OrderBy(r => r.Numero).ThenBy(r => r.Id).ToList();

            _collectionView = CollectionViewSource.GetDefaultView(_todosRelatorios);
            dgRelatorios.ItemsSource = _collectionView;

            // DEBUG: Mostra no Output Window
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

            // DEBUG
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
                // 1. Fazer backup dos arquivos atuais
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

                // 2. DELETAR os arquivos atuais
                foreach (var arquivo in arquivosParaBackup)
                {
                    if (File.Exists(arquivo))
                    {
                        File.Delete(arquivo);
                    }
                }

                // 3. Criar NOVO arquivo de sequenciais limpo
                var sequenciaisLimpos = new SequenciaManager.NumerosSequenciais
                {
                    ProximaOS = 3001,      // Primeira OS será 3001
                    ProximoOrcamento = 500 // Primeiro orçamento será 500
                };

                SequenciaManager.SalvarNumeros(sequenciaisLimpos);

                // 4. Criar NOVO arquivo de relatórios vazio
                File.WriteAllText(@"dados\relatorios_os.json", "[]");

                // 5. Atualizar o RelatorioOSmanager
                RelatorioOSmanager.Recarregar();

                // 6. Atualizar interface
                CarregarRelatorios();
                AtualizarContadores();

                // 7. Mostrar confirmação
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

        // Botão para forçar correção
        private void BtnCorrigirNumeros_Click(object sender, RoutedEventArgs e)
        {
            RelatorioOSmanager.CorrigirNumerosAusentes();
            CarregarRelatorios();
            AtualizarContadores();
            MessageBox.Show("Números corrigidos com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Botão para ver detalhes
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

        // EVENTOS DE CLIQUE

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

            // Abre o arquivo após exportar
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

            if (orcamento == null)
            {
                MessageBox.Show("Selecione um orçamento primeiro.");
                return;
            }

            if (orcamento.Tipo != "Orçamento")
            {
                MessageBox.Show("Selecione um orçamento para converter.");
                return;
            }

            try
            {
                // 1. OBTER O PRÓXIMO NÚMERO MAS NÃO INCREMENTAR AINDA
                int proximoNumeroOS = SequenciaManager.VisualizarProximaOS();

                // 2. Carregar dados do orçamento
                var dados = CarregarOrcamento(orcamento.CaminhoArquivo);
                if (dados == null)
                {
                    MessageBox.Show("Não foi possível carregar os dados deste orçamento.");
                    return;
                }

                // 3. Perguntar confirmação
                var confirmacao = MessageBox.Show($"Converter orçamento {orcamento.NumeroFormatado} em OS?\n\n" +
                                                 $"Próxima OS disponível: OS_{proximoNumeroOS:000000}\n" +
                                                 $"Cliente: {orcamento.Cliente}\n" +
                                                 $"Valor: R$ {orcamento.ValorTotal:N2}\n\n" +
                                                 "Deseja continuar?\n\n" +
                                                 "NOTA: A OS será criada quando você salvar na tela de Emitir OS.",
                                                 "Converter Orçamento para OS",
                                                 MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmacao != MessageBoxResult.Yes)
                {
                    return;
                }

                // 4. Converter itens
                var itens = new System.Collections.ObjectModel.ObservableCollection<ItemEstoque>();
                foreach (var item in dados.Itens)
                {
                    var itemEstoque = new ItemEstoque
                    {
                        Codigo = item.Codigo,
                        Nome = item.Nome,
                        Descricao = item.Descricao,
                        Quantidade = item.Quantidade.ToString("N2"),
                        Preco = item.Preco.ToString("N2"),
                        Desconto = $"{item.Desconto}%",
                        Total = (item.Quantidade * item.Preco * (1 - item.Desconto / 100)).ToString("N2")
                    };
                    itens.Add(itemEstoque);
                }

                // 5. Criar e configurar a janela de OS
                var osWindow = new EmitirOS(new System.Collections.ObjectModel.ObservableCollection<ItemEstoque>());

                // IMPORTANTE: Preencher o número da OS que será usada
                var numeroOSField = osWindow.GetType().GetField("txtNumeroOS",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (numeroOSField != null)
                {
                    var txtNumeroOS = numeroOSField.GetValue(osWindow) as TextBlock;
                    if (txtNumeroOS != null)
                    {
                        txtNumeroOS.Text = $"OS_{proximoNumeroOS:000000}";

                        // Também armazenar o número em uma propriedade se necessário
                        var numeroField = osWindow.GetType().GetField("_numeroOS",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (numeroField != null)
                        {
                            numeroField.SetValue(osWindow, proximoNumeroOS);
                        }
                    }
                }

                // Preencher dados do cliente
                var clienteField = osWindow.GetType().GetField("txtCliente",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (clienteField != null)
                {
                    var txtCliente = clienteField.GetValue(osWindow) as TextBox;
                    if (txtCliente != null)
                        txtCliente.Text = orcamento.Cliente;
                }

                // Adicionar itens
                var itensField = osWindow.GetType().GetField("ItensSelecionados",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (itensField != null)
                {
                    var itensSelecionados = itensField.GetValue(osWindow) as System.Collections.ObjectModel.ObservableCollection<ItemEstoque>;
                    if (itensSelecionados != null)
                    {
                        foreach (var item in itens)
                            itensSelecionados.Add(item);
                    }
                }

                // 6. Configurar evento para quando a OS for salva
                // (Você precisa adicionar este evento na classe EmitirOS)
                // ou usar uma abordagem diferente...

                // 7. Mostrar a janela
                this.Hide();
                var resultado = osWindow.ShowDialog();
                this.Show();

                if (resultado.HasValue && resultado.Value)
                {
                    // Se a OS foi salva com sucesso
                    MessageBox.Show($"✅ OS OS_{proximoNumeroOS:000000} criada com sucesso a partir do orçamento {orcamento.NumeroFormatado}!",
                                  "Sucesso",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Conversão cancelada.", "Cancelado",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 8. Recarregar e atualizar contadores
                CarregarRelatorios();
                AtualizarContadores();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro ao converter orçamento: {ex.Message}",
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeletar_Click(object sender, RoutedEventArgs e)
        {
            var selecionado = ObterSelecionado();

            if (selecionado == null)
            {
                MessageBox.Show("Selecione um item para deletar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Confirmar deleção
            var result = MessageBox.Show($"Tem certeza que deseja deletar o seguinte item?\n\n" +
                                        $"Tipo: {selecionado.Tipo}\n" +
                                        $"Número: {selecionado.NumeroFormatado}\n" +
                                        $"Cliente: {selecionado.Cliente}\n" +
                                        $"Data: {selecionado.Data}\n" +
                                        $"Valor: R$ {selecionado.ValorTotal:N2}\n\n" +
                                        "Esta ação não pode ser desfeita!",
                                        "Confirmar Deleção",
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // 1. Verificar se o arquivo PDF existe e perguntar se quer deletar
                bool deletarArquivo = false;
                if (File.Exists(selecionado.CaminhoArquivo))
                {
                    var resultArquivo = MessageBox.Show($"O arquivo PDF também existe:\n{selecionado.CaminhoArquivo}\n\n" +
                                                       "Deseja deletar o arquivo físico também?",
                                                       "Deletar arquivo PDF?",
                                                       MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (resultArquivo == MessageBoxResult.Cancel) return;

                    deletarArquivo = (resultArquivo == MessageBoxResult.Yes);
                }

                // 2. Verificar se existe arquivo JSON associado (para orçamentos)
                bool deletarJSON = false;
                if (selecionado.Tipo == "Orçamento")
                {
                    string caminhoJson = selecionado.CaminhoArquivo.Replace(".pdf", ".json");
                    if (File.Exists(caminhoJson))
                    {
                        var resultJson = MessageBox.Show($"Existe um arquivo JSON associado a este orçamento.\n\n" +
                                                        "Deseja deletar o arquivo JSON também?",
                                                        "Deletar arquivo JSON?",
                                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                        deletarJSON = (resultJson == MessageBoxResult.Yes);
                    }
                }

                // 3. Criar backup antes de deletar
                string backupDir = $@"dados\backup\deletados_{DateTime.Now:yyyyMMdd_HHmmss}";
                Directory.CreateDirectory(backupDir);

                // Backup do registro
                string registroBackup = Path.Combine(backupDir, $"{selecionado.Tipo}_{selecionado.NumeroFormatado}_{DateTime.Now:HHmmss}.json");
                string registroJson = Newtonsoft.Json.JsonConvert.SerializeObject(selecionado, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(registroBackup, registroJson);

                // 4. Remover da lista em memória
                _todosRelatorios.Remove(selecionado);

                // 5. Atualizar o arquivo JSON principal
                SalvarRelatoriosAtualizados(_todosRelatorios);

                // 6. Deletar arquivos físicos se solicitado
                if (deletarArquivo && File.Exists(selecionado.CaminhoArquivo))
                {
                    try
                    {
                        // Mover para a pasta de backup primeiro
                        string arquivoBackup = Path.Combine(backupDir, Path.GetFileName(selecionado.CaminhoArquivo));
                        File.Copy(selecionado.CaminhoArquivo, arquivoBackup);
                        File.Delete(selecionado.CaminhoArquivo);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Não foi possível deletar o arquivo PDF: {ex.Message}\n" +
                                       "O registro foi removido, mas o arquivo permanece no sistema.",
                                       "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                if (deletarJSON && selecionado.Tipo == "Orçamento")
                {
                    string caminhoJson = selecionado.CaminhoArquivo.Replace(".pdf", ".json");
                    if (File.Exists(caminhoJson))
                    {
                        try
                        {
                            // Mover para backup
                            string jsonBackup = Path.Combine(backupDir, Path.GetFileName(caminhoJson));
                            File.Copy(caminhoJson, jsonBackup);
                            File.Delete(caminhoJson);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Não foi possível deletar o arquivo JSON: {ex.Message}",
                                           "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                // 7. Atualizar interface
                _collectionView.Refresh();
                AtualizarContadores();

                // 8. Mostrar mensagem de sucesso
                MessageBox.Show($"✅ {selecionado.Tipo} {selecionado.NumeroFormatado} deletado com sucesso!\n\n" +
                               $"Backup salvo em: {backupDir}",
                               "Item Deletado",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // 9. Recarregar do arquivo para garantir sincronização
                CarregarRelatorios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro ao deletar item: {ex.Message}",
                               "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Mostrar resumo dos itens selecionados
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
                // Criar backup
                string backupDir = $@"dados\backup\deletados_multiplos_{DateTime.Now:yyyyMMdd_HHmmss}";
                Directory.CreateDirectory(backupDir);

                int sucesso = 0;
                int falhas = 0;

                foreach (var selecionado in selecionados)
                {
                    try
                    {
                        // Backup do registro
                        string registroBackup = Path.Combine(backupDir,
                            $"{selecionado.Tipo}_{selecionado.NumeroFormatado}_{DateTime.Now:HHmmss}.json");
                        string registroJson = Newtonsoft.Json.JsonConvert.SerializeObject(selecionado,
                            Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(registroBackup, registroJson);

                        // Remover da lista
                        _todosRelatorios.Remove(selecionado);
                        sucesso++;
                    }
                    catch (Exception ex)
                    {
                        falhas++;
                        Debug.WriteLine($"Erro ao deletar {selecionado.NumeroFormatado}: {ex.Message}");
                    }
                }

                // Salvar alterações
                SalvarRelatoriosAtualizados(_todosRelatorios);

                // Atualizar interface
                _collectionView.Refresh();
                AtualizarContadores();

                // Mostrar resultado
                MessageBox.Show($"✅ Deleção concluída!\n\n" +
                               $"• Itens deletados com sucesso: {sucesso}\n" +
                               $"• Falhas: {falhas}\n" +
                               $"• Backup salvo em: {backupDir}",
                               "Deleção Múltipla Concluída",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarRelatorios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro na deleção múltipla: {ex.Message}",
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
                    return System.Text.Json.JsonSerializer.Deserialize<DadosOrcamentoCompleto>(json);
                }
            }
            catch { }
            return null;
        }

        // MÉTODOS DE GERENCIAMENTO

        private void BtnGerenciarNumeracao_Click(object sender, RoutedEventArgs e)
        {
            // Cria uma janela de diálogo para gerenciamento
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

            // Área de informações
            var scrollViewer = new ScrollViewer();
            var stackInfo = new StackPanel { Margin = new Thickness(10) };

            try
            {
                // Status atual do SequenciaManager
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

                // Contadores atuais dos relatórios
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

                // Últimos números registrados
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

            // Botões de ação
            var stackBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Botão Ver Detalhes
            var btnDetalhes = new Button
            {
                Content = "🔍 Detalhes",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5),
                ToolTip = "Ver detalhes completos da numeração"
            };
            btnDetalhes.Click += (s, args) =>
            {
                dialog.Close();
                VerificarNumeracao();
            };

            // Botão Reset Simples
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

            // Botão Reset Completo
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

            // Botão Fechar
            var btnFechar = new Button
            {
                Content = "✖️ Fechar",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnFechar.Click += (s, args) => dialog.Close();

            stackBotoes.Children.Add(btnDetalhes);
            stackBotoes.Children.Add(btnResetSimples);
            stackBotoes.Children.Add(btnResetCompleto);
            stackBotoes.Children.Add(btnFechar);

            stackInfo.Children.Add(stackBotoes);
            scrollViewer.Content = stackInfo;
            Grid.SetRow(scrollViewer, 0);
            grid.Children.Add(scrollViewer);

            // Área de aviso
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
                    // Cria uma nova instância com valores iniciais
                    var novosNumeros = new SequenciaManager.NumerosSequenciais
                    {
                        ProximaOS = 3001,
                        ProximoOrcamento = 500
                    };

                    // Salva substituindo o arquivo existente
                    SequenciaManager.SalvarNumeros(novosNumeros);

                    MessageBox.Show("✅ JSON resetado com sucesso!\n" +
                                  "Próxima OS: OS_003001\n" +
                                  "Próximo Orçamento: ORC_000500",
                                  "Sucesso",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Atualiza a interface
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
                    // Cria backup com timestamp
                    string backupDir = @"dados\backup";
                    Directory.CreateDirectory(backupDir);
                    string backupPath = Path.Combine(backupDir, $"numeros_sequenciais_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    File.Copy(caminhoArquivo, backupPath, true);

                    MessageBox.Show($"✅ Backup criado em:\n{backupPath}",
                                  "Backup",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Reset para valores iniciais
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
            // 1. Reset do arquivo de numeração
            var novosNumeros = new SequenciaManager.NumerosSequenciais
            {
                ProximaOS = 3001,
                ProximoOrcamento = 500
            };

            SequenciaManager.SalvarNumeros(novosNumeros);

            // 2. Atualizar números nos relatórios existentes (opcional)
            var relatorios = RelatorioOSmanager.Relatorios.ToList();

            // Ordena OS por ID e renumera
            int contadorOS = 3000;
            foreach (var os in relatorios.Where(r => r.Tipo == "OS").OrderBy(r => r.Id))
            {
                os.Numero = ++contadorOS;
            }

            // Ordena Orçamentos por ID e renumera
            int contadorOrc = 499; // Começa em 499 porque vai incrementar para 500
            foreach (var orc in relatorios.Where(r => r.Tipo == "Orçamento").OrderBy(r => r.Id))
            {
                orc.Numero = ++contadorOrc;
            }

            // Salva os relatórios atualizados
            SalvarRelatoriosAtualizados(relatorios);

            // 3. Recarrega tudo
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

        private void VerificarNumeracao()
        {
            // Verifica o SequenciaManager
            int proximaOS = SequenciaManager.VisualizarProximaOS();
            int proximoOrc = SequenciaManager.VisualizarProximoOrcamento();

            // Verifica os relatórios
            var maiorOS = _todosRelatorios.Where(r => r.Tipo == "OS" && r.Numero > 0)
                                         .OrderByDescending(r => r.Numero)
                                         .FirstOrDefault();

            var maiorOrc = _todosRelatorios.Where(r => r.Tipo == "Orçamento" && r.Numero > 0)
                                          .OrderByDescending(r => r.Numero)
                                          .FirstOrDefault();

            string mensagem = $"=== VERIFICAÇÃO DE NUMERAÇÃO ===\n\n";
            mensagem += $"📊 STATUS DO SEQUENCIAMANAGER:\n";
            mensagem += $"• Próxima OS: {proximaOS} (OS_{proximaOS:000000})\n";
            mensagem += $"• Próximo Orçamento: {proximoOrc} (ORC_{proximoOrc:000000})\n\n";

            mensagem += $"📈 RELATÓRIOS REGISTRADOS:\n";
            mensagem += $"• Maior OS: {(maiorOS != null ? maiorOS.NumeroFormatado : "Nenhuma")}\n";
            mensagem += $"• Maior Orçamento: {(maiorOrc != null ? maiorOrc.NumeroFormatado : "Nenhum")}\n";
            mensagem += $"• Total OS: {_todosRelatorios.Count(r => r.Tipo == "OS")}\n";
            mensagem += $"• Total Orçamentos: {_todosRelatorios.Count(r => r.Tipo == "Orçamento")}\n\n";

            mensagem += $"📋 LISTA COMPLETA DE OS:\n";
            foreach (var os in _todosRelatorios.Where(r => r.Tipo == "OS").OrderBy(r => r.Numero))
            {
                mensagem += $"  {os.NumeroFormatado} - {os.Cliente} - {os.Data}\n";
            }

            mensagem += $"\n📋 LISTA COMPLETA DE ORÇAMENTOS:\n";
            foreach (var orc in _todosRelatorios.Where(r => r.Tipo == "Orçamento").OrderBy(r => r.Numero))
            {
                mensagem += $"  {orc.NumeroFormatado} - {orc.Cliente} - {orc.Data}\n";
            }

            // Cria uma janela de scroll para a mensagem
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 400,
                Width = 500
            };

            var textBlock = new TextBlock
            {
                Text = mensagem,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            scrollViewer.Content = textBlock;

            var dialog = new Window
            {
                Title = "🔍 Verificação de Numeração",
                Content = scrollViewer,
                Width = 550,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            dialog.ShowDialog();
        }
    }
}