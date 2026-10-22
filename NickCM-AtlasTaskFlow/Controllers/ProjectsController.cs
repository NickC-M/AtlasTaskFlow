using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NickCM_AtlasTaskFlow.Data;
using NickCM_AtlasTaskFlow.Models;
using NickCM_AtlasTaskFlow.ViewModels;
using System.ComponentModel.DataAnnotations;

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

        // Checks whether the current user manages this project.
        private async Task<bool> IsProjectManagerAsync(
            int projectId, string userId)
        {
            return await _context.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == projectId &&
                pm.UserId == userId &&
                pm.Role == ProjectRole.Manager);
        }

        // GET: /Projects/ManageMembers/5
        [HttpGet]
        public async Task<IActionResult> ManageMembers(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectManagerAsync(id, userId))
            {
                return NotFound();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new { p.Id, p.Name })
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectName = project.Name;

            ViewBag.Members = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == id)
                .Include(pm => pm.User)
                .OrderBy(pm => pm.User.DisplayName)
                .ToListAsync();

            return View(new NickCM_AtlasTaskFlow.ViewModels
                .AddProjectMemberViewModel
            {
                ProjectId = id
            });
        }

        // POST: /Projects/ManageMembers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageMembers(
            NickCM_AtlasTaskFlow.ViewModels
                .AddProjectMemberViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectManagerAsync(model.ProjectId, userId))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return await ReloadManageMembersView(model);
            }

            var email = model.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "No registered user was found with that email address.");

                return await ReloadManageMembersView(model);
            }

            var alreadyMember = await _context.ProjectMembers
                .AnyAsync(pm =>
                    pm.ProjectId == model.ProjectId &&
                    pm.UserId == user.Id);

            if (alreadyMember)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This user is already a member of the project.");

                return await ReloadManageMembersView(model);
            }

            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = model.ProjectId,
                UserId = user.Id,
                Role = ProjectRole.Member,
                JoinedAtUtc = DateTime.UtcNow
            });

            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = model.ProjectId,
                ActorId = userId,
                Action = "MemberAdded",
                Details = $"Added {user.Email} to the project.",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Member added successfully.";

            return RedirectToAction(
                nameof(ManageMembers),
                new { id = model.ProjectId });
        }

        private async Task<IActionResult> ReloadManageMembersView(
            NickCM_AtlasTaskFlow.ViewModels
                .AddProjectMemberViewModel model)
        {
            ViewBag.ProjectName = await _context.Projects
                .Where(p => p.Id == model.ProjectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

            ViewBag.Members = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == model.ProjectId)
                .Include(pm => pm.User)
                .OrderBy(pm => pm.User.DisplayName)
                .ToListAsync();

            return View("ManageMembers", model);
        }

    }
}