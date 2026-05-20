using HrManagement.Model;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Controllers;

public sealed class HrManagementController
{
    private readonly HrManagementDbContext db;

    public HrManagementController(HrManagementDbContext dbContext)
    {
        db = dbContext;
    }

    public List<Employee> GetEmployees(string? departmentName = null)
    {
        var query = db.Employee
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            query = query.Where(e => e.Department != null && e.Department.NameDepartment == departmentName);
        }

        return query.ToList();
    }

    public void SaveEmployee(Employee employee)
    {
        if (employee.Id == 0)
        {
            db.Employee.Add(employee);
        }
        else
        {
            db.Employee.Update(employee);
        }

        db.SaveChanges();
    }
}
