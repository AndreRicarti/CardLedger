using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController(
    IInvoiceService invoiceService,
    ICsvParserService csvParserService)
    : ControllerBase
{
    [HttpPost("import")]
    public async Task<ActionResult<ImportResponse>> ImportInvoice([FromQuery] string source = "nubank", IFormFile? file = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Arquivo não fornecido" });

        if (!source.Equals("nubank", StringComparison.CurrentCultureIgnoreCase))
            return BadRequest(new { message = "Apenas Nubank é suportado no momento" });

        try
        {
            await using var stream = file.OpenReadStream();

            var transactions = await csvParserService.ParseNubankCsvAsync(stream, file.FileName);

            var imported = await invoiceService.ImportTransactionsAsync(transactions);

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

    [HttpGet("key/{invoiceKey}/transactions-by-category")]
    public async Task<ActionResult<List<TransactionsByCategoryResponse>>> GetTransactionsByCategory(string invoiceKey, [FromQuery] string? category = null)
    {
        var result = await invoiceService.GetTransactionsByCategoryAsync(invoiceKey, category);

        if (result == null)
            return NotFound(new { message = "Nenhuma transação encontrada para esta chave" });

        return Ok(result);
    }

    [HttpGet("{year}/{month}")]
    public async Task<ActionResult<MonthlyInvoice>> GetMonthlyInvoice(int year, int month)
    {
        var invoiceKey = $"{year}-{month:D2}";
        var invoice = await invoiceService.GetInvoiceByKeyAsync(invoiceKey);
        if (invoice == null)
            return NotFound(new { message = "Nenhuma fatura encontrada para este mês" });

        return Ok(invoice);
    }
}