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
    private readonly HrManagementController controller;

    private ObservableCollection<EmployeeCardModel> employeeCards = new();
    private ObservableCollection<Department> departments = new();
    private ObservableCollection<Position> positions = new();
    private ObservableCollection<EmployeeLookupModel> departmentEmployees = new();
    private EmployeeCardModel? selectedEmployee;
    private bool isEmployeeCardOpen;
    private bool isEditing;
    private bool isNewEmployee;
    private string? selectedDepartmentName;

    public HrManagementPageController(HrManagementController controller)
    {
        this.controller = controller;
        LoadEmployees();
        LoadReferenceData();
    }

    public ObservableCollection<EmployeeCardModel> EmployeeCards { get => employeeCards; private set { employeeCards = value; } }
    public ObservableCollection<Department> Departments { get => departments; private set { departments = value; } }
    public ObservableCollection<Position> Positions { get => positions; private set { positions = value; } }
    public ObservableCollection<EmployeeLookupModel> DepartmentEmployees { get => departmentEmployees; private set { departmentEmployees = value; } }
    public ObservableCollection<string> ValidationErrors => validationErrors;

    public EmployeeCardModel? SelectedEmployee
    {
        get => selectedEmployee;
        private set
        {
            selectedEmployee = value;
            if (selectedEmployee != null)
            {
                UpdateDepartmentEmployees(selectedEmployee.IdEmployeeDepartment);
            }
            else
            {
                DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
            }
        }
    }

    public bool IsEmployeeCardOpen { get => isEmployeeCardOpen; private set { isEmployeeCardOpen = value; } }
    public bool IsEditing { get => isEditing; private set { isEditing = value; } }
    public bool IsNewEmployee { get => isNewEmployee; private set { isNewEmployee = value; } }
    public string? SelectedDepartmentName { get => selectedDepartmentName; private set { selectedDepartmentName = value; } }

    private void LoadEmployees()
    {
        var employees = controller.GetEmployees();
        allEmployeeCards.Clear();
        allEmployeeCards.AddRange(employees.Select(e => new EmployeeCardModel
        {
            Id = e.Id,
            IdEmployeeDepartment = e.IdEmployeeDepartment,
            IdPosition = e.IdPosition,
            DirectSupervisor = e.DirectSupervisor,
            AssistantEmployee = e.AssistantEmployee,
            DepartmentName = e.Department?.NameDepartment,
            PositionName = e.Position?.NamePosition,
            FullName = e.FullName,
            PersonalPhone = e.PersonalPhone,
            BirthDate = e.BirthDate,
            WorkPhone = e.WorkPhone,
            Email = e.Email,
            EmployeeOffice = e.EmployeeOffice,
            Other = e.Other,
            EmploymentEndDate = e.EmploymentEndDate
        }));
        ApplyDepartmentFilter();
    }

    private void LoadReferenceData()
    {
        Departments = new ObservableCollection<Department>(AppData.Db.Department.ToList());
        Positions = new ObservableCollection<Position>(AppData.Db.Position.ToList());
    }

    public void FilterEmployeesByDepartment(string? departmentName)
    {
        SelectedDepartmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName;
        ApplyDepartmentFilter();
    }

    private void ApplyDepartmentFilter()
    {
        IEnumerable<EmployeeCardModel> query = allEmployeeCards.Where(IsVisibleInList);
        if (!string.IsNullOrWhiteSpace(SelectedDepartmentName))
            query = query.Where(e => string.Equals(e.DepartmentName, SelectedDepartmentName, StringComparison.OrdinalIgnoreCase));
        EmployeeCards = new ObservableCollection<EmployeeCardModel>(query);
    }

    private static bool IsVisibleInList(EmployeeCardModel employee) => !employee.EmploymentEndDate.HasValue || employee.EmploymentEndDate.Value.Date >= DateTime.Today.AddDays(-30);

    public void OpenEmployeeCard(EmployeeCardModel? employee)
    {
        if (employee == null) return;
        SelectedEmployee = employee;
        IsEmployeeCardOpen = true;
        IsEditing = false;
        IsNewEmployee = false;
        ValidationErrors.Clear();
    }

    public void CloseEmployeeCard() { ValidationErrors.Clear(); IsEditing = false; IsEmployeeCardOpen = false; IsNewEmployee = false; }
    public void StartEditEmployee() { if (SelectedEmployee == null) return; SelectedEmployee.BeginEdit(); IsEditing = true; }

    public void CancelEditEmployee()
    {
        if (SelectedEmployee == null) return;
        SelectedEmployee.CancelEdit();
        ValidationErrors.Clear();
        IsEditing = false;
        if (IsNewEmployee) { IsEmployeeCardOpen = false; IsNewEmployee = false; SelectedEmployee = null; return; }
        UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);
    }

    public void SaveEmployee()
    {
        if (SelectedEmployee == null || !ValidateSelectedEmployee()) return;
        var isCreating = IsNewEmployee || SelectedEmployee.Id <= 0;
        var employee = isCreating ? new Employee() : AppData.Db.Employee.FirstOrDefault(x => x.Id == SelectedEmployee.Id);
        if (employee == null) { ValidationErrors.Clear(); ValidationErrors.Add("Сотрудник не найден в базе данных."); return; }

        employee.FullName = SelectedEmployee.FullName;
        employee.PersonalPhone = SelectedEmployee.PersonalPhone;
        employee.BirthDate = SelectedEmployee.BirthDate;
        employee.IdEmployeeDepartment = SelectedEmployee.IdEmployeeDepartment;
        employee.IdPosition = SelectedEmployee.IdPosition;
        employee.DirectSupervisor = SelectedEmployee.DirectSupervisor;
        employee.AssistantEmployee = SelectedEmployee.AssistantEmployee;
        employee.WorkPhone = SelectedEmployee.WorkPhone;
        employee.Email = SelectedEmployee.Email;
        employee.EmployeeOffice = SelectedEmployee.EmployeeOffice;
        employee.Other = SelectedEmployee.Other;
        employee.EmploymentEndDate = SelectedEmployee.EmploymentEndDate;

        SelectedEmployee.DepartmentName = Departments.FirstOrDefault(d => d.Id == SelectedEmployee.IdEmployeeDepartment)?.NameDepartment;
        SelectedEmployee.PositionName = Positions.FirstOrDefault(p => p.Id == SelectedEmployee.IdPosition)?.NamePosition;

        if (isCreating) AppData.Db.Employee.Add(employee);
        AppData.Db.SaveChanges();

        if (isCreating)
        {
            SelectedEmployee.Id = employee.Id;
            allEmployeeCards.Add(SelectedEmployee);
            ApplyDepartmentFilter();
        }

        IsNewEmployee = false;
        ValidationErrors.Clear();
        IsEditing = false;
    }

    private bool ValidateSelectedEmployee()
    {
        ValidationErrors.Clear();
        if (SelectedEmployee == null) { ValidationErrors.Add("Карточка сотрудника не выбрана."); return false; }
        if (string.IsNullOrWhiteSpace(SelectedEmployee.FullName)) ValidationErrors.Add("ФИО является обязательным полем.");
        if (SelectedEmployee.IdEmployeeDepartment <= 0) ValidationErrors.Add("Структурное подразделение является обязательным полем.");
        if (SelectedEmployee.IdPosition <= 0) ValidationErrors.Add("Должность является обязательным полем.");
        if (!string.IsNullOrWhiteSpace(SelectedEmployee.PersonalPhone) && !PhoneRegex.IsMatch(SelectedEmployee.PersonalPhone)) ValidationErrors.Add("Мобильный телефон содержит недопустимые символы или превышает 20 символов.");
        if (string.IsNullOrWhiteSpace(SelectedEmployee.WorkPhone)) ValidationErrors.Add("Рабочий телефон является обязательным полем.");
        else if (!PhoneRegex.IsMatch(SelectedEmployee.WorkPhone)) ValidationErrors.Add("Рабочий телефон содержит недопустимые символы или превышает 20 символов.");
        if (string.IsNullOrWhiteSpace(SelectedEmployee.Email)) ValidationErrors.Add("Электронная почта является обязательным полем.");
        else if (!EmailRegex.IsMatch(SelectedEmployee.Email)) ValidationErrors.Add("Электронная почта должна быть в формате x@x.x.");
        if (string.IsNullOrWhiteSpace(SelectedEmployee.EmployeeOffice)) ValidationErrors.Add("Кабинет является обязательным полем.");
        else if (SelectedEmployee.EmployeeOffice.Length > OfficeMaxLength) ValidationErrors.Add($"Кабинет не должен превышать {OfficeMaxLength} символов.");
        return ValidationErrors.Count == 0;
    }

    public void DismissEmployee()
    {
        ValidationErrors.Clear();
        if (SelectedEmployee == null || SelectedEmployee.Id <= 0) return;
        var dbEmployee = AppData.Db.Employee.Include(x => x.Calendar).Include(x => x.Calendar.LearningCalendar).FirstOrDefault(x => x.Id == SelectedEmployee.Id);
        if (dbEmployee == null) { ValidationErrors.Add("Сотрудник не найден в базе данных."); return; }

        var today = DateTime.Today;
        if (dbEmployee.Calendar?.LearningCalendar != null && dbEmployee.Calendar.LearningCalendar.EndLearningn.Date >= today)
        { ValidationErrors.Add("Нельзя уволить сотрудника: у него запланировано обучение."); return; }

        var confirmation = MessageBox.Show("Подтвердите увольнение сотрудника.", "Подтверждение увольнения", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes) return;

        dbEmployee.EmploymentEndDate = today;
        SelectedEmployee.EmploymentEndDate = today;
        AppData.Db.SaveChanges();
        ApplyDepartmentFilter();
    }

    public void AddEmployee()
    {
        var dep = Departments.FirstOrDefault(d => string.Equals(d.NameDepartment, SelectedDepartmentName, StringComparison.OrdinalIgnoreCase));
        SelectedEmployee = new EmployeeCardModel { Id = 0, DepartmentName = dep?.NameDepartment, IdEmployeeDepartment = dep?.Id ?? 0 };
        IsEmployeeCardOpen = true;
        IsEditing = true;
        IsNewEmployee = true;
        ValidationErrors.Clear();
    }

    private void UpdateDepartmentEmployees(int departmentId)
    {
        var items = allEmployeeCards.Where(e => e.IdEmployeeDepartment == departmentId && e.Id != SelectedEmployee?.Id)
            .Select(e => new EmployeeLookupModel(e.Id, e.FullName ?? string.Empty))
            .OrderBy(e => e.FullName)
            .ToList();
        DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>(items);
    }

    public sealed class EmployeeLookupModel
    {
        public EmployeeLookupModel(int id, string fullName) { Id = id; FullName = fullName; }
        public int Id { get; }
        public string FullName { get; }
    }
}
