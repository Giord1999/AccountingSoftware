// File: Controllers/AccountsController.cs
using AccountingSystem.Models;
using AccountingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccountingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireContabileOrAdmin")]
    public class AccountsController(IAccountService accountService, ILogger<AccountsController> logger) : ControllerBase
    {
        private readonly IAccountService _accountService = accountService;
        private readonly ILogger<AccountsController> _logger = logger;

        /// <summary>
        /// Restituisce tutti i conti associati a una specifica azienda.
        /// </summary>
        [HttpGet("company/{companyId:guid}")]
        public async Task<IActionResult> GetAccountsByCompany(Guid companyId)
        {
            try
            {
                var accounts = await _accountService.GetAccountsByCompanyAsync(companyId);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dei conti per l'azienda {CompanyId}", companyId);
                return StatusCode(500, "Errore interno del server.");
            }
        }

        /// <summary>
        /// Restituisce un conto specifico dato l'ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? companyId = null)
        {
            try
            {
                var account = await _accountService.GetAccountByIdAsync(id, companyId);
                if (account == null)
                    return NotFound($"Nessun conto trovato con ID {id}");

                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del conto {AccountId}", id);
                return StatusCode(500, "Errore interno del server.");
            }
        }

        /// <summary>
        /// Crea un nuovo conto.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Account account)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized("Utente non autenticato.");

                var created = await _accountService.CreateAccountAsync(account, userId);
                _logger.LogInformation("Creato conto {AccountCode} dall'utente {UserId}", created.Code, userId);

                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del conto {AccountCode}", account.Code);
                return StatusCode(500, "Errore interno del server.");
            }
        }

        /// <summary>
        /// Aggiorna un conto esistente.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Account account)
        {
            if (id != account.Id)
                return BadRequest("L'ID nel corpo non coincide con quello della route.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized("Utente non autenticato.");

                var updated = await _accountService.UpdateAccountAsync(id, account, userId);
                _logger.LogInformation("Aggiornato conto {AccountId} dall'utente {UserId}", id, userId);

                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Nessun conto trovato con ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento del conto {AccountId}", id);
                return StatusCode(500, "Errore interno del server.");
            }
        }

        /// <summary>
        /// Elimina un conto.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Unauthorized("Utente non autenticato.");

                await _accountService.DeleteAccountAsync(id, userId);
                _logger.LogInformation("Eliminato conto {AccountId} dall'utente {UserId}", id, userId);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Nessun conto trovato con ID {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del conto {AccountId}", id);
                return StatusCode(500, "Errore interno del server.");
            }
        }
    }
}