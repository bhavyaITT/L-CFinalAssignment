using PRM.Application.DTOs;
using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.Project;
using PRM.Application.DTOs.Users;
using PRM.Client.Services;
using PRM.Client.UI;

namespace PRM.Client.Flows;

public sealed class AdminFlow(PrmApiClient api, UserSession session)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteWelcomeHeader(session.FullName);
            Console.WriteLine("ADMIN PANEL");
            Console.WriteLine();
            Console.WriteLine("1. Manage Employees");
            Console.WriteLine("2. Manage Projects");
            Console.WriteLine("3. View All Allocations");
            Console.WriteLine("4. Manage Users");
            Console.WriteLine("5. System Configuration");
            Console.WriteLine("6. Logout");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 6))
            {
                case 1: await ManageEmployeesAsync(ct); break;
                case 2: await ManageProjectsAsync(ct); break;
                case 3: await ViewAllocationsAsync(ct); break;
                case 4: await ManageUsersAsync(ct); break;
                case 5: await SystemConfigurationAsync(ct); break;
                case 6: session.Logout(); return;
            }
        }
    }

    // ── Employees ───────────────────────────────────────────────

    private async Task ManageEmployeesAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MANAGE EMPLOYEES");
            Console.WriteLine("1. View All Employees");
            Console.WriteLine("2. Update Employee");
            Console.WriteLine("3. Deactivate Employee");
            Console.WriteLine("4. Manage Employee Skills");
            Console.WriteLine("5. Assign Manager");
            Console.WriteLine("6. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 6))
            {
                case 1: await ViewAllEmployeesAsync(ct); break;
                case 2: await UpdateEmployeeAsync(ct); break;
                case 3: await DeactivateEmployeeAsync(ct); break;
                case 4: await ManageSkillsAsync(ct); break;
                case 5: await AssignManagerAsync(ct); break;
                case 6: return;
            }
        }
    }

    private async Task ViewAllEmployeesAsync(CancellationToken ct)
    {
        var result = await api.GetEmployeesAsync(ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        var data = result.Data!;
        var employees = data.Employees.ToList();
        string? statusFilter = null;
        string? deptFilter = null;

        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("ALL EMPLOYEES");
            Console.WriteLine($"{"ID",-5} {"Name",-18} {"Department",-12} {"Status"}");
            ConsoleUi.WriteDivider();

            var filtered = employees.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(statusFilter))
                filtered = filtered.Where(e => e.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(deptFilter))
                filtered = filtered.Where(e => e.Department.Contains(deptFilter, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            foreach (var e in list)
                Console.WriteLine($"{e.Id,-5} {e.FullName,-18} {e.Department,-12} {e.Status.ToUpperInvariant()}");

            ConsoleUi.WriteDivider();
            Console.WriteLine($"Total: {list.Count}   |   Allocated: {list.Count(e => e.Status.Equals("Allocated", StringComparison.OrdinalIgnoreCase))}   |   Bench: {list.Count(e => e.Status.Equals("Bench", StringComparison.OrdinalIgnoreCase))}");
            Console.WriteLine();
            Console.WriteLine("[F] Filter by Status / Department     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "F")
            {
                statusFilter = ConsoleUi.ReadLine("Status (Bench/Allocated) or Enter to clear: ");
                if (string.IsNullOrWhiteSpace(statusFilter)) statusFilter = null;
                deptFilter = ConsoleUi.ReadLine("Department contains or Enter to clear: ");
                if (string.IsNullOrWhiteSpace(deptFilter)) deptFilter = null;
            }
        }
    }

    private async Task UpdateEmployeeAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("UPDATE EMPLOYEE");
        var idText = ConsoleUi.ReadRequired("Enter Employee ID: ");
        if (!int.TryParse(idText, out var id)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var existing = await api.GetEmployeeAsync(id, ct);
        if (!existing.IsSuccess) { ConsoleUi.ShowApiError(existing.Error); return; }

        var emp = existing.Data!;
        Console.WriteLine();
        Console.WriteLine($"── {emp.FullName} ─────────────────────────────────");
        var fullName = ConsoleUi.ReadLine($"Full Name ({emp.FullName}): ");
        var email = ConsoleUi.ReadLine($"Email ({emp.Email}): ");
        var department = ConsoleUi.ReadLine($"Department ({emp.Department}): ");
        var designation = ConsoleUi.ReadLine($"Designation ({emp.Designation}): ");
        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");

        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var request = new UpdateEmployeeRequest(
            string.IsNullOrWhiteSpace(fullName) ? emp.FullName : fullName,
            string.IsNullOrWhiteSpace(email) ? emp.Email : email,
            string.IsNullOrWhiteSpace(department) ? emp.Department : department,
            string.IsNullOrWhiteSpace(designation) ? emp.Designation : designation);

        var result = await api.UpdateEmployeeAsync(id, request, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Employee updated."); ConsoleUi.Pause(); }
    }

    private async Task DeactivateEmployeeAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("DEACTIVATE EMPLOYEE");
        var idText = ConsoleUi.ReadRequired("Enter Employee ID: ");
        if (!int.TryParse(idText, out var id)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var empResult = await api.GetEmployeeAsync(id, ct);
        if (!empResult.IsSuccess) { ConsoleUi.ShowApiError(empResult.Error); return; }
        var emp = empResult.Data!;

        var allocResult = await api.GetAllocationsAsync(id, activeOnly: true, ct: ct);
        var allocations = allocResult.IsSuccess ? allocResult.Data!.Allocations.ToList() : [];

        Console.WriteLine();
        Console.WriteLine($"── {emp.FullName} ─────────────────────────────────");
        Console.WriteLine($"Department : {emp.Department}");
        Console.WriteLine($"Status     : {emp.Status.ToUpperInvariant()}");
        Console.WriteLine();

        if (allocations.Count > 0)
        {
            Console.WriteLine($"Warning: This employee has {allocations.Count} active allocation(s).");
            Console.WriteLine("Ending their employment will remove them from:");
            foreach (var a in allocations)
                Console.WriteLine($"  - {a.ProjectName}  ({a.UtilisationPercentage}%,  ends {ConsoleUi.FormatDate(a.ToDate)})");
            Console.WriteLine();
        }

        Console.WriteLine($"Are you sure you want to deactivate {emp.FullName}?");
        Console.WriteLine("This will end all active allocations today and block their login account.");
        Console.WriteLine("[Y] Yes, Deactivate     [B] Cancel");

        if (!ConsoleUi.Confirm("Confirm")) return;

        var result = await api.DeactivateEmployeeAsync(id, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Employee deactivated."); ConsoleUi.Pause(); }
    }

    private async Task ManageSkillsAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("MANAGE SKILLS");
        var idText = ConsoleUi.ReadRequired("Enter Employee ID: ");
        if (!int.TryParse(idText, out var employeeId)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var empResult = await api.GetEmployeeAsync(employeeId, ct);
        if (!empResult.IsSuccess) { ConsoleUi.ShowApiError(empResult.Error); return; }

        while (true)
        {
            var skillsResult = await api.GetEmployeeSkillsAsync(employeeId, ct);
            if (!skillsResult.IsSuccess) { ConsoleUi.ShowApiError(skillsResult.Error); return; }
            var skills = skillsResult.Data!;

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MANAGE SKILLS");
            Console.WriteLine($"── {empResult.Data!.FullName} ─────────────────────────────────");
            Console.WriteLine("Current Skills:");
            for (var i = 0; i < skills.Count; i++)
                Console.WriteLine($"  {i + 1}.  {skills[i].SkillName,-18} {skills[i].Proficiency}");
            ConsoleUi.WriteDivider();
            Console.WriteLine("1. Add Skill");
            Console.WriteLine("2. Update Proficiency Level");
            Console.WriteLine("3. Remove Skill");
            Console.WriteLine("4. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 4))
            {
                case 1:
                    await AddSkillAsync(employeeId, ct);
                    break;
                case 2:
                    if (skills.Count == 0) { ConsoleUi.ShowError("No skills to update."); ConsoleUi.Pause(); break; }
                    var updIdx = ConsoleUi.ReadMenuOption("Enter skill #: ", 1, skills.Count) - 1;
                    await UpdateProficiencyAsync(skills[updIdx].Id, ct);
                    break;
                case 3:
                    if (skills.Count == 0) { ConsoleUi.ShowError("No skills to remove."); ConsoleUi.Pause(); break; }
                    var remIdx = ConsoleUi.ReadMenuOption("Enter skill #: ", 1, skills.Count) - 1;
                    if (ConsoleUi.Confirm($"Remove {skills[remIdx].SkillName}?"))
                    {
                        var rem = await api.RemoveSkillAsync(skills[remIdx].Id, ct);
                        if (!rem.IsSuccess) ConsoleUi.ShowApiError(rem.Error);
                        else { ConsoleUi.ShowSuccess("Skill removed."); ConsoleUi.Pause(); }
                    }
                    break;
                case 4:
                    return;
            }
        }
    }

    private async Task AddSkillAsync(int employeeId, CancellationToken ct)
    {
        Console.WriteLine();
        var name = ConsoleUi.ReadRequired("Skill Name        : ");
        Console.WriteLine("Category          : (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        var catChoice = ConsoleUi.ReadMenuOption("Enter choice      : ", 1, 5);
        var category = catChoice switch { 1 => "Backend", 2 => "Frontend", 3 => "DevOps", 4 => "QA", _ => "Other" };
        Console.WriteLine("Proficiency Level : (1) Beginner  (2) Intermediate  (3) Advanced");
        var profChoice = ConsoleUi.ReadMenuOption("Enter choice      : ", 1, 3);
        var proficiency = profChoice switch { 1 => "Beginner", 2 => "Intermediate", _ => "Advanced" };

        var result = await api.AddSkillAsync(employeeId, new AddSkillRequest(name, category, proficiency), ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Skill added."); ConsoleUi.Pause(); }
    }

    private async Task UpdateProficiencyAsync(int skillId, CancellationToken ct)
    {
        Console.WriteLine("Proficiency Level : (1) Beginner  (2) Intermediate  (3) Advanced");
        var profChoice = ConsoleUi.ReadMenuOption("Enter choice: ", 1, 3);
        var proficiency = profChoice switch { 1 => "Beginner", 2 => "Intermediate", _ => "Advanced" };
        var result = await api.UpdateSkillProficiencyAsync(skillId, new UpdateSkillProficiencyRequest(proficiency), ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Proficiency updated."); ConsoleUi.Pause(); }
    }

    private async Task AssignManagerAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("ASSIGN MANAGER");
        var empIdText = ConsoleUi.ReadRequired("Employee User ID : ");
        var mgrIdText = ConsoleUi.ReadRequired("Manager User ID  : ");
        if (!int.TryParse(empIdText, out var empId) || !int.TryParse(mgrIdText, out var mgrId))
        {
            ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return;
        }

        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var result = await api.AssignManagerAsync(empId, mgrId, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Manager assigned successfully."); ConsoleUi.Pause(); }
    }

    // ── Projects ────────────────────────────────────────────────

    private async Task ManageProjectsAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MANAGE PROJECTS");
            Console.WriteLine("1. Create Project");
            Console.WriteLine("2. View All Projects");
            Console.WriteLine("3. Update Project Details");
            Console.WriteLine("4. Manage Milestones");
            Console.WriteLine("5. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 5))
            {
                case 1: await CreateProjectAsync(ct); break;
                case 2: await ViewAllProjectsAsync(ct); break;
                case 3: await UpdateProjectAsync(ct); break;
                case 4: await ManageMilestonesAsync(ct); break;
                case 5: return;
            }
        }
    }

    private async Task CreateProjectAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("CREATE PROJECT");
        var name = ConsoleUi.ReadRequired("Project Name        : ");
        var description = ConsoleUi.ReadLine("Description         : ");
        var startDate = ConsoleUi.ReadDateRequired("Start Date");
        var endDate = ConsoleUi.ReadDateRequired("End Date");
        Console.WriteLine("Status              : (1) PLANNED   (2) ACTIVE   (3) ON_HOLD");
        var statusChoice = ConsoleUi.ReadMenuOption("Enter choice: ", 1, 3);
        var status = statusChoice switch { 1 => "Planned", 2 => "Active", _ => "OnHold" };
        var managerIdText = ConsoleUi.ReadRequired("Assign Manager (Manager ID): ");
        if (!int.TryParse(managerIdText, out var managerId)) { ConsoleUi.ShowError("Invalid manager ID."); ConsoleUi.Pause(); return; }

        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var result = await api.CreateProjectAsync(new CreateProjectRequest(name, description, startDate, endDate, status, managerId), ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Project created."); ConsoleUi.Pause(); }
    }

    private async Task ViewAllProjectsAsync(CancellationToken ct)
    {
        var result = await api.GetProjectsAsync(ct);
        if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("ALL PROJECTS");
        Console.WriteLine($"{"ID",-5} {"Name",-18} {"Manager",-14} {"End Date",-12} {"Status",-10} {"Health"}");
        ConsoleUi.WriteDivider();

        foreach (var p in result.Data!.Projects)
            Console.WriteLine($"{p.Id,-5} {p.Name,-18} {p.ManagerName,-14} {ConsoleUi.FormatDate(p.EndDate),-12} {p.Status,-10} {ConsoleUi.HealthWithEmoji(p.Health)}");

        ConsoleUi.WriteDivider();
        Console.WriteLine("[B] Back");
        ConsoleUi.ReadLine("");
    }

    private async Task UpdateProjectAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("UPDATE PROJECT DETAILS");
        var idText = ConsoleUi.ReadRequired("Enter Project ID: ");
        if (!int.TryParse(idText, out var id)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        var existing = await api.GetProjectAsync(id, ct);
        if (!existing.IsSuccess) { ConsoleUi.ShowApiError(existing.Error); return; }
        var p = existing.Data!;

        Console.WriteLine();
        Console.WriteLine($"── {p.Name} ───────────────────────────────");
        var name = ConsoleUi.ReadLine($"Project Name ({p.Name}): ");
        var description = ConsoleUi.ReadLine($"Description ({p.Description}): ");
        var startInput = ConsoleUi.ReadLine($"Start Date ({ConsoleUi.FormatDate(p.StartDate)}): ");
        var endInput = ConsoleUi.ReadLine($"End Date ({ConsoleUi.FormatDate(p.EndDate)}): ");
        Console.WriteLine("Status: (1) PLANNED  (2) ACTIVE  (3) ON_HOLD  (4) COMPLETED");
        var statusInput = ConsoleUi.ReadLine("Enter choice or press Enter to keep: ");
        var managerInput = ConsoleUi.ReadLine($"Assign Manager ID ({p.ManagerId}): ");

        var status = p.Status;
        if (!string.IsNullOrWhiteSpace(statusInput) && int.TryParse(statusInput, out var sc))
            status = sc switch { 1 => "Planned", 2 => "Active", 3 => "OnHold", 4 => "Completed", _ => p.Status };

        var startDate = p.StartDate;
        if (!string.IsNullOrWhiteSpace(startInput) && ConsoleUi.TryParseDate(startInput, out var sd)) startDate = sd;
        var endDate = p.EndDate;
        if (!string.IsNullOrWhiteSpace(endInput) && ConsoleUi.TryParseDate(endInput, out var ed)) endDate = ed;
        var managerId = p.ManagerId;
        if (!string.IsNullOrWhiteSpace(managerInput) && int.TryParse(managerInput, out var mid)) managerId = mid;

        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var request = new UpdateProjectRequest(
            string.IsNullOrWhiteSpace(name) ? p.Name : name,
            string.IsNullOrWhiteSpace(description) ? p.Description : description,
            startDate, endDate, status, managerId);

        var result = await api.UpdateProjectAsync(id, request, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Project updated."); ConsoleUi.Pause(); }
    }

    private async Task ManageMilestonesAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("MILESTONES");
        var idText = ConsoleUi.ReadRequired("Enter Project ID: ");
        if (!int.TryParse(idText, out var projectId)) { ConsoleUi.ShowError("Invalid ID."); ConsoleUi.Pause(); return; }

        while (true)
        {
            var project = await api.GetProjectAsync(projectId, ct);
            if (!project.IsSuccess) { ConsoleUi.ShowApiError(project.Error); return; }
            var p = project.Data!;
            var milestones = p.Milestones.ToList();

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MILESTONES");
            Console.WriteLine($"── {p.Name} ───────────────────────────────");
            Console.WriteLine($"{"#",-4} {"Title",-20} {"Due Date",-12} {"Status"}");
            ConsoleUi.WriteDivider();

            for (var i = 0; i < milestones.Count; i++)
                Console.WriteLine($"{i + 1}.  {milestones[i].Title,-18} {ConsoleUi.FormatDate(milestones[i].DueDate),-12} {milestones[i].Status}");

            var done = milestones.Count(m => m.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
            ConsoleUi.WriteDivider();
            Console.WriteLine($"Total: {milestones.Count} milestones   |   Completed: {done}");
            Console.WriteLine();
            Console.WriteLine("1. Add Milestone");
            Console.WriteLine("2. Update Milestone Status");
            Console.WriteLine("3. Back");

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 3))
            {
                case 1:
                    var title = ConsoleUi.ReadRequired("Milestone Title  : ");
                    var due = ConsoleUi.ReadDateRequired("Due Date");
                    var add = await api.AddMilestoneAsync(projectId, new AddMilestoneRequest(title, due), ct);
                    if (!add.IsSuccess) ConsoleUi.ShowApiError(add.Error);
                    else { ConsoleUi.ShowSuccess("Milestone added."); ConsoleUi.Pause(); }
                    break;
                case 2:
                    if (milestones.Count == 0) { ConsoleUi.ShowError("No milestones."); ConsoleUi.Pause(); break; }
                    var idx = ConsoleUi.ReadMenuOption("Enter Milestone #: ", 1, milestones.Count) - 1;
                    Console.WriteLine("New Status: (1) NOT_STARTED  (2) IN_PROGRESS  (3) DONE");
                    var st = ConsoleUi.ReadMenuOption("Enter choice: ", 1, 3);
                    var status = st switch { 1 => "NotStarted", 2 => "InProgress", _ => "Done" };
                    var upd = await api.UpdateMilestoneStatusAsync(milestones[idx].Id, new UpdateMilestoneStatusRequest(status), ct);
                    if (!upd.IsSuccess) ConsoleUi.ShowApiError(upd.Error);
                    else { ConsoleUi.ShowSuccess("Milestone updated."); ConsoleUi.Pause(); }
                    break;
                case 3:
                    return;
            }
        }
    }

    // ── Allocations ─────────────────────────────────────────────

    private async Task ViewAllocationsAsync(CancellationToken ct)
    {
        int? employeeFilter = null;
        int? projectFilter = null;

        while (true)
        {
            var result = await api.GetAllocationsAsync(employeeFilter, projectFilter, true, ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("ALL ALLOCATIONS");
            Console.WriteLine($"{"Employee",-18} {"Project",-18} {"%",-6} {"From",-12} {"To"}");
            ConsoleUi.WriteDivider();

            foreach (var a in result.Data!.Allocations)
                Console.WriteLine($"{a.EmployeeName,-18} {a.ProjectName,-18} {a.UtilisationPercentage,4}%  {ConsoleUi.FormatDate(a.FromDate),-12} {ConsoleUi.FormatDate(a.ToDate)}");

            ConsoleUi.WriteDivider();
            Console.WriteLine($"Total Active Allocations: {result.Data.TotalActiveCount}");
            Console.WriteLine("[F] Filter by Employee / Project     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "F")
            {
                var empInput = ConsoleUi.ReadLine("Employee ID or Enter to clear: ");
                employeeFilter = int.TryParse(empInput, out var eid) ? eid : null;
                var projInput = ConsoleUi.ReadLine("Project ID or Enter to clear: ");
                projectFilter = int.TryParse(projInput, out var pid) ? pid : null;
            }
        }
    }

    // ── Users ───────────────────────────────────────────────────

    private async Task ManageUsersAsync(CancellationToken ct)
    {
        while (true)
        {
            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("MANAGE USERS");
            Console.WriteLine("1. Create User Account");
            Console.WriteLine("2. View All Users");
            Console.WriteLine("3. Reset User Password");
            Console.WriteLine("4. Deactivate User");
            Console.WriteLine("5. Back");
            Console.WriteLine();

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 5))
            {
                case 1: await CreateUserAsync(ct); break;
                case 2: await ViewAllUsersAsync(ct); break;
                case 3: await ResetPasswordAsync(ct); break;
                case 4: await DeactivateUserAsync(ct); break;
                case 5: return;
            }
        }
    }

    private async Task CreateUserAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("CREATE USER ACCOUNT");
        var fullName = ConsoleUi.ReadRequired("Full Name         : ");
        var email = ConsoleUi.ReadRequired("Email             : ");
        var username = ConsoleUi.ReadRequired("Username          : ");
        var tempPassword = ConsoleUi.ReadPassword("Temporary Password: ");
        Console.WriteLine("Role              : (1) Admin  (2) Manager  (3) Employee");
        var roleChoice = ConsoleUi.ReadMenuOption("Enter choice: ", 1, 3);
        var role = roleChoice switch { 1 => "Admin", 2 => "Manager", _ => "Employee" };
        var department = ConsoleUi.ReadRequired("Department        : ");
        var designation = ConsoleUi.ReadRequired("Designation       : ");

        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var result = await api.CreateUserAsync(new CreateUserRequest(fullName, email, username, tempPassword, role, department, designation), ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else
        {
            ConsoleUi.ShowSuccess("Account created. User must change password on first login.");
            ConsoleUi.Pause();
        }
    }

    private async Task ViewAllUsersAsync(CancellationToken ct)
    {
        while (true)
        {
            var result = await api.GetUsersAsync(ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("ALL USERS");
            Console.WriteLine($"{"ID",-5} {"Username",-18} {"Role",-10} {"Status"}");
            ConsoleUi.WriteDivider();

            foreach (var u in result.Data!.Users)
                Console.WriteLine($"{u.Id,-5} {u.Username,-18} {u.Role.ToUpperInvariant(),-10} {(u.IsActive ? "Active" : "Inactive")}");

            ConsoleUi.WriteDivider();
            Console.WriteLine($"Total: {result.Data.TotalCount}   |   Active: {result.Data.ActiveCount}   |   Inactive: {result.Data.InactiveCount}");
            Console.WriteLine("[R] Reactivate a user     [B] Back");

            var action = ConsoleUi.ReadLine("Choice: ").ToUpperInvariant();
            if (action == "B") return;
            if (action == "R")
                await ReactivateUserAsync(ct);
        }
    }

    private async Task ReactivateUserAsync(CancellationToken ct)
    {
        var idText = ConsoleUi.ReadRequired("Enter User ID to reactivate: ");
        var users = await api.GetUsersAsync(ct);
        if (!users.IsSuccess) { ConsoleUi.ShowApiError(users.Error); return; }

        var user = users.Data!.Users.FirstOrDefault(u => u.Id.ToString() == idText);
        if (user is null) { ConsoleUi.ShowError("User not found."); ConsoleUi.Pause(); return; }

        Console.WriteLine();
        Console.WriteLine($"User: {user.FullName} ({user.Role}) — currently {(user.IsActive ? "Active" : "Inactive")}");
        if (!ConsoleUi.Confirm("Reactivate this account?")) return;

        var result = await api.ReactivateUserAsync(user.Username, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else
        {
            ConsoleUi.ShowSuccess($"Account reactivated. {user.FullName} can now log in.");
            Console.WriteLine("Note: Previous allocations are NOT restored. Admin must re-allocate manually if needed.");
            ConsoleUi.Pause();
        }
    }

    private async Task ResetPasswordAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("RESET USER PASSWORD");
        var identifier = ConsoleUi.ReadRequired("Enter Username or User ID: ");

        var users = await api.GetUsersAsync(ct);
        if (!users.IsSuccess) { ConsoleUi.ShowApiError(users.Error); return; }

        var user = users.Data!.Users.FirstOrDefault(u =>
            u.Username.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            u.Id.ToString() == identifier);

        if (user is null) { ConsoleUi.ShowError("User not found."); ConsoleUi.Pause(); return; }

        Console.WriteLine();
        Console.WriteLine($"User found: {user.FullName} ({user.Role})");
        var newPassword = ConsoleUi.ReadPassword("New Temporary Password: ");
        ConsoleUi.WriteDivider();
        Console.WriteLine("[S] Save     [B] Back");
        if (ConsoleUi.ReadLine("Choice: ").ToUpperInvariant() != "S") return;

        var result = await api.ResetPasswordAsync(user.Username, new ResetPasswordRequest(newPassword), ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else
        {
            ConsoleUi.ShowSuccess("Password reset. User will be prompted to change it on next login.");
            ConsoleUi.Pause();
        }
    }

    private async Task DeactivateUserAsync(CancellationToken ct)
    {
        ConsoleUi.ClearScreen();
        ConsoleUi.WriteHeader("DEACTIVATE USER");
        var identifier = ConsoleUi.ReadRequired("Enter Username or User ID: ");

        var users = await api.GetUsersAsync(ct);
        if (!users.IsSuccess) { ConsoleUi.ShowApiError(users.Error); return; }

        var user = users.Data!.Users.FirstOrDefault(u =>
            u.Username.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            u.Id.ToString() == identifier);

        if (user is null) { ConsoleUi.ShowError("User not found."); ConsoleUi.Pause(); return; }

        Console.WriteLine();
        Console.WriteLine($"User found: {user.FullName} ({user.Role})");
        Console.WriteLine($"Status     : {(user.IsActive ? "Active" : "Inactive")}");
        Console.WriteLine();
        Console.WriteLine("Are you sure you want to deactivate this account?");
        Console.WriteLine("Deactivated users cannot log in. Their data is preserved.");

        if (!ConsoleUi.Confirm("Confirm deactivation")) return;

        var result = await api.DeactivateUserAsync(user.Username, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("User deactivated."); ConsoleUi.Pause(); }
    }

    // ── System Config ───────────────────────────────────────────

    private async Task SystemConfigurationAsync(CancellationToken ct)
    {
        while (true)
        {
            var result = await api.GetSystemConfigAsync(ct);
            if (!result.IsSuccess) { ConsoleUi.ShowApiError(result.Error); return; }
            var config = result.Data!;

            ConsoleUi.ClearScreen();
            ConsoleUi.WriteHeader("SYSTEM CONFIGURATION");
            Console.WriteLine("Current Settings:");
            Console.WriteLine($"  LLM Provider        :  {config.LlmProvider}");
            Console.WriteLine($"  LLM API Key         :  {(config.HasApiKey ? "****************************" : "(not set)")}");
            Console.WriteLine($"  Scheduler Interval  :  {config.SchedulerIntervalHours} hours");
            Console.WriteLine($"  Max Weekly Hours    :  {config.MaxWeeklyHours}");
            ConsoleUi.WriteDivider();
            Console.WriteLine("1. Update LLM API Key");
            Console.WriteLine("2. Change LLM Provider  (Gemini / Groq / InHouse)");
            Console.WriteLine("3. Update Scheduler Interval");
            Console.WriteLine("4. Update Max Weekly Hours");
            Console.WriteLine("5. Back");

            switch (ConsoleUi.ReadMenuOption("Enter option: ", 1, 5))
            {
                case 1:
                    var key = ConsoleUi.ReadRequired("New LLM API Key: ");
                    await PatchConfigAsync(new UpdateSystemConfigRequest(null, key, null, null), ct);
                    break;
                case 2:
                    Console.WriteLine("(1) Gemini  (2) Groq  (3) InHouse");
                    var provChoice = ConsoleUi.ReadMenuOption("Enter choice: ", 1, 3);
                    var prov = provChoice switch { 1 => "Gemini", 2 => "Groq", _ => "InHouse" };
                    await PatchConfigAsync(new UpdateSystemConfigRequest(prov, null, null, null), ct);
                    break;
                case 3:
                    var hours = int.Parse(ConsoleUi.ReadRequired("Scheduler interval (hours): "));
                    await PatchConfigAsync(new UpdateSystemConfigRequest(null, null, hours, null), ct);
                    break;
                case 4:
                    var maxHrs = int.Parse(ConsoleUi.ReadRequired("Max weekly hours: "));
                    await PatchConfigAsync(new UpdateSystemConfigRequest(null, null, null, maxHrs), ct);
                    break;
                case 5:
                    return;
            }
        }
    }

    private async Task PatchConfigAsync(UpdateSystemConfigRequest request, CancellationToken ct)
    {
        var result = await api.UpdateSystemConfigAsync(request, ct);
        if (!result.IsSuccess) ConsoleUi.ShowApiError(result.Error);
        else { ConsoleUi.ShowSuccess("Settings updated."); ConsoleUi.Pause(); }
    }
}
