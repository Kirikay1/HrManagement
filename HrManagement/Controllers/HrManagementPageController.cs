using HrManagement.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows;

namespace HrManagement.Controllers
{
    public class HrManagementPageController
    {
        private const int OfficeMaxLength = 10;
        private const int NewEmployeeTemporaryKey = -1;
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+()\-\s#]{0,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly List<EmployeeCardModel> allEmployeeCards = new List<EmployeeCardModel>();
        private readonly ObservableCollection<string> eventTypeOptions = new ObservableCollection<string> { "Обучение", "Отгул", "Отпуск" };
        private ObservableCollection<EmployeeCardModel> employeeCards;
        private ObservableCollection<Department> departments;
        private ObservableCollection<Position> positions;
        private ObservableCollection<EmployeeLookupModel> departmentEmployees;
        private ObservableCollection<EmployeeEventModel> employeeEvents;
        private ICollectionView employeeEventsView;
        private EmployeeCardModel selectedEmployee;
        private bool isEmployeeCardOpen;
        private bool isEditing;
        private bool isNewEmployee;
        private string selectedDepartmentName;
        private readonly ObservableCollection<string> validationErrors = new ObservableCollection<string>();
        private readonly ObservableCollection<string> eventValidationErrors = new ObservableCollection<string>();
        private string selectedEventType;
        private DateTime? newEventStartDate = DateTime.Today;
        private DateTime? newEventEndDate = DateTime.Today;
        private string newEventReason;
        private bool showPastEvents;
        private bool showCurrentEvents = true;
        private bool showFutureEvents = true;
        private int currentEmployeeKey = NewEmployeeTemporaryKey;

        private readonly HrManagementController controller;

        public HrManagementPageController(HrManagementController controller)
        {
            this.controller = controller;
            LoadEmployees();
            LoadReferenceData();
        }

        public ObservableCollection<EmployeeCardModel> EmployeeCards
        {
            get => employeeCards;
            private set
            {
                employeeCards = value;
            }
        }

        public ObservableCollection<Department> Departments
        {
            get => departments;
            private set
            {
                departments = value;
            }
        }

        public ObservableCollection<Position> Positions
        {
            get => positions;
            private set
            {
                positions = value;
            }
        }

        public ObservableCollection<EmployeeLookupModel> DepartmentEmployees
        {
            get => departmentEmployees;
            private set
            {
                departmentEmployees = value;
            }
        }

        public ObservableCollection<EmployeeEventModel> EmployeeEvents
        {
            get => employeeEvents;
            private set
            {
                employeeEvents = value;
            }
        }

        public ICollectionView EmployeeEventsView
        {
            get => employeeEventsView;
            private set
            {
                employeeEventsView = value;
            }
        }

        public EmployeeCardModel SelectedEmployee
        {
            get => selectedEmployee;
            private set
            {
                if (selectedEmployee != null)
                {
                }

                selectedEmployee = value;

                if (selectedEmployee != null)
                {
                    UpdateDepartmentEmployees(selectedEmployee.IdEmployeeDepartment);
                    LoadEmployeeEvents(selectedEmployee);
                }
                else
                {
                    DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
                    LoadEmployeeEvents(null);
                }
            }
        }

        public bool IsEmployeeCardOpen
        {
            get => isEmployeeCardOpen;
            private set
            {
                isEmployeeCardOpen = value;
            }
        }

        public bool IsEditing
        {
            get => isEditing;
            private set
            {
                isEditing = value;
            }
        }

        public bool IsNewEmployee
        {
            get => isNewEmployee;
            private set
            {
                isNewEmployee = value;
            }
        }

        public string SelectedDepartmentName
        {
            get => selectedDepartmentName;
            private set
            {
                selectedDepartmentName = value;
            }
        }

        public ObservableCollection<string> ValidationErrors => validationErrors;

        public ObservableCollection<string> EventValidationErrors => eventValidationErrors;
        public ObservableCollection<string> EventTypeOptions => eventTypeOptions;

        public string SelectedEventType
        {
            get => selectedEventType;
            set
            {
                selectedEventType = value;
            }
        }

        public DateTime? NewEventStartDate
        {
            get => newEventStartDate;
            set
            {
                newEventStartDate = value;
            }
        }

        public DateTime? NewEventEndDate
        {
            get => newEventEndDate;
            set
            {
                newEventEndDate = value;
            }
        }

        public string NewEventReason
        {
            get => newEventReason;
            set
            {
                newEventReason = value;
            }
        }

        public bool ShowPastEvents
        {
            get => showPastEvents;
            set
            {
                showPastEvents = value;
                RefreshEmployeeEventsView();
            }
        }

        public bool ShowCurrentEvents
        {
            get => showCurrentEvents;
            set
            {
                showCurrentEvents = value;
                RefreshEmployeeEventsView();
            }
        }

        public bool ShowFutureEvents
        {
            get => showFutureEvents;
            set
            {
                showFutureEvents = value;
                RefreshEmployeeEventsView();
            }
        }


        private void LoadEmployees()
        {
            var employees = controller.GetEmployees();

            allEmployeeCards.Clear();
            allEmployeeCards.AddRange(employees.Select(employee => new EmployeeCardModel
            {
                Id = employee.Id,
                IdEmployeeDepartment = employee.IdEmployeeDepartment,
                IdPosition = employee.IdPosition,
                DirectSupervisor = employee.DirectSupervisor,
                AssistantEmployee = employee.AssistantEmployee,
                DepartmentName = employee.Department?.NameDepartment,
                PositionName = employee.Position?.NamePosition,
                FullName = employee.FullName,
                PersonalPhone = employee.PersonalPhone,
                BirthDate = employee.BirthDate,
                WorkPhone = employee.WorkPhone,
                Email = employee.Email,
                EmployeeOffice = employee.EmployeeOffice,
                Other = employee.Other,
                EmploymentEndDate = employee.EmploymentEndDate
            }));

            EmployeeCards = new ObservableCollection<EmployeeCardModel>(allEmployeeCards.Where(IsVisibleInList));
        }

        private void LoadReferenceData()
        {
            Departments = new ObservableCollection<Department>(AppData.Db.Department.ToList());
            Positions = new ObservableCollection<Position>(AppData.Db.Position.ToList());
            DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
        }

        public void FilterEmployeesByDepartment(string departmentName)
        {
            SelectedDepartmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName;
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                EmployeeCards = new ObservableCollection<EmployeeCardModel>(allEmployeeCards.Where(IsVisibleInList));
                return;
            }

            var filtered = allEmployeeCards
                .Where(IsVisibleInList)
                .Where(employee => string.Equals(employee.DepartmentName, departmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardModel>(filtered);
        }

        public void OpenEmployeeCard(EmployeeCardModel employee)
        {
            if (employee == null)
            {
                return;
            }

            SelectedEmployee = employee;
            IsEmployeeCardOpen = true;
            IsEditing = false;
            IsNewEmployee = false;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
        }

        public void CloseEmployeeCard()
        {
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
            IsEditing = false;
            IsEmployeeCardOpen = false;
            IsNewEmployee = false;
        }

        public void StartEditEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.BeginEdit();
            IsEditing = true;
        }

        public void CancelEditEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.CancelEdit();
            ValidationErrors.Clear();
            IsEditing = false;
            if (IsNewEmployee)
            {
                IsEmployeeCardOpen = false;
                IsNewEmployee = false;
                SelectedEmployee = null;
                return;
            }
            UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);
        }

        public void SaveEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            if (!ValidateSelectedEmployee())
            {
                return;
            }

            var isCreating = IsNewEmployee || SelectedEmployee.Id <= 0;
            var employee = isCreating
                ? new Employee()
                : AppData.Db.Employee.FirstOrDefault(item => item.Id == SelectedEmployee.Id);
            if (employee == null)
            {
                ValidationErrors.Clear();
                ValidationErrors.Add("Сотрудник не найден в базе данных.");
                return;
            }

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

            var department = Departments.FirstOrDefault(item => item.Id == SelectedEmployee.IdEmployeeDepartment);
            var position = Positions.FirstOrDefault(item => item.Id == SelectedEmployee.IdPosition);
            SelectedEmployee.DepartmentName = department?.NameDepartment;
            SelectedEmployee.PositionName = position?.NamePosition;

            if (isCreating)
            {
                AppData.Db.Employee.Add(employee);
            }

            AppData.Db.SaveChanges();
            if (isCreating)
            {
                SelectedEmployee.Id = employee.Id;
                allEmployeeCards.Add(SelectedEmployee);
                ApplyDepartmentFilter();
            }
            IsNewEmployee = false;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
            IsEditing = false;
        }

        private bool ValidateSelectedEmployee()
        {
            ValidationErrors.Clear();

            if (SelectedEmployee == null)
            {
                ValidationErrors.Add("Карточка сотрудника не выбрана.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.FullName))
            {
                ValidationErrors.Add("ФИО является обязательным полем.");
            }

            if (SelectedEmployee.IdEmployeeDepartment <= 0)
            {
                ValidationErrors.Add("Структурное подразделение является обязательным полем.");
            }

            if (SelectedEmployee.IdPosition <= 0)
            {
                ValidationErrors.Add("Должность является обязательным полем.");
            }

            if (!string.IsNullOrWhiteSpace(SelectedEmployee.PersonalPhone)
                && !PhoneRegex.IsMatch(SelectedEmployee.PersonalPhone))
            {
                ValidationErrors.Add("Мобильный телефон содержит недопустимые символы или превышает 20 символов.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.WorkPhone))
            {
                ValidationErrors.Add("Рабочий телефон является обязательным полем.");
            }
            else if (!PhoneRegex.IsMatch(SelectedEmployee.WorkPhone))
            {
                ValidationErrors.Add("Рабочий телефон содержит недопустимые символы или превышает 20 символов.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.Email))
            {
                ValidationErrors.Add("Электронная почта является обязательным полем.");
            }
            else if (!EmailRegex.IsMatch(SelectedEmployee.Email))
            {
                ValidationErrors.Add("Электронная почта должна быть в формате x@x.x.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.EmployeeOffice))
            {
                ValidationErrors.Add("Кабинет является обязательным полем.");
            }
            else if (SelectedEmployee.EmployeeOffice.Length > OfficeMaxLength)
            {
                ValidationErrors.Add($"Кабинет не должен превышать {OfficeMaxLength} символов.");
            }

            return ValidationErrors.Count == 0;
        }


        public void AddEmployee()
        {
            var employee = new EmployeeCardModel
            {
                Id = currentEmployeeKey--,
                BirthDate = DateTime.Today,
                EmploymentEndDate = null
            };

            SelectedEmployee = employee;
            IsEmployeeCardOpen = true;
            IsEditing = true;
            IsNewEmployee = true;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
        }

        public void DismissEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.EmploymentEndDate = DateTime.Today;

            if (SelectedEmployee.Id > 0)
            {
                var employee = AppData.Db.Employee.FirstOrDefault(item => item.Id == SelectedEmployee.Id);
                if (employee != null)
                {
                    employee.EmploymentEndDate = SelectedEmployee.EmploymentEndDate;
                    AppData.Db.SaveChanges();
                }
            }

            ApplyDepartmentFilter();
        }

        public void AddEmployeeEvent()
        {
            EventValidationErrors.Clear();

            if (SelectedEmployee == null)
            {
                EventValidationErrors.Add("Сотрудник не выбран.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedEventType))
            {
                EventValidationErrors.Add("Выберите тип события.");
            }

            if (!NewEventStartDate.HasValue || !NewEventEndDate.HasValue)
            {
                EventValidationErrors.Add("Укажите дату начала и окончания.");
            }
            else if (NewEventStartDate.Value.Date > NewEventEndDate.Value.Date)
            {
                EventValidationErrors.Add("Дата начала не может быть позже даты окончания.");
            }

            if (EventValidationErrors.Count > 0)
            {
                return;
            }

            var newEvent = new EmployeeEventModel
            {
                EmployeeId = SelectedEmployee.Id,
                EventType = SelectedEventType,
                StartDate = NewEventStartDate!.Value.Date,
                EndDate = NewEventEndDate!.Value.Date,
                Reason = NewEventReason
            };

            EmployeeEvents.Add(newEvent);
            RefreshEmployeeEventsView();

            SelectedEventType = null;
            NewEventStartDate = DateTime.Today;
            NewEventEndDate = DateTime.Today;
            NewEventReason = null;
        }

        public void DeleteEmployeeEvent(EmployeeEventModel employeeEvent)
        {
            if (employeeEvent == null || EmployeeEvents == null)
            {
                return;
            }

            EmployeeEvents.Remove(employeeEvent);
            RefreshEmployeeEventsView();
        }

        private void LoadEmployeeEvents(EmployeeCardModel employee)
        {
            EmployeeEvents = new ObservableCollection<EmployeeEventModel>();
            EmployeeEventsView = CollectionViewSource.GetDefaultView(EmployeeEvents);
            EmployeeEventsView.Filter = item => FilterEventByPeriod(item as EmployeeEventModel);
            RefreshEmployeeEventsView();
        }

        private bool FilterEventByPeriod(EmployeeEventModel employeeEvent)
        {
            if (employeeEvent == null)
            {
                return false;
            }

            var today = DateTime.Today;
            var isPast = employeeEvent.EndDate.Date < today;
            var isFuture = employeeEvent.StartDate.Date > today;
            var isCurrent = !isPast && !isFuture;

            return (ShowPastEvents && isPast)
                || (ShowCurrentEvents && isCurrent)
                || (ShowFutureEvents && isFuture);
        }

        private void RefreshEmployeeEventsView()
        {
            EmployeeEventsView?.Refresh();
        }

        private void UpdateDepartmentEmployees(int departmentId)
        {
            if (departmentId <= 0)
            {
                DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
                return;
            }

            var employees = AppData.Db.Employee
                .AsNoTracking()
                .Where(item => item.IdEmployeeDepartment == departmentId)
                .Select(item => new EmployeeLookupModel
                {
                    Id = item.Id,
                    FullName = item.FullName
                })
                .ToList();

            DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>(employees);
        }

        private bool IsVisibleInList(EmployeeCardModel employee)
        {
            return employee != null && !employee.IsDismissedRecently;
        }

        private void ApplyDepartmentFilter()
        {
            FilterEmployeesByDepartment(SelectedDepartmentName);
        }



        public sealed class EmployeeLookupModel
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }

        public sealed class EmployeeEventModel
        {
            public int EmployeeId { get; set; }
            public string EventType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; }
        }
    }
}
