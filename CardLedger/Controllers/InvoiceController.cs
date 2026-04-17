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

