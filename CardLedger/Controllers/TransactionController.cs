using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryOption>>> GetCategories()
    {
        var categories = await transactionService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpDelete("{invoiceKey}")]
    public async Task<IActionResult> DeleteByInvoiceKey(string invoiceKey)
    {
        var deleted = await transactionService.DeleteByInvoiceKeyAsync(invoiceKey);

        return deleted
            ? Ok(new { message = "Transações excluídas com sucesso" })
            : NotFound(new { message = "Nenhuma transação encontrada para este InvoiceKey" });
    }

    [HttpPatch("{id:int}/category")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request)
    {
        if (request.CategoryId <= 0)
            return BadRequest(new { message = "CategoryId inválido" });

        var updated = await transactionService.UpdateCategoryAsync(id, request.CategoryId);

        return updated
            ? Ok(new { message = "Categoria atualizada com sucesso" })
            : NotFound(new { message = "Transação ou categoria não encontrada" });
    }

    [HttpPatch("{invoiceKey}/{id:int}/title")]
    public async Task<IActionResult> UpdateTitle(
        string invoiceKey,
        int id,
        [FromBody] UpdateTitleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title inválido" });

        var updated = await transactionService.UpdateTitleAsync(invoiceKey, id, request.Title.Trim());

        return updated
            ? Ok(new { message = "Título atualizado com sucesso" })
            : NotFound(new { message = "Transação não encontrada para este InvoiceKey" });
    }
}