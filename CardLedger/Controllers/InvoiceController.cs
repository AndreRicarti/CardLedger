using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICsvParserService _csvParserService;
        private readonly ITransactionService _transactionService;

        public InvoiceController(IInvoiceService invoiceService, ICsvParserService csvParserService, ITransactionService transactionService)
        {
            _invoiceService = invoiceService;
            _csvParserService = csvParserService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Importar fatura CSV do Nubank
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ImportResponse>> ImportInvoice(
            [FromQuery] string source = "nubank",
            IFormFile? file = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Arquivo não fornecido" });

            if (!source.Equals("nubank", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest(new { message = "Apenas Nubank é suportado no momento" });

            try
            {
                using var stream = file.OpenReadStream();

                var transactions = await _csvParserService.ParseNubankCsvAsync(stream, file.FileName);

                var imported = await _invoiceService.ImportTransactionsAsync(transactions);

                var invoiceKeys = transactions
                    .Where(t => !string.IsNullOrEmpty(t.InvoiceKey))
                    .Select(t => t.InvoiceKey)
                    .Distinct()
                    .ToList();

                return Ok(new ImportResponse { Imported = imported, Months = invoiceKeys });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Alterar a categoria de uma transação de uma fatura específica
        /// </summary>
        [HttpPatch("key/{invoiceKey}/transactions/{id}/category")]
        public async Task<IActionResult> UpdateTransactionCategory(string invoiceKey, int id, [FromBody] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest(new { message = "Categoria não pode estar vazia" });

            var updated = await _transactionService.UpdateCategoryByInvoiceAsync(invoiceKey, id, category);
            if (!updated)
                return NotFound(new { message = "Transação não encontrada para esta fatura" });

            return Ok(new { message = "Categoria atualizada com sucesso" });
        }

        /// <summary>
        /// Summary da fatura por InvoiceKey com breakdown por categoria
        /// </summary>
        [HttpGet("key/{invoiceKey}/summary")]
        public async Task<ActionResult<InvoiceSummary>> GetInvoiceSummary(string invoiceKey)
        {
            var summary = await _invoiceService.GetInvoiceSummaryByKeyAsync(invoiceKey);
            if (summary == null)
                return NotFound(new { message = "Nenhuma fatura encontrada para esta chave" });

            return Ok(summary);
        }

        /// <summary>
        /// Transações de uma fatura agrupadas por categoria
        /// </summary>
        [HttpGet("key/{invoiceKey}/transactions-by-category")]
        public async Task<ActionResult<List<TransactionsByCategoryResponse>>> GetTransactionsByCategory(string invoiceKey)
        {
            var result = await _invoiceService.GetTransactionsByCategoryAsync(invoiceKey);
            if (result == null)
                return NotFound(new { message = "Nenhuma transação encontrada para esta chave" });

            return Ok(result);
        }

        /// <summary>
        /// Fatura de um mês específico
        /// </summary>
        [HttpGet("{year}/{month}")]
        public async Task<ActionResult<MonthlyInvoice>> GetMonthlyInvoice(int year, int month)
        {
            // Converter year/month para InvoiceKey e usar busca por chave
            var invoiceKey = $"{year}-{month:D2}";
            var invoice = await _invoiceService.GetInvoiceByKeyAsync(invoiceKey);
            if (invoice == null)
                return NotFound(new { message = "Nenhuma fatura encontrada para este mês" });

            return Ok(invoice);
        }
    }
}

