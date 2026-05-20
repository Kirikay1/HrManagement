namespace HrManagement.Model;

public class EmployeeCardModel : HrManagement.Controllers.ControllerBase
{
    private int id;
    private int idEmployeeDepartment;
    private int idPosition;
    private int? directSupervisor;
    private int? assistantEmployee;
    private string? departmentName;
    private string? positionName;
    private string? fullName;
    private string? personalPhone;
    private System.DateTime? birthDate;
    private string? workPhone;
    private string? email;
    private string? employeeOffice;
    private string? other;
    private System.DateTime? employmentEndDate;
    private EmployeeSnapshot? backup;

    public int Id { get => id; set { id = value; OnPropertyChanged(); } }
    public int IdEmployeeDepartment { get => idEmployeeDepartment; set { idEmployeeDepartment = value; OnPropertyChanged(); } }
    public int IdPosition { get => idPosition; set { idPosition = value; OnPropertyChanged(); } }
    public int? DirectSupervisor { get => directSupervisor; set { directSupervisor = value; OnPropertyChanged(); } }
    public int? AssistantEmployee { get => assistantEmployee; set { assistantEmployee = value; OnPropertyChanged(); } }
    public string? DepartmentName { get => departmentName; set { departmentName = value; OnPropertyChanged(); } }
    public string? PositionName { get => positionName; set { positionName = value; OnPropertyChanged(); } }
    public string? FullName { get => fullName; set { fullName = value; OnPropertyChanged(); } }
    public string? Email { get => email; set { email = value; OnPropertyChanged(); } }
    public string? EmployeeOffice { get => employeeOffice; set { employeeOffice = value; OnPropertyChanged(); } }
    public string? PersonalPhone { get => personalPhone; set { personalPhone = value; OnPropertyChanged(); } }
    public System.DateTime? BirthDate { get => birthDate; set { birthDate = value; OnPropertyChanged(); } }
    public string? WorkPhone { get => workPhone; set { workPhone = value; OnPropertyChanged(); } }
    public string? Other { get => other; set { other = value; OnPropertyChanged(); } }
    public System.DateTime? EmploymentEndDate { get => employmentEndDate; set { employmentEndDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsDismissed)); OnPropertyChanged(nameof(IsDismissedRecently)); } }

    public bool IsDismissed => EmploymentEndDate.HasValue;
    public bool IsDismissedRecently => EmploymentEndDate.HasValue && EmploymentEndDate.Value.Date >= System.DateTime.Today.AddDays(-30);

    public void BeginEdit() => backup = new EmployeeSnapshot(this);
    public void CancelEdit() { if (backup is null) return; backup.Apply(this); }

    private sealed class EmployeeSnapshot
    {
        private readonly EmployeeCardModel model;
        public EmployeeSnapshot(EmployeeCardModel model) { this.model = new EmployeeCardModel { Id = model.Id, IdEmployeeDepartment = model.IdEmployeeDepartment, IdPosition = model.IdPosition, DirectSupervisor = model.DirectSupervisor, AssistantEmployee = model.AssistantEmployee, DepartmentName = model.DepartmentName, PositionName = model.PositionName, FullName = model.FullName, PersonalPhone = model.PersonalPhone, BirthDate = model.BirthDate, WorkPhone = model.WorkPhone, Email = model.Email, EmployeeOffice = model.EmployeeOffice, Other = model.Other, EmploymentEndDate = model.EmploymentEndDate }; }
        public void Apply(EmployeeCardModel target) { target.Id = model.Id; target.IdEmployeeDepartment = model.IdEmployeeDepartment; target.IdPosition = model.IdPosition; target.DirectSupervisor = model.DirectSupervisor; target.AssistantEmployee = model.AssistantEmployee; target.DepartmentName = model.DepartmentName; target.PositionName = model.PositionName; target.FullName = model.FullName; target.PersonalPhone = model.PersonalPhone; target.BirthDate = model.BirthDate; target.WorkPhone = model.WorkPhone; target.Email = model.Email; target.EmployeeOffice = model.EmployeeOffice; target.Other = model.Other; target.EmploymentEndDate = model.EmploymentEndDate; }
    }
}
