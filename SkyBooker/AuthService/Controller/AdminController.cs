using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using AuthService.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AuthService.Controllers
{
   [ApiController]
   [Route("api/admin")]
   [Authorize(Roles = "Admin")]
   public class AdminController : ControllerBase
   {
      private readonly UserContext _context;

      public AdminController(UserContext context)
      {
         _context = context;
      }

      // GET: api/admin/users
      [HttpGet("users")]
      public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
      {
         var users = await _context.Users
            .Select(u => new
            {
               u.Id,
               u.Username,
               u.EMail,
               u.Role
            })
            .ToListAsync();

         return Ok(users);
      }
   }
}