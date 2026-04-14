using CardLedger.Models;
using CardLedger.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardLedger.Controllers
{
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
        /// Filtrar transações por ano, mês e/ou categoria
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Transaction>>> FilterTransactions(
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] string? category = null)
        {
            var transactions = await _transactionService.FilterTransactionsAsync(year, month, category);
            return Ok(transactions);
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
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] string category)
        {
            if (string.IsNullOrEmpty(category))
                return BadRequest(new { message = "Categoria não pode estar vazia" });

            var updated = await _transactionService.UpdateCategoryAsync(id, category);
            if (!updated)
                return NotFound(new { message = "Transação não encontrada" });

            return Ok(new { message = "Categoria atualizada com sucesso" });
        }
    }
}
