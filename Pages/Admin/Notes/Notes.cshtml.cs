using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Notes
{
    [Authorize(Roles = "Admin,Teacher")]
    public class NotesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;


        public NotesModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Dropdowny
        [BindProperty]
    public int? SelectedStudentId { get; set; }

    [BindProperty]
    public int? SelectedTeacherId { get; set; }

    // Nová poznámka
    [BindProperty]
    public string NewNoteText { get; set; }

    // Data pro dropdowny
    public List<Student> Students { get; set; }
    public List<Teacher> Teachers { get; set; }

    // Vybraný student/učitel
    public Student SelectedStudent { get; set; }
    public Teacher SelectedTeacher { get; set; }

    // Poznámky
    public List<Note> Notes { get; set; }

    public void OnGet()
    {
        LoadDropdowns();
    }

    public IActionResult OnPostSelect()
    {
        LoadDropdowns();
        LoadSelected();
        LoadNotes();
        return Page();
    }

    public async Task<IActionResult> OnPostAddNoteAsync()
    {
        LoadDropdowns();
        LoadSelected();

        if (string.IsNullOrWhiteSpace(NewNoteText))
        {
            LoadNotes();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);

        var note = new Note
        {
            StudentId = SelectedStudentId,
            TeacherId = SelectedTeacherId,
            Text = NewNoteText,
            CreatedBy = user.Name
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        LoadNotes();
        return Page();
    }

    private void LoadDropdowns()
    {
        Students = _context.Students.OrderBy(s => s.LastName).ToList();
            Teachers = _context.Teachers
        .Include(t => t.User)
        .OrderBy(t => t.User.FirstName)
        .ThenBy(t => t.User.LastName)
        .ToList();

        }

        private void LoadSelected()
    {
        if (SelectedStudentId != null)
            SelectedStudent = _context.Students.FirstOrDefault(s => s.Id == SelectedStudentId);

        if (SelectedTeacherId != null)
            SelectedTeacher = _context.Teachers.FirstOrDefault(t => t.Id == SelectedTeacherId);
    }

    private void LoadNotes()
    {
        Notes = _context.Notes
            .Where(n =>
                (SelectedStudentId != null && n.StudentId == SelectedStudentId) ||
                (SelectedTeacherId != null && n.TeacherId == SelectedTeacherId)
            )
            .OrderByDescending(n => n.Created)
            .ToList();
    }
}
}

