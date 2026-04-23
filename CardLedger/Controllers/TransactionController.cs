using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers
{
    public sealed class UpdateCategoryRequest
    {
        public int CategoryId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Retornar todas as categorias distintas
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<CategoryOption>>> GetCategories()
        {
            var categories = await _transactionService.GetCategoriesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Obter uma transação específica
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var transaction = await _transactionService.GetTransactionAsync(id);
            if (transaction == null)
                return NotFound(new { message = "Transação não encontrada" });

            return Ok(transaction);
        }

        /// <summary>
        /// Atualizar categoria de uma transação
        /// </summary>
        [HttpPatch("{id}/category")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            if (request is null || request.CategoryId <= 0)
                return BadRequest(new { message = "CategoryId inválido" });

            var updated = await _transactionService.UpdateCategoryAsync(id, request.CategoryId);
            if (!updated)
                return NotFound(new { message = "Transação ou categoria não encontrada" });

            return Ok(new { message = "Categoria atualizada com sucesso" });
        }
    }
}
