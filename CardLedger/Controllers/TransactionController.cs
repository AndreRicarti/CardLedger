using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers;

public sealed class UpdateCategoryRequest
{
    public int CategoryId { get; set; }
}

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
}