using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.API.Controllers.ExamAdmin
{
    [ApiController]
    [Route("api/cms/grading/exam-questions")]
    public class ExamQuestionsController : ControllerBase
    {
        private readonly PRN232_G9_AutoGradingToolDbContext _db;

        public ExamQuestionsController(PRN232_G9_AutoGradingToolDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamQuestion>>> GetAll([FromQuery] Guid? examTopicId = null)
        {
            var query = _db.ExamQuestions.AsQueryable();
            if (examTopicId.HasValue)
                query = query.Where(q => q.ExamTopicId == examTopicId.Value);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExamQuestion>> Get(Guid id)
        {
            var question = await _db.ExamQuestions.FindAsync(id);
            return question == null ? NotFound() : Ok(question);
        }

        [HttpPost]
        public async Task<ActionResult<ExamQuestion>> Create(ExamQuestion input)
        {
            _db.ExamQuestions.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ExamQuestion input)
        {
            if (id != input.Id) return BadRequest();
            _db.Entry(input).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var question = await _db.ExamQuestions.FindAsync(id);
            if (question == null) return NotFound();
            _db.ExamQuestions.Remove(question);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
