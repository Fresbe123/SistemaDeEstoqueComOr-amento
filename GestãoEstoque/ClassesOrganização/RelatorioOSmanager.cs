using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GestãoEstoque.ClassesOrganização
{
    public static class RelatorioOSmanager
    {
        private static string CaminhoArquivo = @"dados\relatorios_os.json";
        private static ObservableCollection<RelatorioOS> _relatorios;

        public static ObservableCollection<RelatorioOS> Relatorios
        {
            get
            {
                if (_relatorios == null)
                {
                    CarregarRelatorios();
                }
                return _relatorios;
            }
            set { _relatorios = value; }
        }

        private static void CarregarRelatorios()
        {
            try
            {
                if (File.Exists(CaminhoArquivo))
                {
                    string json = File.ReadAllText(CaminhoArquivo);
                    var lista = JsonConvert.DeserializeObject<List<RelatorioOS>>(json);
                    _relatorios = new ObservableCollection<RelatorioOS>(lista);

                    SincronizarSequenciaisComManager();
                }
                else
                {
                    _relatorios = new ObservableCollection<RelatorioOS>();
                }
            }
            catch (Exception ex)
            {
                _relatorios = new ObservableCollection<RelatorioOS>();
                Console.WriteLine($"Erro ao carregar relatórios: {ex.Message}");
            }
        }

        // Sincroniza com SequenciaManager
        private static void SincronizarSequenciaisComManager()
        {
            if (!_relatorios.Any()) return;

            // Encontra o maior número de OS
            var maiorOS = _relatorios.Where(r => r.Tipo == "OS" && r.Numero > 0)
                                     .OrderByDescending(r => r.Numero)
                                     .FirstOrDefault();
            if (maiorOS != null)
            {
                SequenciaManager.SincronizarNumeroOS(maiorOS.Numero);
            }

            // Encontra o maior número de Orçamento
            var maiorOrc = _relatorios.Where(r => r.Tipo == "Orçamento" && r.Numero > 0)
                                      .OrderByDescending(r => r.Numero)
                                      .FirstOrDefault();
            if (maiorOrc != null)
            {
                SequenciaManager.SincronizarNumeroOrcamento(maiorOrc.Numero);
            }
        }

        private static void SalvarRelatorios()
        {
            try
            {
                string diretorio = Path.GetDirectoryName(CaminhoArquivo);
                if (!Directory.Exists(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                string json = JsonConvert.SerializeObject(Relatorios.ToList(), Formatting.Indented);
                File.WriteAllText(CaminhoArquivo, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar relatórios: {ex.Message}");
            }
        }

        public static void Registrar(RelatorioOS r)
        {
            int novoId = Relatorios.Count > 0 ? Relatorios.Max(x => x.Id) + 1 : 1;
            r.Id = novoId;

            Relatorios.Add(r);
            SalvarRelatorios();

            if (r.Tipo == "OS" && r.Numero > 0)
            {
                SequenciaManager.SincronizarNumeroOS(r.Numero);
            }
            else if (r.Tipo == "Orçamento" && r.Numero > 0)
            {
                SequenciaManager.SincronizarNumeroOrcamento(r.Numero);
            }
        }

        // Método para corrigir números ausentes em relatórios existentes
        public static void CorrigirNumerosAusentes()
        {
            bool correcaoNecessaria = false;

            var osList = Relatorios.Where(r => r.Tipo == "OS" && r.Numero == 0).ToList();
            if (osList.Any())
            {
                correcaoNecessaria = true;
                int numeroAtualOS = 3000;

                var osComNumero = Relatorios.Where(r => r.Tipo == "OS" && r.Numero > 0).ToList();
                if (osComNumero.Any())
                {
                    numeroAtualOS = osComNumero.Max(r => r.Numero);
                }

                foreach (var os in osList)
                {
                    os.Numero = numeroAtualOS + 1;
                    numeroAtualOS++;
                }
            }

            // Verifica Orçamentos sem número
            var orcList = Relatorios.Where(r => r.Tipo == "Orçamento" && r.Numero == 0).ToList();
            if (orcList.Any())
            {
                correcaoNecessaria = true;
                int numeroAtualOrc = 500;

                var orcComNumero = Relatorios.Where(r => r.Tipo == "Orçamento" && r.Numero > 0).ToList();
                if (orcComNumero.Any())
                {
                    numeroAtualOrc = orcComNumero.Max(r => r.Numero);
                }

                foreach (var orc in orcList)
                {
                    orc.Numero = numeroAtualOrc + 1;
                    numeroAtualOrc++;
                }
            }

            // Se houve correção, salva as mudanças
            if (correcaoNecessaria)
            {
                SalvarRelatorios();
                SincronizarSequenciaisComManager();
            }
        }

        public static void Recarregar()
        {
            CarregarRelatorios();
        }
    }
}