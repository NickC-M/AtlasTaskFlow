using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NickCM_AtlasTaskFlow.Data;
using NickCM_AtlasTaskFlow.Models;
using NickCM_AtlasTaskFlow.ViewModels;
using TaskStatus = NickCM_AtlasTaskFlow.Models.TaskStatus;

namespace NickCM_AtlasTaskFlow.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> IsProjectMemberAsync(
            int projectId, string userId)
        {
            return await _context.ProjectMembers.AnyAsync(
                pm => pm.ProjectId == projectId &&
                      pm.UserId == userId);
        }

        //GET: /Tasks/Index/5
        public async Task<IActionResult> Index(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectMemberAsync(id, userId))
            {
                return NotFound();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new { p.Id, p.Name })
                .FirstOrDefaultAsync();

            if (project == null)
                return NotFound();

            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;

            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(t => t.ProjectId == id)
                .Include(t => t.Owner)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return View(tasks);
        }

        //GET: /Tasks/Create/5
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectMemberAsync(projectId, userId))
            {
                return NotFound();
            }

            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId);

            if (!projectExists)
                return NotFound();

            await PopulateMembersAsync(projectId);

            return View(new TaskCreateViewModel
            {
                ProjectId = projectId
            });
        }

        //POST: /Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TaskCreateViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectMemberAsync(model.ProjectId, userId))
            {
                return NotFound();
            }

            if (model.OwnerId != null)
            {
                var ownerIsMember =
                    await IsProjectMemberAsync(
                        model.ProjectId, model.OwnerId);

                if (!ownerIsMember)
                {
                    ModelState.AddModelError(
                        nameof(model.OwnerId),
                        "The selected user is not a member of this project.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateMembersAsync(model.ProjectId);
                return View(model);
            }

            var task = new TaskItem
            {
                ProjectId = model.ProjectId,
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                DueDate = model.DueDate,
                Priority = model.Priority,
                Status = NickCM_AtlasTaskFlow.Models.TaskStatus.ToDo,
                OwnerId = model.OwnerId
            };

            _context.TaskItems.Add(task);

            //save first so task has an ID
            await _context.SaveChangesAsync();


            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = model.ProjectId,
                TaskItemId = task.Id,
                ActorId = userId,
                Action = "TaskCreated",
                Details = $"Created task '{task.Title}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task created successfully.";

            return RedirectToAction(
                nameof(Index),
                new { id = model.ProjectId });
        }

        private async Task PopulateMembersAsync(int projectId)
        {
            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => new
                {
                    pm.UserId,
                    Name = pm.User.DisplayName ?? pm.User.Email
                })
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Members = new SelectList(
                members,
                "UserId",
                "Name");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            //only allow project members to access this task
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Project.Members.Any(
                        m => m.UserId == userId));

            if (task == null)
            {
                return NotFound();
            }

            var projectId = task.ProjectId;

            //never overwrite an existing owner
            if (task.OwnerId != null)
            {
                TempData["ErrorMessage"] =
                    "This task has already been claimed.";

                return RedirectToAction(
                    nameof(Index),
                    new { id = projectId });
            }

            //claim the task
            task.OwnerId = userId;

            //record the ownership change
            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = projectId,
                TaskItemId = task.Id,
                ActorId = userId,
                Action = "TaskClaimed",
                Details = $"Claimed task '{task.Title}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            try
            {
                //RowVersion detects conflicting updates
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Task claimed successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "This task was claimed or changed by another user. " +
                    "Refresh the task list and try again.";
            }

            return RedirectToAction(
                nameof(Index),
                new { id = projectId });
        }


        // GET: /Tasks/Board/5
        public async Task<IActionResult> Board(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null ||
                !await IsProjectMemberAsync(id, userId))
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
            ViewBag.ProjectId = project.Id;

            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(t => t.ProjectId == id)
                .Include(t => t.Owner)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return View(tasks);
        }

        // POST: /Tasks/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int taskId, TaskStatus status)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!Enum.IsDefined(typeof(TaskStatus), status))
            {
                return BadRequest(new
                {
                    message = "Invalid task status."
                });
            }

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t =>
                    t.Id == taskId &&
                    t.Project.Members.Any(
                        m => m.UserId == userId));

            if (task == null)
            {
                return NotFound();
            }

            var oldStatus = task.Status;

            if (oldStatus == status)
            {
                return Ok(new { success = true });
            }

            task.Status = status;

            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = task.ProjectId,
                TaskItemId = task.Id,
                ActorId = userId,
                Action = "TaskStatusChanged",
                Details =
                    $"Changed '{task.Title}' from {oldStatus} to {status}.",
                OccurredAtUtc = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message =
                        "This task was changed by another user. " +
                        "Refresh the board and try again."
                });
            }
        }


        // GET: /Tasks/Details/12
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var task = await _context.TaskItems
                .AsNoTracking()
                .Where(t =>
                    t.Id == id &&
                    t.Project.Members.Any(
                        m => m.UserId == userId))
                .Include(t => t.Project)
                .Include(t => t.Owner)
                .FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }
    }
}