using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NickCM_AtlasTaskFlow.Data;
using NickCM_AtlasTaskFlow.Models;
using NickCM_AtlasTaskFlow.ViewModels;

namespace NickCM_AtlasTaskFlow.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //GET: /Projects
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var projects = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .Select(pm => pm.Project)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync();

            return View(projects);
        }

        //GET: /Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        //POST: /Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProjectCreateViewModel model)
        {
            if (model.StartDate.HasValue &&
                model.EndDate.HasValue &&
                model.EndDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "The end date cannot be earlier than the start date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            var project = new Project
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedById = user.Id
            };

            project.Members.Add(new ProjectMember
            {
                UserId = user.Id,
                Role = ProjectRole.Manager,
                JoinedAtUtc = DateTime.UtcNow
            });

            project.Activities.Add(new ProjectActivity
            {
                ActorId = user.Id,
                Action = "ProjectCreated",
                Details = $"Created project '{project.Name}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            _context.Projects.Add(project);

            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Project created successfully.";

            return RedirectToAction(nameof(Details),
                new { id = project.Id });
        }

        //GET: /Projects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == id &&
                    p.Members.Any(m => m.UserId == userId))
                .Include(p => p.Members)
                    .ThenInclude(m => m.User)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Owner)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }
    }
}