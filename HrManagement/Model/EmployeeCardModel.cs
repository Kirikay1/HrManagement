namespace HrManagement.Model;

public class EmployeeCardModel
{
    private EmployeeSnapshot? backup;

    public int Id { get; set; }
    public int IdEmployeeDepartment { get; set; }
    public int IdPosition { get; set; }
    public int? DirectSupervisor { get; set; }
    public int? AssistantEmployee { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? EmployeeOffice { get; set; }
    public string? PersonalPhone { get; set; }
    public System.DateTime? BirthDate { get; set; }
    public string? WorkPhone { get; set; }
    public string? Other { get; set; }
    public System.DateTime? EmploymentEndDate { get; set; }

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
