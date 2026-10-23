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

            return RedirectToAction(nameof(Details), new { id = task.Id });

        }


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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(
    TaskCommentCreateViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var task = await _context.TaskItems
                .Where(t =>
                    t.Id == model.TaskItemId &&
                    t.Project.Members.Any(m => m.UserId == userId))
                .Select(t => new
                {
                    t.Id,
                    t.ProjectId,
                    t.Title
                })
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound();

            model.Content = model.Content?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please enter a comment of 1–2,000 characters.";

                return RedirectToAction(
                    nameof(Details), new { id = task.Id });
            }

            var comment = new TaskComment
            {
                TaskItemId = task.Id,
                AuthorId = userId,
                Content = model.Content,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.TaskComments.Add(comment);

            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = task.ProjectId,
                TaskItemId = task.Id,
                ActorId = userId,
                Action = "TaskCommentAdded",
                Details = $"Added a comment to task '{task.Title}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully.";

            return RedirectToAction(nameof(Details), new { id = task.Id });
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var task = await _context.TaskItems
                .AsNoTracking()
                .Where(t =>
                    t.Id == id &&
                    t.Project.Members.Any(m => m.UserId == userId))
                .Include(t => t.Project)
                .Include(t => t.Owner)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound();

            task.Comments = task.Comments
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToList();

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var task = await _context.TaskItems
                .AsNoTracking()
                .Where(t =>
                    t.Id == id &&
                    t.Project.Members.Any(m => m.UserId == userId))
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound();

            await PopulateMembersAsync(task.ProjectId);

            var model = new TaskEditViewModel
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                Status = task.Status,
                OwnerId = task.OwnerId
            };

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> MyTasks()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(t =>
                    t.OwnerId == userId &&
                    t.Project.Members.Any(m => m.UserId == userId))
                .Include(t => t.Project)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return View(tasks);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskEditViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            //load the task using project ID
            var task = await _context.TaskItems
                .Where(t =>
                    t.Id == model.Id &&
                    t.Project.Members.Any(m => m.UserId == userId))
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound();

            //validate selected owner against project
            if (model.OwnerId != null &&
                !await IsProjectMemberAsync(task.ProjectId, model.OwnerId))
            {
                ModelState.AddModelError(
                    nameof(model.OwnerId),
                    "The selected user is not a member of this project.");
            }

            if (!Enum.IsDefined(typeof(TaskPriority), model.Priority))
            {
                ModelState.AddModelError(
                    nameof(model.Priority), "Invalid priority.");
            }

            if (!Enum.IsDefined(typeof(TaskStatus), model.Status))
            {
                ModelState.AddModelError(
                    nameof(model.Status), "Invalid status.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateMembersAsync(task.ProjectId);
                return View(model);
            }

            var oldTitle = task.Title;
            var oldStatus = task.Status;
            var oldOwnerId = task.OwnerId;

            task.Title = model.Title.Trim();
            task.Description = model.Description?.Trim();
            task.DueDate = model.DueDate;
            task.Priority = model.Priority;
            task.Status = model.Status;
            task.OwnerId = model.OwnerId;

            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = task.ProjectId,
                TaskItemId = task.Id,
                ActorId = userId,
                Action = "TaskUpdated",
                Details = $"Updated task '{oldTitle}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            if (oldStatus != task.Status)
            {
                _context.ProjectActivities.Add(new ProjectActivity
                {
                    ProjectId = task.ProjectId,
                    TaskItemId = task.Id,
                    ActorId = userId,
                    Action = "TaskStatusChanged",
                    Details = $"Changed status from {oldStatus} to {task.Status}.",
                    OccurredAtUtc = DateTime.UtcNow
                });
            }

            if (oldOwnerId != task.OwnerId)
            {
                _context.ProjectActivities.Add(new ProjectActivity
                {
                    ProjectId = task.ProjectId,
                    TaskItemId = task.Id,
                    ActorId = userId,
                    Action = "TaskOwnershipChanged",
                    Details = "Changed task ownership.",
                    OccurredAtUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task updated successfully.";

            return RedirectToAction(nameof(Details), new { id = task.Id });
        }

    


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            //only allow project managerss to delete the task
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var isManager = await _context.ProjectMembers
                .AnyAsync(m =>
                    m.ProjectId == task.ProjectId &&
                    m.UserId == userId &&
                    m.Role == ProjectRole.Manager);

            if (!isManager)
                return View("~/Views/Shared/AccessDenied.cshtml");

            var projectId = task.ProjectId;
            var taskTitle = task.Title;

            //preserve the activity history, remove FK reference to task that is deleted
            var existingActivities = await _context.ProjectActivities
                .Where(a => a.TaskItemId == id)
                .ToListAsync();

            foreach (var activity in existingActivities)
            {
                activity.TaskItemId = null;
            }


            //record the deletion before removing the task.
            _context.ProjectActivities.Add(new ProjectActivity
            {
                ProjectId = projectId,
                TaskItemId = null,
                ActorId = userId,
                Action = "TaskDeleted",
                Details = $"Deleted task '{taskTitle}'.",
                OccurredAtUtc = DateTime.UtcNow
            });

            _context.TaskItems.Remove(task);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { id = projectId });
        }
    }
}