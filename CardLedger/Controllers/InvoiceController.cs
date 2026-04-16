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

        public InvoiceController(IInvoiceService invoiceService, ICsvParserService csvParserService)
        {
            _invoiceService = invoiceService;
            _csvParserService = csvParserService;
        }

        /// <summary>
        /// Importar fatura CSV do Nubank
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ImportResponse>> ImportInvoice([FromQuery] string source = "nubank", IFormFile? file = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Arquivo não fornecido" });

            if (source.ToLower() != "nubank")
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
        /// Listar todas as faturas (agrupadas por mês)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<MonthlyInvoice>>> GetAllInvoices()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        /// <summary>
        /// Fatura de um mês específico
        /// </summary>
        [HttpGet("{year}/{month}")]
        public async Task<ActionResult<MonthlyInvoice>> GetMonthlyInvoice(int year, int month)
        {
            var invoice = await _invoiceService.GetMonthlyInvoiceAsync(year, month);
            if (invoice == null)
                return NotFound(new { message = "Nenhuma fatura encontrada para este mês" });

            return Ok(invoice);
        }

        /// <summary>
        /// Resumo por categoria de um mês específico
        /// </summary>
        [HttpGet("{year}/{month}/summary")]
        public async Task<ActionResult<MonthlySummary>> GetMonthlySummary(int year, int month)
        {
            var summary = await _invoiceService.GetMonthlySummaryAsync(year, month);
            return Ok(summary);
        }

        /// <summary>
        /// Fatura por chave (formato: YYYY-MM, ex: 2026-03)
        /// </summary>
        [HttpGet("key/{invoiceKey}")]
        public async Task<ActionResult<MonthlyInvoice>> GetInvoiceByKey(string invoiceKey)
        {
            var invoice = await _invoiceService.GetInvoiceByKeyAsync(invoiceKey);
            if (invoice == null)
                return NotFound(new { message = "Nenhuma fatura encontrada para esta chave" });

            return Ok(invoice);
        }

        /// <summary>
        /// Resumo por categoria de uma fatura específica (por chave)
        /// </summary>
        [HttpGet("key/{invoiceKey}/summary")]
        public async Task<ActionResult<MonthlySummary>> GetMonthlySummaryByKey(string invoiceKey)
        {
            var summary = await _invoiceService.GetMonthlySummaryByKeyAsync(invoiceKey);
            return Ok(summary);
        }
    }
}

