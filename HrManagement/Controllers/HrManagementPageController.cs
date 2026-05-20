using HrManagement.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace HrManagement.Controllers;

public class HrManagementPageController
{
    private const int OfficeMaxLength = 10;
    private static readonly Regex PhoneRegex = new(@"^[0-9+()\-\s#]{0,20}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly List<EmployeeCardModel> allEmployeeCards = new();
    private readonly ObservableCollection<string> validationErrors = new();
    private readonly ObservableCollection<string> eventValidationErrors = new();
    private readonly HrManagementController controller;

    public HrManagementPageController(HrManagementController controller)
    {
        this.controller = controller;
        LoadEmployees();
        LoadReferenceData();
    }

    public ObservableCollection<EmployeeCardModel> EmployeeCards { get; private set; } = new();
    public ObservableCollection<Department> Departments { get; private set; } = new();
    public ObservableCollection<Position> Positions { get; private set; } = new();
    public ObservableCollection<EmployeeLookupModel> DepartmentEmployees { get; private set; } = new();
    public ObservableCollection<EmployeeEventModel> EmployeeEvents { get; private set; } = new();
    public ObservableCollection<string> EventTypeOptions { get; } = new() { "Обучение", "Отгул", "Отпуск" };
    public ObservableCollection<string> ValidationErrors => validationErrors;
    public ObservableCollection<string> EventValidationErrors => eventValidationErrors;

    public string? SelectedEventType { get; set; }
    public DateTime? NewEventStartDate { get; set; } = DateTime.Today;
    public DateTime? NewEventEndDate { get; set; } = DateTime.Today;
    public string? NewEventReason { get; set; }

    public EmployeeCardModel? SelectedEmployee { get; private set; }
    public bool IsEmployeeCardOpen { get; private set; }
    public bool IsEditing { get; private set; }
    public bool IsNewEmployee { get; private set; }
    public string? SelectedDepartmentName { get; private set; }

    private void LoadEmployees(){var employees=controller.GetEmployees();allEmployeeCards.Clear();allEmployeeCards.AddRange(employees.Select(e=>new EmployeeCardModel{Id=e.Id,IdEmployeeDepartment=e.IdEmployeeDepartment,IdPosition=e.IdPosition,DirectSupervisor=e.DirectSupervisor,AssistantEmployee=e.AssistantEmployee,DepartmentName=e.Department?.NameDepartment,PositionName=e.Position?.NamePosition,FullName=e.FullName,PersonalPhone=e.PersonalPhone,BirthDate=e.BirthDate,WorkPhone=e.WorkPhone,Email=e.Email,EmployeeOffice=e.EmployeeOffice,Other=e.Other,EmploymentEndDate=e.EmploymentEndDate}));ApplyDepartmentFilter();}
    private void LoadReferenceData(){Departments=new ObservableCollection<Department>(AppData.Db.Department.ToList());Positions=new ObservableCollection<Position>(AppData.Db.Position.ToList());}
    public void FilterEmployeesByDepartment(string? departmentName){SelectedDepartmentName=string.IsNullOrWhiteSpace(departmentName)?null:departmentName;ApplyDepartmentFilter();}
    private void ApplyDepartmentFilter(){IEnumerable<EmployeeCardModel> q=allEmployeeCards.Where(e=>!e.EmploymentEndDate.HasValue||e.EmploymentEndDate.Value.Date>=DateTime.Today.AddDays(-30));if(!string.IsNullOrWhiteSpace(SelectedDepartmentName))q=q.Where(e=>string.Equals(e.DepartmentName,SelectedDepartmentName,StringComparison.OrdinalIgnoreCase));EmployeeCards=new ObservableCollection<EmployeeCardModel>(q);}

    public void OpenEmployeeCard(EmployeeCardModel? employee){if(employee==null)return;SelectedEmployee=employee;IsEmployeeCardOpen=true;IsEditing=false;IsNewEmployee=false;ValidationErrors.Clear();LoadEmployeeEvents();UpdateDepartmentEmployees(employee.IdEmployeeDepartment);}    
    public void CloseEmployeeCard(){ValidationErrors.Clear();EventValidationErrors.Clear();IsEditing=false;IsEmployeeCardOpen=false;IsNewEmployee=false;}
    public void StartEditEmployee(){if(SelectedEmployee==null)return;SelectedEmployee.BeginEdit();IsEditing=true;}
    public void CancelEditEmployee(){if(SelectedEmployee==null)return;SelectedEmployee.CancelEdit();ValidationErrors.Clear();IsEditing=false;if(IsNewEmployee){IsEmployeeCardOpen=false;IsNewEmployee=false;SelectedEmployee=null;return;}UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);}

    public void SaveEmployee(){if(SelectedEmployee==null||!ValidateSelectedEmployee())return;var creating=IsNewEmployee||SelectedEmployee.Id<=0;var employee=creating?new Employee():AppData.Db.Employee.FirstOrDefault(x=>x.Id==SelectedEmployee.Id);if(employee==null){ValidationErrors.Clear();ValidationErrors.Add("Сотрудник не найден в базе данных.");return;}employee.FullName=SelectedEmployee.FullName;employee.PersonalPhone=SelectedEmployee.PersonalPhone;employee.BirthDate=SelectedEmployee.BirthDate;employee.IdEmployeeDepartment=SelectedEmployee.IdEmployeeDepartment;employee.IdPosition=SelectedEmployee.IdPosition;employee.DirectSupervisor=SelectedEmployee.DirectSupervisor;employee.AssistantEmployee=SelectedEmployee.AssistantEmployee;employee.WorkPhone=SelectedEmployee.WorkPhone;employee.Email=SelectedEmployee.Email;employee.EmployeeOffice=SelectedEmployee.EmployeeOffice;employee.Other=SelectedEmployee.Other;employee.EmploymentEndDate=SelectedEmployee.EmploymentEndDate;SelectedEmployee.DepartmentName=Departments.FirstOrDefault(d=>d.Id==SelectedEmployee.IdEmployeeDepartment)?.NameDepartment;SelectedEmployee.PositionName=Positions.FirstOrDefault(p=>p.Id==SelectedEmployee.IdPosition)?.NamePosition;if(creating)AppData.Db.Employee.Add(employee);AppData.Db.SaveChanges();if(creating){SelectedEmployee.Id=employee.Id;allEmployeeCards.Add(SelectedEmployee);ApplyDepartmentFilter();}IsNewEmployee=false;ValidationErrors.Clear();IsEditing=false;LoadEmployeeEvents();}

    private bool ValidateSelectedEmployee(){ValidationErrors.Clear();if(SelectedEmployee==null){ValidationErrors.Add("Карточка сотрудника не выбрана.");return false;}if(string.IsNullOrWhiteSpace(SelectedEmployee.FullName))ValidationErrors.Add("ФИО является обязательным полем.");if(SelectedEmployee.IdEmployeeDepartment<=0)ValidationErrors.Add("Структурное подразделение является обязательным полем.");if(SelectedEmployee.IdPosition<=0)ValidationErrors.Add("Должность является обязательным полем.");if(!string.IsNullOrWhiteSpace(SelectedEmployee.PersonalPhone)&&!PhoneRegex.IsMatch(SelectedEmployee.PersonalPhone))ValidationErrors.Add("Мобильный телефон содержит недопустимые символы или превышает 20 символов.");if(string.IsNullOrWhiteSpace(SelectedEmployee.WorkPhone))ValidationErrors.Add("Рабочий телефон является обязательным полем.");else if(!PhoneRegex.IsMatch(SelectedEmployee.WorkPhone))ValidationErrors.Add("Рабочий телефон содержит недопустимые символы или превышает 20 символов.");if(string.IsNullOrWhiteSpace(SelectedEmployee.Email))ValidationErrors.Add("Электронная почта является обязательным полем.");else if(!EmailRegex.IsMatch(SelectedEmployee.Email))ValidationErrors.Add("Электронная почта должна быть в формате x@x.x.");if(string.IsNullOrWhiteSpace(SelectedEmployee.EmployeeOffice))ValidationErrors.Add("Кабинет является обязательным полем.");else if(SelectedEmployee.EmployeeOffice.Length>OfficeMaxLength)ValidationErrors.Add($"Кабинет не должен превышать {OfficeMaxLength} символов.");return ValidationErrors.Count==0;}
    public void AddEmployee(){var dep=Departments.FirstOrDefault(d=>string.Equals(d.NameDepartment,SelectedDepartmentName,StringComparison.OrdinalIgnoreCase));SelectedEmployee=new EmployeeCardModel{Id=0,DepartmentName=dep?.NameDepartment,IdEmployeeDepartment=dep?.Id??0};IsEmployeeCardOpen=true;IsEditing=true;IsNewEmployee=true;ValidationErrors.Clear();EmployeeEvents = new();}
    public void DismissEmployee(){if(SelectedEmployee==null||SelectedEmployee.Id<=0)return;var db=AppData.Db.Employee.FirstOrDefault(x=>x.Id==SelectedEmployee.Id);if(db==null)return;db.EmploymentEndDate=DateTime.Today;SelectedEmployee.EmploymentEndDate=DateTime.Today;AppData.Db.SaveChanges();ApplyDepartmentFilter();LoadEmployeeEvents();}

    public void AddEmployeeEvent(){EventValidationErrors.Clear();if(SelectedEmployee==null||SelectedEmployee.Id<=0){EventValidationErrors.Add("Сначала сохраните карточку сотрудника, затем добавляйте события.");return;}if(string.IsNullOrWhiteSpace(SelectedEventType)||!NewEventStartDate.HasValue||!NewEventEndDate.HasValue){EventValidationErrors.Add("Заполните тип события и даты.");return;}var dbEmployee=AppData.Db.Employee.Include(e=>e.Calendar).FirstOrDefault(e=>e.Id==SelectedEmployee.Id);if(dbEmployee==null)return;var cal=EnsureEmployeeCalendar(dbEmployee);if(SelectedEventType=="Отпуск"){var v=new VacationCalendar{BeginVacation=NewEventStartDate.Value.Date,EndVacation=NewEventEndDate.Value.Date,reasonVacation=NewEventReason};AppData.Db.VacationCalendar.Add(v);AppData.Db.SaveChanges();cal.IdVacationCalendar=v.Id;}else if(SelectedEventType=="Обучение"){var l=new LearningCalendar{BeginLearning=NewEventStartDate.Value.Date,EndLearningn=NewEventEndDate.Value.Date,reasonLearning=NewEventReason};AppData.Db.LearningCalendar.Add(l);AppData.Db.SaveChanges();cal.IdLearningCalendar=l.Id;}else if(SelectedEventType=="Отгул"){var w=new WorkingCalendar{ExceptionDate=NewEventStartDate.Value.Date,EndExceptionDate=NewEventEndDate.Value.Date,IsWorkingDay=false,reasonWorking=NewEventReason};AppData.Db.WorkingCalendar.Add(w);AppData.Db.SaveChanges();cal.IdWorkingCalendar=w.Id;}AppData.Db.SaveChanges();LoadEmployeeEvents();}
    public void DeleteEmployeeEvent(EmployeeEventModel? ev){if(ev==null||SelectedEmployee==null)return;var db=AppData.Db.Employee.Include(e=>e.Calendar).FirstOrDefault(e=>e.Id==SelectedEmployee.Id);var c=db?.Calendar;if(c==null)return;if(ev.VacationCalendarId.HasValue){var x=AppData.Db.VacationCalendar.FirstOrDefault(i=>i.Id==ev.VacationCalendarId.Value);if(x!=null)AppData.Db.VacationCalendar.Remove(x);if(c.IdVacationCalendar==ev.VacationCalendarId)c.IdVacationCalendar=null;}if(ev.LearningCalendarId.HasValue){var x=AppData.Db.LearningCalendar.FirstOrDefault(i=>i.Id==ev.LearningCalendarId.Value);if(x!=null)AppData.Db.LearningCalendar.Remove(x);if(c.IdLearningCalendar==ev.LearningCalendarId)c.IdLearningCalendar=null;}if(ev.WorkingCalendarId.HasValue){var x=AppData.Db.WorkingCalendar.FirstOrDefault(i=>i.Id==ev.WorkingCalendarId.Value);if(x!=null)AppData.Db.WorkingCalendar.Remove(x);if(c.IdWorkingCalendar==ev.WorkingCalendarId)c.IdWorkingCalendar=null;}AppData.Db.SaveChanges();LoadEmployeeEvents();}

    private void LoadEmployeeEvents(){if(SelectedEmployee==null||SelectedEmployee.Id<=0){EmployeeEvents=new();return;}var db=AppData.Db.Employee.Include(e=>e.Calendar).Include(e=>e.Calendar.VacationCalendar).Include(e=>e.Calendar.LearningCalendar).Include(e=>e.Calendar.WorkingCalendar).FirstOrDefault(e=>e.Id==SelectedEmployee.Id);var list=new ObservableCollection<EmployeeEventModel>();var c=db?.Calendar;if(c?.VacationCalendar!=null)list.Add(new EmployeeEventModel{TypeName="Отпуск",StartDate=c.VacationCalendar.BeginVacation,EndDate=c.VacationCalendar.EndVacation,Reason=c.VacationCalendar.reasonVacation,VacationCalendarId=c.VacationCalendar.Id});if(c?.WorkingCalendar!=null)list.Add(new EmployeeEventModel{TypeName="Отгул",StartDate=c.WorkingCalendar.ExceptionDate,EndDate=c.WorkingCalendar.EndExceptionDate,Reason=c.WorkingCalendar.reasonWorking,WorkingCalendarId=c.WorkingCalendar.Id});if(c?.LearningCalendar!=null)list.Add(new EmployeeEventModel{TypeName="Обучение",StartDate=c.LearningCalendar.BeginLearning,EndDate=c.LearningCalendar.EndLearningn,Reason=c.LearningCalendar.reasonLearning,LearningCalendarId=c.LearningCalendar.Id});EmployeeEvents=list;}
    private Calendar EnsureEmployeeCalendar(Employee employee){if(employee.Calendar!=null)return employee.Calendar;var c=new Calendar();AppData.Db.Calendar.Add(c);AppData.Db.SaveChanges();employee.CalendarEmployee=c.Id;AppData.Db.SaveChanges();return c;}

    private void UpdateDepartmentEmployees(int departmentId){var items=allEmployeeCards.Where(e=>e.IdEmployeeDepartment==departmentId&&e.Id!=SelectedEmployee?.Id).Select(e=>new EmployeeLookupModel(e.Id,e.FullName??string.Empty)).OrderBy(e=>e.FullName).ToList();DepartmentEmployees=new ObservableCollection<EmployeeLookupModel>(items);}    

    public sealed class EmployeeLookupModel{public EmployeeLookupModel(int id,string fullName){Id=id;FullName=fullName;}public int Id{get;}public string FullName{get;}}
    public sealed class EmployeeEventModel{public string? TypeName{get;set;}public DateTime StartDate{get;set;}public DateTime EndDate{get;set;}public string? Reason{get;set;}public int? VacationCalendarId{get;set;}public int? LearningCalendarId{get;set;}public long? WorkingCalendarId{get;set;}public string DateRangeDisplay=>StartDate.Date==EndDate.Date?StartDate.ToString("dd.MM.yyyy"):$"{StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";}
}
