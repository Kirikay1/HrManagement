using System;

namespace HrManagement.ViewModel
{
    public class EmployeeCardViewModel : ViewModelBase
    {
        private string departmentName;
        private string positionName;
        private string fullName;
        private string workPhone;
        private string email;
        private string employeeOffice;

        public string DepartmentName
        {
            get => departmentName;
            set
            {
                departmentName = value;
                OnPropertyChanged();
            }
        }

        public string PositionName
        {
            get => positionName;
            set
            {
                positionName = value;
                OnPropertyChanged();
            }
        }

        public string FullName
        {
            get => fullName;
            set
            {
                fullName = value;
                OnPropertyChanged();
            }
        }

        public string WorkPhone
        {
            get => workPhone;
            set
            {
                workPhone = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged();
            }
        }

        public string EmployeeOffice
        {
            get => employeeOffice;
            set
            {
                employeeOffice = value;
                OnPropertyChanged();
            }
        }
    }
}
