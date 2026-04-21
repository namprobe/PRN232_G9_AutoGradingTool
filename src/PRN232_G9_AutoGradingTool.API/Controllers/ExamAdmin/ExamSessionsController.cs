using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.API.Controllers.ExamAdmin
{
    [ApiController]
    [Route("api/cms/grading/exam-sessions")]
    public class ExamSessionsController : ControllerBase
    {
        private readonly PRN232_G9_AutoGradingToolDbContext _db;

        public ExamSessionsController(PRN232_G9_AutoGradingToolDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamSession>>> GetAll([FromQuery] Guid? semesterId = null)
        {
            var query = _db.ExamSessions.AsQueryable();
            if (semesterId.HasValue)
                query = query.Where(e => e.SemesterId == semesterId.Value);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExamSession>> Get(Guid id)
        {
            var session = await _db.ExamSessions.FindAsync(id);
            return session == null ? NotFound() : Ok(session);
        }

        [HttpPost]
        public async Task<ActionResult<ExamSession>> Create(ExamSession input)
        {
            _db.ExamSessions.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ExamSession input)
        {
            if (id != input.Id) return BadRequest();
            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var session = await _db.ExamSessions.FindAsync(id);
            if (session == null) return NotFound();
            _db.ExamSessions.Remove(session);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
