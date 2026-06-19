using System;
using System.Linq;
using System.Threading.Tasks;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMarahuyo.Api.Controllers
{
    [ApiController]
    [Route("api/promos")]
    [Authorize] // Require login. Read allowed for staff, Write restricted to admin
    public class PromosController : ControllerBase
    {
        private readonly PosDbContext _context;

        public PromosController(PosDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Both staff and admin can read this to populate the dropdown
            var promos = await _context.Promos
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Code)
                .ToListAsync();
            return Ok(promos);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] Promo req)
        {
            if (string.IsNullOrEmpty(req.Code) || string.IsNullOrEmpty(req.DiscountType) || req.Value <= 0)
                return BadRequest(new { error = "Code, Type, and Value are required." });

            req.CreatedAt = DateTime.UtcNow;
            _context.Promos.Add(req);
            await _context.SaveChangesAsync();
            return Ok(req);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Promo req)
        {
            var promo = await _context.Promos.FindAsync(id);
            if (promo == null) return NotFound();

            promo.Code = req.Code;
            promo.Category = req.Category;
            promo.DiscountType = req.DiscountType;
            promo.Value = req.Value;
            promo.IsActive = req.IsActive;
            promo.ValidFrom = req.ValidFrom;
            promo.ValidUntil = req.ValidUntil;

            await _context.SaveChangesAsync();
            return Ok(promo);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.Promos.FindAsync(id);
            if (promo == null) return NotFound();

            _context.Promos.Remove(promo);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }
    }
}
