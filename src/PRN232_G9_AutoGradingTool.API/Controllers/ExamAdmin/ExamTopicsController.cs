using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.API.Controllers.ExamAdmin
{
    [ApiController]
    [Route("api/cms/grading/exam-topics")]
    public class ExamTopicsController : ControllerBase
    {
        private readonly PRN232_G9_AutoGradingToolDbContext _db;

        public ExamTopicsController(PRN232_G9_AutoGradingToolDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamTopic>>> GetAll([FromQuery] Guid? examSessionId = null)
        {
            var query = _db.ExamTopics.Include(t => t.Questions).AsQueryable();
            if (examSessionId.HasValue)
                query = query.Where(t => t.ExamSessionId == examSessionId.Value);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExamTopic>> Get(Guid id)
        {
            var topic = await _db.ExamTopics
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);
            return topic == null ? NotFound() : Ok(topic);
        }

        [HttpPost]
        public async Task<ActionResult<ExamTopic>> Create(ExamTopic input)
        {
            _db.ExamTopics.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ExamTopic input)
        {
            if (id != input.Id) return BadRequest();
            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var topic = await _db.ExamTopics.FindAsync(id);
            if (topic == null) return NotFound();
            _db.ExamTopics.Remove(topic);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
