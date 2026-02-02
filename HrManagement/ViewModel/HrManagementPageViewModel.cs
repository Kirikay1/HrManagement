﻿using HrManagement.Model;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace HrManagement.ViewModel
{
    public class HrManagementPageViewModel : ViewModelBase
    {
        private readonly List<EmployeeCardViewModel> allEmployeeCards = new List<EmployeeCardViewModel>();
        private ObservableCollection<EmployeeCardViewModel> employeeCards;

        public HrManagementPageViewModel()
        {
            LoadEmployees();
            FilterByDepartmentCommand = new RelayCommand(parameter => FilterEmployeesByDepartment(parameter as string));
        }

        public ObservableCollection<EmployeeCardViewModel> EmployeeCards
        {
            get => employeeCards;
            private set
            {
                employeeCards = value;
                OnPropertyChanged();
            }
        }

        public ICommand FilterByDepartmentCommand { get; }

        private void LoadEmployees()
        {
            var employees = AppData.db.Employee
                .Include(employee => employee.Department)
                .Include(employee => employee.Position)
                .ToList();

            allEmployeeCards.Clear();
            allEmployeeCards.AddRange(employees.Select(employee => new EmployeeCardViewModel
            {
                DepartmentName = employee.Department?.NameDepartment,
                PositionName = employee.Position?.NamePosition,
                FullName = employee.FullName,
                WorkPhone = employee.WorkPhone,
                Email = employee.Email,
                EmployeeOffice = employee.EmployeeOffice
            }));

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(allEmployeeCards);
        }

        public void FilterEmployeesByDepartment(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(allEmployeeCards);
                return;
            }

            var filtered = allEmployeeCards
                .Where(employee => string.Equals(employee.DepartmentName, departmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(filtered);
        }
    }
}
