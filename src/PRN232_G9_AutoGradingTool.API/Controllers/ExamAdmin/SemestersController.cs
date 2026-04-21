using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.API.Controllers.ExamAdmin
{
    [ApiController]
    [Route("api/cms/grading/semesters")]
    public class SemestersController : ControllerBase
    {
        private readonly PRN232_G9_AutoGradingToolDbContext _db;

        public SemestersController(PRN232_G9_AutoGradingToolDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Semester>>> GetAll()
            => await _db.Semesters.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Semester>> Get(Guid id)
        {
            var semester = await _db.Semesters.FindAsync(id);
            return semester == null ? NotFound() : Ok(semester);
        }

        [HttpPost]
        public async Task<ActionResult<Semester>> Create(Semester input)
        {
            _db.Semesters.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Semester input)
        {
            if (id != input.Id) return BadRequest();
            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var semester = await _db.Semesters.FindAsync(id);
            if (semester == null) return NotFound();
            _db.Semesters.Remove(semester);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
