//using ChineseAuction.Api.Dtos;
//using ChineseAuction.Api.Services;
//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace ChineseAuction.Api.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class DonorController : ControllerBase
//    {
//        private readonly IDonorService _service;

//        public DonorController(IDonorService service)
//        {
//            _service = service;
//        }

//        // GET: api/donor
//        // מחזיר את כל התורמים (קיצור)
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<DonorDto>>> GetAll()
//        {
//            var list = await _service.GetAllAsync();
//            return Ok(list);
//        }

//        // GET: api/donor/{id}
//        // מחזיר פרטי תורם לפי id (כולל מתנות)
//        [HttpGet("{id:int}")]
//        public async Task<ActionResult<DonorDetailDto>> GetById(int id)
//        {
//            var dto = await _service.GetByIdAsync(id);
//            if (dto == null) return NotFound();
//            return Ok(dto);
//        }

//        // GET: api/donor/by-email?email=x@x
//        // מחזיר תורמים לפי אימייל (exact)
//        [HttpGet("by-email")]
//        public async Task<ActionResult<IEnumerable<DonorDto>>> GetByEmail([FromQuery] string email)
//        {
//            if (string.IsNullOrWhiteSpace(email)) return BadRequest("email is required");
//            var list = await _service.GetByEmailAsync(email);
//            return Ok(list);
//        }

//        // GET: api/donor/by-name?name=abc
//        // מחזיר תורמים לפי שם (חיפוש חלקי)
//        [HttpGet("by-name")]
//        public async Task<ActionResult<IEnumerable<DonorDto>>> GetByName([FromQuery] string name)
//        {
//            if (string.IsNullOrWhiteSpace(name)) return BadRequest("name is required");
//            var list = await _service.GetByNameAsync(name);
//            return Ok(list);
//        }

//        // POST: api/donor
//        // יוצר תורם; ניתן לצרף מתנות חדשות או קישורים למתנות קיימות. מחזיר 201 ו-body עם ה-id החדש
//        [HttpPost]
//        public async Task<ActionResult<int>> Create([FromBody] DonorCreateDto dto)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            try
//            {
//                var id = await _service.CreateAsync(dto);
//                return CreatedAtAction(nameof(GetById), new { id }, id);
//            }
//            catch (InvalidOperationException ex)
//            {
//                return Conflict(new { message = ex.Message });
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//            catch
//            {
//                return StatusCode(500, "Internal server error");
//            }
//        }

//        // PUT: api/donor/{id}
//        // מעדכן פרטי תורם
//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> Update(int id, [FromBody] DonorUpdateDto dto)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            try
//            {
//                var updated = await _service.UpdateAsync(id, dto);
//                if (!updated) return NotFound();
//                return NoContent();
//            }
//            catch (InvalidOperationException ex)
//            {
//                return Conflict(new { message = ex.Message });
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//            catch
//            {
//                return StatusCode(500, "Internal server error");
//            }
//        }

//        // DELETE: api/donor/{id}
//        // מוחק תורם ומחזיר את הרשימה המעודכנת של תורמים
//        [HttpDelete("{id:int}")]
//        public async Task<ActionResult<IEnumerable<DonorDto>>> Delete(int id)
//        {
//            try
//            {
//                var updatedList = await _service.DeleteAsync(id);
//                return Ok(updatedList);
//            }
//            catch
//            {
//                return StatusCode(500, "Internal server error");
//            }
//        }
//    }
//}   


using ChineseAuction.Api.Dtos;
using ChineseAuction.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChineseAuction.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class DonorController : ControllerBase
    {
        private readonly IDonorService _service;

        public DonorController(IDonorService service)
        {
            _service = service;
        }

        /// <summary>
        /// שליפת רשימת כל התורמים (תצוגה מקוצרת)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DonorDto>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        /// <summary>
        /// שליפת תורם ספציפי כולל רשימת המתנות שלו
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DonorDetailDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound(new { message = $"Donor with ID {id} not found" }) : Ok(result);
        }

        /// <summary>
        /// חיפוש תורמים לפי אימייל
        /// </summary>
        [HttpGet("search/email")]
        public async Task<ActionResult<IEnumerable<DonorDto>>> GetByEmail([FromQuery] string email)
            => Ok(await _service.GetByEmailAsync(email));

        /// <summary>
        /// חיפוש תורמים לפי שם
        /// </summary>
        [HttpGet("search/name")]
        public async Task<ActionResult<IEnumerable<DonorDto>>> GetByName([FromQuery] string name)
            => Ok(await _service.GetByNameAsync(name));


        /// <summary>
        /// יצירת תורם חדש במערכת
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] DonorCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, id);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
     

        /// <summary>
        /// עדכון פרטי תורם קיים
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DonorUpdateDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(id, dto);
                return success ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// מחיקת תורם מהמערכת והחזרת הרשימה המעודכנת
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<IEnumerable<DonorDto>>> Delete(int id)
            => Ok(await _service.DeleteAsync(id));
    }
}
