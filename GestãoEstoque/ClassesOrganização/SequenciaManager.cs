// ClassesOrganização/SequenciaManager.cs
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

namespace GestãoEstoque.ClassesOrganização
{
    public class SequenciaManager
    {
        private static string CaminhoArquivo = @"dados\numeros_sequenciais.json";

        public class NumerosSequenciais
        {
            public int ProximaOS { get; set; } = 3001; // Começa em 3001
            public int ProximoOrcamento { get; set; } = 500; // Começa em 500
        }

        public static NumerosSequenciais CarregarNumeros()
        {
            try
            {
                if (File.Exists(CaminhoArquivo))
                {
                    string json = File.ReadAllText(CaminhoArquivo);
                    return JsonConvert.DeserializeObject<NumerosSequenciais>(json);
                }
            }
            catch { }

            return new NumerosSequenciais();
        }

        public static void SalvarNumeros(NumerosSequenciais numeros)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CaminhoArquivo));
                string json = JsonConvert.SerializeObject(numeros, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(CaminhoArquivo, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar números sequenciais: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método CORRIGIDO: Retorna o próximo número e INCREMENTA
        public static int ObterProximaOS()
        {
            var numeros = CarregarNumeros();
            int numero = numeros.ProximaOS;
            numeros.ProximaOS++;
            SalvarNumeros(numeros);
            return numero;
        }

        // Método CORRIGIDO: Retorna o próximo número e INCREMENTA
        public static int ObterProximoOrcamento()
        {
            var numeros = CarregarNumeros();
            int numero = numeros.ProximoOrcamento;
            numeros.ProximoOrcamento++;
            SalvarNumeros(numeros);
            return numero;
        }

        // Para visualizar o próximo número SEM incrementar
        public static int VisualizarProximaOS()
        {
            var numeros = CarregarNumeros();
            return numeros.ProximaOS;
        }

        public static int VisualizarProximoOrcamento()
        {
            var numeros = CarregarNumeros();
            return numeros.ProximoOrcamento;
        }

        // NOVO MÉTODO: Para usar quando já tem o número
        public static void SincronizarNumeroOS(int numeroUsado)
        {
            var numeros = CarregarNumeros();
            // Se o número usado for maior ou igual ao próximo, atualiza
            if (numeroUsado >= numeros.ProximaOS)
            {
                numeros.ProximaOS = numeroUsado + 1;
                SalvarNumeros(numeros);
            }
        }

        // NOVO MÉTODO: Para usar quando já tem o número
        public static void SincronizarNumeroOrcamento(int numeroUsado)
        {
            var numeros = CarregarNumeros();
            // Se o número usado for maior ou igual ao próximo, atualiza
            if (numeroUsado >= numeros.ProximoOrcamento)
            {
                numeros.ProximoOrcamento = numeroUsado + 1;
                SalvarNumeros(numeros);
            }
        }
    }
}