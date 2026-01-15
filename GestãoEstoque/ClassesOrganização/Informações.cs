using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestãoEstoque.ClassesOrganização
{
    public class Informações
    {
        // Classe para representar os dados do emitente
        public class Emitente
        {
            public string Nome { get; set; } = "[NOME DA SUA EMPRESA]";
            public string CNPJ { get; set; } = "[00.000.000/0001-00]";
            public string Endereco { get; set; } = "[ENDEREÇO COMPLETO]";
            public string Bairro { get; set; } = "[BAIRRO]";
            public string Cidade { get; set; } = "[CIDADE]";
            public string Estado { get; set; } = "[UF]";
            public string CEP { get; set; } = "[00.000-000]";
            public string Telefone { get; set; } = "[(00) 0000-0000]";
            public string Email { get; set; } = "[seu@email.com]";
        }

        // Classe para representar os dados do destinatário
        public class Destinatario
        {
            public string Nome { get; set; } = "________________________";
            public string CNPJ_CPF { get; set; } = "________________";
            public string Endereco { get; set; } = "________________________";
            public string Bairro { get; set; } = "_______________";
            public string Cidade { get; set; } = "_______________";
            public string Estado { get; set; } = "__";
            public string CEP { get; set; } = "___________";
            public string Telefone { get; set; } = "________________";
            public string Email { get; set; } = "________________________";
        }

        // Classe para dados adicionais (compartilhada)
        public class DadosAdicionais
        {
            public string Data { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
            public string HoraKM { get; set; } = "__________________";
            public string Veiculo { get; set; } = "__________________";
            public string Modelo { get; set; } = "__________________";
            public string Marca { get; set; } = "__________________";
            public string Cliente { get; set; } = "__________________";
            public string Telefone { get; set; } = "__________________";
            public string PatrimonioPlaca { get; set; } = "__________________";
        }

        // Classe para dados da transportadora (apenas para orçamento)
        public class Transportadora
        {
            public string Nome { get; set; } = "________________________";
            public string TipoFrete { get; set; } = "_______";
            public string NumeroDocumento { get; set; } = "_______";
            public string Validade { get; set; } = "_______";
            public string Emissao { get; set; } = "__/__/____";
            public string CondicaoPagamento { get; set; } = "________________";
            public string Vendedor { get; set; } = "________________";
            public string Observacoes { get; set; } = "________________________";
        }

        // Classe para deslocamento (compartilhada)
        public class Deslocamento
        {
            public decimal KmPercorridos { get; set; }
            public decimal ValorPorKm { get; set; }
            public decimal ValorDeslocamento { get; set; }
        }

        // CLASSE PARA ORDEM DE SERVIÇO (OS)
        public class OrdemServico
        {
            // Identificação
            public int NumeroOS { get; set; } // Ex: 3001, 3002, 3003...
            public DateTime DataEmissao { get; set; }

            // Dados do estabelecimento
            public Emitente Emitente { get; set; } = new Emitente();

            // Dados do cliente
            public Destinatario Destinatario { get; set; } = new Destinatario();

            // Dados do veículo/serviço
            public DadosAdicionais DadosVeiculo { get; set; } = new DadosAdicionais();

            // Custos
            public Deslocamento Deslocamento { get; set; } = new Deslocamento();
            public decimal ValorMaoDeObra { get; set; }

            // Itens da OS
            public List<ItemEstoque> Itens { get; set; } = new List<ItemEstoque>();
            public decimal ValorTotal { get; set; }

            // Informações adicionais da OS
            public string Vendedor { get; set; } = "________________";
            public string Observacoes { get; set; } = "________________________";

            // Construtor
            public OrdemServico()
            {
                DataEmissao = DateTime.Now;
            }
        }

        // CLASSE PARA ORÇAMENTO (mantém a original)
        public class Orcamento
        {
            // Identificação
            public int NumeroOrcamento { get; set; } // Ex: 500, 501, 502...
            public DateTime DataEmissao { get; set; }
            public DateTime DataValidade { get; set; } = DateTime.Now.AddDays(30);

            // Dados do estabelecimento
            public Emitente Emitente { get; set; } = new Emitente();

            // Dados do cliente
            public Destinatario Destinatario { get; set; } = new Destinatario();

            // Dados adicionais
            public DadosAdicionais DadosAdicionais { get; set; } = new DadosAdicionais();

            // Dados da transportadora (apenas para orçamento)
            public Transportadora Transportadora { get; set; } = new Transportadora();

            // Custos
            public Deslocamento Deslocamento { get; set; } = new Deslocamento();
            public decimal ValorMaoDeObra { get; set; }

            // Itens do orçamento
            public List<ItemEstoque> Itens { get; set; } = new List<ItemEstoque>();
            public decimal ValorTotal { get; set; }

            // Propriedades calculadas
            public decimal ValorTotalComDeslocamento => ValorTotal + (Deslocamento?.ValorDeslocamento ?? 0);

            // Construtor
            public Orcamento()
            {
                DataEmissao = DateTime.Now;
            }
        }
    }
}